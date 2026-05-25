using FuelMeter.Core.DTOs;
using FuelMeter.Core.Models;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

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
            settingsDto
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
            sb.AppendLine($"{reading.Id},{EscapeCsv(reading.FuelType)},{reading.ReadingDate:yyyy-MM-dd HH:mm:ss},{reading.ReadingValue},{EscapeCsv(reading.Notes ?? "")}");
        }

        sb.AppendLine();
        sb.AppendLine("=== BUDGETS ===");
        sb.AppendLine("Id,FuelType,Year,Month,BudgetLimit,Notes");
        foreach (var budget in data.Budgets)
        {
            sb.AppendLine($"{budget.Id},{EscapeCsv(budget.FuelType)},{budget.Year},{budget.Month},{budget.BudgetLimit},{EscapeCsv(budget.Notes ?? "")}");
        }

        if (data.UserSettings != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== USER SETTINGS ===");
            sb.AppendLine("FuelType,UnitRate,StandingCharge,BillingDay");
            sb.AppendLine($"{EscapeCsv(data.UserSettings.FuelType)},{data.UserSettings.UnitRate},{data.UserSettings.StandingCharge},{data.UserSettings.BillingDay}");
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
        var errors = new List<string>();
        var warnings = new List<string>();
        var meterReadings = new List<MeterReadingImportDto>();
        var budgets = new List<BudgetImportDto>();

        var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var currentSection = "";

        foreach (var line in lines)
        {
            if (line.StartsWith("==="))
            {
                if (line.Contains("METER READINGS"))
                    currentSection = "readings";
                else if (line.Contains("BUDGETS"))
                    currentSection = "budgets";
                else if (line.Contains("USER SETTINGS"))
                    currentSection = "settings";
                continue;
            }

            if (line.Contains("FuelType") || line.Contains("Id,"))
                continue; // Skip header rows

            try
            {
                var parts = ParseCsvLine(line);

                if (currentSection == "readings" && parts.Length >= 4)
                {
                    meterReadings.Add(new MeterReadingImportDto(
                        parts[1],
                        DateTime.Parse(parts[2], CultureInfo.InvariantCulture),
                        decimal.Parse(parts[3], CultureInfo.InvariantCulture),
                        parts.Length > 4 ? parts[4] : null
                    ));
                }
                else if (currentSection == "budgets" && parts.Length >= 5)
                {
                    budgets.Add(new BudgetImportDto(
                        parts[1],
                        int.Parse(parts[2]),
                        int.Parse(parts[3]),
                        decimal.Parse(parts[4], CultureInfo.InvariantCulture),
                        parts.Length > 5 ? parts[5] : null
                    ));
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Skipped line due to parsing error: {ex.Message}");
            }
        }

        var importData = new ImportDataDto(meterReadings, budgets, null);
        return await ImportDataAsync(userId, importData);
    }

    public async Task<ImportResultDto> ImportDataAsync(int userId, ImportDataDto importData)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        int readingsImported = 0;
        int budgetsImported = 0;
        bool settingsImported = false;

        // Validate
        var validationErrors = await ValidateImportDataAsync(importData);
        if (validationErrors.Any())
        {
            errors.AddRange(validationErrors);
            return new ImportResultDto(false, 0, 0, false, errors, warnings);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Import meter readings
            foreach (var readingDto in importData.MeterReadings)
            {
                // Check for duplicates
                var exists = await _context.MeterReadings
                    .AnyAsync(r => r.UserId == userId &&
                                 r.FuelType == readingDto.FuelType &&
                                 r.ReadingDate == readingDto.ReadingDate &&
                                 r.ReadingValue == readingDto.ReadingValue);

                if (exists)
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

            // Import budgets
            foreach (var budgetDto in importData.Budgets)
            {
                // Check for existing budget
                var existing = await _context.Budgets
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
                }

                budgetsImported++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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
            await transaction.RollbackAsync();
            errors.Add($"Import failed: {ex.Message}");
            return new ImportResultDto(false, 0, 0, false, errors, warnings);
        }
    }

    public Task<List<string>> ValidateImportDataAsync(ImportDataDto importData)
    {
        var errors = new List<string>();

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

        return Task.FromResult(errors);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
