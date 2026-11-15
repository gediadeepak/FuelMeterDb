using System.ComponentModel.DataAnnotations;

namespace FuelMeter.Core.DTOs;

public record CreateBudgetDto(
    [Required, RegularExpression("^(Electricity|Gas)$")] string FuelType,
    [Range(1, 9998)] int Year,
    [Range(1, 12)] int Month,
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] decimal BudgetLimit,
    [MaxLength(500)] string? Notes
);

public record UpdateBudgetDto(
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] decimal BudgetLimit,
    [MaxLength(500)] string? Notes
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
