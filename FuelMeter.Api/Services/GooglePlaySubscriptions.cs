using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FuelMeter.Data.Services;
using Google.Apis.Auth.OAuth2;

namespace FuelMeter.Api.Services;

public sealed record VerifiedGoogleSubscription(
    string ProductId, string State, DateTime StartUtc, DateTime EndUtc,
    string AccountId, string? LinkedToken, string? OrderId, bool AutoRenewing);

public sealed class GooglePlaySubscriptions(HttpClient http, IConfiguration configuration)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["GooglePlay:CredentialJson"]) ||
        !string.IsNullOrWhiteSpace(configuration["GooglePlay:CredentialPath"]) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"));

    private async Task<HttpRequestMessage> RequestAsync(
        HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var credentialPath = configuration["GooglePlay:CredentialPath"];
        var credentialJson = configuration["GooglePlay:CredentialJson"];
        var credential = !string.IsNullOrWhiteSpace(credentialJson)
            ? CredentialFactory.FromJson<ServiceAccountCredential>(credentialJson).ToGoogleCredential()
            : !string.IsNullOrWhiteSpace(credentialPath)
                ? (await CredentialFactory.FromFileAsync<ServiceAccountCredential>(credentialPath, cancellationToken)).ToGoogleCredential()
                : await GoogleCredential.GetApplicationDefaultAsync(cancellationToken);
        credential = credential.CreateScoped("https://www.googleapis.com/auth/androidpublisher");
        var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
        var request = new HttpRequestMessage(method, "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<VerifiedGoogleSubscription> VerifyAsync(
        SubscriptionPolicy policy, string purchaseToken, CancellationToken cancellationToken)
    {
        var package = Uri.EscapeDataString(policy.Text("Google.PackageName"));
        using var request = await RequestAsync(HttpMethod.Get,
            $"{package}/purchases/subscriptionsv2/tokens/{Uri.EscapeDataString(purchaseToken)}", cancellationToken);
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            throw new GooglePurchaseUnavailableException();
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new InvalidPurchaseException("Google Play could not verify this purchase.");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Google Play purchase verification is temporarily unavailable.");
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = payload.RootElement;
        if (!root.TryGetProperty("regionCode", out var region) || region.GetString() != "GB")
            throw new InvalidPurchaseException("FuelMeter subscriptions are currently available for UK Google Play accounts.");
        var items = root.GetProperty("lineItems").EnumerateArray()
            .Where(x => x.GetProperty("productId").GetString() == policy.Text("Google.ProductId") &&
                x.TryGetProperty("offerDetails", out var offer) &&
                offer.GetProperty("basePlanId").GetString() == policy.Text("Google.BasePlanId"))
            .OrderByDescending(x => x.GetProperty("expiryTime").GetDateTime()).ToArray();
        if (items.Length != 1) throw new InvalidPurchaseException("The purchase does not match the FuelMeter monthly plan.");
        var item = items[0];
        var start = root.GetProperty("startTime").GetDateTime().ToUniversalTime();
        var end = item.GetProperty("expiryTime").GetDateTime().ToUniversalTime();
        var state = root.GetProperty("subscriptionState").GetString() ?? "";
        var account = root.TryGetProperty("externalAccountIdentifiers", out var ids) &&
            ids.TryGetProperty("obfuscatedExternalAccountId", out var id) ? id.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(account)) throw new InvalidPurchaseException("This purchase has no FuelMeter account reference.");
        var order = item.TryGetProperty("latestSuccessfulOrderId", out var orderId) ? orderId.GetString() : null;
        var linked = root.TryGetProperty("linkedPurchaseToken", out var linkedToken) ? linkedToken.GetString() : null;
        var renewing = item.TryGetProperty("autoRenewingPlan", out var plan) &&
            plan.TryGetProperty("autoRenewEnabled", out var enabled) && enabled.GetBoolean();

        if (state is "SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" or "SUBSCRIPTION_STATE_CANCELED" &&
            end > DateTime.UtcNow &&
            root.GetProperty("acknowledgementState").GetString() == "ACKNOWLEDGEMENT_STATE_PENDING")
        {
            using var acknowledge = await RequestAsync(HttpMethod.Post,
                $"{package}/purchases/subscriptions/{Uri.EscapeDataString(policy.Text("Google.ProductId"))}/tokens/{Uri.EscapeDataString(purchaseToken)}:acknowledge",
                cancellationToken);
            acknowledge.Content = JsonContent.Create(new { });
            using var acknowledged = await http.SendAsync(acknowledge, cancellationToken);
            if (!acknowledged.IsSuccessStatusCode)
                throw new InvalidOperationException("Google Play acknowledgement is pending. Restore the purchase shortly.");
        }
        return new(item.GetProperty("productId").GetString()!, state, start, end, account, linked, order, renewing);
    }
}

public sealed class InvalidPurchaseException(string message) : Exception(message);
public sealed class GooglePurchaseUnavailableException : Exception;
