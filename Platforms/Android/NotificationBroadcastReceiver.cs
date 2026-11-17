using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace FuelMeter.Services;

[BroadcastReceiver(Name = "com.companyname.fuelmeter.NotificationBroadcastReceiver", Enabled = true, Exported = false)]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced }, Priority = (int)IntentFilterPriority.LowPriority)]
public class NotificationBroadcastReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;

        var action = intent.Action;

        if (action is Intent.ActionBootCompleted or Intent.ActionMyPackageReplaced)
        {
            RescheduleSavedReminders(context);
            return;
        }

        int    id        = intent.GetIntExtra("notif_id",        0);
        var prefs = context.GetSharedPreferences("fuelmeter_notif", FileCreationMode.Private);
        var isLegacyAlarm = !intent.GetBooleanExtra("notif_managed", false);
        if (prefs?.Contains($"notif_{id}") != true && !isLegacyAlarm) return;
        string title     = intent.GetStringExtra("notif_title")     ?? "FuelMeter Reminder";
        string body      = intent.GetStringExtra("notif_body")      ?? "Time to log your meter reading.";
        string frequency = intent.GetStringExtra("notif_frequency") ?? "Daily";
        int    hour      = intent.GetIntExtra("notif_hour",   8);
        int    minute    = intent.GetIntExtra("notif_minute", 0);
        int    dow       = intent.GetIntExtra("notif_dow",    0);
        int    dom       = intent.GetIntExtra("notif_dom",    1);

        var notifications = NotificationManagerCompat.From(context);
        if (notifications?.AreNotificationsEnabled() == true)
        {
            EnsureChannel(context);

            using var builder = new NotificationCompat.Builder(context, "fuelmeter_reminders");
            builder.SetSmallIcon(Resource.Mipmap.appicon);
            builder.SetContentTitle(title);
            builder.SetContentText(body);
            builder.SetPriority(NotificationCompat.PriorityHigh);
            builder.SetAutoCancel(true);
            using var notification = builder.Build();
            if (notification is not null) notifications.Notify(id, notification);
        }

        if (isLegacyAlarm) SaveExtras(context, id, title, body, frequency, hour, minute, dow, dom);
        ScheduleNext(context, id, title, body, frequency, hour, minute, dow, dom);
    }

    internal static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
        var nm = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (nm?.GetNotificationChannel("fuelmeter_reminders") is not null) return;
        var ch = new NotificationChannel("fuelmeter_reminders", "FuelMeter Reminders",
                                          NotificationImportance.Default);
        nm?.CreateNotificationChannel(ch);
    }

    internal static void ScheduleNext(Context context, int id, string title, string body,
        string frequency, int hour, int minute, int dow, int dom)
    {
        var fireAt    = NotificationSchedulerService.ComputeNextFire(frequency, hour, minute, dow, dom);
        long triggerMs = new DateTimeOffset(fireAt).ToUnixTimeMilliseconds();

        var nextIntent = new Intent(context, typeof(NotificationBroadcastReceiver));
        nextIntent.PutExtra("notif_id",        id);
        nextIntent.PutExtra("notif_title",     title);
        nextIntent.PutExtra("notif_body",      body);
        nextIntent.PutExtra("notif_frequency", frequency);
        nextIntent.PutExtra("notif_hour",      hour);
        nextIntent.PutExtra("notif_minute",    minute);
        nextIntent.PutExtra("notif_dow",       dow);
        nextIntent.PutExtra("notif_dom",       dom);
        nextIntent.PutExtra("notif_managed",   true);

        var pending = PendingIntent.GetBroadcast(context, id, nextIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;

        var am = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (am is null) return;

        am.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMs, pending);
    }

    internal static void SaveExtras(Context context, int id, string title, string body,
        string frequency, int hour, int minute, int dow, int dom)
    {
        var prefs = context.GetSharedPreferences("fuelmeter_notif", FileCreationMode.Private);
        var ed    = prefs!.Edit()!;
        string key = $"notif_{id}";
        ed.PutString(key, $"{title}|{body}|{frequency}|{hour}|{minute}|{dow}|{dom}");
        ed.Apply();
    }

    private static void RescheduleSavedReminders(Context context)
    {
        var prefs = context.GetSharedPreferences("fuelmeter_notif", FileCreationMode.Private);
        if (prefs is null) return;
        var all = prefs.All;
        if (all is null) return;
        foreach (var entry in all)
        {
            if (!entry.Key.StartsWith("notif_")) continue;
            if (entry.Value?.ToString() is not { } val) continue;
            var parts = val.Split('|');
            if (parts.Length < 7) continue;
            if (!int.TryParse(entry.Key.Replace("notif_", ""), out int id)) continue;
            ScheduleNext(context, id, parts[0], parts[1], parts[2],
                int.Parse(parts[3]), int.Parse(parts[4]),
                int.Parse(parts[5]), int.Parse(parts[6]));
        }
    }
}
