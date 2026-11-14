using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Firebase.Messaging;
using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FuelMeter;

[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public sealed class BudgetFirebaseMessagingService : FirebaseMessagingService
{
    private const string ChannelId = "fuelmeter_budgets";

    public override async void OnRegistered(string installationId)
    {
        var settings = IPlatformApplication.Current?.Services.GetService<IBudgetPushSettings>();
        if (settings is AndroidBudgetPushSettings androidSettings)
            await androidSettings.OnRegisteredAsync(installationId);
    }

    public override async void OnMessageReceived(RemoteMessage message)
    {
        if (!message.Data.TryGetValue("type", out var type) || type != "budget" ||
            !message.Data.TryGetValue("userId", out var owner) || !int.TryParse(owner, out var userId) ||
            !message.Data.TryGetValue("budgetId", out var budget) || !int.TryParse(budget, out var budgetId) ||
            !AndroidBudgetPushSettings.IsEnabledFor(userId))
        {
            return;
        }

        var services = IPlatformApplication.Current?.Services;
        var auth = services?.GetService<AuthStateService>();
        var currentUserId = auth?.CurrentUser?.Id;
        if (currentUserId is null && services?.GetService<IRememberedSessionStore>() is { } sessions)
        {
            try
            {
                var savedSession = await sessions.ReadAsync();
                if (savedSession is not null && savedSession.ExpiresAt > DateTimeOffset.UtcNow)
                    currentUserId = savedSession.User.Id;
            }
            catch (Exception)
            {
                return;
            }
        }

        if ((auth?.CurrentUser?.Id ?? currentUserId) != userId)
            return;

        var manager = NotificationManagerCompat.From(this);
        if (manager is null || !manager.AreNotificationsEnabled())
            return;

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            using var channel = new NotificationChannel(ChannelId, "FuelMeter budget alerts", NotificationImportance.Default);
            manager.CreateNotificationChannel(channel);
        }

        var openApp = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(MainActivity)),
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        using var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentTitle("FuelMeter budget alert");
        builder.SetContentText("Your estimated energy costs have crossed a budget limit. Open FuelMeter to review.");
        builder.SetContentIntent(openApp);
        builder.SetAutoCancel(true);

        manager.Notify(ChannelId, budgetId, builder.Build());
    }
}
