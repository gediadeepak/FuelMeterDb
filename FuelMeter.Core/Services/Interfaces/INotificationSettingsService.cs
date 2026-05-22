using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public interface INotificationSettingsService
{
    Task<NotificationSettingsDto> GetAsync(int userId);
    Task<SaveNotificationSettingsResult> SaveAsync(int userId, NotificationSettingsDto dto);
}
