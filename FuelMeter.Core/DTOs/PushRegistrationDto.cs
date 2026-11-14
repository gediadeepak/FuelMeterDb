using System.ComponentModel.DataAnnotations;

namespace FuelMeter.Core.DTOs;

public class PushRegistrationDto
{
    [Required]
    [RegularExpression(@"\A[c-f][A-Za-z0-9_-]{21}\z")]
    public string InstallationId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
