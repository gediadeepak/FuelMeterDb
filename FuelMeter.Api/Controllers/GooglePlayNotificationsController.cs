using System.Text;
using System.Text.Json;
using FuelMeter.Api.Services;
using FuelMeter.Data;
using FuelMeter.Data.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/subscriptions/google/notifications")]
public sealed class GooglePlayNotificationsController(
    FuelMeterDbContext db, SubscriptionSettings settings,
    SubscriptionService subscriptions) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> Receive(JsonElement payload, CancellationToken cancellationToken)
    {
        var policy = await settings.GetAsync(cancellationToken);
        var audience = policy.Text("Google.NotificationAudience");
        var serviceAccount = policy.Text("Google.NotificationServiceAccount");
        if (string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(serviceAccount)) return StatusCode(503);
        var bearer = Request.Headers.Authorization.ToString();
        if (!bearer.StartsWith("Bearer ", StringComparison.Ordinal)) return Unauthorized();
        try
        {
            var identity = await GoogleJsonWebSignature.ValidateAsync(bearer[7..],
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { audience } });
            if (identity.Email != serviceAccount || !identity.EmailVerified ||
                identity.Issuer is not ("accounts.google.com" or "https://accounts.google.com")) return Unauthorized();
        }
        catch (InvalidJwtException) { return Unauthorized(); }

        try
        {
            var encoded = payload.GetProperty("message").GetProperty("data").GetString()!;
            using var notification = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
            var root = notification.RootElement;
            if (root.GetProperty("packageName").GetString() != policy.Text("Google.PackageName")) return BadRequest();
            if (root.TryGetProperty("testNotification", out _)) return NoContent();
            string? token = null;
            if (root.TryGetProperty("subscriptionNotification", out var subscription))
                token = subscription.GetProperty("purchaseToken").GetString();
            else if (root.TryGetProperty("voidedPurchaseNotification", out var voided))
                token = voided.GetProperty("purchaseToken").GetString();
            if (string.IsNullOrWhiteSpace(token)) return NoContent();
            var hash = SubscriptionSecurity.TokenHash(token);
            var account = await db.AccountSubscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
            if (account is null) return NoContent();
            // Re-read Google's authoritative state. Duplicate and out-of-order events cannot reset credits.
            await subscriptions.VerifyPurchaseAsync(account.UserId, token, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) when (ex is JsonException or FormatException or KeyNotFoundException)
        {
            return BadRequest();
        }
    }
}
