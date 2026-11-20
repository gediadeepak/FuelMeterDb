namespace FuelMeter.Core.Services.Interfaces;

public sealed record SubscriptionOffer(string Price, string BillingPeriod, string OfferToken);

public interface ISubscriptionPurchases
{
    string Platform { get; }
    Task<SubscriptionOffer?> GetOfferAsync(string productId, string basePlanId);
    Task<IReadOnlyList<string>> PurchaseAsync(string productId, string basePlanId, string accountId);
    Task<IReadOnlyList<string>> RestoreAsync();
}
