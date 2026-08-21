namespace IoTSensorTelemetryService.Models;

// Wire-level shape of an incoming reading. Fields are nullable/string-typed on purpose:
// a missing or malformed field should surface as a validation error, not a JSON
// deserialization failure that the client can't easily interpret.
public sealed class TelemetryIngestRequest
{
    public string? SensorId { get; set; }
    public string? SensorType { get; set; }
    public double? Value { get; set; }
    public DateTime? Timestamp { get; set; }
}
