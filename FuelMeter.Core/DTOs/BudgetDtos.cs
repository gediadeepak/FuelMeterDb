namespace FuelMeter.Core.DTOs;

public record CreateBudgetDto(
    string FuelType,
    int Year,
    int Month,
    decimal BudgetLimit,
    string? Notes
);

public record UpdateBudgetDto(
    decimal BudgetLimit,
    string? Notes
);

public record BudgetDto(
    int Id,
    int UserId,
    string FuelType,
    int Year,
    int Month,
    decimal BudgetLimit,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record BudgetSummaryDto(
    int Id,
    string FuelType,
    int Year,
    int Month,
    decimal BudgetLimit,
    decimal ActualSpent,
    decimal RemainingBudget,
    double PercentageUsed,
    bool IsOverBudget
);

public record MonthlyBudgetStatusDto(
    string FuelType,
    int Year,
    int Month,
    decimal? BudgetLimit,
    decimal ActualSpent,
    decimal? RemainingBudget,
    double? PercentageUsed,
    bool HasBudget,
    bool IsOverBudget
);
