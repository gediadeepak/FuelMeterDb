using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public interface IAnomalyService
{
    /// <summary>
    /// Detect days where daily usage exceeded the user's rolling 30-day average
    /// by more than <paramref name="thresholdPercent"/> (default 50%).
    /// </summary>
    Task<AnomalyResultDto> DetectAnomaliesAsync(
        int userId,
        string? fuelType = null,
        int daysBack = 60,
        decimal thresholdPercent = 50m);
}
