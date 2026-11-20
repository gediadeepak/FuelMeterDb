using Android.BillingClient.Api;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Core.Services;
using FuelMeter.Core.DTOs;

namespace FuelMeter.Platforms.Android;

public sealed class GooglePlayPurchases : Java.Lang.Object, ISubscriptionPurchases, IPurchasesUpdatedListener
{
    private readonly BillingClient _client;
    private readonly SemaphoreSlim _connection = new(1, 1);
    private TaskCompletionSource<IReadOnlyList<string>>? _purchase;
    private string? _productId;
    private readonly IApiClientService _api;
    private readonly AuthStateService _auth;
    private readonly SemaphoreSlim _synchronization = new(1, 1);
    public string Platform => "Android";

    public GooglePlayPurchases(IApiClientService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        _client = BillingClient.NewBuilder(global::Android.App.Application.Context)
            .SetListener(this)
            .EnablePendingPurchases(PendingPurchasesParams.NewBuilder().EnableOneTimeProducts().Build())
            .EnableAutoServiceReconnection()
            .Build();
    }

    public void Refresh() => _ = SynchronizeAsync();

    private async Task SynchronizeAsync()
    {
        if (!_auth.IsAuthenticated || _purchase is not null || !await _synchronization.WaitAsync(0)) return;
        var userId = _auth.CurrentUser?.Id;
        try
        {
            var status = await _api.GetAsync<SubscriptionStatus>("api/subscriptions/status?platform=Android");
            if (status?.PurchasesEnabled != true) return;
            foreach (var token in await RestoreAsync())
            {
                if (!_auth.IsAuthenticated || _auth.CurrentUser?.Id != userId) return;
                await _api.PostAsync<GooglePurchaseRequest, SubscriptionActionResult>(
                    "api/subscriptions/google/verify", new() { PurchaseToken = token });
            }
        }
        catch (Exception)
        {
            // A network failure or a pending payment is retried on resume or through Restore.
        }
        finally { _synchronization.Release(); }
    }

    private static void EnsureSuccess(BillingResult result)
    {
        if (result.ResponseCode == BillingResponseCode.UserCancelled) throw new OperationCanceledException();
        if (result.ResponseCode != BillingResponseCode.Ok)
            throw new InvalidOperationException("Google Play billing is unavailable.");
    }

    private async Task ConnectAsync()
    {
        await _connection.WaitAsync();
        try
        {
            if (!_client.IsReady)
                EnsureSuccess(await _client.StartConnectionAsync().WaitAsync(TimeSpan.FromSeconds(20)));
        }
        finally { _connection.Release(); }
    }

    private async Task<(ProductDetails Product, ProductDetails.SubscriptionOfferDetails Offer)?> GetProductAsync(string productId, string basePlanId)
    {
        await ConnectAsync();
        var product = QueryProductDetailsParams.Product.NewBuilder()
            .SetProductId(productId).SetProductType(BillingClient.ProductType.Subs).Build();
        var query = QueryProductDetailsParams.NewBuilder().SetProductList(new[] { product }).Build();
        var response = await _client.QueryProductDetailsAsync(query).WaitAsync(TimeSpan.FromSeconds(20));
        EnsureSuccess(response.Result);
        var details = response.ProductDetails.SingleOrDefault(x => x.ProductId == productId);
        var offer = details?.GetSubscriptionOfferDetails()?.SingleOrDefault(x => x.BasePlanId == basePlanId && x.OfferId is null);
        if (details is null || offer is null) return null;
        var phases = offer.PricingPhases.PricingPhaseList;
        // Launch only the regular UK monthly plan, whose actual store price is displayed to the customer.
        if (phases.Count != 1 || phases[0].BillingPeriod != "P1M" || phases[0].PriceCurrencyCode != "GBP")
            return null;
        return (details, offer);
    }

    public async Task<SubscriptionOffer?> GetOfferAsync(string productId, string basePlanId)
    {
        var product = await GetProductAsync(productId, basePlanId);
        if (product is null) return null;
        var phase = product.Value.Offer.PricingPhases.PricingPhaseList[0];
        return new(phase.FormattedPrice, phase.BillingPeriod, product.Value.Offer.OfferToken);
    }

    public async Task<IReadOnlyList<string>> PurchaseAsync(string productId, string basePlanId, string accountId)
    {
        if (_purchase is not null) throw new InvalidOperationException("A purchase is already processing.");
        var details = await GetProductAsync(productId, basePlanId)
            ?? throw new InvalidOperationException("The FuelMeter monthly plan is unavailable.");
        // Refresh purchases first to avoid purchasing the same plan twice after reinstalling.
        var owned = await RestoreAsync();
        if (owned.Count > 0) return owned;
        _productId = productId;
        _purchase = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var product = BillingFlowParams.ProductDetailsParams.NewBuilder()
                .SetProductDetails(details.Product).SetOfferToken(details.Offer.OfferToken).Build();
            var flow = BillingFlowParams.NewBuilder().SetObfuscatedAccountId(accountId)
                .SetProductDetailsParamsList(new[] { product }).Build();
            var result = await MainThread.InvokeOnMainThreadAsync(() =>
                _client.LaunchBillingFlow(global::Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                    ?? throw new InvalidOperationException("Open FuelMeter before purchasing."), flow));
            EnsureSuccess(result);
            return await _purchase.Task.WaitAsync(TimeSpan.FromMinutes(5));
        }
        finally { _purchase = null; }
    }

    public async Task<IReadOnlyList<string>> RestoreAsync()
    {
        await ConnectAsync();
        var query = QueryPurchasesParams.NewBuilder().SetProductType(BillingClient.ProductType.Subs).Build();
        var response = await _client.QueryPurchasesAsync(query).WaitAsync(TimeSpan.FromSeconds(20));
        EnsureSuccess(response.Result);
        return response.Purchases.Where(x => x.PurchaseState == PurchaseState.Purchased)
            .Select(x => x.PurchaseToken).Distinct().ToArray();
    }

    public void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases)
    {
        try
        {
            EnsureSuccess(result);
            var tokens = purchases?.Where(x => x.PurchaseState == PurchaseState.Purchased &&
                (_productId is null || x.Products.Contains(_productId)))
                .Select(x => x.PurchaseToken).Distinct().ToArray() ?? [];
            _purchase?.TrySetResult(tokens);
        }
        catch (Exception ex) { _purchase?.TrySetException(ex); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client.EndConnection();
            _client.Dispose();
            _connection.Dispose();
            _synchronization.Dispose();
        }
        base.Dispose(disposing);
    }
}
