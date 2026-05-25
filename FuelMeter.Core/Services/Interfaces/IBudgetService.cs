using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public interface IBudgetService
{
    Task<BudgetDto?> GetBudgetByIdAsync(int budgetId, int userId);
    Task<List<BudgetDto>> GetUserBudgetsAsync(int userId);
    Task<List<BudgetDto>> GetUserBudgetsByYearAsync(int userId, int year);
    Task<BudgetDto?> GetBudgetForMonthAsync(int userId, string fuelType, int year, int month);
    Task<BudgetDto> CreateBudgetAsync(int userId, CreateBudgetDto dto);
    Task<BudgetDto?> UpdateBudgetAsync(int budgetId, int userId, UpdateBudgetDto dto);
    Task<bool> DeleteBudgetAsync(int budgetId, int userId);

    // Budget tracking methods
    Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int userId, string fuelType, int year, int month);
    Task<List<MonthlyBudgetStatusDto>> GetMonthlyBudgetStatusAsync(int userId, int year);
    Task<List<BudgetSummaryDto>> GetOverBudgetAlertsAsync(int userId);
}
