namespace FuelMeter.Core.Services;

/// <summary>Holds a validated reset link until the Blazor router is ready.</summary>
public sealed class PasswordResetLinks
{
    public const string Host = "fuelmeter.shrijiitservices.com";
    public const string ResetPath = "/reset-password";
    private readonly object _gate = new();
    private string? _pendingRoute;
    public event Action? LinkReceived;

    public string? PendingRoute
    {
        get { lock (_gate) return _pendingRoute; }
    }

    public static bool TryGetRoute(Uri? uri, out string route)
    {
        route = string.Empty;
        if (uri is null || !uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.IdnHost, Host, StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 ||
            uri.AbsolutePath != ResetPath || uri.OriginalString.Length > 2048)
            return false;

        string? token = null;
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0]) != "token") continue;
            if (token is not null || parts.Length != 2) return false;
            token = Uri.UnescapeDataString(parts[1]);
        }

        // Accept current 32-byte and earlier 16-byte Base64URL tokens without padding.
        if (token is null || token.Length is not (22 or 43) ||
            token.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            return false;
        route = ResetPath + "?token=" + token;
        return true;
    }

    public bool Receive(Uri uri)
    {
        if (!TryGetRoute(uri, out var route)) return false;
        lock (_gate) _pendingRoute = route;
        LinkReceived?.Invoke();
        return true;
    }

    public bool Acknowledge(string? displayedToken)
    {
        lock (_gate)
        {
            if (displayedToken is null || _pendingRoute != ResetPath + "?token=" + displayedToken)
                return false;
            _pendingRoute = null;
            return true;
        }
    }

    public static string? AndroidIntent(string? token)
    {
        if (token is null) return null;
        var webUrl = "https://" + Host + ResetPath + "?token=" + Uri.EscapeDataString(token);
        if (!TryGetRoute(new Uri(webUrl), out var route)) return null;
        return "intent://" + Host + route + "#Intent;scheme=https;package=com.fuelmeter.app;S.browser_fallback_url="
            + Uri.EscapeDataString(webUrl) + ";end";
    }
}
