using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public interface ISettingsService
{
    Task<UserSettingsDto?> GetSettingsAsync(int userId);
    Task<SaveSettingsResult> SaveSettingsAsync(int userId, UserSettingsDto dto);
}
