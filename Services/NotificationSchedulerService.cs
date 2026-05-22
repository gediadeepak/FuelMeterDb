using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
#endif

#if WINDOWS
using FuelMeter.Platforms.Windows;
#endif

namespace FuelMeter.Services;

public class NotificationSchedulerService : INotificationScheduler
{
    private const int ElecId = 1001;
    private const int GasId  = 1002;

#if WINDOWS
    private bool _registered;
#endif

    // ── Permission ───────────────────────────────────────────────
    public async Task RequestPermissionAsync()
    {
#if ANDROID
        // Android 13+ (API 33) requires POST_NOTIFICATIONS runtime permission
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            _ = status;
        }

        // Android 12+ (API 31) requires the user to allow exact alarms in system settings
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            var ctx = Android.App.Application.Context;
            var am  = (AlarmManager?)ctx.GetSystemService(Context.AlarmService);
            if (am is not null && !am.CanScheduleExactAlarms())
            {
                var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                intent.SetFlags(ActivityFlags.NewTask);
                ctx.StartActivity(intent);
            }
        }
#endif
#if WINDOWS
        EnsureWindowsRegistered();
#endif
        await Task.CompletedTask;
    }

    // ── Schedule ─────────────────────────────────────────────────
    public Task ScheduleElectricityReminderAsync(NotificationSettingsDto dto)
    {
        if (dto.ElectricityEnabled)
            Schedule(ElecId, "⚡ Electricity Meter Reminder",
                "Time to check and log your electricity meter reading.",
                dto.ElectricityFrequency, dto.ElectricityHour, dto.ElectricityMinute,
                dto.ElectricityDayOfWeek, dto.ElectricityDayOfMonth);
        else
            Cancel(ElecId);
        return Task.CompletedTask;
    }

    public Task ScheduleGasReminderAsync(NotificationSettingsDto dto)
    {
        if (dto.GasEnabled)
            Schedule(GasId, "🔥 Gas Meter Reminder",
                "Time to check and log your gas meter reading.",
                dto.GasFrequency, dto.GasHour, dto.GasMinute,
                dto.GasDayOfWeek, dto.GasDayOfMonth);
        else
            Cancel(GasId);
        return Task.CompletedTask;
    }

    public Task CancelElectricityReminderAsync() { Cancel(ElecId); return Task.CompletedTask; }
    public Task CancelGasReminderAsync()          { Cancel(GasId);  return Task.CompletedTask; }

    // ── Core scheduling ──────────────────────────────────────────
    private void Schedule(int id, string title, string body,
        string frequency, int hour, int minute, int dayOfWeek, int dayOfMonth)
    {
#if ANDROID
        Cancel(id);

        var ctx = Android.App.Application.Context;

        // Create notification channel (Android 8+)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var nm = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
            if (nm?.GetNotificationChannel("fuelmeter_reminders") is null)
            {
                var ch = new Android.App.NotificationChannel(
                    "fuelmeter_reminders", "FuelMeter Reminders",
                    NotificationImportance.Default);
                nm?.CreateNotificationChannel(ch);
            }
        }

        var fireAt    = ComputeNextFire(frequency, hour, minute, dayOfWeek, dayOfMonth);
        long triggerMs = new DateTimeOffset(fireAt).ToUnixTimeMilliseconds();

        var intent = new Intent(ctx, typeof(NotificationBroadcastReceiver));
        intent.PutExtra("notif_id",        id);
        intent.PutExtra("notif_title",     title);
        intent.PutExtra("notif_body",      body);
        intent.PutExtra("notif_frequency", frequency);
        intent.PutExtra("notif_hour",      hour);
        intent.PutExtra("notif_minute",    minute);
        intent.PutExtra("notif_dow",       dayOfWeek);
        intent.PutExtra("notif_dom",       dayOfMonth);

        var pending = PendingIntent.GetBroadcast(
            ctx, id, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;

        var am = (AlarmManager?)ctx.GetSystemService(Context.AlarmService);
        if (am is null) return;

        // SetExactAndAllowWhileIdle fires reliably in Doze mode (Android 6+).
        // The receiver will reschedule the next occurrence so repetition works.
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            am.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMs, pending);
        else
            am.SetExact(AlarmType.RtcWakeup, triggerMs, pending);

#elif WINDOWS
        EnsureWindowsRegistered();
        var fireAt = ComputeNextFire(frequency, hour, minute, dayOfWeek, dayOfMonth);
        WindowsNotificationHelper.Schedule(id, title, body, fireAt);
#endif
    }

    private void Cancel(int id)
    {
#if ANDROID
        var ctx     = Android.App.Application.Context;
        var intent  = new Intent(ctx, typeof(NotificationBroadcastReceiver));
        var pending = PendingIntent.GetBroadcast(
            ctx, id, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        var am = (AlarmManager?)ctx.GetSystemService(Context.AlarmService);
        if (pending is not null) am?.Cancel(pending);
#elif WINDOWS
        WindowsNotificationHelper.Cancel(id);
#endif
    }

#if WINDOWS
    private void EnsureWindowsRegistered()
    {
        if (_registered) return;
        WindowsNotificationHelper.RegisterAumid();
        _registered = true;
    }
#endif

    // ── Date helpers ─────────────────────────────────────────────
    private static DateTime ComputeNextFire(
        string frequency, int hour, int minute, int dayOfWeek, int dayOfMonth)
    {
        var now = DateTime.Now;
        return frequency switch
        {
            "Weekly"  => NextWeekly(now, (DayOfWeek)dayOfWeek, hour, minute),
            "Monthly" => NextMonthly(now, dayOfMonth, hour, minute),
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
