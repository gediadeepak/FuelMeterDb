namespace FuelMeter.Core.Models;

public class NotificationSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // Electricity reminder
    public bool   ElectricityEnabled    { get; set; }
    public string ElectricityFrequency  { get; set; } = "Weekly";  // Daily | Weekly | Monthly
    public int    ElectricityHour       { get; set; } = 9;
    public int    ElectricityMinute     { get; set; } = 0;
    public int    ElectricityDayOfWeek  { get; set; } = 1;         // 0=Sun … 6=Sat (used for Weekly)
    public int    ElectricityDayOfMonth { get; set; } = 1;         // 1-28  (used for Monthly)

    // Gas reminder
    public bool   GasEnabled    { get; set; }
    public string GasFrequency  { get; set; } = "Weekly";
    public int    GasHour       { get; set; } = 9;
    public int    GasMinute     { get; set; } = 0;
    public int    GasDayOfWeek  { get; set; } = 1;
    public int    GasDayOfMonth { get; set; } = 1;

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
