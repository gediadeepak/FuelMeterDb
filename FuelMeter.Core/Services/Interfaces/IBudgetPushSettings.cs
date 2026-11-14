namespace FuelMeter.Core.Services.Interfaces;

public interface IBudgetPushSettings
{
    bool IsEnabled { get; }
    Task<bool> SetEnabledAsync(bool enabled);
}
