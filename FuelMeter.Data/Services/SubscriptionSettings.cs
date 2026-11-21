using System.Globalization;
using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public sealed class SubscriptionSettings(FuelMeterDbContext db)
{
    public async Task<SubscriptionPolicy> GetAsync(CancellationToken cancellationToken = default)
    {
        var values = await db.ApplicationSettings.AsNoTracking()
            .Where(x => x.Key.StartsWith("Subscription."))
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        return new SubscriptionPolicy(values);
    }
}

public sealed class SubscriptionPolicy(IReadOnlyDictionary<string, string> values)
{
    public string Text(string key) => values.TryGetValue("Subscription." + key, out var value)
        ? value : throw new InvalidOperationException($"Subscription setting {key} is missing.");

    public int Number(string key, int minimum = 0, int maximum = 100000)
    {
        if (!int.TryParse(Text(key), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ||
            value < minimum || value > maximum)
            throw new InvalidOperationException($"Subscription setting {key} is invalid.");
        return value;
    }

    public bool Enabled(string key) => bool.TryParse(Text(key), out var value)
        ? value : throw new InvalidOperationException($"Subscription setting {key} is invalid.");

    public string DayKey(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById(Text("Scans.TimeZone")))
        .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public bool CanUsePremium(string platform) => platform is "Android" or "Web" or "Windows" or "iOS" &&
        Enabled(platform + ".PremiumAccessEnabled");

    public DateTime? ComplimentaryUntil(int userId)
    {
        if (!values.TryGetValue("Subscription.ComplimentaryUser." + userId, out var value)) return null;
        if (value == "true") return DateTime.MaxValue;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var end) &&
            end.UtcDateTime > DateTime.UtcNow ? end.UtcDateTime : null;
    }

    public bool HasUnlimitedScans(int userId) => ComplimentaryUntil(userId) is not null &&
        values.TryGetValue("Subscription.UnlimitedScansUser." + userId, out var value) &&
        bool.TryParse(value, out var enabled) && enabled;

    public static bool IsActive(AccountSubscription subscription, DateTime now) =>
        subscription.PeriodEndUtc > now && subscription.PeriodStartUtc <= now &&
        subscription.State is "SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" or "SUBSCRIPTION_STATE_CANCELED";
}
