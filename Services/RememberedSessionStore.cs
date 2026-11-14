using System.Text.Json;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

public class RememberedSessionStore : IRememberedSessionStore
{
    private const string Key = "fuelmeter.remembered-session";

    public async Task<RememberedSession?> ReadAsync()
    {
        var json = await SecureStorage.Default.GetAsync(Key);
        return json is null ? null : JsonSerializer.Deserialize<RememberedSession>(json);
    }

    public Task SaveAsync(RememberedSession session) =>
        SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(session));

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(Key);
        return Task.CompletedTask;
    }
}
