using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

public class HttpNotificationSettingsService(IApiClientService api) : INotificationSettingsService
{
    public async Task<NotificationSettingsDto> GetAsync(int userId)
    {
        try
        {
            return await api.GetAsync<NotificationSettingsDto>("api/notification-settings")
                   ?? new NotificationSettingsDto();
        }
        catch (HttpRequestException) { return new NotificationSettingsDto(); }
    }

    public async Task<SaveNotificationSettingsResult> SaveAsync(int userId, NotificationSettingsDto dto)
    {
        try
        {
            return await api.PostAsync<NotificationSettingsDto, SaveNotificationSettingsResult>(
                       "api/notification-settings", dto)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) { return Fail(ex.Message); }
    }

    private static SaveNotificationSettingsResult Fail(string msg) => new() { Success = false, Message = msg };
}
