namespace IoTSensorTelemetryService.Models;

// The three sensor kinds this service accepts. Kept as an enum (not a free-text
// string) so invalid types are rejected at parse time instead of leaking into storage.
public enum SensorType
{
    Temperature,
    Humidity,
    Pressure
}
