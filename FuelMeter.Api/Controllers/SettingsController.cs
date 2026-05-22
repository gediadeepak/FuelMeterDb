using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

/// <summary>User settings — tariff rates, providers and monthly budgets.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class SettingsController(ISettingsService service) : ControllerBase
{
    /// <summary>Return the settings for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSettingsDto>> Get()
    {
        var userId   = User.GetUserId();
        var settings = await service.GetSettingsAsync(userId);
        return settings is null ? NotFound() : Ok(settings);
    }

    /// <summary>Create or update the settings for the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SaveSettingsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SaveSettingsResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveSettingsResult>> Save([FromBody] UserSettingsDto dto)
    {
        var userId = User.GetUserId();
        var result = await service.SaveSettingsAsync(userId, dto);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
