using System.Collections.Concurrent;
using IoTSensorTelemetryService.Models;
using IoTSensorTelemetryService.Storage;

namespace IoTSensorTelemetryService.Services;

// Computing a KPI here is just reading three running totals per sensor type
// (see InMemoryTelemetryStore) and dividing — O(1) per sensor type, independent
// of how many readings were ingested. The computed result is cached separately
// from the live aggregates so a fetch always returns a stable, previously
// "finalized" snapshot rather than numbers that could still be changing.
public sealed class KpiService(ITelemetryStore store, ILogger<KpiService> logger) : IKpiService
{
    private readonly ConcurrentDictionary<DateOnly, IReadOnlyList<DailyKpi>> _computedByDate = new();

    public IReadOnlyList<DailyKpi> ComputeForDate(DateOnly date)
    {
        var results = new List<DailyKpi>();

        foreach (var sensorType in Enum.GetValues<SensorType>())
        {
            if (!store.TryGetAggregate(date, sensorType, out var snapshot) || snapshot.Count == 0)
            {
                continue;
            }

            var average = snapshot.Sum / snapshot.Count;
            results.Add(new DailyKpi(date, sensorType, (int)snapshot.Count, (int)snapshot.HighValueCount, average));
        }

        _computedByDate[date] = results;
        logger.LogInformation(
            "Computed KPIs for {Date}: {SensorTypeCount} sensor type(s) with data", date, results.Count);
        return results;
    }

    public IReadOnlyList<DailyKpi>? GetComputedForDate(DateOnly date, SensorType? sensorTypeFilter)
    {
        if (!_computedByDate.TryGetValue(date, out var kpis))
        {
            return null;
        }

        return sensorTypeFilter is null
            ? kpis
            : kpis.Where(k => k.SensorType == sensorTypeFilter).ToList();
    }
}
