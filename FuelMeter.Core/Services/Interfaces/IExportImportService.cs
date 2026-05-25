using FuelMeter.Core.DTOs;

namespace FuelMeter.Core.Services.Interfaces;

public interface IExportImportService
{
    // Export methods
    Task<ExportDataDto> ExportUserDataAsync(int userId);
    Task<string> ExportToJsonAsync(int userId);
    Task<string> ExportToCsvAsync(int userId);
    Task<byte[]> ExportToJsonBytesAsync(int userId);
    Task<byte[]> ExportToCsvBytesAsync(int userId);

    // Import methods
    Task<ImportResultDto> ImportFromJsonAsync(int userId, string jsonContent);
    Task<ImportResultDto> ImportFromCsvAsync(int userId, string csvContent);
    Task<ImportResultDto> ImportDataAsync(int userId, ImportDataDto importData);

    // Validation
    Task<List<string>> ValidateImportDataAsync(ImportDataDto importData);
}
