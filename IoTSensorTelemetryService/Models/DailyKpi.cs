namespace IoTSensorTelemetryService.Models;

// Computed KPI summary for one sensor type on one calendar day.
public sealed record DailyKpi(
    DateOnly Date,
    SensorType SensorType,
    int ReadingCount,
    int HighValueCount,
    double AverageValue
);
