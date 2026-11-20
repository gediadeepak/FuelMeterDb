using FuelMeter.Data;
using FuelMeter.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Api.Services;

public sealed class SubscriptionMaintenance(IServiceScopeFactory scopes, ILogger<SubscriptionMaintenance> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FuelMeterDbContext>();
                await db.AccountTrials.Where(x => x.RetainUntilUtc != null && x.RetainUntilUtc <= DateTime.UtcNow)
                    .ExecuteDeleteAsync(stoppingToken);
                var subscriptions = scope.ServiceProvider.GetRequiredService<SubscriptionService>();
                var users = await db.AccountSubscriptions.AsNoTracking()
                    .Where(x => x.State != "SUBSCRIPTION_STATE_EXPIRED")
                    .Select(x => x.UserId).Distinct().ToListAsync(stoppingToken);
                foreach (var userId in users)
                {
                    try { await subscriptions.GetStatusAsync(userId, "Web", stoppingToken); }
                    catch (Exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning("Subscription reconciliation could not complete for account {UserId}. It will be retried.", userId);
                    }
                }
            }
            catch (Exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Subscription maintenance could not complete. It will be retried.");
            }
        }
    }
}
