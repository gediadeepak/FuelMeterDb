using System.ComponentModel.DataAnnotations;

namespace FuelMeter.Core.DTOs;

public class MeterReadingDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Fuel type is required.")]
    public string FuelType { get; set; } = "Electricity";

    [Required(ErrorMessage = "Reading date is required.")]
    public DateTime? ReadingDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Meter reading is required.")]
    [Range(0, 9999999, ErrorMessage = "Enter a valid meter reading.")]
    public decimal? ReadingValue { get; set; }

    public string? Notes { get; set; }
}

public class MeterReadingRowDto
{
    public int Id { get; set; }
    public DateTime ReadingDate { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public decimal PreviousReading { get; set; }
    public decimal CurrentReading { get; set; }
    public decimal Usage { get; set; }
    public decimal Rate { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Notes { get; set; }
}

public class SaveMeterReadingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChartBarDto
{
    public string  Label            { get; set; } = string.Empty;
    public decimal ElectricityUsage { get; set; }
    public decimal ElectricityCost  { get; set; }
    public decimal GasUsage         { get; set; }
    public decimal GasCost          { get; set; }
}

public class UsageChartDto
{
    public List<ChartBarDto> Bars          { get; set; } = [];
    public string            CurrencySymbol { get; set; } = "£";
    public string            UsageUnit     { get; set; } = "kWh / units";
}

public class DashboardDto
{
    // Electricity
    public decimal ElectricityUsage          { get; set; }
    public decimal ElectricityUsageCost      { get; set; }  // usage × rate
    public decimal ElectricityStandingCharge { get; set; }  // p/day
    public decimal ElectricityStandingCost   { get; set; }  // standing × days / 100
    public decimal ElectricityCost           { get; set; }  // usage cost + standing cost
    public decimal ElectricityRate           { get; set; }
    public string  ElectricityProvider       { get; set; } = string.Empty;
    public decimal ElectricityBudget         { get; set; }  // 0 = no budget set

    // Gas
    public decimal GasUsage          { get; set; }
    public decimal GasUsageCost      { get; set; }
    public decimal GasStandingCharge { get; set; }
    public decimal GasStandingCost   { get; set; }
    public decimal GasCost           { get; set; }
    public decimal GasRate           { get; set; }
    public string  GasProvider       { get; set; } = string.Empty;
    public decimal GasBudget         { get; set; }

    // Meta
    public string CurrencySymbol { get; set; } = "£";
    public int    Month          { get; set; }
    public int    Year           { get; set; }
    public bool   HasReadings    { get; set; }
}
