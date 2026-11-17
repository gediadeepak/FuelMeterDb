using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
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
            await Permissions.RequestAsync<Permissions.PostNotifications>();
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

        NotificationBroadcastReceiver.EnsureChannel(ctx);
        NotificationBroadcastReceiver.SaveExtras(ctx, id, title, body, frequency, hour, minute, dayOfWeek, dayOfMonth);
        NotificationBroadcastReceiver.ScheduleNext(ctx, id, title, body, frequency, hour, minute, dayOfWeek, dayOfMonth);

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
            PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);
        var am = (AlarmManager?)ctx.GetSystemService(Context.AlarmService);
        ctx.GetSharedPreferences("fuelmeter_notif", FileCreationMode.Private)?.Edit()?.Remove($"notif_{id}")?.Apply();
        if (pending is not null)
        {
            am?.Cancel(pending);
            pending.Cancel();
        }
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
    internal static DateTime ComputeNextFire(
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
