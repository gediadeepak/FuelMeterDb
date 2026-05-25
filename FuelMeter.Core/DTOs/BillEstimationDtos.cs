namespace FuelMeter.Core.DTOs;

public record BillEstimationRequestDto(
    string FuelType,
    DateTime StartDate,
    DateTime EndDate,
    decimal? CustomUnitRate = null,
    decimal? CustomStandingCharge = null
);

public record BillEstimationDto(
    string FuelType,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int DaysInPeriod,
    decimal StartReading,
    decimal EndReading,
    decimal UnitsConsumed,
    decimal UnitRate,
    decimal UnitsCost,
    decimal StandingCharge,
    decimal StandingChargeCost,
    decimal TotalEstimatedBill,
    decimal DailyAverageCost,
    List<ReadingPointDto> ReadingPoints
);

public record ReadingPointDto(
    DateTime ReadingDate,
    decimal ReadingValue
);

public record MonthlyBillSummaryDto(
    int Year,
    int Month,
    string FuelType,
    decimal EstimatedBill,
    decimal UnitsConsumed,
    bool IsComplete,
    DateTime? LastReadingDate
);

public record BillComparisonDto(
    string FuelType,
    int CurrentYear,
    int CurrentMonth,
    decimal CurrentMonthBill,
    decimal PreviousMonthBill,
    decimal ChangeAmount,
    double ChangePercentage,
    string Trend  // "Up", "Down", "Same"
);
