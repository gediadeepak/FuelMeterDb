using FuelMeter.Core.Options;
using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FuelMeter.Data;

public static class DataServiceRegistration
{
    public static IServiceCollection AddFuelMeterData(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<FuelMeterDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sql.CommandTimeout(60);
            }));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IMeterReadingService, MeterReadingService>();
        services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IExportImportService, ExportImportService>();
        services.AddScoped<IBillEstimationService, BillEstimationService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.Configure<EmailSettings>(opts =>
        {
            var section = configuration.GetSection(EmailSettings.SectionName);
            opts.SmtpHost    = section["SmtpHost"]    ?? string.Empty;
            opts.SmtpPort    = int.TryParse(section["SmtpPort"], out var p) ? p : 587;
            opts.EnableSsl   = !string.Equals(section["EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);
            opts.SenderEmail = section["SenderEmail"] ?? string.Empty;
            opts.SenderName  = section["SenderName"]  ?? string.Empty;
            opts.Username    = section["Username"]    ?? string.Empty;
            opts.Password    = section["Password"]    ?? string.Empty;
        });
        services.AddSingleton<AuthStateService>();

        return services;
    }
}
