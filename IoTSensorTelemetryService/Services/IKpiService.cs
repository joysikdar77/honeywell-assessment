using IoTSensorTelemetryService.Models;

namespace IoTSensorTelemetryService.Services;

public interface IKpiService
{
    // Finalizes KPIs for the given date from the current running aggregates and
    // caches the result so repeated fetches don't recompute it.
    IReadOnlyList<DailyKpi> ComputeForDate(DateOnly date);

    // Returns the cached KPIs for a date, or null if that date has never been computed.
    IReadOnlyList<DailyKpi>? GetComputedForDate(DateOnly date, SensorType? sensorTypeFilter);
}
