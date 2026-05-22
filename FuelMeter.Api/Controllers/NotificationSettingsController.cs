using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

/// <summary>Notification settings — schedule electricity and gas reminders.</summary>
[ApiController]
[Authorize]
[Route("api/notification-settings")]
[Produces("application/json")]
public class NotificationSettingsController(INotificationSettingsService service) : ControllerBase
{
    /// <summary>Return the notification settings for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSettingsDto>> Get()
    {
        var userId = User.GetUserId();
        var dto    = await service.GetAsync(userId);
        return Ok(dto);
    }

    /// <summary>Create or update the notification settings for the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SaveNotificationSettingsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SaveNotificationSettingsResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveNotificationSettingsResult>> Save([FromBody] NotificationSettingsDto dto)
    {
        var userId = User.GetUserId();
        var result = await service.SaveAsync(userId, dto);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
