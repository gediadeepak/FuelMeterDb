using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public interface IBillEstimationService
{
    // Bill estimation methods
    Task<BillEstimationDto?> EstimateBillAsync(int userId, BillEstimationRequestDto request);
    Task<BillEstimationDto?> EstimateCurrentMonthBillAsync(int userId, string fuelType);
    Task<List<MonthlyBillSummaryDto>> GetMonthlyBillSummariesAsync(int userId, int year);

    // Bill comparison methods
    Task<BillComparisonDto?> CompareBillsAsync(int userId, string fuelType, int year, int month);
    Task<List<MonthlyBillSummaryDto>> GetBillHistoryAsync(int userId, string fuelType, int months = 12);

    // Projection methods
    Task<decimal> ProjectMonthlyBillAsync(int userId, string fuelType, int year, int month);
    Task<decimal> CalculateDailyAverageCostAsync(int userId, string fuelType, DateTime startDate, DateTime endDate);
}
