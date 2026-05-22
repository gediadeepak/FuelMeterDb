using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

/// <summary>Meter readings — CRUD, dashboard summary and usage charts.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class MeterReadingsController(IMeterReadingService service) : ControllerBase
{
    /// <summary>Return a filtered grid of meter readings for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MeterReadingRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MeterReadingRowDto>>> GetGrid(
        [FromQuery] string? fuelType,
        [FromQuery] int?    year,
        [FromQuery] int?    month)
    {
        var userId = User.GetUserId();
        var rows   = await service.GetGridAsync(userId, fuelType, year, month);
        return Ok(rows);
    }

    /// <summary>Return a single meter reading by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MeterReadingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeterReadingDto>> GetById(int id)
    {
        var userId  = User.GetUserId();
        var reading = await service.GetByIdAsync(userId, id);
        return reading is null ? NotFound() : Ok(reading);
    }

    /// <summary>Create a new reading (Id = 0) or update an existing one (Id &gt; 0).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SaveMeterReadingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SaveMeterReadingResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveMeterReadingResult>> Save([FromBody] MeterReadingDto dto)
    {
        var userId = User.GetUserId();
        var result = await service.SaveAsync(userId, dto);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Delete a meter reading by id.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(SaveMeterReadingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SaveMeterReadingResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveMeterReadingResult>> Delete(int id)
    {
        var userId = User.GetUserId();
        var result = await service.DeleteAsync(userId, id);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Return the monthly dashboard summary for the authenticated user.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> GetDashboard(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var userId = User.GetUserId();
        var dto    = await service.GetDashboardAsync(userId, year, month);
        return Ok(dto);
    }

    /// <summary>Return usage chart data. <c>view</c> = Monthly | Yearly.</summary>
    [HttpGet("usage-chart")]
    [ProducesResponseType(typeof(UsageChartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsageChartDto>> GetUsageChart(
        [FromQuery] string view,
        [FromQuery] int    year,
        [FromQuery] int?   month)
    {
        var userId = User.GetUserId();
        var dto    = await service.GetUsageChartAsync(userId, view, year, month);
        return Ok(dto);
    }
}
