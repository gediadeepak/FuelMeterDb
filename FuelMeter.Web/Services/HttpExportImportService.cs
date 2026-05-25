using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;

namespace FuelMeter.Web.Services;

public class HttpExportImportService(IApiClientService api) : IExportImportService
{
    public async Task<ExportDataDto> ExportUserDataAsync(int userId)
        => await api.GetAsync<ExportDataDto>("api/exportimport/export/json")
           ?? new ExportDataDto(DateTime.UtcNow, string.Empty, [], [], null);

    public async Task<string> ExportToJsonAsync(int userId)
    {
        var data = await ExportUserDataAsync(userId);
        return System.Text.Json.JsonSerializer.Serialize(data);
    }

    public async Task<string> ExportToCsvAsync(int userId)
        => await api.GetAsync<string>("api/exportimport/download/csv") ?? string.Empty;

    public async Task<byte[]> ExportToJsonBytesAsync(int userId)
    {
        var json = await ExportToJsonAsync(userId);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public async Task<byte[]> ExportToCsvBytesAsync(int userId)
    {
        var csv = await ExportToCsvAsync(userId);
        return System.Text.Encoding.UTF8.GetBytes(csv);
    }

    public async Task<ImportResultDto> ImportFromJsonAsync(int userId, string jsonContent)
        => await api.PostAsync<string, ImportResultDto>("api/exportimport/import/json", jsonContent)
           ?? new ImportResultDto(false, 0, 0, false, ["Failed to connect to server"], []);

    public async Task<ImportResultDto> ImportFromCsvAsync(int userId, string csvContent)
        => await api.PostAsync<string, ImportResultDto>("api/exportimport/import/csv", csvContent)
           ?? new ImportResultDto(false, 0, 0, false, ["Failed to connect to server"], []);

    public async Task<ImportResultDto> ImportDataAsync(int userId, ImportDataDto importData)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(importData);
        return await ImportFromJsonAsync(userId, json);
    }

    public async Task<List<string>> ValidateImportDataAsync(ImportDataDto importData)
        => await api.PostAsync<ImportDataDto, List<string>>("api/exportimport/validate", importData) ?? [];
}
