using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public static class SubscriptionAccountLock
{
    // Every entitlement, quota and deletion mutation uses this transaction-owned SQL lock.
    public static Task AcquireAsync(FuelMeterDbContext db, int userId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = {"fuelmeter-subscription:" + userId},
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 51000, 'Subscription data is busy. Please retry.', 1;
            """, cancellationToken);
}
