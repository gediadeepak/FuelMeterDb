using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Core.Services;

/// <summary>
/// In-memory auth session.
/// Holds the currently logged-in user and their JWT bearer token.
/// When an IApiClientService is provided the token is forwarded automatically.
/// </summary>
public class AuthStateService
{
    private IApiClientService? _apiClient;

    public UserDto? CurrentUser { get; private set; }
    public string?  Token       { get; private set; }
    public bool     IsAuthenticated => CurrentUser is not null;

    public event Action? OnAuthStateChanged;

    /// <summary>Call once after DI resolves both services.</summary>
    public void AttachApiClient(IApiClientService apiClient) => _apiClient = apiClient;

    public void SetUser(UserDto user, string token)
    {
        CurrentUser = user;
        Token       = token;
        _apiClient?.SetToken(token);
        OnAuthStateChanged?.Invoke();
    }

    public void ClearUser()
    {
        CurrentUser = null;
        Token       = null;
        _apiClient?.SetToken(null);
        OnAuthStateChanged?.Invoke();
    }
}
