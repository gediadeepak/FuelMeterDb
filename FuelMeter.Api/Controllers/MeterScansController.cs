using System.Text.Json;
using FuelMeter.Api.Services;
using FuelMeter.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FuelMeter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/meter-scans")]
public sealed class MeterScansController(MeterImageAnalysis analysis) : ControllerBase
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

        try
        {
            return Ok(await analysis.AnalyzeAsync(request.Image, imageType, cancellationToken));
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
            ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}
