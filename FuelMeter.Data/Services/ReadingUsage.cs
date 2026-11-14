using FuelMeter.Core.Models;

namespace FuelMeter.Data.Services;

internal static class ReadingUsage
{
    public static decimal Between(decimal previous, decimal current) => Math.Max(0, current - previous);

    public static decimal Total(IEnumerable<MeterReading> readings, decimal previous)
    {
        decimal total = 0;
        foreach (var reading in readings)
        {
            total += Between(previous, reading.ReadingValue);
            previous = reading.ReadingValue;
        }

        return total;
    }
}
