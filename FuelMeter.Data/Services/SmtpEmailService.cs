using System.Net;
using System.Net.Mail;
using FuelMeter.Core.Options;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FuelMeter.Data.Services;

public class SmtpEmailService(IOptions<EmailSettings> options) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var subject = "Reset your FuelMeter password";
        var body    = BuildResetEmailHtml(toName, resetLink);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl   = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        using var message = new MailMessage
        {
            From       = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject    = subject,
            Body       = body,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail, toName));

        await client.SendMailAsync(message);
    }

    private static string BuildResetEmailHtml(string name, string resetLink) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"/></head>
        <body style="font-family:sans-serif;background:#f4f6f8;margin:0;padding:0;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6f8;padding:40px 0;">
            <tr><td align="center">
              <table width="480" cellpadding="0" cellspacing="0"
                     style="background:#ffffff;border-radius:16px;box-shadow:0 4px 24px rgba(0,0,0,0.07);overflow:hidden;">
                <tr>
                  <td style="background:linear-gradient(135deg,#1565C0,#1976D2);padding:32px 40px;text-align:center;">
                    <span style="font-size:26px;font-weight:800;color:#ffffff;letter-spacing:-0.5px;">⚡ FuelMeter</span>
                  </td>
                </tr>
                <tr>
                  <td style="padding:40px 40px 32px;">
                    <h2 style="margin:0 0 12px;font-size:22px;color:#1a202c;">Reset your password</h2>
                    <p style="margin:0 0 24px;color:#546e7a;line-height:1.6;">
                      Hi {name},<br/><br/>
                      We received a request to reset the password for your FuelMeter account.
                      Click the button below to choose a new password. This link is valid for <strong>24 hours</strong>.
                    </p>
                    <table cellpadding="0" cellspacing="0" width="100%">
                      <tr><td align="center">
                        <a href="{resetLink}"
                           style="display:inline-block;background:linear-gradient(135deg,#1565C0,#1976D2);
                                  color:#fff;font-weight:700;font-size:15px;border-radius:12px;
                                  padding:14px 36px;text-decoration:none;">
                          Reset Password
                        </a>
                      </td></tr>
                    </table>
                    <p style="margin:24px 0 0;font-size:13px;color:#90a4ae;line-height:1.6;">
                      If you didn't request this, you can safely ignore this email — your password will not change.<br/><br/>
                      Or paste this link into your browser:<br/>
                      <a href="{resetLink}" style="color:#1976D2;word-break:break-all;">{resetLink}</a>
                    </p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:20px 40px;background:#f8fafc;border-top:1px solid #e2e8f0;
                             font-size:12px;color:#90a4ae;text-align:center;">
                    © {DateTime.UtcNow.Year} FuelMeter. All rights reserved.
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
