using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

/// <summary>Budget management — CRUD operations and budget tracking.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class BudgetController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    /// <summary>Get all budgets for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BudgetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BudgetDto>>> GetAll()
    {
        var userId = User.GetUserId();
        var budgets = await _budgetService.GetUserBudgetsAsync(userId);
        return Ok(budgets);
    }

    /// <summary>Get budgets for a specific year.</summary>
    [HttpGet("year/{year:int}")]
    [ProducesResponseType(typeof(List<BudgetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BudgetDto>>> GetByYear(int year)
    {
        var userId = User.GetUserId();
        var budgets = await _budgetService.GetUserBudgetsByYearAsync(userId, year);
        return Ok(budgets);
    }

    /// <summary>Get a single budget by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetDto>> GetById(int id)
    {
        var userId = User.GetUserId();
        var budget = await _budgetService.GetBudgetByIdAsync(id, userId);
        return budget == null ? NotFound() : Ok(budget);
    }

    /// <summary>Get budget for a specific month.</summary>
    [HttpGet("month/{fuelType}/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetDto>> GetByMonth(string fuelType, int year, int month)
    {
        var userId = User.GetUserId();
        var budget = await _budgetService.GetBudgetForMonthAsync(userId, fuelType, year, month);
        return budget == null ? NotFound() : Ok(budget);
    }

    /// <summary>Create a new budget.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BudgetDto>> Create([FromBody] CreateBudgetDto dto)
    {
        var userId = User.GetUserId();

        try
        {
            var budget = await _budgetService.CreateBudgetAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = budget.Id }, budget);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update an existing budget.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BudgetDto>> Update(int id, [FromBody] UpdateBudgetDto dto)
    {
        var userId = User.GetUserId();

        try
        {
            var budget = await _budgetService.UpdateBudgetAsync(id, userId, dto);
            return budget == null ? NotFound() : Ok(budget);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete a budget.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var success = await _budgetService.DeleteBudgetAsync(id, userId);
        return success ? NoContent() : NotFound();
    }

    /// <summary>Get budget summary with actual spend for a specific month.</summary>
    [HttpGet("summary/{fuelType}/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(BudgetSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetSummaryDto>> GetSummary(string fuelType, int year, int month)
    {
        var userId = User.GetUserId();
        var summary = await _budgetService.GetBudgetSummaryAsync(userId, fuelType, year, month);
        return summary == null ? NotFound() : Ok(summary);
    }

    /// <summary>Get monthly budget status for all months in a year.</summary>
    [HttpGet("status/{year:int}")]
    [ProducesResponseType(typeof(List<MonthlyBudgetStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MonthlyBudgetStatusDto>>> GetMonthlyStatus(int year)
    {
        var userId = User.GetUserId();
        var statuses = await _budgetService.GetMonthlyBudgetStatusAsync(userId, year);
        return Ok(statuses);
    }

    /// <summary>Get all over-budget alerts for the user.</summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(List<BudgetSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BudgetSummaryDto>>> GetAlerts()
    {
        var userId = User.GetUserId();
        var alerts = await _budgetService.GetOverBudgetAlertsAsync(userId);
        return Ok(alerts);
    }
}
