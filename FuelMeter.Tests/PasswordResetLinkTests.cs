using FuelMeter.Core.Services;
using Xunit;

namespace FuelMeter.Tests;

public class PasswordResetLinkTests
{
    private static readonly string Token = new('A', 43);
    private static readonly string Link = "https://" + PasswordResetLinks.Host + "/reset-password?token=" + Token;

    [Theory]
    [InlineData(22)]
    [InlineData(43)]
    public void CurrentAndEarlierResetTokensOpenOnlyTheResetRoute(int length)
    {
        var token = new string('B', length);
        Assert.True(PasswordResetLinks.TryGetRoute(new Uri("https://" + PasswordResetLinks.Host + "/reset-password?token=" + token + "&source=email"), out var route));
        Assert.Equal("/reset-password?token=" + token, route);
    }

    [Theory]
    [InlineData("http://fuelmeter.shrijiitservices.com/reset-password?token=")]
    [InlineData("https://fuelmeter.shrijiitservices.com.evil.example/reset-password?token=")]
    [InlineData("https://fuelmeter.shrijiitservices.com:8443/reset-password?token=")]
    [InlineData("https://someone@fuelmeter.shrijiitservices.com/reset-password?token=")]
    [InlineData("https://fuelmeter.shrijiitservices.com/dashboard?token=")]
    public void ForeignOriginsAndOtherRoutesCannotOpenTheApp(string prefix) =>
        Assert.False(PasswordResetLinks.TryGetRoute(new Uri(prefix + Token), out _));

    [Theory]
    [InlineData("&token=another-token")]
    [InlineData("#fragment")]
    [InlineData("%3Bpackage%3Devil")]
    public void AmbiguousOrMalformedTokensAreRejected(string suffix) =>
        Assert.False(PasswordResetLinks.TryGetRoute(new Uri(Link + suffix), out _));

    [Fact]
    public void AColdStartLinkWaitsForTheRouterAndIsConsumedOnce()
    {
        var links = new PasswordResetLinks();
        Assert.True(links.Receive(new Uri(Link)));
        Assert.Equal("/reset-password?token=" + Token, links.TakePendingRoute());
        Assert.Null(links.TakePendingRoute());
    }

    [Fact]
    public void AWarmLinkNotifiesTheRouterWithoutKeepingAnOldLink()
    {
        var links = new PasswordResetLinks();
        string? route = null;
        links.LinkReceived += () => route = links.TakePendingRoute();
        Assert.True(links.Receive(new Uri(Link)));
        Assert.Equal("/reset-password?token=" + Token, route);
        Assert.Null(links.TakePendingRoute());
        Assert.False(links.Receive(new Uri("https://evil.example/reset-password?token=" + Token)));
    }

    [Fact]
    public void BrowserButtonTargetsFuelMeterWithTheSameHttpsFallback()
    {
        var intent = PasswordResetLinks.AndroidIntent(Token);
        Assert.NotNull(intent);
        Assert.StartsWith("intent://" + PasswordResetLinks.Host + "/reset-password?token=" + Token, intent);
        Assert.Contains("package=com.fuelmeter.app;", intent);
        Assert.Contains("S.browser_fallback_url=" + Uri.EscapeDataString(Link), intent);
        Assert.Null(PasswordResetLinks.AndroidIntent("bad#Intent;package=evil"));
    }
}
