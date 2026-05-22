using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public class NotificationSettingsService(FuelMeterDbContext db) : INotificationSettingsService
{
    public async Task<NotificationSettingsDto> GetAsync(int userId)
    {
        var entity = await db.NotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        return entity is null ? new NotificationSettingsDto() : ToDto(entity);
    }

    public async Task<SaveNotificationSettingsResult> SaveAsync(int userId, NotificationSettingsDto dto)
    {
        try
        {
            var entity = await db.NotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);

            if (entity is null)
            {
                entity = new NotificationSettings { UserId = userId, CreatedAt = DateTime.UtcNow };
                db.NotificationSettings.Add(entity);
            }
            else
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }

            entity.ElectricityEnabled    = dto.ElectricityEnabled;
            entity.ElectricityFrequency  = dto.ElectricityFrequency;
            entity.ElectricityHour       = dto.ElectricityHour;
            entity.ElectricityMinute     = dto.ElectricityMinute;
            entity.ElectricityDayOfWeek  = dto.ElectricityDayOfWeek;
            entity.ElectricityDayOfMonth = dto.ElectricityDayOfMonth;

            entity.GasEnabled    = dto.GasEnabled;
            entity.GasFrequency  = dto.GasFrequency;
            entity.GasHour       = dto.GasHour;
            entity.GasMinute     = dto.GasMinute;
            entity.GasDayOfWeek  = dto.GasDayOfWeek;
            entity.GasDayOfMonth = dto.GasDayOfMonth;

            await db.SaveChangesAsync();
            return new SaveNotificationSettingsResult { Success = true, Message = "Notification settings saved." };
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            return new SaveNotificationSettingsResult { Success = false, Message = "Unable to reach the server. Please check your connection." };
        }
    }

    private static NotificationSettingsDto ToDto(NotificationSettings s) => new()
    {
        ElectricityEnabled    = s.ElectricityEnabled,
        ElectricityFrequency  = s.ElectricityFrequency,
        ElectricityHour       = s.ElectricityHour,
        ElectricityMinute     = s.ElectricityMinute,
        ElectricityDayOfWeek  = s.ElectricityDayOfWeek,
        ElectricityDayOfMonth = s.ElectricityDayOfMonth,
        GasEnabled    = s.GasEnabled,
        GasFrequency  = s.GasFrequency,
        GasHour       = s.GasHour,
        GasMinute     = s.GasMinute,
        GasDayOfWeek  = s.GasDayOfWeek,
        GasDayOfMonth = s.GasDayOfMonth,
    };

    private static bool IsConnectionError(Exception ex) =>
        ex is Microsoft.Data.SqlClient.SqlException ||
        ex.InnerException is Microsoft.Data.SqlClient.SqlException;
}
