namespace FuelMeter.Core.Models;

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string FuelType { get; set; } = string.Empty;   // "Electricity" | "Gas"
    public int Year { get; set; }
    public int Month { get; set; }  // 1-12
    public decimal BudgetLimit { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
