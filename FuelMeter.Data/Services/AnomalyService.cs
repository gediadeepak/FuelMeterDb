using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public class AnomalyService(FuelMeterDbContext db) : IAnomalyService
{
    public async Task<AnomalyResultDto> DetectAnomaliesAsync(
        int userId,
        string? fuelType = null,
        int daysBack = 60,
        decimal thresholdPercent = 50m)
    {
        var result = new AnomalyResultDto
        {
            ThresholdPercent = thresholdPercent,
            DaysAnalyzed = daysBack
        };

        var fromDate = DateTime.Today.AddDays(-daysBack - 30); // extra 30 days for rolling baseline

        var fuels = string.IsNullOrEmpty(fuelType)
            ? new[] { "Electricity", "Gas" }
            : new[] { fuelType };

        foreach (var fuel in fuels)
        {
            var readings = await db.MeterReadings
                .AsNoTracking()
                .Where(r => r.UserId == userId
                            && r.FuelType == fuel
                            && r.ReadingDate >= fromDate)
                .OrderBy(r => r.ReadingDate)
                .ToListAsync();

            if (readings.Count < 2) continue;

            // Compute per-day usage by spreading consumption across days between consecutive readings
            var dailyUsage = new List<(DateTime Date, decimal Usage)>();
            for (int i = 1; i < readings.Count; i++)
            {
                var prev = readings[i - 1];
                var curr = readings[i];
                var diffDays = (curr.ReadingDate.Date - prev.ReadingDate.Date).Days;
                if (diffDays <= 0) continue;

                var totalUsage = curr.ReadingValue - prev.ReadingValue;
                if (totalUsage < 0) continue; // ignore meter resets

                var perDay = totalUsage / diffDays;
                for (int d = 1; d <= diffDays; d++)
                {
                    dailyUsage.Add((prev.ReadingDate.Date.AddDays(d), perDay));
                }
            }

            var analysisStart = DateTime.Today.AddDays(-daysBack);

            // For each day in the analysis window, compute rolling 30-day average of prior days
            foreach (var (date, usage) in dailyUsage.Where(d => d.Date >= analysisStart))
            {
                var windowStart = date.AddDays(-30);
                var prior = dailyUsage
                    .Where(d => d.Date >= windowStart && d.Date < date)
                    .Select(d => d.Usage)
                    .ToList();

                if (prior.Count < 7) continue; // not enough baseline data

                var avg = prior.Average();
                if (avg <= 0) continue;

                var pctAbove = ((usage - avg) / avg) * 100m;
                if (pctAbove >= thresholdPercent)
                {
                    result.Anomalies.Add(new AnomalyDto
                    {
                        Date = date,
                        FuelType = fuel,
                        DailyUsage = Math.Round(usage, 2),
                        RollingAverage = Math.Round(avg, 2),
                        PercentAboveAverage = Math.Round(pctAbove, 1),
                        Severity = pctAbove >= thresholdPercent * 2 ? "High" : "Warning"
                    });
                }
            }
        }

        result.Anomalies = result.Anomalies
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.PercentAboveAverage)
            .ToList();

        return result;
    }
}
