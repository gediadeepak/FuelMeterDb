using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public class BudgetService : IBudgetService
{
    private readonly FuelMeterDbContext _context;
    private readonly IMeterReadingService _meterReadingService;
    private readonly ISettingsService _settingsService;

    public BudgetService(
        FuelMeterDbContext context,
        IMeterReadingService meterReadingService,
        ISettingsService settingsService)
    {
        _context = context;
        _meterReadingService = meterReadingService;
        _settingsService = settingsService;
    }

    public async Task<BudgetDto?> GetBudgetByIdAsync(int budgetId, int userId)
    {
        var budget = await _context.Budgets
            .Where(b => b.Id == budgetId && b.UserId == userId)
            .FirstOrDefaultAsync();

        return budget == null ? null : MapToDto(budget);
    }

    public async Task<List<BudgetDto>> GetUserBudgetsAsync(int userId)
    {
        var budgets = await _context.Budgets
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ToListAsync();

        return budgets.Select(MapToDto).ToList();
    }

    public async Task<List<BudgetDto>> GetUserBudgetsByYearAsync(int userId, int year)
    {
        var budgets = await _context.Budgets
            .Where(b => b.UserId == userId && b.Year == year)
            .OrderByDescending(b => b.Month)
            .ToListAsync();

        return budgets.Select(MapToDto).ToList();
    }

    public async Task<BudgetDto?> GetBudgetForMonthAsync(int userId, string fuelType, int year, int month)
    {
        var budget = await _context.Budgets
            .Where(b => b.UserId == userId && b.FuelType == fuelType && b.Year == year && b.Month == month)
            .FirstOrDefaultAsync();

        return budget == null ? null : MapToDto(budget);
    }

    public async Task<BudgetDto> CreateBudgetAsync(int userId, CreateBudgetDto dto)
    {
        var budget = new Budget
        {
            UserId = userId,
            FuelType = dto.FuelType,
            Year = dto.Year,
            Month = dto.Month,
            BudgetLimit = dto.BudgetLimit,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        return MapToDto(budget);
    }

    public async Task<BudgetDto?> UpdateBudgetAsync(int budgetId, int userId, UpdateBudgetDto dto)
    {
        var budget = await _context.Budgets
            .Where(b => b.Id == budgetId && b.UserId == userId)
            .FirstOrDefaultAsync();

        if (budget == null)
            return null;

        budget.BudgetLimit = dto.BudgetLimit;
        budget.Notes = dto.Notes;
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(budget);
    }

    public async Task<bool> DeleteBudgetAsync(int budgetId, int userId)
    {
        var budget = await _context.Budgets
            .Where(b => b.Id == budgetId && b.UserId == userId)
            .FirstOrDefaultAsync();

        if (budget == null)
            return false;

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int userId, string fuelType, int year, int month)
    {
        var budget = await GetBudgetForMonthAsync(userId, fuelType, year, month);
        if (budget == null)
            return null;

        var actualSpent = await CalculateActualSpentAsync(userId, fuelType, year, month);

        var remainingBudget = budget.BudgetLimit - actualSpent;
        var percentageUsed = budget.BudgetLimit > 0
            ? (double)(actualSpent / budget.BudgetLimit * 100)
            : 0;
        var isOverBudget = actualSpent > budget.BudgetLimit;

        return new BudgetSummaryDto(
            budget.Id,
            budget.FuelType,
            budget.Year,
            budget.Month,
            budget.BudgetLimit,
            actualSpent,
            remainingBudget,
            percentageUsed,
            isOverBudget
        );
    }

    public async Task<List<MonthlyBudgetStatusDto>> GetMonthlyBudgetStatusAsync(int userId, int year)
    {
        var budgets = await GetUserBudgetsByYearAsync(userId, year);
        var statuses = new List<MonthlyBudgetStatusDto>();

        var fuelTypes = new[] { "Electricity", "Gas" };

        foreach (var fuelType in fuelTypes)
        {
            for (int month = 1; month <= 12; month++)
            {
                var budget = budgets.FirstOrDefault(b => b.FuelType == fuelType && b.Month == month);
                var actualSpent = await CalculateActualSpentAsync(userId, fuelType, year, month);

                decimal? remainingBudget = budget != null ? budget.BudgetLimit - actualSpent : null;
                double? percentageUsed = budget != null && budget.BudgetLimit > 0
                    ? (double)(actualSpent / budget.BudgetLimit * 100)
                    : null;
                bool isOverBudget = budget != null && actualSpent > budget.BudgetLimit;

                statuses.Add(new MonthlyBudgetStatusDto(
                    fuelType,
                    year,
                    month,
                    budget?.BudgetLimit,
                    actualSpent,
                    remainingBudget,
                    percentageUsed,
                    budget != null,
                    isOverBudget
                ));
            }
        }

        return statuses;
    }

    public async Task<List<BudgetSummaryDto>> GetOverBudgetAlertsAsync(int userId)
    {
        var currentDate = DateTime.UtcNow;
        var budgets = await _context.Budgets
            .Where(b => b.UserId == userId)
            .Where(b => b.Year >= currentDate.Year - 1)
            .ToListAsync();

        var overBudgetSummaries = new List<BudgetSummaryDto>();

        foreach (var budget in budgets)
        {
            var actualSpent = await CalculateActualSpentAsync(userId, budget.FuelType, budget.Year, budget.Month);

            if (actualSpent > budget.BudgetLimit)
            {
                var remainingBudget = budget.BudgetLimit - actualSpent;
                var percentageUsed = budget.BudgetLimit > 0
                    ? (double)(actualSpent / budget.BudgetLimit * 100)
                    : 0;

                overBudgetSummaries.Add(new BudgetSummaryDto(
                    budget.Id,
                    budget.FuelType,
                    budget.Year,
                    budget.Month,
                    budget.BudgetLimit,
                    actualSpent,
                    remainingBudget,
                    percentageUsed,
                    true
                ));
            }
        }

        return overBudgetSummaries.OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToList();
    }

    private async Task<decimal> CalculateActualSpentAsync(int userId, string fuelType, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var readings = await _context.MeterReadings
            .Where(r => r.UserId == userId && r.FuelType == fuelType)
            .Where(r => r.ReadingDate >= startDate && r.ReadingDate <= endDate)
            .OrderBy(r => r.ReadingDate)
            .ToListAsync();

        if (readings.Count < 2)
            return 0;

        var startReading = readings.First().ReadingValue;
        var endReading = readings.Last().ReadingValue;
        var unitsConsumed = endReading - startReading;

        if (unitsConsumed < 0)
            return 0;

        var settings = await _settingsService.GetSettingsAsync(userId);
        if (settings == null)
            return 0;

        decimal unitRate = fuelType == "Electricity"
            ? settings.ElectricityPricePerKwh
            : settings.GasPricePerUnit;

        decimal standingCharge = fuelType == "Electricity"
            ? settings.ElectricityStandingCharge
            : settings.GasStandingCharge;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var unitsCost = unitsConsumed * unitRate;
        // Standing charge is stored as pence/day — convert to currency units to match
        // the Dashboard formula: (standingCharge p/day ÷ 100) × days in month.
        var standingChargeCost = standingCharge / 100m * daysInMonth;

        return unitsCost + standingChargeCost;
    }

    private static BudgetDto MapToDto(Budget budget)
    {
        return new BudgetDto(
            budget.Id,
            budget.UserId,
            budget.FuelType,
            budget.Year,
            budget.Month,
            budget.BudgetLimit,
            budget.Notes,
            budget.CreatedAt,
            budget.UpdatedAt
        );
    }
}
