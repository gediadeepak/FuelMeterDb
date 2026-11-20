using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

public sealed class UnavailableSubscriptionPurchases : ISubscriptionPurchases
{
    public string Platform =>
#if IOS
        "iOS";
#elif WINDOWS
        "Windows";
#else
        "Web";
#endif
    public Task<SubscriptionOffer?> GetOfferAsync(string productId, string basePlanId) => Task.FromResult<SubscriptionOffer?>(null);
    public Task<IReadOnlyList<string>> PurchaseAsync(string productId, string basePlanId, string accountId) =>
        Task.FromException<IReadOnlyList<string>>(new InvalidOperationException("Purchases are unavailable on this platform."));
    public Task<IReadOnlyList<string>> RestoreAsync() =>
        Task.FromException<IReadOnlyList<string>>(new InvalidOperationException("Restore this subscription through Google Play on Android."));
}
