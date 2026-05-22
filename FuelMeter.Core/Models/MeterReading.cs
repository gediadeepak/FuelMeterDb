namespace FuelMeter.Core.Models;

public class MeterReading
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string FuelType { get; set; } = string.Empty;   // "Electricity" | "Gas"
    public DateTime ReadingDate { get; set; }
    public decimal ReadingValue { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
