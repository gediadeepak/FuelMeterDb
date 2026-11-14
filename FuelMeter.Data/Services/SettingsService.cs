using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public class SettingsService(FuelMeterDbContext db) : ISettingsService
{
    public async Task<UserSettingsDto?> GetSettingsAsync(int userId)
    {
        var s = await db.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        if (s is null) return null;

        return ToDto(s);
    }

    public async Task<SaveSettingsResult> SaveSettingsAsync(int userId, UserSettingsDto dto)
    {
        try
        {
            var existing = await db.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);

            if (existing is null)
            {
                existing = new UserSettings { UserId = userId, CreatedAt = DateTime.UtcNow };
                db.UserSettings.Add(existing);
            }
            else
            {
                existing.UpdatedAt = DateTime.UtcNow;
            }

            ApplySettings(existing, dto);

            await db.SaveChangesAsync();

            return new SaveSettingsResult { Success = true, Message = "Settings saved successfully.", Settings = ToDto(existing) };
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            return new SaveSettingsResult { Success = false, Message = "Unable to reach the server. Please check your connection." };
        }
    }

    internal static void ApplySettings(UserSettings settings, UserSettingsDto dto)
    {
        settings.ElectricityProvider      = dto.ElectricityProvider.Trim();
        settings.ElectricityPricePerKwh   = dto.ElectricityPricePerKwh;
        settings.ElectricityStandingCharge = dto.ElectricityStandingCharge;
        settings.ElectricityMonthlyBudget = dto.ElectricityMonthlyBudget;
        settings.GasProvider              = dto.GasProvider.Trim();
        settings.GasPricePerUnit          = dto.GasPricePerUnit;
        settings.GasStandingCharge        = dto.GasStandingCharge;
        settings.GasMonthlyBudget         = dto.GasMonthlyBudget;
        settings.Currency                 = dto.Currency;
        settings.CurrencySymbol           = dto.CurrencySymbol;
    }

    internal static UserSettingsDto ToDto(UserSettings s) => new()
    {
        ElectricityProvider       = s.ElectricityProvider,
        ElectricityPricePerKwh    = s.ElectricityPricePerKwh,
        ElectricityStandingCharge = s.ElectricityStandingCharge,
        ElectricityMonthlyBudget  = s.ElectricityMonthlyBudget,
        GasProvider               = s.GasProvider,
        GasPricePerUnit           = s.GasPricePerUnit,
        GasStandingCharge         = s.GasStandingCharge,
        GasMonthlyBudget          = s.GasMonthlyBudget,
        Currency                 = s.Currency,
        CurrencySymbol           = s.CurrencySymbol,
    };

    private static bool IsConnectionError(Exception ex) =>
        ex is Microsoft.Data.SqlClient.SqlException ||
        ex.InnerException is Microsoft.Data.SqlClient.SqlException;
}
