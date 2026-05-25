using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data.Services;

public class BillEstimationService : IBillEstimationService
{
    private readonly FuelMeterDbContext _context;
    private readonly ISettingsService _settingsService;

    public BillEstimationService(FuelMeterDbContext context, ISettingsService settingsService)
    {
        _context = context;
        _settingsService = settingsService;
    }

    public async Task<BillEstimationDto?> EstimateBillAsync(int userId, BillEstimationRequestDto request)
    {
        var readings = await _context.MeterReadings
            .Where(r => r.UserId == userId && r.FuelType == request.FuelType)
            .Where(r => r.ReadingDate >= request.StartDate && r.ReadingDate <= request.EndDate)
            .OrderBy(r => r.ReadingDate)
            .ToListAsync();

        if (readings.Count < 2)
            return null;

        var startReading = readings.First();
        var endReading = readings.Last();
        var unitsConsumed = endReading.ReadingValue - startReading.ReadingValue;

        if (unitsConsumed < 0)
            return null;

        // Get rates
        decimal unitRate;
        decimal standingCharge;

        if (request.CustomUnitRate.HasValue && request.CustomStandingCharge.HasValue)
        {
            unitRate = request.CustomUnitRate.Value;
            standingCharge = request.CustomStandingCharge.Value;
        }
        else
        {
            var settings = await _settingsService.GetSettingsAsync(userId);
            if (settings == null)
                return null;

            unitRate = request.FuelType == "Electricity"
                ? settings.ElectricityPricePerKwh
                : settings.GasPricePerUnit;

            standingCharge = request.FuelType == "Electricity"
                ? settings.ElectricityStandingCharge
                : settings.GasStandingCharge;
        }

        // Calculate costs
        var daysInPeriod = (int)(request.EndDate - request.StartDate).TotalDays + 1;
        var unitsCost = unitsConsumed * unitRate;
        // Standing charge is stored as pence/day — convert to currency units to match
        // the Dashboard formula: (standingCharge p/day ÷ 100) × days in period.
        var standingChargeCost = standingCharge / 100m * daysInPeriod;
        var totalBill = unitsCost + standingChargeCost;
        var dailyAverage = daysInPeriod > 0 ? totalBill / daysInPeriod : 0;

        var readingPoints = readings.Select(r => new ReadingPointDto(r.ReadingDate, r.ReadingValue)).ToList();

        return new BillEstimationDto(
            request.FuelType,
            request.StartDate,
            request.EndDate,
            daysInPeriod,
            startReading.ReadingValue,
            endReading.ReadingValue,
            unitsConsumed,
            unitRate,
            unitsCost,
            standingCharge,
            standingChargeCost,
            totalBill,
            dailyAverage,
            readingPoints
        );
    }

    public async Task<BillEstimationDto?> EstimateCurrentMonthBillAsync(int userId, string fuelType)
    {
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1);
        var endDate = now;

