namespace FuelMeter.Core.DTOs;

public record ExportDataDto(
    DateTime ExportDate,
    string UserEmail,
    List<MeterReadingExportDto> MeterReadings,
    List<BudgetExportDto> Budgets,
    UserSettingsExportDto? UserSettings
);

public record MeterReadingExportDto(
    int Id,
    string FuelType,
    DateTime ReadingDate,
    decimal ReadingValue,
    string? Notes
);

public record BudgetExportDto(
    int Id,
    string FuelType,
    int Year,
    int Month,
    decimal BudgetLimit,
    string? Notes
);

public record UserSettingsExportDto(
    string FuelType,
    decimal UnitRate,
    decimal StandingCharge,
    int BillingDay
);

public record ImportResultDto(
    bool Success,
    int MeterReadingsImported,
    int BudgetsImported,
    bool UserSettingsImported,
    List<string> Errors,
    List<string> Warnings
);

public record ImportDataDto(
    List<MeterReadingImportDto> MeterReadings,
    List<BudgetImportDto> Budgets,
    UserSettingsImportDto? UserSettings
);

public record MeterReadingImportDto(
    string FuelType,
    DateTime ReadingDate,
    decimal ReadingValue,
    string? Notes
);

public record BudgetImportDto(
    string FuelType,
    int Year,
    int Month,
    decimal BudgetLimit,
    string? Notes
);

public record UserSettingsImportDto(
    string FuelType,
    decimal UnitRate,
    decimal StandingCharge,
    int BillingDay
);
