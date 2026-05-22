using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

public class HttpSettingsService(IApiClientService api) : ISettingsService
{
    public async Task<UserSettingsDto?> GetSettingsAsync(int userId)
    {
        try
        {
            return await api.GetAsync<UserSettingsDto>("api/settings");
        }
        catch (HttpRequestException) { return null; }
    }

    public async Task<SaveSettingsResult> SaveSettingsAsync(int userId, UserSettingsDto dto)
    {
        try
        {
            return await api.PostAsync<UserSettingsDto, SaveSettingsResult>("api/settings", dto)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) { return Fail(ex.Message); }
    }

    private static SaveSettingsResult Fail(string msg) => new() { Success = false, Message = msg };
}
