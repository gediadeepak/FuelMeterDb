using System.Text.Json;
using System.Security.Cryptography;
using FuelMeter.Api.Services;
using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FuelMeter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/meter-scans")]
public sealed class MeterScansController(MeterImageAnalysis analysis, SubscriptionService subscriptions) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("meter-scans")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<MeterScanResult>> Analyze(
        MeterScanRequest request, CancellationToken cancellationToken)
    {
        var imageType = MeterScanRequest.GetImageType(request.Image);
        if (imageType is null)
            return BadRequest("Use a JPEG or PNG image.");
        if (!analysis.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (request.IdempotencyKey == Guid.Empty)
        {
            // Earlier clients did not send a key. Identical photos from the same account
            // reuse their stored result rather than creating duplicate provider charges.
            request = new MeterScanRequest
            {
                Image = request.Image,
                IdempotencyKey = new Guid(SHA256.HashData(request.Image).AsSpan(0, 16))
            };
        }

        ScanReservation reservation;
        try
        {
            reservation = await subscriptions.ReserveAsync(User.GetUserId(), request, cancellationToken);
        }
        catch (InvalidPurchaseException ex) { return BadRequest(ex.Message); }
        catch (ScanAccessException ex) { return StatusCode(StatusCodes.Status402PaymentRequired, ex.Message); }
        catch (ScanBusyException) { return Conflict("This scan is already processing. Retry the same request shortly."); }

        if (reservation.RequestId is null) return Ok(reservation.CachedResult);
        MeterAnalysisResult? result = null;
        try
        {
            result = await analysis.AnalyzeAsync(request.Image, imageType, cancellationToken);
            return Ok(result.Reading);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
            ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
        finally
        {
            // Persist the result even if the client disconnects after the provider responds.
            using var completion = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await subscriptions.CompleteAsync(User.GetUserId(), reservation.RequestId.Value, result, completion.Token);
        }
    }
}
