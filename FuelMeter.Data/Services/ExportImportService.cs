using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace FuelMeter.Data.Services;

public class ExportImportService : IExportImportService
{
    private readonly FuelMeterDbContext _context;

    public ExportImportService(FuelMeterDbContext context)
    {
        _context = context;
    }

    public async Task<ExportDataDto> ExportUserDataAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var meterReadings = await _context.MeterReadings
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.ReadingDate)
            .Select(r => new MeterReadingExportDto(
                r.Id,
                r.FuelType,
                r.ReadingDate,
                r.ReadingValue,
                r.Notes
            ))
            .ToListAsync();

        var budgets = await _context.Budgets
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Year)
            .ThenBy(b => b.Month)
            .Select(b => new BudgetExportDto(
                b.Id,
                b.FuelType,
                b.Year,
                b.Month,
                b.BudgetLimit,
                b.Notes
            ))
            .ToListAsync();

        var settings = await _context.UserSettings
            .Where(s => s.UserId == userId)
            .FirstOrDefaultAsync();

        UserSettingsExportDto? settingsDto = null;
        if (settings != null)
        {
            // Export both electricity and gas settings
            settingsDto = new UserSettingsExportDto(
                "Combined",
                settings.ElectricityPricePerKwh,
                settings.ElectricityStandingCharge,
                1 // Default billing day
            );
        }

        return new ExportDataDto(
            DateTime.UtcNow,
            user.Email,
            meterReadings,
            budgets,
            settingsDto,
            settings is null ? null : SettingsService.ToDto(settings)
        );
    }

    public async Task<string> ExportToJsonAsync(int userId)
    {
        var data = await ExportUserDataAsync(userId);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(data, options);
    }

    public async Task<byte[]> ExportToJsonBytesAsync(int userId)
    {
        var json = await ExportToJsonAsync(userId);
        return Encoding.UTF8.GetBytes(json);
    }

    public async Task<string> ExportToCsvAsync(int userId)
    {
        var data = await ExportUserDataAsync(userId);
        var sb = new StringBuilder();

        // Meter Readings CSV
        sb.AppendLine("=== METER READINGS ===");
        sb.AppendLine("Id,FuelType,ReadingDate,ReadingValue,Notes");
        foreach (var reading in data.MeterReadings)
        {
            sb.AppendLine(FormattableString.Invariant($"{reading.Id},{EscapeCsv(reading.FuelType)},{reading.ReadingDate:yyyy-MM-dd HH:mm:ss},{reading.ReadingValue},{EscapeCsv(reading.Notes ?? "")}"));
        }

        sb.AppendLine();
        sb.AppendLine("=== BUDGETS ===");
        sb.AppendLine("Id,FuelType,Year,Month,BudgetLimit,Notes");
        foreach (var budget in data.Budgets)
        {
            sb.AppendLine(FormattableString.Invariant($"{budget.Id},{EscapeCsv(budget.FuelType)},{budget.Year},{budget.Month},{budget.BudgetLimit},{EscapeCsv(budget.Notes ?? "")}"));
        }

        if (data.TariffSettings is not null)
        {
            sb.AppendLine();
            sb.AppendLine("=== TARIFF SETTINGS ===");
            sb.AppendLine("SettingsJson");
            sb.AppendLine(EscapeCsv(JsonSerializer.Serialize(data.TariffSettings)));
        }

        return sb.ToString();
    }

    public async Task<byte[]> ExportToCsvBytesAsync(int userId)
    {
        var csv = await ExportToCsvAsync(userId);
        return Encoding.UTF8.GetBytes(csv);
    }

    public async Task<ImportResultDto> ImportFromJsonAsync(int userId, string jsonContent)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var importData = JsonSerializer.Deserialize<ImportDataDto>(jsonContent, options);
            if (importData == null)
            {
                return new ImportResultDto(false, 0, 0, false, new List<string> { "Invalid JSON format" }, new List<string>());
            }

            return await ImportDataAsync(userId, importData);
        }
        catch (JsonException ex)
        {
            return new ImportResultDto(false, 0, 0, false, new List<string> { $"JSON parsing error: {ex.Message}" }, new List<string>());
        }
    }

    public async Task<ImportResultDto> ImportFromCsvAsync(int userId, string csvContent)
    {
        var warnings = new List<string>();
        var readings = new List<MeterReadingImportDto>();
        var budgets = new List<BudgetImportDto>();
        UserSettingsImportDto? legacySettings = null;
        UserSettingsDto? tariffSettings = null;
        var section = string.Empty;

        using var parser = new TextFieldParser(new StringReader(csvContent.TrimStart('\uFEFF')))
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        while (!parser.EndOfData)
        {
            var lineNumber = parser.LineNumber;
            try
            {
                var fields = parser.ReadFields();
                if (fields is null) break;

                if (fields.Length == 1 && fields[0].StartsWith("==="))
                {
                    section = fields[0];
                    continue;
                }

                switch (section)
                {
                    case "=== METER READINGS ===" when fields[0] != "Id":
                        readings.Add(new MeterReadingImportDto(
                            fields[1], DateTime.Parse(fields[2], CultureInfo.InvariantCulture),
                            decimal.Parse(fields[3], CultureInfo.InvariantCulture), fields.ElementAtOrDefault(4)));
                        break;
                    case "=== BUDGETS ===" when fields[0] != "Id":
                        budgets.Add(new BudgetImportDto(
                            fields[1], int.Parse(fields[2], CultureInfo.InvariantCulture),
                            int.Parse(fields[3], CultureInfo.InvariantCulture),
                            decimal.Parse(fields[4], CultureInfo.InvariantCulture), fields.ElementAtOrDefault(5)));
                        break;
                    case "=== USER SETTINGS ===" when fields[0] != "FuelType":
                        legacySettings = new UserSettingsImportDto(
                            fields[0], decimal.Parse(fields[1], CultureInfo.InvariantCulture),
                            decimal.Parse(fields[2], CultureInfo.InvariantCulture),
                            int.Parse(fields[3], CultureInfo.InvariantCulture));
                        break;
                    case "=== TARIFF SETTINGS ===" when fields[0] != "SettingsJson":
                        tariffSettings = JsonSerializer.Deserialize<UserSettingsDto>(fields[0])
                            ?? throw new FormatException();
                        break;
                }
            }
            catch (Exception ex) when (ex is MalformedLineException or FormatException
                or OverflowException or IndexOutOfRangeException or JsonException)
            {
                warnings.Add($"Skipped invalid CSV record starting on line {lineNumber}.");
            }
        }

        var data = new ImportDataDto(readings, budgets, legacySettings, tariffSettings);
        var result = await ImportDataAsync(userId, data);
        result.Warnings.InsertRange(0, warnings);
        return result;
    }

    public async Task<ImportResultDto> ImportDataAsync(int userId, ImportDataDto importData)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        int readingsImported = 0;
        int budgetsImported = 0;

        // Validate
        var validationErrors = await ValidateImportDataAsync(importData);
        if (validationErrors.Any())
        {
            errors.AddRange(validationErrors);
            return new ImportResultDto(false, 0, 0, false, errors, warnings);
        }

        try
        {
            var addedReadings = new HashSet<(string FuelType, DateTime Date, decimal Value)>();

            // Import meter readings
            foreach (var readingDto in importData.MeterReadings)
            {
                // Check for duplicates
                var exists = await _context.MeterReadings
                    .AnyAsync(r => r.UserId == userId &&
                                 r.FuelType == readingDto.FuelType &&
                                 r.ReadingDate == readingDto.ReadingDate &&
                                 r.ReadingValue == readingDto.ReadingValue);

                if (exists || !addedReadings.Add((readingDto.FuelType, readingDto.ReadingDate, readingDto.ReadingValue)))
                {
                    warnings.Add($"Skipped duplicate reading: {readingDto.FuelType} on {readingDto.ReadingDate:yyyy-MM-dd}");
                    continue;
                }

                var reading = new MeterReading
                {
                    UserId = userId,
                    FuelType = readingDto.FuelType,
                    ReadingDate = readingDto.ReadingDate,
                    ReadingValue = readingDto.ReadingValue,
                    Notes = readingDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MeterReadings.Add(reading);
                readingsImported++;
            }

            var addedBudgets = new Dictionary<(string FuelType, int Year, int Month), Budget>();

            // Import budgets
            foreach (var budgetDto in importData.Budgets)
            {
                // Check for existing budget
                var key = (budgetDto.FuelType, budgetDto.Year, budgetDto.Month);
                var existing = addedBudgets.GetValueOrDefault(key) ?? await _context.Budgets
                    .FirstOrDefaultAsync(b => b.UserId == userId &&
                                            b.FuelType == budgetDto.FuelType &&
                                            b.Year == budgetDto.Year &&
                                            b.Month == budgetDto.Month);

                if (existing != null)
                {
                    // Update existing
                    existing.BudgetLimit = budgetDto.BudgetLimit;
                    existing.Notes = budgetDto.Notes;
                    existing.UpdatedAt = DateTime.UtcNow;
                    warnings.Add($"Updated existing budget: {budgetDto.FuelType} {budgetDto.Year}-{budgetDto.Month:D2}");
                }
                else
                {
                    // Create new
                    var budget = new Budget
                    {
                        UserId = userId,
                        FuelType = budgetDto.FuelType,
                        Year = budgetDto.Year,
                        Month = budgetDto.Month,
                        BudgetLimit = budgetDto.BudgetLimit,
                        Notes = budgetDto.Notes,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Budgets.Add(budget);
                    addedBudgets.Add(key, budget);
                }

                budgetsImported++;
            }

            var settingsImported = importData.TariffSettings is not null || importData.UserSettings is not null;
            if (settingsImported)
            {
                var settings = await _context.UserSettings.SingleOrDefaultAsync(s => s.UserId == userId);
                if (settings is null)
                {
                    settings = new UserSettings { UserId = userId };
                    _context.UserSettings.Add(settings);
                }
                else
                {
                    settings.UpdatedAt = DateTime.UtcNow;
                }

                if (importData.TariffSettings is { } tariff)
                    SettingsService.ApplySettings(settings, tariff);
                else if (importData.UserSettings is { FuelType: "Gas" } gas)
                {
                    settings.GasPricePerUnit = gas.UnitRate;
                    settings.GasStandingCharge = gas.StandingCharge;
                }
                else if (importData.UserSettings is { } electricity)
                {
                    settings.ElectricityPricePerKwh = electricity.UnitRate;
                    settings.ElectricityStandingCharge = electricity.StandingCharge;
                }
            }

            // A single SaveChanges uses EF's transaction and SQL Server retry strategy.
            await _context.SaveChangesAsync();

            return new ImportResultDto(
                true,
                readingsImported,
                budgetsImported,
                settingsImported,
                errors,
                warnings
            );
        }
        catch (Exception ex)
        {
            errors.Add($"Import failed: {ex.Message}");
            return new ImportResultDto(false, 0, 0, false, errors, warnings);
        }
    }

    public Task<List<string>> ValidateImportDataAsync(ImportDataDto importData)
    {
        var errors = new List<string>();

        if (importData.MeterReadings is null || importData.Budgets is null)
            return Task.FromResult(new List<string> { "Backup must contain readings and budget lists." });

        // Validate meter readings
        foreach (var reading in importData.MeterReadings)
        {
            if (string.IsNullOrWhiteSpace(reading.FuelType))
                errors.Add("Meter reading missing FuelType");

            if (reading.FuelType != "Electricity" && reading.FuelType != "Gas")
                errors.Add($"Invalid FuelType: {reading.FuelType}. Must be 'Electricity' or 'Gas'");

            if (reading.ReadingValue < 0)
                errors.Add($"Invalid negative reading value: {reading.ReadingValue}");

            if (reading.ReadingDate > DateTime.UtcNow.AddDays(1))
                errors.Add($"Reading date is in the future: {reading.ReadingDate}");
        }

        // Validate budgets
        foreach (var budget in importData.Budgets)
        {
            if (string.IsNullOrWhiteSpace(budget.FuelType))
                errors.Add("Budget missing FuelType");

            if (budget.FuelType != "Electricity" && budget.FuelType != "Gas")
                errors.Add($"Invalid FuelType: {budget.FuelType}. Must be 'Electricity' or 'Gas'");

            if (budget.Year < 2000 || budget.Year > 2100)
                errors.Add($"Invalid year: {budget.Year}");

            if (budget.Month < 1 || budget.Month > 12)
                errors.Add($"Invalid month: {budget.Month}");

            if (budget.BudgetLimit < 0)
                errors.Add($"Invalid negative budget limit: {budget.BudgetLimit}");
        }

        if (importData.TariffSettings is { } tariff)
        {
            if (tariff.ElectricityProvider is null || tariff.ElectricityProvider.Length > 150
                || tariff.GasProvider is null || tariff.GasProvider.Length > 150
                || string.IsNullOrWhiteSpace(tariff.Currency) || tariff.Currency.Length > 10
                || string.IsNullOrWhiteSpace(tariff.CurrencySymbol) || tariff.CurrencySymbol.Length > 5)
                errors.Add("Invalid provider or currency settings.");

            if (tariff.ElectricityPricePerKwh < 0 || tariff.ElectricityStandingCharge < 0
                || tariff.GasPricePerUnit < 0 || tariff.GasStandingCharge < 0
                || tariff.ElectricityMonthlyBudget < 0 || tariff.GasMonthlyBudget < 0)
                errors.Add("Tariff rates, standing charges and budgets must be non-negative.");
        }
        else if (importData.UserSettings is { } legacy
            && (legacy.FuelType is not ("Combined" or "Electricity" or "Gas")
                || legacy.UnitRate < 0 || legacy.StandingCharge < 0))
        {
            errors.Add("Invalid settings in legacy backup.");
        }

        return Task.FromResult(errors);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
