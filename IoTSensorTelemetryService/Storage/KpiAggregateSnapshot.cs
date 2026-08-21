namespace IoTSensorTelemetryService.Storage;

// Point-in-time read of a running aggregate. A value type so returning one from
// the store never allocates or exposes a reference into mutable internal state.
public readonly record struct KpiAggregateSnapshot(long Count, double Sum, long HighValueCount);
