using FuelMeter.Api.Extensions;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

/// <summary>Data export and import — JSON and CSV formats.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExportImportController : ControllerBase
{
    private readonly IExportImportService _exportImportService;

    public ExportImportController(IExportImportService exportImportService)
    {
        _exportImportService = exportImportService;
    }

    /// <summary>Export all user data (meter readings, budgets, settings) as JSON.</summary>
    [HttpGet("export/json")]
    [ProducesResponseType(typeof(ExportDataDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExportDataDto>> ExportJson()
    {
        var userId = User.GetUserId();
        var data = await _exportImportService.ExportUserDataAsync(userId);
        return Ok(data);
    }

    /// <summary>Download user data as a JSON file.</summary>
    [HttpGet("download/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadJson()
    {
        var userId = User.GetUserId();
        var bytes = await _exportImportService.ExportToJsonBytesAsync(userId);
        var fileName = $"fuelmeter-data-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(bytes, "application/json", fileName);
    }

    /// <summary>Download user data as a CSV file.</summary>
    [HttpGet("download/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadCsv()
    {
        var userId = User.GetUserId();
        var bytes = await _exportImportService.ExportToCsvBytesAsync(userId);
        var fileName = $"fuelmeter-data-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    /// <summary>Import data from JSON string.</summary>
    [HttpPost("import/json")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> ImportJson([FromBody] string jsonContent)
    {
        var userId = User.GetUserId();

        try
        {
            var result = await _exportImportService.ImportFromJsonAsync(userId, jsonContent);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ImportResultDto(
                false,
                0,
                0,
                false,
                new List<string> { ex.Message },
                new List<string>()
            ));
        }
    }

    /// <summary>Import data from CSV string.</summary>
    [HttpPost("import/csv")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> ImportCsv([FromBody] string csvContent)
    {
        var userId = User.GetUserId();

        try
        {
            var result = await _exportImportService.ImportFromCsvAsync(userId, csvContent);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ImportResultDto(
                false,
                0,
                0,
                false,
                new List<string> { ex.Message },
                new List<string>()
            ));
        }
    }

    /// <summary>Upload and import JSON file.</summary>
    [HttpPost("upload/json")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> UploadJson(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var userId = User.GetUserId();

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var jsonContent = await reader.ReadToEndAsync();
            var result = await _exportImportService.ImportFromJsonAsync(userId, jsonContent);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ImportResultDto(
                false,
                0,
                0,
                false,
                new List<string> { ex.Message },
                new List<string>()
            ));
        }
    }

    /// <summary>Upload and import CSV file.</summary>
    [HttpPost("upload/csv")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> UploadCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var userId = User.GetUserId();

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var csvContent = await reader.ReadToEndAsync();
            var result = await _exportImportService.ImportFromCsvAsync(userId, csvContent);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ImportResultDto(
                false,
                0,
                0,
                false,
                new List<string> { ex.Message },
                new List<string>()
            ));
        }
    }

    /// <summary>Validate import data without saving.</summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> Validate([FromBody] ImportDataDto data)
    {
        var errors = await _exportImportService.ValidateImportDataAsync(data);
        return Ok(errors);
    }
}
