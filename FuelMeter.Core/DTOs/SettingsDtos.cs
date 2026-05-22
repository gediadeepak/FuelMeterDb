namespace FuelMeter.Core.DTOs;

public class UserSettingsDto
{
    public string ElectricityProvider { get; set; } = string.Empty;
    public decimal ElectricityPricePerKwh { get; set; }
    public decimal ElectricityStandingCharge { get; set; }
    public decimal? ElectricityMonthlyBudget { get; set; }

    public string GasProvider { get; set; } = string.Empty;
    public decimal GasPricePerUnit { get; set; }
    public decimal GasStandingCharge { get; set; }
    public decimal? GasMonthlyBudget { get; set; }

    public string Currency { get; set; } = "GBP";
    public string CurrencySymbol { get; set; } = "£";
}

public class SaveSettingsResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserSettingsDto? Settings { get; set; }
}
