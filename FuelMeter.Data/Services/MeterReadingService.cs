using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public class MeterReadingService(FuelMeterDbContext db) : IMeterReadingService
{
    // ── Save (Insert or Update) ─────────────────────────────────
    public async Task<SaveMeterReadingResult> SaveAsync(int userId, MeterReadingDto dto)
    {
        try
        {
            if (dto.Id == 0)
            {
                // Insert
                var entity = new MeterReading
                {
                    UserId       = userId,
                    FuelType     = dto.FuelType,
                    ReadingDate  = dto.ReadingDate!.Value.Date,
                    ReadingValue = dto.ReadingValue!.Value,
                    Notes        = dto.Notes,
                    CreatedAt    = DateTime.UtcNow
                };
                db.MeterReadings.Add(entity);
            }
            else
            {
                // Update
                var existing = await db.MeterReadings
                    .FirstOrDefaultAsync(r => r.Id == dto.Id && r.UserId == userId);

                if (existing is null)
                    return Fail("Reading not found.");

                existing.FuelType     = dto.FuelType;
                existing.ReadingDate  = dto.ReadingDate!.Value.Date;
                existing.ReadingValue = dto.ReadingValue!.Value;
                existing.Notes        = dto.Notes;
                existing.UpdatedAt    = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Ok("Reading saved successfully.");
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            return Fail("Unable to reach the server. Please check your connection.");
        }
    }

    // ── Delete ──────────────────────────────────────────────────
    public async Task<SaveMeterReadingResult> DeleteAsync(int userId, int readingId)
    {
        try
        {
            var entity = await db.MeterReadings
                .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);

            if (entity is null) return Fail("Reading not found.");

            db.MeterReadings.Remove(entity);
            await db.SaveChangesAsync();
            return Ok("Reading deleted.");
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            return Fail("Unable to reach the server. Please check your connection.");
        }
    }

    // ── Grid (with previous reading + calculated columns) ───────
    public async Task<List<MeterReadingRowDto>> GetGridAsync(
        int userId, string? fuelType, int? year, int? month)
    {
        var query = db.MeterReadings
            .Where(r => r.UserId == userId);

        if (!string.IsNullOrEmpty(fuelType) && fuelType != "All")
            query = query.Where(r => r.FuelType == fuelType);

        if (year.HasValue && month.HasValue)
            query = query.Where(r => r.ReadingDate.Year == year && r.ReadingDate.Month == month);

        var readings = await query
            .OrderBy(r => r.FuelType)
            .ThenBy(r => r.ReadingDate).ThenBy(r => r.Id)
            .ToListAsync();

        // Load settings for rate lookup
        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        var rows = new List<MeterReadingRowDto>();

        // Group by fuel type to calculate previous reading per group
        foreach (var group in readings.GroupBy(r => r.FuelType))
        {
            var sorted = group.ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var current = sorted[i];

                // Previous reading: last reading before this one for same fuel type
                decimal prevValue = i == 0
                    ? await GetPreviousReadingAsync(userId, current.FuelType, current.ReadingDate, current.ReadingValue)
                    : sorted[i - 1].ReadingValue;

                decimal usage = ReadingUsage.Between(prevValue, current.ReadingValue);

                decimal rate = current.FuelType == "Electricity"
                    ? (settings?.ElectricityPricePerKwh ?? 0)
                    : (settings?.GasPricePerUnit ?? 0);

                rows.Add(new MeterReadingRowDto
                {
                    Id              = current.Id,
                    ReadingDate     = current.ReadingDate,
                    FuelType        = current.FuelType,
                    PreviousReading = prevValue,
                    CurrentReading  = current.ReadingValue,
                    Usage           = usage,
                    Rate            = rate,
                    EstimatedCost   = Math.Round(usage * rate, 2),
                    Notes           = current.Notes
                });
            }
        }

        return rows.OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Id).ToList();
    }

    // ── Get single by ID ────────────────────────────────────────
    public async Task<MeterReadingDto?> GetByIdAsync(int userId, int readingId)
    {
        var entity = await db.MeterReadings
            .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);

        if (entity is null) return null;

        return new MeterReadingDto
        {
            Id           = entity.Id,
            FuelType     = entity.FuelType,
            ReadingDate  = entity.ReadingDate,
            ReadingValue = entity.ReadingValue,
            Notes        = entity.Notes
        };
    }

    // ── Helpers ─────────────────────────────────────────────────
    private async Task<decimal> GetPreviousReadingAsync(int userId, string fuelType, DateTime before, decimal fallback)
    {
        var prev = await db.MeterReadings
            .Where(r => r.UserId == userId && r.FuelType == fuelType && r.ReadingDate < before)
            .OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        return prev?.ReadingValue ?? fallback;
    }

    private static SaveMeterReadingResult Ok(string msg) => new() { Success = true,  Message = msg };
    private static SaveMeterReadingResult Fail(string msg) => new() { Success = false, Message = msg };

    private static bool IsConnectionError(Exception ex) =>
        ex is Microsoft.Data.SqlClient.SqlException ||
        ex.InnerException is Microsoft.Data.SqlClient.SqlException;

    // ── Dashboard ────────────────────────────────────────────────
    public async Task<DashboardDto> GetDashboardAsync(int userId, int year, int month)
    {
        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        // All readings for this month
        var monthReadings = await db.MeterReadings
            .Where(r => r.UserId == userId
                     && r.ReadingDate.Year  == year
                     && r.ReadingDate.Month == month)
            .OrderBy(r => r.ReadingDate)
            .ToListAsync();

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var elecUsage = await CalcPeriodUsageAsync(userId, "Electricity", monthStart, monthEnd);
        var gasUsage = await CalcPeriodUsageAsync(userId, "Gas", monthStart, monthEnd);
        var budgets = await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToListAsync();

        var elecRate = settings?.ElectricityPricePerKwh ?? 0;
        var gasRate  = settings?.GasPricePerUnit        ?? 0;

        int daysInMonth = DateTime.DaysInMonth(year, month);
        decimal elecStandingCost = Math.Round((settings?.ElectricityStandingCharge ?? 0) / 100m * daysInMonth, 2);
        decimal gasStandingCost  = Math.Round((settings?.GasStandingCharge ?? 0) / 100m * daysInMonth, 2);

        decimal elecUsageCost = Math.Round(elecUsage * elecRate, 2);
        decimal gasUsageCost  = Math.Round(gasUsage  * gasRate,  2);

        return new DashboardDto
        {
            ElectricityUsage          = elecUsage,
            ElectricityUsageCost      = elecUsageCost,
            ElectricityStandingCharge = settings?.ElectricityStandingCharge ?? 0,
            ElectricityStandingCost   = elecStandingCost,
            ElectricityCost           = elecUsageCost + elecStandingCost,
            ElectricityRate           = elecRate,
            ElectricityProvider       = settings?.ElectricityProvider ?? string.Empty,
            ElectricityBudget         = budgets.FirstOrDefault(b => b.FuelType == "Electricity")?.BudgetLimit ?? settings?.ElectricityMonthlyBudget ?? 0,

            GasUsage          = gasUsage,
            GasUsageCost      = gasUsageCost,
            GasStandingCharge = settings?.GasStandingCharge ?? 0,
            GasStandingCost   = gasStandingCost,
            GasCost           = gasUsageCost + gasStandingCost,
            GasRate           = gasRate,
            GasProvider       = settings?.GasProvider ?? string.Empty,
            GasBudget         = budgets.FirstOrDefault(b => b.FuelType == "Gas")?.BudgetLimit ?? settings?.GasMonthlyBudget ?? 0,

            CurrencySymbol = settings?.CurrencySymbol ?? "£",
            Month          = month,
            Year           = year,
            HasReadings    = monthReadings.Count > 0
        };
    }

    // ── Usage Chart (Daily / Monthly / Yearly) ───────────────────
    public async Task<UsageChartDto> GetUsageChartAsync(int userId, string view, int year, int? month)
    {
        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        var elecRate = settings?.ElectricityPricePerKwh ?? 0;
        var gasRate  = settings?.GasPricePerUnit        ?? 0;
        var symbol   = settings?.CurrencySymbol ?? "£";

        var bars = new List<ChartBarDto>();

        if (view == "Daily" && month.HasValue)
        {
            var monthStart = new DateTime(year, month.Value, 1);
            var monthEnd = monthStart.AddMonths(1);
            var readings = await db.MeterReadings.AsNoTracking()
                .Where(r => r.UserId == userId && r.ReadingDate >= monthStart && r.ReadingDate < monthEnd)
                .OrderBy(r => r.ReadingDate).ThenBy(r => r.Id)
                .ToListAsync();

            for (var day = monthStart; day < monthEnd; day = day.AddDays(1))
                bars.Add(new() { Label = day.Day.ToString(), ReadingDate = day });

            foreach (var fuel in new[] { "Electricity", "Gas" })
            {
                var fuelReadings = readings.Where(r => r.FuelType == fuel).GroupBy(r => r.ReadingDate.Date).ToList();
                if (fuelReadings.Count == 0)
                    continue;

                var previous = await db.MeterReadings.AsNoTracking()
                    .Where(r => r.UserId == userId && r.FuelType == fuel && r.ReadingDate < monthStart)
                    .OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                foreach (var group in fuelReadings)
                {
                    var first = group.First();
                    var start = previous ?? first;
                    var hasInterval = previous is not null || group.Skip(1).Any();
                    var usage = ReadingUsage.Total(group, start.ReadingValue);
                    var bar = bars[group.Key.Day - 1];

                    if (fuel == "Electricity")
                    {
                        bar.ElectricityReadingFrom = hasInterval ? start.ReadingDate.Date : null;
                        bar.ElectricityUsage = Math.Round(usage, 2);
                        bar.ElectricityCost = Math.Round(usage * elecRate, 2);
                    }
                    else
                    {
                        bar.GasReadingFrom = hasInterval ? start.ReadingDate.Date : null;
                        bar.GasUsage = Math.Round(usage, 2);
                        bar.GasCost = Math.Round(usage * gasRate, 2);
                    }

                    previous = group.Last();
                }
            }
        }
        else if (view == "Monthly")
        {
            var budgets = await db.Budgets.AsNoTracking()
                .Where(b => b.UserId == userId && b.Year == year)
                .Select(b => new { b.Month, b.FuelType, b.BudgetLimit })
                .ToDictionaryAsync(b => (b.Month, b.FuelType), b => b.BudgetLimit);
            var today = DateTime.Today;
            for (int m = 1; m <= 12; m++)
            {
                var monthStart = new DateTime(year, m, 1);
                var monthEnd   = monthStart.AddMonths(1);

                decimal elecUsage = await CalcPeriodUsageAsync(userId, "Electricity", monthStart, monthEnd);
                decimal gasUsage  = await CalcPeriodUsageAsync(userId, "Gas",         monthStart, monthEnd);

                bars.Add(new ChartBarDto
                {
                    Label            = monthStart.ToString("MMM"),
                    ElectricityUsage = Math.Round(elecUsage, 2),
                    ElectricityCost  = Math.Round(elecUsage * elecRate, 2),
                    GasUsage         = Math.Round(gasUsage, 2),
                    GasCost          = Math.Round(gasUsage * gasRate, 2),
                    ElectricityStandingCost = monthStart <= today
                        ? Math.Round((settings?.ElectricityStandingCharge ?? 0) * DateTime.DaysInMonth(year, m) / 100m, 2) : 0,
                    GasStandingCost = monthStart <= today
                        ? Math.Round((settings?.GasStandingCharge ?? 0) * DateTime.DaysInMonth(year, m) / 100m, 2) : 0,
                    ElectricityBudget = budgets.GetValueOrDefault((m, "Electricity"), settings?.ElectricityMonthlyBudget ?? 0),
                    GasBudget = budgets.GetValueOrDefault((m, "Gas"), settings?.GasMonthlyBudget ?? 0)
                });
            }
        }
        else // Yearly — last 5 years
        {
            int startYear = year - 4;
            for (int y = startYear; y <= year; y++)
            {
                var yearStart = new DateTime(y, 1, 1);
                var yearEnd   = yearStart.AddYears(1);

                decimal elecUsage = await CalcPeriodUsageAsync(userId, "Electricity", yearStart, yearEnd);
                decimal gasUsage  = await CalcPeriodUsageAsync(userId, "Gas",         yearStart, yearEnd);

                bars.Add(new ChartBarDto
                {
                    Label            = y.ToString(),
                    ElectricityUsage = Math.Round(elecUsage, 2),
                    ElectricityCost  = Math.Round(elecUsage * elecRate, 2),
                    GasUsage         = Math.Round(gasUsage, 2),
                    GasCost          = Math.Round(gasUsage * gasRate, 2),
                });
            }
        }

        return new UsageChartDto { Bars = bars, CurrencySymbol = symbol, HasReadingIntervals = view == "Daily" && month.HasValue };
    }

    private async Task<decimal> CalcPeriodUsageAsync(
        int userId, string fuelType, DateTime periodStart, DateTime periodEnd)
    {
        var periodReadings = await db.MeterReadings
            .Where(r => r.UserId == userId && r.FuelType == fuelType
                     && r.ReadingDate >= periodStart && r.ReadingDate < periodEnd)
            .OrderBy(r => r.ReadingDate).ThenBy(r => r.Id)
            .ToListAsync();

        if (periodReadings.Count == 0) return 0;

        var firstInPeriod = periodReadings.First().ReadingValue;
        var prevReading   = (await db.MeterReadings
            .Where(r => r.UserId == userId && r.FuelType == fuelType && r.ReadingDate < periodStart)
            .OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync())?.ReadingValue ?? firstInPeriod;

        return ReadingUsage.Total(periodReadings, prevReading);
    }
}
