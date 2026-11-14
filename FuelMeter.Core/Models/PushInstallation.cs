namespace FuelMeter.Core.Models;

public class PushInstallation
{
    public string InstallationId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
