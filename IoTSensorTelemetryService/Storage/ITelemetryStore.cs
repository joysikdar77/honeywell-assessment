using IoTSensorTelemetryService.Models;

namespace IoTSensorTelemetryService.Storage;

public interface ITelemetryStore
{
    // isHighValue is the caller's business-rule decision (see SensorThresholds) —
    // the store only counts, it doesn't decide what "high" means.
    void Add(TelemetryEvent telemetryEvent, bool isHighValue);

    IReadOnlyList<TelemetryEvent> GetBySensor(string sensorId);

    bool TryGetAggregate(DateOnly date, SensorType sensorType, out KpiAggregateSnapshot snapshot);
}