        var request = new BillEstimationRequestDto(fuelType, startDate, endDate);
        return await EstimateBillAsync(userId, request);
    }

    public async Task<List<MonthlyBillSummaryDto>> GetMonthlyBillSummariesAsync(int userId, int year)
    {
        var summaries = new List<MonthlyBillSummaryDto>();
        var fuelTypes = new[] { "Electricity", "Gas" };

        foreach (var fuelType in fuelTypes)
        {
            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var readings = await _context.MeterReadings
                    .Where(r => r.UserId == userId && r.FuelType == fuelType)
                    .Where(r => r.ReadingDate >= startDate && r.ReadingDate <= endDate)
                    .OrderBy(r => r.ReadingDate)
                    .ToListAsync();

                decimal estimatedBill = 0;
                decimal unitsConsumed = 0;
                bool isComplete = false;
                DateTime? lastReadingDate = null;

                if (readings.Count >= 2)
                {
                    var request = new BillEstimationRequestDto(fuelType, startDate, endDate);
                    var estimation = await EstimateBillAsync(userId, request);

                    if (estimation != null)
                    {
                        estimatedBill = estimation.TotalEstimatedBill;
                        unitsConsumed = estimation.UnitsConsumed;
                        isComplete = true;
                        lastReadingDate = readings.Last().ReadingDate;
                    }
                }

                summaries.Add(new MonthlyBillSummaryDto(
                    year,
                    month,
                    fuelType,
                    estimatedBill,
                    unitsConsumed,
                    isComplete,
                    lastReadingDate
                ));
            }
        }

        return summaries;
    }

    public async Task<BillComparisonDto?> CompareBillsAsync(int userId, string fuelType, int year, int month)
    {
        var currentMonthStart = new DateTime(year, month, 1);
        var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var previousMonthEnd = currentMonthStart.AddDays(-1);

        var currentRequest = new BillEstimationRequestDto(fuelType, currentMonthStart, currentMonthEnd);
        var previousRequest = new BillEstimationRequestDto(fuelType, previousMonthStart, previousMonthEnd);

        var currentBill = await EstimateBillAsync(userId, currentRequest);
        var previousBill = await EstimateBillAsync(userId, previousRequest);

        if (currentBill == null)
            return null;

        var previousMonthBill = previousBill?.TotalEstimatedBill ?? 0;
        var changeAmount = currentBill.TotalEstimatedBill - previousMonthBill;
        var changePercentage = previousMonthBill > 0
            ? (double)(changeAmount / previousMonthBill * 100)
            : 0;

        // If there is no previous-month data, flag as "New" so the UI can avoid
        // the misleading "↑ £X (0.0%)" rendering.
        string trend = previousBill == null
            ? "New"
            : changeAmount > 0 ? "Up" : changeAmount < 0 ? "Down" : "Same";

        return new BillComparisonDto(
            fuelType,
            year,
            month,
            currentBill.TotalEstimatedBill,
            previousMonthBill,
            changeAmount,
            changePercentage,
            trend
        );
    }

    public async Task<List<MonthlyBillSummaryDto>> GetBillHistoryAsync(int userId, string fuelType, int months = 12)
    {
        var summaries = new List<MonthlyBillSummaryDto>();
        var currentDate = DateTime.UtcNow;

        for (int i = 0; i < months; i++)
        {
            var targetDate = currentDate.AddMonths(-i);
            var year = targetDate.Year;
            var month = targetDate.Month;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var readings = await _context.MeterReadings
                .Where(r => r.UserId == userId && r.FuelType == fuelType)
                .Where(r => r.ReadingDate >= startDate && r.ReadingDate <= endDate)
                .OrderBy(r => r.ReadingDate)
                .ToListAsync();

            decimal estimatedBill = 0;
            decimal unitsConsumed = 0;
            bool isComplete = false;
            DateTime? lastReadingDate = null;

            if (readings.Count >= 2)
            {
                var request = new BillEstimationRequestDto(fuelType, startDate, endDate);
                var estimation = await EstimateBillAsync(userId, request);

                if (estimation != null)
                {
                    estimatedBill = estimation.TotalEstimatedBill;
                    unitsConsumed = estimation.UnitsConsumed;
                    isComplete = true;
                    lastReadingDate = readings.Last().ReadingDate;
                }
            }

            summaries.Add(new MonthlyBillSummaryDto(
                year,
                month,
                fuelType,
                estimatedBill,
                unitsConsumed,
                isComplete,
                lastReadingDate
            ));
        }

        return summaries.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ToList();
    }

    public async Task<decimal> ProjectMonthlyBillAsync(int userId, string fuelType, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var today = DateTime.UtcNow.Date;

        // If month hasn't started yet
        if (startDate > today)
            return 0;

        var daysElapsed = (today - startDate).Days + 1;
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var request = new BillEstimationRequestDto(fuelType, startDate, today);
        var currentEstimation = await EstimateBillAsync(userId, request);

        if (currentEstimation == null || daysElapsed == 0)
            return 0;

        // Project for the full month
        var dailyAverage = currentEstimation.TotalEstimatedBill / daysElapsed;
        var projectedBill = dailyAverage * daysInMonth;

        return projectedBill;
    }

    public async Task<decimal> CalculateDailyAverageCostAsync(int userId, string fuelType, DateTime startDate, DateTime endDate)
    {
        var request = new BillEstimationRequestDto(fuelType, startDate, endDate);
        var estimation = await EstimateBillAsync(userId, request);

        return estimation?.DailyAverageCost ?? 0;
    }
}
