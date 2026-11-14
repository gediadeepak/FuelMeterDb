using FirebaseAdmin.Messaging;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Data;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Api.Services;

public sealed class BudgetPushAlerts(
    FuelMeterDbContext db,
    IBudgetService budgets,
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<BudgetPushAlerts> logger)
{
    public async Task<BudgetSummaryDto?> GetBudgetBeforeSaveAsync(
        int userId, MeterReadingDto reading, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Firebase:BudgetAlertsEnabled") ||
            reading.Id != 0 || reading.ReadingDate is not { } date)
        {
            return null;
        }

        try
        {
            if (!await db.PushInstallations.AnyAsync(device => device.UserId == userId, cancellationToken))
                return null;

            var budget = await budgets.GetBudgetSummaryAsync(userId, reading.FuelType, date.Year, date.Month);
            return budget is { IsOverBudget: false, BudgetLimit: > 0 } ? budget : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not check the budget before saving a meter reading.");
            return null;
        }
    }

    public async Task SendIfCrossedAsync(int userId, BudgetSummaryDto before, CancellationToken cancellationToken)
    {
        try
        {
            var after = await budgets.GetBudgetSummaryAsync(userId, before.FuelType, before.Year, before.Month);
            if (after is not { IsOverBudget: true } || after.Id != before.Id || after.BudgetLimit != before.BudgetLimit)
                return;

            var devices = await db.PushInstallations.Where(device => device.UserId == userId)
                .Select(device => device.InstallationId).ToListAsync(cancellationToken);
            if (devices.Count == 0)
                return;

            var push = services.GetRequiredService<FirebasePushService>();
            var claimed = await db.Budgets.Where(budget => budget.Id == after.Id &&
                    budget.UserId == userId && budget.BudgetLimit == after.BudgetLimit && budget.PushAlertSentAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(budget => budget.PushAlertSentAt, DateTime.UtcNow),
                    cancellationToken);
            if (claimed == 0)
                return;

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await Task.WhenAll(devices.Select(id => SendAsync(push, id, userId, after.Id, timeout.Token)));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send a FuelMeter budget alert.");
        }
    }

    private async Task SendAsync(
        FirebasePushService push, string installationId, int userId, int budgetId, CancellationToken cancellationToken)
    {
        try
        {
            await push.SendBudgetAlertAsync(installationId, userId, budgetId, cancellationToken);
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            await using var scope = services.CreateAsyncScope();
            var cleanupDb = scope.ServiceProvider.GetRequiredService<FuelMeterDbContext>();
            await cleanupDb.PushInstallations
                .Where(device => device.InstallationId == installationId && device.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase rejected a budget alert or the request timed out.");
        }
    }
}
