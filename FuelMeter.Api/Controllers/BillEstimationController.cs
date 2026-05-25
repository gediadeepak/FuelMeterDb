using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

/// <summary>Bill estimation and cost analysis based on meter readings.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class BillEstimationController : ControllerBase
{
    private readonly IBillEstimationService _billEstimationService;

    public BillEstimationController(IBillEstimationService billEstimationService)
    {
        _billEstimationService = billEstimationService;
    }

    /// <summary>Estimate bill for a custom date range.</summary>
    [HttpPost("estimate")]
    [ProducesResponseType(typeof(BillEstimationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillEstimationDto>> EstimateBill([FromBody] BillEstimationRequestDto request)
    {
        var userId = User.GetUserId();
        var estimation = await _billEstimationService.EstimateBillAsync(userId, request);
        return estimation == null ? NotFound(new { error = "Insufficient data for estimation" }) : Ok(estimation);
    }

    /// <summary>Estimate bill for the current month.</summary>
    [HttpGet("current/{fuelType}")]
    [ProducesResponseType(typeof(BillEstimationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillEstimationDto>> EstimateCurrentMonth(string fuelType)
    {
        var userId = User.GetUserId();
        var estimation = await _billEstimationService.EstimateCurrentMonthBillAsync(userId, fuelType);
        return estimation == null ? NotFound(new { error = "Insufficient data for estimation" }) : Ok(estimation);
    }

    /// <summary>Get monthly bill summaries for a year.</summary>
    [HttpGet("summaries/{year:int}")]
    [ProducesResponseType(typeof(List<MonthlyBillSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MonthlyBillSummaryDto>>> GetMonthlySummaries(int year)
    {
        var userId = User.GetUserId();
        var summaries = await _billEstimationService.GetMonthlyBillSummariesAsync(userId, year);
        return Ok(summaries);
    }

    /// <summary>Get bill history for a specific fuel type.</summary>
    [HttpGet("history/{fuelType}")]
    [ProducesResponseType(typeof(List<MonthlyBillSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MonthlyBillSummaryDto>>> GetBillHistory(
        string fuelType,
        [FromQuery] int months = 12)
    {
        var userId = User.GetUserId();
        var history = await _billEstimationService.GetBillHistoryAsync(userId, fuelType, months);
        return Ok(history);
    }

    /// <summary>Compare current month's bill with previous month.</summary>
    [HttpGet("compare/{fuelType}/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(BillComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillComparisonDto>> CompareBills(string fuelType, int year, int month)
    {
        var userId = User.GetUserId();
        var comparison = await _billEstimationService.CompareBillsAsync(userId, fuelType, year, month);
        return comparison == null ? NotFound(new { error = "Insufficient data for comparison" }) : Ok(comparison);
    }

    /// <summary>Project full month bill based on current usage.</summary>
    [HttpGet("project/{fuelType}/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<ActionResult<decimal>> ProjectMonthlyBill(string fuelType, int year, int month)
    {
        var userId = User.GetUserId();
        var projection = await _billEstimationService.ProjectMonthlyBillAsync(userId, fuelType, year, month);
        return Ok(projection);
    }

    /// <summary>Calculate daily average cost for a date range.</summary>
    [HttpGet("daily-average/{fuelType}")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<ActionResult<decimal>> GetDailyAverage(
        string fuelType,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var userId = User.GetUserId();
        var average = await _billEstimationService.CalculateDailyAverageCostAsync(userId, fuelType, startDate, endDate);
        return Ok(average);
    }

    /// <summary>Get comprehensive dashboard data including current bills and comparisons.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.GetUserId();
        var now = DateTime.UtcNow;

        var electricityCurrent = await _billEstimationService.EstimateCurrentMonthBillAsync(userId, "Electricity");
        var gasCurrent = await _billEstimationService.EstimateCurrentMonthBillAsync(userId, "Gas");

        var electricityComparison = await _billEstimationService.CompareBillsAsync(userId, "Electricity", now.Year, now.Month);
        var gasComparison = await _billEstimationService.CompareBillsAsync(userId, "Gas", now.Year, now.Month);

        var electricityProjection = await _billEstimationService.ProjectMonthlyBillAsync(userId, "Electricity", now.Year, now.Month);
        var gasProjection = await _billEstimationService.ProjectMonthlyBillAsync(userId, "Gas", now.Year, now.Month);

        return Ok(new
        {
            electricity = new
            {
                current = electricityCurrent,
                comparison = electricityComparison,
                projection = electricityProjection
            },
            gas = new
            {
                current = gasCurrent,
                comparison = gasComparison,
                projection = gasProjection
            }
        });
    }
}
