using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

/// <summary>
/// Schedules or cancels local notifications for meter reading reminders.
/// Implemented per-platform in the MAUI project.
/// </summary>
public interface INotificationScheduler
{
    Task RequestPermissionAsync();
    Task ScheduleElectricityReminderAsync(NotificationSettingsDto dto);
    Task ScheduleGasReminderAsync(NotificationSettingsDto dto);
    Task CancelElectricityReminderAsync();
    Task CancelGasReminderAsync();
}
