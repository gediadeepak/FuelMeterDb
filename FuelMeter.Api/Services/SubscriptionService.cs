using System.Security.Cryptography;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Data;
using FuelMeter.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Api.Services;

public sealed record ScanReservation(long? RequestId, MeterScanResult? CachedResult);

public sealed class SubscriptionService(
    FuelMeterDbContext db, SubscriptionSettings settings, SubscriptionSecurity security,
    GooglePlaySubscriptions google)
{
    public async Task VerifyPurchaseAsync(int userId, string token, CancellationToken cancellationToken)
    {
        var requestedAtUtc = DateTime.UtcNow;
        var policy = await settings.GetAsync(cancellationToken);
        var hash = SubscriptionSecurity.TokenHash(token);
        VerifiedGoogleSubscription verified;
        try { verified = await google.VerifyAsync(policy, token, cancellationToken); }
        catch (GooglePurchaseUnavailableException)
        {
            // Google eventually stops returning old purchase tokens. Expire only a token
            // already bound to this account, so old purchases cannot break its status page.
            await using var expiryTransaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await SubscriptionAccountLock.AcquireAsync(db, userId, cancellationToken);
            var expired = await db.AccountSubscriptions
                .Where(x => x.UserId == userId && x.TokenHash == hash && x.LastVerifiedAtUtc <= requestedAtUtc)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.State, "SUBSCRIPTION_STATE_EXPIRED")
                    .SetProperty(s => s.LastVerifiedAtUtc, DateTime.UtcNow), cancellationToken);
            if (expired == 0 && !await db.AccountSubscriptions.AnyAsync(x => x.UserId == userId && x.TokenHash == hash, cancellationToken))
                throw new InvalidPurchaseException("Google Play could not verify this purchase.");
            await expiryTransaction.CommitAsync(cancellationToken);
            return;
        }
        if (verified.AccountId != security.AccountId(userId))
            throw new InvalidPurchaseException("Sign in to the FuelMeter account used for this purchase.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SubscriptionAccountLock.AcquireAsync(db, userId, cancellationToken);
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken))
            throw new InvalidPurchaseException("The FuelMeter account is unavailable.");
        var subscription = await db.AccountSubscriptions.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (subscription is not null && subscription.UserId != userId)
            throw new InvalidPurchaseException("This purchase is linked to a different FuelMeter account.");
        subscription ??= new AccountSubscription { UserId = userId, TokenHash = hash };
        // Older network responses cannot roll a renewed subscription back to an earlier paid period.
        if (subscription.LastVerifiedAtUtc > requestedAtUtc)
            return;
        if (subscription.Id == 0) db.AccountSubscriptions.Add(subscription);
        subscription.EncryptedPurchaseToken = security.EncryptToken(token);
        subscription.ProductId = verified.ProductId;
        subscription.State = verified.State;
        subscription.PeriodEndUtc = verified.EndUtc;
        subscription.PeriodStartUtc = verified.EndUtc.AddMonths(-1) > verified.StartUtc
            ? verified.EndUtc.AddMonths(-1) : verified.StartUtc;
        subscription.LastVerifiedAtUtc = DateTime.UtcNow;
        subscription.LatestOrderId = verified.OrderId;
        subscription.AutoRenewing = verified.AutoRenewing;
        if (!string.IsNullOrWhiteSpace(verified.LinkedToken))
        {
            var linkedHash = SubscriptionSecurity.TokenHash(verified.LinkedToken);
            var linked = await db.AccountSubscriptions.SingleOrDefaultAsync(x => x.TokenHash == linkedHash, cancellationToken);
            if (linked is not null)
            {
                if (linked.UserId != userId) throw new InvalidPurchaseException("A linked purchase belongs to another account.");
                linked.State = "SUBSCRIPTION_STATE_EXPIRED";
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RefreshAsync(int userId, SubscriptionPolicy policy, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-policy.Number("Google.ReverifyMinutes", 1, 60));
        var subscriptions = await db.AccountSubscriptions.AsNoTracking()
            .Where(x => x.UserId == userId && x.LastVerifiedAtUtc < cutoff &&
                x.State != "SUBSCRIPTION_STATE_EXPIRED")
            .ToListAsync(cancellationToken);
        foreach (var subscription in subscriptions)
            await VerifyPurchaseAsync(userId, security.DecryptToken(subscription.EncryptedPurchaseToken), cancellationToken);
    }

    public async Task<SubscriptionStatus> GetStatusAsync(int userId, string platform, CancellationToken cancellationToken)
    {
        var policy = await settings.GetAsync(cancellationToken);
        await RefreshAsync(userId, policy, cancellationToken);
        return await StatusAsync(userId, platform, policy, cancellationToken);
    }

    private async Task<(AccountSubscription? Subscription, AccountTrial? Trial)> AccessAsync(int userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var subscription = (await db.AccountSubscriptions.AsNoTracking()
            .Where(x => x.UserId == userId && x.PeriodEndUtc > now)
            .OrderByDescending(x => x.PeriodEndUtc).ToListAsync(cancellationToken))
            .FirstOrDefault(x => SubscriptionPolicy.IsActive(x, now));
        var trial = await db.AccountTrials.AsNoTracking().SingleOrDefaultAsync(
            x => x.UserId == userId && x.EndsAtUtc > now && x.DeletedAtUtc == null, cancellationToken);
        return (subscription, trial);
    }

    private static string PeriodKey(int userId, SubscriptionPolicy policy, AccountSubscription? subscription, AccountTrial? trial)
    {
        if (subscription is not null)
            return "google:" + (subscription.LatestOrderId ?? subscription.TokenHash + ":" + subscription.PeriodEndUtc.Ticks);
        if (policy.ComplimentaryUntil(userId) is not null)
            return "complimentary:" + userId + ":" + policy.DayKey(DateTime.UtcNow)[..7];
        return trial is not null ? "trial:" + trial.IdentityHash : "";
    }

    private async Task<SubscriptionStatus> StatusAsync(
        int userId, string platform, SubscriptionPolicy policy, CancellationToken cancellationToken)
    {
        var (subscription, trial) = await AccessAsync(userId, cancellationToken);
        var complimentaryEnd = policy.ComplimentaryUntil(userId);
        var hasAccess = subscription is not null || trial is not null || complimentaryEnd is not null;
        var period = PeriodKey(userId, policy, subscription, trial);
        var day = policy.DayKey(DateTime.UtcNow);
        var leaseCutoff = DateTime.UtcNow.AddSeconds(-policy.Number("Scans.ReservationSeconds", 60, 600));
        var requests = db.AiScanRequests.AsNoTracking().Where(x => x.UserId == userId);
        var counted = requests.Where(x => x.Status == "Succeeded" ||
            x.Status == "Reserved" && x.ReservedAtUtc > leaseCutoff);
        var monthlyLimit = subscription is not null || complimentaryEnd is not null || trial is null
            ? policy.Number("Scans.Monthly", 1) : policy.Number("Trial.Scans", 0);
        var dailyLimit = policy.Number("Scans.Daily", 1);
        var monthlyUsed = await counted.CountAsync(x => x.PeriodKey == period, cancellationToken);
        var dailyUsed = await counted.CountAsync(x => x.DayKey == day, cancellationToken);
        var attemptsDaily = await requests.CountAsync(x => x.DayKey == day, cancellationToken);
        var attemptsMonthly = await requests.CountAsync(x => x.PeriodKey == period, cancellationToken);
        var attemptsAvailable = attemptsDaily < policy.Number("Scans.AttemptsDaily", 1) &&
            attemptsMonthly < policy.Number("Scans.AttemptsMonthly", 1);
        var userEmail = await db.Users.Where(x => x.Id == userId).Select(x => x.Email).SingleAsync(cancellationToken);
        var identity = security.TrialIdentity(userEmail);
        var priorTrial = await db.AccountTrials.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityHash == identity, cancellationToken);
        var eligible = priorTrial is null || priorTrial.RetainUntilUtc <= DateTime.UtcNow;
        var platformEnabled = platform == "Account"
            ? new[] { "Android", "Web", "Windows", "iOS" }.Any(policy.CanUsePremium)
            : policy.CanUsePremium(platform);
        var purchases = platform == "Android" && policy.Enabled("Android.PurchasesEnabled") && google.IsConfigured;
        var canScan = platformEnabled && hasAccess && monthlyUsed < monthlyLimit && dailyUsed < dailyLimit && attemptsAvailable;
        var message = !platformEnabled ? "Subscriptions are unavailable on this platform." :
            !hasAccess ? "A subscription or an eligible trial is required for AI scanning." :
            !attemptsAvailable ? "The scan retry allowance has been reached. Try again after the allowance resets." :
            monthlyUsed >= monthlyLimit ? "Your scan allowance has been used for this period." :
            dailyUsed >= dailyLimit ? "Your daily scan allowance has been used. It resets at midnight UK time." :
            "AI scanning is available.";
        return new(platform, platformEnabled, purchases, subscription is not null || complimentaryEnd is not null, trial is not null && subscription is null && complimentaryEnd is null,
            platformEnabled && !hasAccess && eligible, canScan,
            complimentaryEnd == DateTime.MaxValue && subscription is null ? null : subscription?.PeriodEndUtc ?? complimentaryEnd ?? trial?.EndsAtUtc,
            monthlyLimit, Math.Max(0, monthlyLimit - monthlyUsed), dailyLimit, Math.Max(0, dailyLimit - dailyUsed),
            purchases ? policy.Text("Google.ProductId") : null, purchases ? policy.Text("Google.BasePlanId") : null,
            purchases ? security.AccountId(userId) : null, message,
            complimentaryEnd is not null && subscription is null, policy.Number("Scans.Monthly", 1));
    }

    public async Task StartTrialAsync(int userId, CancellationToken cancellationToken)
    {
        var policy = await settings.GetAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SubscriptionAccountLock.AcquireAsync(db, userId, cancellationToken);
        var (subscription, activeTrial) = await AccessAsync(userId, cancellationToken);
        if (subscription is not null || activeTrial is not null || policy.ComplimentaryUntil(userId) is not null)
            throw new InvalidPurchaseException("Your account already has access.");
        var email = await db.Users.Where(x => x.Id == userId && x.IsActive).Select(x => x.Email).SingleAsync(cancellationToken);
        var hash = security.TrialIdentity(email);
        var trial = await db.AccountTrials.SingleOrDefaultAsync(x => x.IdentityHash == hash, cancellationToken);
        if (trial is not null && (trial.RetainUntilUtc == null || trial.RetainUntilUtc > DateTime.UtcNow))
            throw new InvalidPurchaseException("This account has already used its trial.");
        if (trial is not null) db.AccountTrials.Remove(trial);
        await db.SaveChangesAsync(cancellationToken);
        db.AccountTrials.Add(new AccountTrial
        {
            IdentityHash = hash, UserId = userId, StartedAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddHours(policy.Number("Trial.Hours", 1, 168))
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ScanReservation> ReserveAsync(
        int userId, MeterScanRequest request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey == Guid.Empty) throw new InvalidPurchaseException("A unique scan request identifier is required.");
        var policy = await settings.GetAsync(cancellationToken);
        await RefreshAsync(userId, policy, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(request.Image));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SubscriptionAccountLock.AcquireAsync(db, userId, cancellationToken);
        var cutoff = DateTime.UtcNow.AddSeconds(-policy.Number("Scans.ReservationSeconds", 60, 600));
        await db.AiScanRequests.Where(x => x.UserId == userId && x.Status == "Reserved" && x.ReservedAtUtc <= cutoff)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.Status, "Released").SetProperty(s => s.CompletedAtUtc, DateTime.UtcNow), cancellationToken);
        var existing = await db.AiScanRequests.AsNoTracking().SingleOrDefaultAsync(
            x => x.UserId == userId && x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.ImageHash != hash) throw new InvalidPurchaseException("This scan identifier was already used for another photo.");
            if (existing.Status == "Reserved") throw new ScanBusyException();
            await transaction.CommitAsync(cancellationToken);
            return new(null, new(existing.FuelType, existing.ReadingValue));
        }
        // Access always requires an account entitlement, independently of any client platform flag.
        var status = await StatusAsync(userId, "Account", policy, cancellationToken);
        if (!status.CanScan) throw new ScanAccessException(status.Message);
        var (subscription, trial) = await AccessAsync(userId, cancellationToken);
        var scan = new AiScanRequest
        {
            UserId = userId, IdempotencyKey = request.IdempotencyKey, ImageHash = hash,
            PeriodKey = PeriodKey(userId, policy, subscription, trial), DayKey = policy.DayKey(DateTime.UtcNow),
            ReservedAtUtc = DateTime.UtcNow
        };
        db.AiScanRequests.Add(scan);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(scan.Id, null);
    }

    public async Task CompleteAsync(int userId, long requestId, MeterAnalysisResult? result, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SubscriptionAccountLock.AcquireAsync(db, userId, cancellationToken);
        var scan = await db.AiScanRequests.SingleOrDefaultAsync(x => x.Id == requestId && x.UserId == userId, cancellationToken);
        if (scan is null) return; // Account deletion can remove an in-flight request.
        if (scan.Status != "Reserved") throw new ScanBusyException();
        scan.Status = result?.Reading is { FuelType: "Electricity" or "Gas", ReadingValue: not null } ? "Succeeded" : "Released";
        scan.CompletedAtUtc = DateTime.UtcNow;
        scan.FuelType = result?.Reading.FuelType;
        scan.ReadingValue = result?.Reading.ReadingValue;
        scan.ModelName = result?.Model;
        scan.InputTokens = result?.InputTokens ?? 0;
        scan.OutputTokens = result?.OutputTokens ?? 0;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

public sealed class ScanAccessException(string message) : Exception(message);
public sealed class ScanBusyException : Exception;
