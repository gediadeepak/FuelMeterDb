using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Web.Services;

public class HttpMeterReadingService(IApiClientService api) : IMeterReadingService
{
    public async Task<List<MeterReadingRowDto>> GetGridAsync(
        int userId, string? fuelType, int? year, int? month)
    {
        var query = BuildQuery(
            ("fuelType", fuelType),
            ("year",     year?.ToString()),
            ("month",    month?.ToString()));

        return await api.GetAsync<List<MeterReadingRowDto>>($"api/meterreadings{query}")
               ?? [];
    }

    public async Task<MeterReadingDto?> GetByIdAsync(int userId, int readingId)
        => await api.GetAsync<MeterReadingDto>($"api/meterreadings/{readingId}");

    public async Task<SaveMeterReadingResult> SaveAsync(int userId, MeterReadingDto dto)
    {
        try
        {
            return await api.PostAsync<MeterReadingDto, SaveMeterReadingResult>("api/meterreadings", dto)
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) { return Fail(ex.Message); }
    }

    public async Task<SaveMeterReadingResult> DeleteAsync(int userId, int readingId)
    {
        try
        {
            return await api.DeleteAsync<SaveMeterReadingResult>($"api/meterreadings/{readingId}")
                   ?? Fail("No response from server.");
        }
        catch (HttpRequestException ex) { return Fail(ex.Message); }
    }

    public async Task<DashboardDto> GetDashboardAsync(int userId, int year, int month)
        => await api.GetAsync<DashboardDto>($"api/meterreadings/dashboard?year={year}&month={month}")
           ?? new DashboardDto();

    public async Task<UsageChartDto> GetUsageChartAsync(int userId, string view, int year, int? month)
    {
        var query = BuildQuery(
            ("view",  view),
            ("year",  year.ToString()),
            ("month", month?.ToString()));

        return await api.GetAsync<UsageChartDto>($"api/meterreadings/usage-chart{query}")
               ?? new UsageChartDto();
    }

    // ── helpers ─────────────────────────────────────────────────
    private static SaveMeterReadingResult Fail(string msg) => new() { Success = false, Message = msg };

    private static string BuildQuery(params (string key, string? value)[] pairs)
    {
        var parts = pairs
            .Where(p => !string.IsNullOrEmpty(p.value))
            .Select(p => $"{p.key}={Uri.EscapeDataString(p.value!)}");
        var qs = string.Join("&", parts);
        return qs.Length > 0 ? "?" + qs : string.Empty;
    }
}
