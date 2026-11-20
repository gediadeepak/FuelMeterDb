using System.ComponentModel.DataAnnotations;

namespace FuelMeter.Core.DTOs;

public sealed record SubscriptionStatus(
    string Platform, bool PremiumAccessEnabled, bool PurchasesEnabled,
    bool HasSubscription, bool HasTrial, bool CanStartTrial, bool CanScan,
    DateTime? AccessEndsAtUtc, int MonthlyAllowance, int MonthlyRemaining,
    int DailyAllowance, int DailyRemaining, string? ProductId, string? BasePlanId,
    string? PurchaseAccountId, string Message,
    bool IsComplimentary, int PaidMonthlyAllowance);

public sealed class GooglePurchaseRequest
{
    [Required, StringLength(4096, MinimumLength = 10)]
    public string PurchaseToken { get; set; } = "";
}

public sealed record SubscriptionActionResult(bool Success, string Message);
