namespace FuelMeter.Core.DTOs;

public class NotificationSettingsDto
{
    // Electricity
    public bool   ElectricityEnabled    { get; set; }
    public string ElectricityFrequency  { get; set; } = "Weekly";
    public int    ElectricityHour       { get; set; } = 9;
    public int    ElectricityMinute     { get; set; } = 0;
    public int    ElectricityDayOfWeek  { get; set; } = 1;
    public int    ElectricityDayOfMonth { get; set; } = 1;

    // Gas
    public bool   GasEnabled    { get; set; }
    public string GasFrequency  { get; set; } = "Weekly";
    public int    GasHour       { get; set; } = 9;
    public int    GasMinute     { get; set; } = 0;
    public int    GasDayOfWeek  { get; set; } = 1;
    public int    GasDayOfMonth { get; set; } = 1;
}

public class SaveNotificationSettingsResult
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
