using BCrypt.Net;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FuelMeter.Data.Services;

public class AuthService(
    FuelMeterDbContext db,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<AuthService> logger)
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
                FullName = request.FullName.Trim(),
                Email = emailLower,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
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
        try
        {
            return await SendPasswordResetAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Password reset could not be completed.");
            return Fail("We couldn't send a reset link right now. Please try again shortly or contact support@shrijiitservices.com.");
        }
    }

    private async Task<AuthResult> SendPasswordResetAsync(ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Fail("Please enter your email address.");

        var baseUrl = configuration["AppSettings:AppBaseUrl"]?.TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var appUri) ||
            (appUri.Scheme != Uri.UriSchemeHttps && appUri.Scheme != Uri.UriSchemeHttp))
            throw new InvalidOperationException("AppSettings:AppBaseUrl must be an absolute web URL.");

        var emailLower = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailLower && u.IsActive);

        // Always return success to prevent email enumeration
        if (user is null)
            return Ok("If that email is registered, a reset link has been sent.");

        // Invalidate any existing active tokens for this user
        var activeTokens = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        // Cryptographically random and URL-safe; never expose the token in logs.
        var rawToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                              .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = rawToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };
        db.PasswordResetTokens.Add(resetToken);

        await db.SaveChangesAsync();

        try
        {
            var resetLink = $"{baseUrl}/reset-password?token={rawToken}";
            await emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);
        }
        catch
        {
            // A delivery failure must not revoke an earlier working reset link.
            db.PasswordResetTokens.Remove(resetToken);
            await db.SaveChangesAsync();
            throw;
        }

        foreach (var t in activeTokens)
            t.IsUsed = true;
        await db.SaveChangesAsync();

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
        resetToken.User.UpdatedAt = DateTime.UtcNow;
        resetToken.IsUsed = true;

        await db.SaveChangesAsync();

        return Ok("Password reset successfully. You can now sign in.");
    }

    public async Task<AuthResult> DeleteAccountAsync(
        int userId, DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var passwordHash = await db.Users
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => user.PasswordHash)
            .SingleOrDefaultAsync(cancellationToken);

        if (passwordHash is null || !BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
            return Fail("Your current password could not be verified.");

        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await SubscriptionAccountLock.AcquireAsync(db, userId, cancellationToken);
            await db.ApplicationSettings.Where(x => x.Key == "Subscription.ComplimentaryUser." + userId)
                .ExecuteDeleteAsync(cancellationToken);
            await db.ApplicationSettings.Where(x => x.Key == "Subscription.UnlimitedScansUser." + userId)
                .ExecuteDeleteAsync(cancellationToken);
            var trial = await db.AccountTrials.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
            if (trial is not null)
            {
                // Keep only the protected eligibility identity and trial dates after account deletion.
                trial.UserId = null;
                trial.DeletedAtUtc = DateTime.UtcNow;
                var policy = await new SubscriptionSettings(db).GetAsync(cancellationToken);
                trial.RetainUntilUtc = trial.DeletedAtUtc.Value.AddYears(policy.Number("Trial.RetentionYears", 1, 1));
                await db.SaveChangesAsync(cancellationToken);
            }

            // The foreign keys cascade to all account-owned records in the same database operation.
            var deleted = await db.Users
                .Where(user => user.Id == userId && user.IsActive && user.PasswordHash == passwordHash)
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted == 1 || !await db.Users.AnyAsync(x => x.Id == userId, cancellationToken))
            {
                await transaction.CommitAsync(cancellationToken);
                return Ok("Your FuelMeter account has been deleted.");
            }
            return Fail("Your account changed. Sign in again before retrying.");
        });
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
        User = user is null ? null : new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt = user.LastLoginAt
        }
    };

    private static AuthResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
