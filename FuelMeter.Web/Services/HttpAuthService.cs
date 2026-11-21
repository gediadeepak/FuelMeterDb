using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Web.Services;

public class HttpAuthService(IApiClientService api) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            return await api.PostAsync<RegisterRequest, AuthResult>("api/auth/register", request)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            return await api.PostAsync<LoginRequest, AuthResult>("api/auth/login", request)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            return await api.PostAsync<ForgotPasswordRequest, AuthResult>("api/auth/forgot-password", request)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException) { return Fail("We couldn't complete your password request. Please try again shortly or contact support@shrijiitservices.com."); }
        catch (OperationCanceledException) { return Fail("The password request timed out. Please check your connection and try again."); }
    }

    public async Task<AuthResult> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            var body = new ResetPasswordRequest { Token = token, NewPassword = newPassword };
            return await api.PostAsync<ResetPasswordRequest, AuthResult>("api/auth/reset-password", body)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException) { return Fail("We couldn't complete your password request. Please try again shortly or contact support@shrijiitservices.com."); }
        catch (OperationCanceledException) { return Fail("The password request timed out. Please check your connection and try again."); }
    }

    public async Task<AuthResult> DeleteAccountAsync(DeleteAccountRequest request)
    {
        try
        {
            return await api.PostAsync<DeleteAccountRequest, AuthResult>("api/auth/delete-account", request)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return Fail("Your session has expired. Sign out and sign in again.");
        }
        catch (HttpRequestException)
        {
            return Fail("FuelMeter could not confirm the deletion. If you can still sign in, try again.");
        }
    }

    private static AuthResult Fail(string message) => new() { Success = false, Message = message };
}
