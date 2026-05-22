using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

public class HttpAuthService(IApiClientService api) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            return await api.PostAsync<RegisterRequest, AuthResult>("api/auth/register", request)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) { return Fail(ex.Message); }
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            return await api.PostAsync<LoginRequest, AuthResult>("api/auth/login", request)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) { return Fail(ex.Message); }
    }

    public async Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            return await api.PostAsync<ForgotPasswordRequest, AuthResult>("api/auth/forgot-password", request)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) { return Fail(ex.Message); }
    }

    public async Task<AuthResult> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            var body = new ResetPasswordRequest { Token = token, NewPassword = newPassword };
            return await api.PostAsync<ResetPasswordRequest, AuthResult>("api/auth/reset-password", body)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) { return Fail(ex.Message); }
    }

    private static AuthResult Fail(string message) => new() { Success = false, Message = message };
}
