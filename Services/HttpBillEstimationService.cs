using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

public class HttpBillEstimationService(IApiClientService api) : IBillEstimationService
{
    public async Task<BillEstimationDto?> EstimateBillAsync(int userId, BillEstimationRequestDto request)
        => await api.PostAsync<BillEstimationRequestDto, BillEstimationDto>("api/billestimation/estimate", request);

    public async Task<BillEstimationDto?> EstimateCurrentMonthBillAsync(int userId, string fuelType)
        => await api.GetAsync<BillEstimationDto>($"api/billestimation/current/{fuelType}");

    public async Task<List<MonthlyBillSummaryDto>> GetMonthlyBillSummariesAsync(int userId, int year)
        => await api.GetAsync<List<MonthlyBillSummaryDto>>($"api/billestimation/summaries/{year}") ?? [];

    public async Task<BillComparisonDto?> CompareBillsAsync(int userId, string fuelType, int year, int month)
        => await api.GetAsync<BillComparisonDto>($"api/billestimation/compare/{fuelType}/{year}/{month}");

    public async Task<List<MonthlyBillSummaryDto>> GetBillHistoryAsync(int userId, string fuelType, int months = 12)
        => await api.GetAsync<List<MonthlyBillSummaryDto>>($"api/billestimation/history/{fuelType}?months={months}") ?? [];

    public async Task<decimal> ProjectMonthlyBillAsync(int userId, string fuelType, int year, int month)
        => await api.GetAsync<decimal>($"api/billestimation/project/{fuelType}/{year}/{month}");

    public async Task<decimal> CalculateDailyAverageCostAsync(int userId, string fuelType, DateTime startDate, DateTime endDate)
    {
        var start = startDate.ToString("yyyy-MM-dd");
        var end = endDate.ToString("yyyy-MM-dd");
        return await api.GetAsync<decimal>($"api/billestimation/daily-average/{fuelType}?startDate={start}&endDate={end}");
    }
}
