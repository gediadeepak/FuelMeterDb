using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using FuelMeter.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/push-installations")]
public class PushInstallationsController(FuelMeterDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<bool>> SetRegistration(
        PushRegistrationDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!request.Enabled)
        {
            await db.PushInstallations
                .Where(device => device.InstallationId == request.InstallationId && device.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            return Ok(true);
        }

        // Lock the key range so two simultaneous registrations cannot insert the same ID.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            MERGE PushInstallations WITH (HOLDLOCK) AS target
            USING (VALUES ({request.InstallationId}, {userId})) AS source (InstallationId, UserId)
                ON target.InstallationId = source.InstallationId
            WHEN MATCHED THEN UPDATE SET UserId = source.UserId
            WHEN NOT MATCHED THEN INSERT (InstallationId, UserId)
                VALUES (source.InstallationId, source.UserId);
            """, cancellationToken);
        return Ok(true);
    }
}
