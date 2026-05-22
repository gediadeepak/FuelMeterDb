using System.ComponentModel.DataAnnotations;

namespace FuelMeter.Components.Models;

public class ForgotPasswordModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
