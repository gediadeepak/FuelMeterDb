namespace FuelMeter.Core.DTOs;

public class AnomalyDto
{
    public DateTime Date { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public decimal DailyUsage { get; set; }
    public decimal RollingAverage { get; set; }
    public decimal PercentAboveAverage { get; set; }
    public string Severity { get; set; } = "Warning"; // Warning | High
}

public class AnomalyResultDto
{
    public List<AnomalyDto> Anomalies { get; set; } = new();
    public int DaysAnalyzed { get; set; }
    public decimal ThresholdPercent { get; set; } = 50m;
}
