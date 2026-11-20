using FuelMeter.Api.Extensions;
using FuelMeter.Api.Services;
using FuelMeter.Core.DTOs;
using FuelMeter.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subscriptions")]
public sealed class SubscriptionsController(SubscriptionService subscriptions, SubscriptionSettings settings) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SubscriptionStatus>> Status(string platform = "Web", CancellationToken cancellationToken = default)
    {
        if (platform is not ("Android" or "iOS" or "Windows" or "Web")) return BadRequest("Unknown platform.");
        return Ok(await subscriptions.GetStatusAsync(User.GetUserId(), platform, cancellationToken));
    }

    [HttpPost("trial/start")]
    public async Task<ActionResult<SubscriptionActionResult>> StartTrial(CancellationToken cancellationToken)
    {
        try
        {
            var policy = await settings.GetAsync(cancellationToken);
            if (!new[] { "Android", "Web", "Windows", "iOS" }.Any(policy.CanUsePremium))
                return Conflict("Trials are currently unavailable.");
            await subscriptions.StartTrialAsync(User.GetUserId(), cancellationToken);
            return Ok(new SubscriptionActionResult(true, "Your 24-hour trial has started."));
        }
        catch (InvalidPurchaseException ex) { return Conflict(new SubscriptionActionResult(false, ex.Message)); }
    }

    [HttpPost("google/verify")]
    public async Task<ActionResult<SubscriptionActionResult>> Verify(GooglePurchaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await subscriptions.VerifyPurchaseAsync(User.GetUserId(), request.PurchaseToken, cancellationToken);
            return Ok(new SubscriptionActionResult(true, "Google Play purchase verified."));
        }
        catch (InvalidPurchaseException ex) { return BadRequest(new SubscriptionActionResult(false, ex.Message)); }
        catch (InvalidOperationException) { return StatusCode(503, new SubscriptionActionResult(false, "Google Play verification is unavailable. Try restoring your purchase shortly.")); }
    }
}
