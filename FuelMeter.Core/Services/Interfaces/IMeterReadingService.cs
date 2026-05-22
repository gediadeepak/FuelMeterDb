using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public interface IMeterReadingService
{
    Task<SaveMeterReadingResult> SaveAsync(int userId, MeterReadingDto dto);
    Task<SaveMeterReadingResult> DeleteAsync(int userId, int readingId);
    Task<List<MeterReadingRowDto>> GetGridAsync(int userId, string? fuelType, int? year, int? month);
    Task<MeterReadingDto?> GetByIdAsync(int userId, int readingId);
    Task<DashboardDto> GetDashboardAsync(int userId, int year, int month);
    Task<UsageChartDto> GetUsageChartAsync(int userId, string view, int year, int? month);
}
