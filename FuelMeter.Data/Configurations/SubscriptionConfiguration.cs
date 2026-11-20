using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelMeter.Data.Configurations;

public sealed class SubscriptionConfiguration :
    IEntityTypeConfiguration<ApplicationSetting>,
    IEntityTypeConfiguration<AccountTrial>,
    IEntityTypeConfiguration<AccountSubscription>,
    IEntityTypeConfiguration<AiScanRequest>
{
    public void Configure(EntityTypeBuilder<ApplicationSetting> entity)
    {
        entity.HasKey(x => x.Key);
        entity.Property(x => x.Key).HasMaxLength(160);
        entity.Property(x => x.Value).HasMaxLength(2048);
        var defaults = new Dictionary<string, string>
        {
            ["Subscription.Android.PurchasesEnabled"] = "true",
            ["Subscription.Android.PremiumAccessEnabled"] = "true",
            ["Subscription.Web.PurchasesEnabled"] = "false",
            ["Subscription.Web.PremiumAccessEnabled"] = "true",
            ["Subscription.Windows.PurchasesEnabled"] = "false",
            ["Subscription.Windows.PremiumAccessEnabled"] = "true",
            ["Subscription.iOS.PurchasesEnabled"] = "false",
            ["Subscription.iOS.PremiumAccessEnabled"] = "false",
            ["Subscription.Trial.Hours"] = "24",
            ["Subscription.Trial.Scans"] = "1",
            ["Subscription.Trial.RetentionYears"] = "1",
            ["Subscription.Scans.Monthly"] = "30",
            ["Subscription.Scans.Daily"] = "5",
            ["Subscription.Scans.AttemptsDaily"] = "10",
            ["Subscription.Scans.AttemptsMonthly"] = "60",
            ["Subscription.Scans.ReservationSeconds"] = "120",
            ["Subscription.Scans.TimeZone"] = "Europe/London",
            ["Subscription.Google.PackageName"] = "com.fuelmeter.app",
            ["Subscription.Google.ProductId"] = "fuelmeter_premium_monthly",
            ["Subscription.Google.BasePlanId"] = "monthly",
            ["Subscription.Google.NotificationAudience"] = "",
            ["Subscription.Google.NotificationServiceAccount"] = "",
            ["Subscription.Google.ReverifyMinutes"] = "5",
            ["Subscription.Scan.Model"] = "gpt-5-mini",
            ["Subscription.Scan.InputUsdPerMillionTokens"] = "0.25",
            ["Subscription.Scan.OutputUsdPerMillionTokens"] = "2.00",
            ["Subscription.PriceGBP"] = "1.99"
        };
        entity.HasData(defaults.Select(x => new ApplicationSetting
        {
            Key = x.Key, Value = x.Value, UpdatedAtUtc = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc)
        }));
    }

    public void Configure(EntityTypeBuilder<AccountTrial> entity)
    {
        entity.HasKey(x => x.IdentityHash);
        entity.Property(x => x.IdentityHash).HasMaxLength(64);
        entity.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        entity.HasIndex(x => x.RetainUntilUtc);
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<AccountSubscription> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.TokenHash).HasMaxLength(64);
        entity.Property(x => x.EncryptedPurchaseToken).HasMaxLength(8192);
        entity.Property(x => x.ProductId).HasMaxLength(200);
        entity.Property(x => x.State).HasMaxLength(80);
        entity.Property(x => x.LatestOrderId).HasMaxLength(200);
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => new { x.UserId, x.PeriodEndUtc });
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<AiScanRequest> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ImageHash).HasMaxLength(64);
        entity.Property(x => x.PeriodKey).HasMaxLength(200);
        entity.Property(x => x.DayKey).HasMaxLength(10);
        entity.Property(x => x.Status).HasMaxLength(20);
        entity.Property(x => x.FuelType).HasMaxLength(20);
        entity.Property(x => x.ModelName).HasMaxLength(100);
        entity.Property(x => x.ReadingValue).HasPrecision(18, 2);
        entity.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();
        entity.HasIndex(x => new { x.UserId, x.PeriodKey, x.Status });
        entity.HasIndex(x => new { x.UserId, x.DayKey, x.Status });
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
