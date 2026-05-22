using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<AuthResult> ResetPasswordAsync(string token, string newPassword);
}
