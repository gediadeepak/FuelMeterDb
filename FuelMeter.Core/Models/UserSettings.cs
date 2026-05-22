namespace FuelMeter.Core.Models;

public class UserSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // Electricity
    public string ElectricityProvider { get; set; } = string.Empty;
    public decimal ElectricityPricePerKwh { get; set; }
    public decimal ElectricityStandingCharge { get; set; }   // pence/day
    public decimal? ElectricityMonthlyBudget { get; set; }

    // Gas
    public string GasProvider { get; set; } = string.Empty;
    public decimal GasPricePerUnit { get; set; }
    public decimal GasStandingCharge { get; set; }           // pence/day
    public decimal? GasMonthlyBudget { get; set; }

    // Preferences
    public string Currency { get; set; } = "GBP";
    public string CurrencySymbol { get; set; } = "£";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
