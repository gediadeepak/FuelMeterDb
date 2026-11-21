using Bunit;
using FuelMeter.Components.Components;
using FuelMeter.Components.Components.Pages;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FuelMeter.Tests;

public class ResetStartupTests
{
    private static readonly string Token = new('A', 43);
    private static readonly string Route = "/reset-password?token=" + Token;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AColdResetLinkNeverRendersLoginWhileHybridNavigationIsPending(bool remembered)
    {
        var navigation = new DeferredNavigation();
        var links = new PasswordResetLinks();
        using var context = CreateContext(remembered, navigation, links);
        links.Receive(new Uri("https://" + PasswordResetLinks.Host + Route));

        var app = context.Render<Routes>();
        app.WaitForAssertion(() => Assert.NotEmpty(navigation.Requests));
        Assert.Equal(Route, links.PendingRoute);
        Assert.Empty(app.FindComponents<Login>());

        await app.InvokeAsync(navigation.CompleteNavigation);
        app.WaitForAssertion(() => Assert.Equal(Token, app.FindComponent<ResetPassword>().Instance.Token));
        Assert.Null(links.PendingRoute);
        Assert.DoesNotContain(navigation.Requests, uri => uri.EndsWith("/dashboard"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ANativeResetStartPathOpensTheFormWithoutRedirecting(bool remembered)
    {
        var navigation = new DeferredNavigation(Route);
        var links = new PasswordResetLinks();
        using var context = CreateContext(remembered, navigation, links);
        links.Receive(new Uri("https://" + PasswordResetLinks.Host + Route));
        var app = context.Render<Routes>();
        app.WaitForAssertion(() => Assert.Equal(Token, app.FindComponent<ResetPassword>().Instance.Token));
        Assert.Empty(app.FindComponents<Login>());
        Assert.Empty(navigation.Requests);
        Assert.Null(links.PendingRoute);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ALinkInARunningAppOpensResetWithoutLogin(bool remembered)
    {
        var navigation = new DeferredNavigation("/forgot-password");
        var links = new PasswordResetLinks();
        using var context = CreateContext(remembered, navigation, links);
        var app = context.Render<Routes>();
        app.WaitForAssertion(() => Assert.Single(app.FindComponents<ForgotPassword>()));
        await app.InvokeAsync(() => links.Receive(new Uri("https://" + PasswordResetLinks.Host + Route)));
        Assert.Equal(Route, links.PendingRoute);
        Assert.Empty(app.FindComponents<Login>());
        await app.InvokeAsync(navigation.CompleteNavigation);
        app.WaitForAssertion(() => Assert.Equal(Token, app.FindComponent<ResetPassword>().Instance.Token));
        Assert.Null(links.PendingRoute);
        Assert.DoesNotContain(navigation.Requests, uri => uri.EndsWith("/dashboard"));
    }

    [Fact]
    public async Task OpeningTheSameLinkOnItsResetScreenDoesNotLeaveAStuckPendingLink()
    {
        var navigation = new DeferredNavigation(Route);
        var links = new PasswordResetLinks();
        using var context = CreateContext(false, navigation, links);
        var app = context.Render<Routes>();
        app.WaitForAssertion(() => Assert.Single(app.FindComponents<ResetPassword>()));
        await app.InvokeAsync(() => links.Receive(new Uri("https://" + PasswordResetLinks.Host + Route)));
        app.WaitForAssertion(() => Assert.Null(links.PendingRoute));
        await app.InvokeAsync(() => { navigation.NavigateTo("/forgot-password"); navigation.CompleteNavigation(); });
        app.WaitForAssertion(() => Assert.Single(app.FindComponents<ForgotPassword>()));
    }

    [Fact]
    public async Task AnInFlightLoginCannotRedirectAnOpenedResetLinkToDashboard()
    {
        var navigation = new DeferredNavigation("/login");
        var links = new PasswordResetLinks();
        var auth = new ControlledAuth();
        using var context = CreateContext(false, navigation, links, auth);
        var app = context.Render<Routes>();
        app.WaitForAssertion(() => Assert.Single(app.FindComponents<Login>()));
        app.Find("#email").Change("test@example.test");
        app.Find("#password").Change("test-password");
        var login = app.Find("form").SubmitAsync();
        await auth.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await app.InvokeAsync(() => { links.Receive(new Uri("https://" + PasswordResetLinks.Host + Route)); navigation.CompleteNavigation(); });
        app.WaitForAssertion(() => Assert.Single(app.FindComponents<ResetPassword>()));
        auth.Result.SetResult(new AuthResult
        {
            Success = true, Token = "test-session", User = new UserDto { Id = 1 }, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });
        await login.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(app.FindComponents<ResetPassword>());
        Assert.DoesNotContain(navigation.Requests, uri => uri.EndsWith("/dashboard"));
    }

    private static BunitContext CreateContext(bool remembered, DeferredNavigation navigation, PasswordResetLinks links, IAuthService? auth = null)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton<NavigationManager>(navigation);
        context.Services.AddSingleton(links);
        context.Services.AddSingleton<IRememberedSessionStore>(new SessionStore(remembered));
        context.Services.AddSingleton<AuthStateService>();
        context.Services.AddSingleton<IAuthService>(auth ?? new UnusedAuth());
        return context;
    }

    // BlazorWebView changes its URI only after the native-to-JavaScript round trip.
    private sealed class DeferredNavigation : NavigationManager
    {
        public List<string> Requests { get; } = [];
        private readonly Queue<string> _pending = new();
        public DeferredNavigation(string path = "/") => Initialize("http://localhost/", "http://localhost" + path);
        protected override void NavigateToCore(string uri, bool forceLoad) => Queue(uri);
        protected override void NavigateToCore(string uri, NavigationOptions options) => Queue(uri);
        private void Queue(string uri)
        {
            var target = ToAbsoluteUri(uri).AbsoluteUri;
            Requests.Add(target);
            _pending.Enqueue(target);
        }
        public void CompleteNavigation()
        {
            var remaining = 10;
            while (_pending.TryDequeue(out var target))
            {
                if (--remaining == 0) throw new InvalidOperationException("Navigation loop.");
                Uri = target;
                NotifyLocationChanged(false);
            }
        }
    }

    private sealed class SessionStore(bool remembered) : IRememberedSessionStore
    {
        public Task<RememberedSession?> ReadAsync() => Task.FromResult(remembered
            ? new RememberedSession(new UserDto { Id = 1, FullName = "Test account" }, "test-session", DateTimeOffset.UtcNow.AddHours(1)) : null);
        public Task SaveAsync(RememberedSession session) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
    }

    private sealed class UnusedAuth : IAuthService
    {
        public Task<AuthResult> LoginAsync(LoginRequest request) => throw new NotSupportedException();
        public Task<AuthResult> RegisterAsync(RegisterRequest request) => throw new NotSupportedException();
        public Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request) => throw new NotSupportedException();
        public Task<AuthResult> ResetPasswordAsync(string token, string password) => throw new NotSupportedException();
        public Task<AuthResult> DeleteAccountAsync(DeleteAccountRequest request) => throw new NotSupportedException();
    }

    private sealed class ControlledAuth : IAuthService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<AuthResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<AuthResult> LoginAsync(LoginRequest request) { Started.SetResult(); return Result.Task; }
        public Task<AuthResult> RegisterAsync(RegisterRequest request) => throw new NotSupportedException();
        public Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request) => throw new NotSupportedException();
        public Task<AuthResult> ResetPasswordAsync(string token, string password) => throw new NotSupportedException();
        public Task<AuthResult> DeleteAccountAsync(DeleteAccountRequest request) => throw new NotSupportedException();
    }
}
