namespace FuelMeter.Core.Services.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);
}
