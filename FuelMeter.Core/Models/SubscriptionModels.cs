namespace FuelMeter.Core.Models;

public sealed class ApplicationSetting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class AccountTrial
{
    public string IdentityHash { get; set; } = "";
    public int? UserId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime? RetainUntilUtc { get; set; }
}

public sealed class AccountSubscription
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public string EncryptedPurchaseToken { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string State { get; set; } = "";
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public bool AutoRenewing { get; set; }
    public DateTime LastVerifiedAtUtc { get; set; }
    public string? LatestOrderId { get; set; }
}

public sealed class AiScanRequest
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string ImageHash { get; set; } = "";
    public string PeriodKey { get; set; } = "";
    public string DayKey { get; set; } = "";
    public string Status { get; set; } = "Reserved";
    public DateTime ReservedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? FuelType { get; set; }
    public decimal? ReadingValue { get; set; }
    public string? ModelName { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
