using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace FuelMeter.Web.Services;

public class RememberedSessionStore(ProtectedLocalStorage storage) : IRememberedSessionStore
{
    private const string Key = "fuelmeter.remembered-session";

    public async Task<RememberedSession?> ReadAsync() =>
        (await storage.GetAsync<RememberedSession>(Key)).Value;

    public Task SaveAsync(RememberedSession session) => storage.SetAsync(Key, session).AsTask();

    public Task ClearAsync() => storage.DeleteAsync(Key).AsTask();
}
