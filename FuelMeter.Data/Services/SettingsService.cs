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

            existing.ElectricityProvider      = dto.ElectricityProvider.Trim();
            existing.ElectricityPricePerKwh   = dto.ElectricityPricePerKwh;
            existing.ElectricityStandingCharge = dto.ElectricityStandingCharge;
            existing.ElectricityMonthlyBudget = dto.ElectricityMonthlyBudget;
            existing.GasProvider              = dto.GasProvider.Trim();
            existing.GasPricePerUnit          = dto.GasPricePerUnit;
            existing.GasStandingCharge        = dto.GasStandingCharge;
            existing.GasMonthlyBudget         = dto.GasMonthlyBudget;
            existing.Currency                 = dto.Currency;
            existing.CurrencySymbol           = dto.CurrencySymbol;

            await db.SaveChangesAsync();

            return new SaveSettingsResult { Success = true, Message = "Settings saved successfully.", Settings = ToDto(existing) };
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            return new SaveSettingsResult { Success = false, Message = "Unable to reach the server. Please check your connection." };
        }
    }

    private static UserSettingsDto ToDto(UserSettings s) => new()
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
