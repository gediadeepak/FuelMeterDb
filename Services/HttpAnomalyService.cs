using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

public class HttpAnomalyService(IApiClientService api) : IAnomalyService
{
    public async Task<AnomalyResultDto> DetectAnomaliesAsync(
        int userId,
        string? fuelType = null,
        int daysBack = 60,
        decimal thresholdPercent = 50m)
    {
        var query = $"?daysBack={daysBack}&thresholdPercent={thresholdPercent}";
        if (!string.IsNullOrEmpty(fuelType))
            query += $"&fuelType={Uri.EscapeDataString(fuelType)}";

        try
        {
            return await api.GetAsync<AnomalyResultDto>($"api/anomalies{query}")
                   ?? new AnomalyResultDto();
        }
        catch (HttpRequestException)
        {
            return new AnomalyResultDto();
        }
    }
}
