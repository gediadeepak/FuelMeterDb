using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace FuelMeter.Services;

[BroadcastReceiver(Name = "com.companyname.fuelmeter.NotificationBroadcastReceiver", Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted }, Priority = (int)IntentFilterPriority.LowPriority)]
public class NotificationBroadcastReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;

        var action = intent.Action;

        // On device reboot, reschedule all active alarms via a saved prefs snapshot
        if (action == Intent.ActionBootCompleted)
        {
            RescheduleOnBoot(context);
            return;
        }

        int    id        = intent.GetIntExtra("notif_id",        0);
        string title     = intent.GetStringExtra("notif_title")     ?? "FuelMeter Reminder";
        string body      = intent.GetStringExtra("notif_body")      ?? "Time to log your meter reading.";
        string frequency = intent.GetStringExtra("notif_frequency") ?? "Daily";
        int    hour      = intent.GetIntExtra("notif_hour",   8);
        int    minute    = intent.GetIntExtra("notif_minute", 0);
        int    dow       = intent.GetIntExtra("notif_dow",    0);
        int    dom       = intent.GetIntExtra("notif_dom",    1);

        // ── Show the notification ────────────────────────────────
        EnsureChannel(context);

        var notification = new NotificationCompat.Builder(context, "fuelmeter_reminders")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetAutoCancel(true)
            .Build();

        NotificationManagerCompat.From(context).Notify(id, notification);

        // ── Reschedule next occurrence ───────────────────────────
        ScheduleNext(context, id, title, body, frequency, hour, minute, dow, dom);

        // Persist extras so they survive a reboot
        SaveExtras(context, id, title, body, frequency, hour, minute, dow, dom);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var nm = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (nm?.GetNotificationChannel("fuelmeter_reminders") is not null) return;
        var ch = new NotificationChannel("fuelmeter_reminders", "FuelMeter Reminders",
                                          NotificationImportance.High);
        nm?.CreateNotificationChannel(ch);
    }

    private static void ScheduleNext(Context context, int id, string title, string body,
        string frequency, int hour, int minute, int dow, int dom)
    {
        var fireAt    = ComputeNextFire(frequency, hour, minute, dow, dom);
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

        var pending = PendingIntent.GetBroadcast(context, id, nextIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;

        var am = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (am is null) return;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            am.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMs, pending);
        else
            am.SetExact(AlarmType.RtcWakeup, triggerMs, pending);
    }

    private static void SaveExtras(Context context, int id, string title, string body,
        string frequency, int hour, int minute, int dow, int dom)
    {
        var prefs = context.GetSharedPreferences("fuelmeter_notif", FileCreationMode.Private);
        var ed    = prefs!.Edit()!;
        string key = $"notif_{id}";
        ed.PutString(key, $"{title}|{body}|{frequency}|{hour}|{minute}|{dow}|{dom}");
        ed.Apply();
    }

    private static void RescheduleOnBoot(Context context)
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

    private static DateTime ComputeNextFire(string frequency, int hour, int minute, int dow, int dom)
    {
        var now = DateTime.Now;
        return frequency switch
        {
            "Weekly"  => NextWeekly(now, (DayOfWeek)dow, hour, minute),
            "Monthly" => NextMonthly(now, dom, hour, minute),
            _         => NextDaily(now, hour, minute)
        };
    }

    private static DateTime NextDaily(DateTime from, int h, int m)
    {
        var t = from.Date.AddHours(h).AddMinutes(m);
        return t > from ? t : t.AddDays(1);
    }

    private static DateTime NextWeekly(DateTime from, DayOfWeek target, int h, int m)
    {
        var t    = from.Date.AddHours(h).AddMinutes(m);
        int diff = ((int)target - (int)from.DayOfWeek + 7) % 7;
        if (diff == 0 && t <= from) diff = 7;
        return t.AddDays(diff);
    }

    private static DateTime NextMonthly(DateTime from, int day, int h, int m)
    {
        day = Math.Clamp(day, 1, 28);
        var t = new DateTime(from.Year, from.Month, day, h, m, 0);
        return t > from ? t : t.AddMonths(1);
    }
}
