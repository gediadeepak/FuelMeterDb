using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public static class SubscriptionAccountLock
{
    public static async Task<T> ExecuteAsync<T>(
        FuelMeterDbContext db, int userId, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await AcquireAsync(db, userId, cancellationToken);
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public static Task ExecuteAsync(
        FuelMeterDbContext db, int userId, Func<Task> action, CancellationToken cancellationToken) =>
        ExecuteAsync(db, userId, async () => { await action(); return true; }, cancellationToken);

    // Every entitlement, quota and deletion mutation uses this transaction-owned SQL lock.
    public static Task AcquireAsync(FuelMeterDbContext db, int userId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = {"fuelmeter-subscription:" + userId},
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 51000, 'Subscription data is busy. Please retry.', 1;
            """, cancellationToken);
}
