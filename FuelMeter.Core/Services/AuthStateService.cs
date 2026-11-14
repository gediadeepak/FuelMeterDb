using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Core.Services;

/// <summary>
/// In-memory auth session.
/// Holds the currently logged-in user and their JWT bearer token.
/// When an IApiClientService is provided the token is forwarded automatically.
/// </summary>
public class AuthStateService(IRememberedSessionStore store)
{
    private IApiClientService? _apiClient;

    public UserDto? CurrentUser { get; private set; }
    public string?  Token       { get; private set; }
    public bool     IsAuthenticated => CurrentUser is not null;

    public event Action? OnAuthStateChanged;

    /// <summary>Call once after DI resolves both services.</summary>
    public void AttachApiClient(IApiClientService apiClient) => _apiClient = apiClient;

    public async Task RestoreAsync()
    {
        if (IsAuthenticated) return;
        try
        {
            var session = await store.ReadAsync();
            if (session is { } && session.ExpiresAt > DateTimeOffset.UtcNow)
                SetUser(session.User, session.Token);
            else if (session is not null)
                await store.ClearAsync();
        }
        catch
        {
            // The app can still sign in when device or browser storage is unavailable.
        }
    }

    public async Task SignInAsync(UserDto user, string token, DateTimeOffset? expiresAt, bool staySignedIn)
    {
        if (staySignedIn)
        {
            if (expiresAt is not { } expiration || expiration <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("The server did not provide a valid remembered session.");

            await store.SaveAsync(new RememberedSession(user, token, expiration));
        }
        else
        {
            await store.ClearAsync();
        }

        SetUser(user, token);
    }

    private void SetUser(UserDto user, string token)
    {
        CurrentUser = user;
        Token       = token;
        _apiClient?.SetToken(token);
        OnAuthStateChanged?.Invoke();
    }

    public async Task SignOutAsync()
    {
        try
        {
            await store.ClearAsync();
        }
        finally
        {
            CurrentUser = null;
            Token       = null;
            _apiClient?.SetToken(null);
            OnAuthStateChanged?.Invoke();
        }
    }
}
