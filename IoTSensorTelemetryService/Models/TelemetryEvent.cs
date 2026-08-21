namespace IoTSensorTelemetryService.Models;

// A single sensor reading. Immutable record: once ingested, a reading never changes,
// so there's no need to guard it against mutation after it's stored.
public sealed record TelemetryEvent(
    string SensorId,
    SensorType SensorType,
    double Value,
    DateTime Timestamp
);
