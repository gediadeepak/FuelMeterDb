using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Web.Services;

/// <summary>
/// No-op scheduler for the web host — push alarms are not available in a browser context.
/// </summary>
public class NullNotificationScheduler : INotificationScheduler
{
    public Task RequestPermissionAsync()              => Task.CompletedTask;
    public Task ScheduleElectricityReminderAsync(NotificationSettingsDto dto) => Task.CompletedTask;
    public Task ScheduleGasReminderAsync(NotificationSettingsDto dto)         => Task.CompletedTask;
    public Task CancelElectricityReminderAsync()      => Task.CompletedTask;
    public Task CancelGasReminderAsync()              => Task.CompletedTask;
}
