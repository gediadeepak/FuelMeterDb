using BCrypt.Net;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FuelMeter.Data.Services;

public class AuthService(
    FuelMeterDbContext db,
    IEmailService emailService,
    IConfiguration configuration) : IAuthService
{
    // ── Register ────────────────────────────────────────────────
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var emailLower = request.Email.Trim().ToLowerInvariant();

            if (await db.Users.AnyAsync(u => u.Email == emailLower))
                return Fail("An account with this email address already exists.");

            var user = new User
            {
                FullName     = request.FullName.Trim(),
                Email        = emailLower,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt    = DateTime.UtcNow,
                IsActive     = true
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Ok("Account created successfully.", user);
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            return Fail("Unable to reach the server. Please check your internet connection and try again.");
        }
    }

    // ── Login ────────────────────────────────────────────────────
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            var emailLower = request.Email.Trim().ToLowerInvariant();

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == emailLower && u.IsActive);

            if (user is null)
                return Fail("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Fail("Invalid email or password.");

            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok("Login successful.", user);
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            return Fail("Unable to reach the server. Please check your internet connection and try again.");
        }
    }

    // ── Forgot Password ──────────────────────────────────────────
    public async Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var emailLower = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailLower && u.IsActive);

        // Always return success to prevent email enumeration
        if (user is null)
            return Ok("If that email is registered, a reset link has been sent.");

        // Invalidate any existing active tokens for this user
        var activeTokens = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var t in activeTokens)
            t.IsUsed = true;

        // Create new token (SHA-256 GUID-based, URL-safe)
        var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                              .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId    = user.Id,
            Token     = rawToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var baseUrl   = configuration["AppSettings:AppBaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var resetLink = $"{baseUrl}/reset-password?token={rawToken}";

        try
        {
            await emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);
        }
        catch
        {
            // Email delivery failure must not expose user existence — swallow and continue
        }

        return Ok("If that email is registered, a reset link has been sent.");
    }

    // ── Reset Password ───────────────────────────────────────────
    public async Task<AuthResult> ResetPasswordAsync(string token, string newPassword)
    {
        var resetToken = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

        if (resetToken is null)
            return Fail("The reset link is invalid or has expired. Please request a new one.");

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        resetToken.User.UpdatedAt    = DateTime.UtcNow;
        resetToken.IsUsed            = true;

        await db.SaveChangesAsync();

        return Ok("Password reset successfully. You can now sign in.");
    }

    // ── Helpers ──────────────────────────────────────────────────
    private static bool IsConnectionError(Exception ex) =>
        ex is Microsoft.Data.SqlClient.SqlException ||
        ex.InnerException is Microsoft.Data.SqlClient.SqlException ||
        ex is TimeoutException ||
        ex.InnerException is TimeoutException;

    private static AuthResult Ok(string message, User? user = null) => new()
    {
        Success = true,
        Message = message,
        User    = user is null ? null : new UserDto
        {
            Id              = user.Id,
            FullName        = user.FullName,
            Email           = user.Email,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt     = user.LastLoginAt
        }
    };

    private static AuthResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
