using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

/// <summary>Usage anomaly detection.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnomaliesController : ControllerBase
{
    private readonly IAnomalyService _anomalyService;

    public AnomaliesController(IAnomalyService anomalyService)
    {
        _anomalyService = anomalyService;
    }

    /// <summary>
    /// Returns days whose daily usage exceeded the user's rolling 30-day average
    /// by more than the threshold percent (default 50%).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AnomalyResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnomalyResultDto>> Get(
        [FromQuery] string? fuelType = null,
        [FromQuery] int daysBack = 60,
        [FromQuery] decimal thresholdPercent = 50m)
    {
        var userId = User.GetUserId();
        var result = await _anomalyService.DetectAnomaliesAsync(userId, fuelType, daysBack, thresholdPercent);
        return Ok(result);
    }
}
