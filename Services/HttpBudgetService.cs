using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Services;

public class HttpBudgetService(IApiClientService api) : IBudgetService
{
    public async Task<BudgetDto?> GetBudgetByIdAsync(int budgetId, int userId)
        => await api.GetAsync<BudgetDto>($"api/budget/{budgetId}");

    public async Task<List<BudgetDto>> GetUserBudgetsAsync(int userId)
        => await api.GetAsync<List<BudgetDto>>("api/budget") ?? [];

    public async Task<List<BudgetDto>> GetUserBudgetsByYearAsync(int userId, int year)
        => await api.GetAsync<List<BudgetDto>>($"api/budget/year/{year}") ?? [];

    public async Task<BudgetDto?> GetBudgetForMonthAsync(int userId, string fuelType, int year, int month)
        => await api.GetAsync<BudgetDto>($"api/budget/month/{fuelType}/{year}/{month}");

    public async Task<BudgetDto> CreateBudgetAsync(int userId, CreateBudgetDto dto)
        => await api.PostAsync<CreateBudgetDto, BudgetDto>("api/budget", dto)
           ?? throw new InvalidOperationException("Failed to create budget");

    public async Task<BudgetDto?> UpdateBudgetAsync(int budgetId, int userId, UpdateBudgetDto dto)
        => await api.PutAsync<UpdateBudgetDto, BudgetDto>($"api/budget/{budgetId}", dto);

    public async Task<bool> DeleteBudgetAsync(int budgetId, int userId)
    {
        try
        {
            await api.DeleteAsync<object>($"api/budget/{budgetId}");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int userId, string fuelType, int year, int month)
        => await api.GetAsync<BudgetSummaryDto>($"api/budget/summary/{fuelType}/{year}/{month}");

    public async Task<List<MonthlyBudgetStatusDto>> GetMonthlyBudgetStatusAsync(int userId, int year)
        => await api.GetAsync<List<MonthlyBudgetStatusDto>>($"api/budget/status/{year}") ?? [];

    public async Task<List<BudgetSummaryDto>> GetOverBudgetAlertsAsync(int userId)
        => await api.GetAsync<List<BudgetSummaryDto>>("api/budget/alerts") ?? [];
}
