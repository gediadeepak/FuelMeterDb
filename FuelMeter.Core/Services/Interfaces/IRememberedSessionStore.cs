using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public record RememberedSession(UserDto User, string Token, DateTimeOffset ExpiresAt);

public interface IRememberedSessionStore
{
    Task<RememberedSession?> ReadAsync();
    Task SaveAsync(RememberedSession session);
    Task ClearAsync();
}
