using System.ComponentModel.DataAnnotations;

namespace FuelMeter.Core.DTOs;

public sealed class MeterScanRequest
{
    public Guid IdempotencyKey { get; init; }
    public const int MaxImageBytes = 3 * 1024 * 1024;

    [Required, MinLength(1), MaxLength(MaxImageBytes)]
    public byte[] Image { get; init; } = [];

    public static string? GetImageType(ReadOnlySpan<byte> image)
    {
        if (image.StartsWith(new byte[] { 0xff, 0xd8, 0xff }))
            return "image/jpeg";
        if (image.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
            return "image/png";
        return null;
    }
}

public sealed record MeterScanResult(string? FuelType, decimal? ReadingValue);
