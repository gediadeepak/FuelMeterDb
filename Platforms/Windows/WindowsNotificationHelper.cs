using Microsoft.Win32;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace FuelMeter.Platforms.Windows;

/// <summary>
/// Wraps Windows toast scheduling for an unpackaged MAUI app.
/// Unpackaged apps must register an AUMID in the registry so the OS can
/// deliver toasts. ScheduledToastNotification lets the OS fire the alert
/// at the requested time even if the app is in the background.
/// </summary>
internal static class WindowsNotificationHelper
{
    // Must match ApplicationId in FuelMeter.csproj
    internal const string Aumid = "com.companyname.fuelmeter";

    private static readonly string RegistryKey =
        $@"HKEY_CURRENT_USER\Software\Classes\AppUserModelId\{Aumid}";

    /// <summary>Call once at app launch (OnLaunched in App.xaml.cs).</summary>
    internal static void RegisterAumid()
    {
        Registry.SetValue(RegistryKey, "DisplayName", "FuelMeter");
        Registry.SetValue(RegistryKey, "IconUri",
            Path.Combine(AppContext.BaseDirectory, "AppIcon.ico"));
    }

    /// <summary>Schedule a toast to fire at <paramref name="deliveryTime"/>.</summary>
    internal static void Schedule(int id, string title, string body, DateTime deliveryTime)
    {
        var delivery = new DateTimeOffset(
            deliveryTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(deliveryTime, DateTimeKind.Local)
                : deliveryTime);

        // ScheduledToastNotification requires delivery to be ≥ 5 s in the future
        if (delivery <= DateTimeOffset.Now.AddSeconds(5))
            delivery = DateTimeOffset.Now.AddSeconds(6);

        Cancel(id); // remove any existing toast with same id first

        var xml      = BuildToastXml(title, body);
        var toast    = new ScheduledToastNotification(xml, delivery) { Id = id.ToString() };

        ToastNotificationManager.CreateToastNotifier(Aumid).AddToSchedule(toast);
    }

    /// <summary>Remove all scheduled toasts that match <paramref name="id"/>.</summary>
    internal static void Cancel(int id)
    {
        var idStr   = id.ToString();
        var notifier = ToastNotificationManager.CreateToastNotifier(Aumid);
        foreach (var n in notifier.GetScheduledToastNotifications()
                                  .Where(n => n.Id == idStr)
                                  .ToList())
        {
            notifier.RemoveFromSchedule(n);
        }
    }

    private static XmlDocument BuildToastXml(string title, string body)
    {
        var xml = new XmlDocument();
        xml.LoadXml($"""
            <toast>
              <visual>
                <binding template="ToastGeneric">
                  <text>{Escape(title)}</text>
                  <text>{Escape(body)}</text>
                </binding>
              </visual>
            </toast>
            """);
        return xml;
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
