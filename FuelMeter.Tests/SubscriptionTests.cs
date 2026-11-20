using System.Security.Cryptography;
using FuelMeter.Api.Services;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Data;
using FuelMeter.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FuelMeter.Tests;

// These tests use an isolated LocalDB database and exercise the actual SQL transaction locks.
public sealed class SubscriptionTests : IAsyncLifetime
{
    private readonly string _database = "FuelMeter_SubscriptionTests_" + Guid.NewGuid().ToString("N");
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["SubscriptionSecurity:IdentityKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        ["SubscriptionSecurity:PurchaseTokenKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    }).Build();

    private FuelMeterDbContext Database() => new(new DbContextOptionsBuilder<FuelMeterDbContext>()
        .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={_database};Integrated Security=true;TrustServerCertificate=true",
            sql => sql.EnableRetryOnFailure())
        .Options);

    private SubscriptionService Service(FuelMeterDbContext db) => new(db, new SubscriptionSettings(db),
        new SubscriptionSecurity(_configuration), new GooglePlaySubscriptions(new HttpClient(), _configuration));

    public async Task InitializeAsync()
    {
        await using var db = Database();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Assert.StartsWith("FuelMeter_SubscriptionTests_", _database);
        await using var db = Database();
        await db.Database.EnsureDeletedAsync();
    }

    private async Task<int> UserAsync(bool complimentary = false)
    {
        await using var db = Database();
        var user = new User
        {
            FullName = "Subscription Test", Email = Guid.NewGuid().ToString("N") + "@example.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("test-password")
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        if (complimentary)
        {
            db.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = "Subscription.ComplimentaryUser." + user.Id,
                Value = DateTime.UtcNow.AddYears(1).ToString("O"), UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        return user.Id;
    }

    private static MeterScanRequest Request() => new()
    {
        IdempotencyKey = Guid.NewGuid(), Image = [0xff, 0xd8, 0xff, 1]
    };

    [Fact]
    public async Task ConcurrentScansCannotExceedTheDailyAllowance()
    {
        var user = await UserAsync(complimentary: true);
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ =>
        {
            await using var db = Database();
            try { await Service(db).ReserveAsync(user, Request(), default); return true; }
            catch (ScanAccessException) { return false; }
        }));
        Assert.Equal(5, results.Count(x => x));
        await using var check = Database();
        Assert.Equal(5, await check.AiScanRequests.CountAsync(x => x.UserId == user));
    }

    [Fact]
    public async Task ACompletedRetryReturnsTheSameReadingAndConsumesOneCredit()
    {
        var user = await UserAsync(complimentary: true);
        var request = Request();
        await using var db = Database();
        var service = Service(db);
        var reserved = await service.ReserveAsync(user, request, default);
        await service.CompleteAsync(user, reserved.RequestId!.Value, new(new("Electricity", 123.45m), "test", 120, 30), default);
        var replay = await service.ReserveAsync(user, request, default);
        Assert.Null(replay.RequestId);
        Assert.Equal(123.45m, replay.CachedResult?.ReadingValue);
        Assert.Equal(1, await db.AiScanRequests.CountAsync(x => x.UserId == user));
        var status = await service.GetStatusAsync(user, "Web", default);
        Assert.Equal(29, status.MonthlyRemaining);
        Assert.Equal(4, status.DailyRemaining);
        await Assert.ThrowsAsync<InvalidPurchaseException>(() => service.ReserveAsync(user,
            new() { IdempotencyKey = request.IdempotencyKey, Image = [9, 8, 7] }, default));
    }

    [Fact]
    public async Task FailedScansReleaseCreditsButHaveABoundedRetryAllowance()
    {
        var user = await UserAsync(complimentary: true);
        await using var db = Database();
        var service = Service(db);
        for (var i = 0; i < 10; i++)
        {
            var scan = await service.ReserveAsync(user, Request(), default);
            await service.CompleteAsync(user, scan.RequestId!.Value, null, default);
        }
        var status = await service.GetStatusAsync(user, "Windows", default);
        Assert.Equal(30, status.MonthlyRemaining);
        Assert.Equal(5, status.DailyRemaining);
        Assert.False(status.CanScan);
        await Assert.ThrowsAsync<ScanAccessException>(() => service.ReserveAsync(user, Request(), default));
    }

    [Fact]
    public async Task ExpiredReservationsCannotConsumeFreedCreditsLater()
    {
        var user = await UserAsync(complimentary: true);
        await using var first = Database();
        var service = Service(first);
        var reservation = await service.ReserveAsync(user, Request(), default);
        await using (var second = Database())
        {
            await second.AiScanRequests.Where(x => x.Id == reservation.RequestId)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.ReservedAtUtc, DateTime.UtcNow.AddMinutes(-5)));
            await Service(second).ReserveAsync(user, Request(), default);
        }
        await Assert.ThrowsAsync<ScanBusyException>(() => service.CompleteAsync(user, reservation.RequestId!.Value,
            new(new("Gas", 88m), "test", 10, 10), default));
    }

    [Fact]
    public async Task TrialCannotBeStartedTwiceAndIsRetainedForOneYearAfterDeletion()
    {
        var user = await UserAsync();
        await using var db = Database();
        var service = Service(db);
        await service.StartTrialAsync(user, default);
        await Assert.ThrowsAsync<InvalidPurchaseException>(() => service.StartTrialAsync(user, default));
        var trial = await db.AccountTrials.SingleAsync(x => x.UserId == user);
        Assert.InRange((trial.EndsAtUtc - trial.StartedAtUtc).TotalHours, 23.999, 24.001);
        var email = await db.Users.Where(x => x.Id == user).Select(x => x.Email).SingleAsync();
        var auth = new AuthService(db, new UnusedEmail(), _configuration, NullLogger<AuthService>.Instance);
        var deleted = await auth.DeleteAccountAsync(user, new() { Password = "test-password" }, default);
        Assert.True(deleted.Success);
        db.ChangeTracker.Clear();
        trial = await db.AccountTrials.SingleAsync();
        Assert.Null(trial.UserId);
        Assert.NotNull(trial.DeletedAtUtc);
        Assert.Equal(trial.DeletedAtUtc!.Value.AddYears(1), trial.RetainUntilUtc);
        Assert.DoesNotContain(email, trial.IdentityHash, StringComparison.OrdinalIgnoreCase);
        var recreated = new User { FullName = "Returned", Email = email, PasswordHash = "unused" };
        db.Users.Add(recreated);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidPurchaseException>(() => service.StartTrialAsync(recreated.Id, default));
    }

    [Fact]
    public async Task OneGoogleEntitlementWorksAcrossPlatformsWithIosDisabled()
    {
        var user = await UserAsync();
        await using var db = Database();
        db.AccountSubscriptions.Add(new AccountSubscription
        {
            UserId = user, TokenHash = new string('a', 64), EncryptedPurchaseToken = "unused",
            ProductId = "fuelmeter_premium_monthly", State = "SUBSCRIPTION_STATE_ACTIVE",
            PeriodStartUtc = DateTime.UtcNow.AddDays(-1), PeriodEndUtc = DateTime.UtcNow.AddDays(29),
            LastVerifiedAtUtc = DateTime.UtcNow, LatestOrderId = "test-period-1"
        });
        await db.SaveChangesAsync();
        foreach (var platform in new[] { "Android", "Windows", "Web" })
            Assert.True((await Service(db).GetStatusAsync(user, platform, default)).CanScan);
        var ios = await Service(db).GetStatusAsync(user, "iOS", default);
        Assert.True(ios.HasSubscription);
        Assert.False(ios.PremiumAccessEnabled);
        Assert.False(ios.CanScan);
        await Service(db).ReserveAsync(user, Request(), default);
        Assert.Equal(29, (await Service(db).GetStatusAsync(user, "Web", default)).MonthlyRemaining);
    }

    [Fact]
    public void PurchaseReferencesAreEncryptedAndTrialIdentifiersAreStable()
    {
        var security = new SubscriptionSecurity(_configuration);
        const string purchase = "secret-purchase-reference";
        var encrypted = security.EncryptToken(purchase);
        Assert.NotEqual(purchase, encrypted);
        Assert.Equal(purchase, security.DecryptToken(encrypted));
        Assert.Equal(security.TrialIdentity("Test@Example.com"), security.TrialIdentity(" test@example.com "));
        Assert.NotEqual(security.AccountId(1), security.AccountId(2));
    }

    [Fact]
    public async Task MonthlyAllowanceIsEnforcedIndependentlyOfTheDailyLimit()
    {
        var user = await UserAsync(complimentary: true);
        await using var db = Database();
        await db.ApplicationSettings.Where(x => x.Key == "Subscription.Scans.Monthly")
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.Value, "2"));
        var service = Service(db);
        await service.ReserveAsync(user, Request(), default);
        await service.ReserveAsync(user, Request(), default);
        await Assert.ThrowsAsync<ScanAccessException>(() => service.ReserveAsync(user, Request(), default));
        var status = await service.GetStatusAsync(user, "Web", default);
        Assert.Equal(0, status.MonthlyRemaining);
        Assert.Equal(3, status.DailyRemaining);
    }

    [Fact]
    public async Task ConcurrentTrialStartsOnlyActivateOnce()
    {
        var user = await UserAsync();
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            await using var db = Database();
            try { await Service(db).StartTrialAsync(user, default); return true; }
            catch (InvalidPurchaseException) { return false; }
        }));
        Assert.Single(results, x => x);
    }

    [Fact]
    public async Task AFreeAccountCannotUseAiEvenWhenThePlatformIsEnabled()
    {
        var user = await UserAsync();
        await using var db = Database();
        var status = await Service(db).GetStatusAsync(user, "Android", default);
        Assert.True(status.PremiumAccessEnabled);
        Assert.False(status.CanScan);
        await Assert.ThrowsAsync<ScanAccessException>(() => Service(db).ReserveAsync(user, Request(), default));
        Assert.Empty(await db.AiScanRequests.Where(x => x.UserId == user).ToListAsync());
    }

    private sealed class UnusedEmail : IEmailService
    {
        public Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink) =>
            throw new NotSupportedException();
    }
}
