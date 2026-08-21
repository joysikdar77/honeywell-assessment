using IoTSensorTelemetryService.Models;
using IoTSensorTelemetryService.Storage;
using IoTSensorTelemetryService.Validation;

namespace IoTSensorTelemetryService.Services;

// Thin layer between the HTTP endpoints and the store: validates input and
// translates it into calls against the store. Keeping this separate from the
// endpoint handlers means the validation/storage rules can be unit tested
// without spinning up the web host.
public sealed class TelemetryService(ITelemetryStore store, ILogger<TelemetryService> logger) : ITelemetryService
{
    public TelemetryIngestResult Ingest(TelemetryIngestRequest request)
    {
        if (!TelemetryValidator.TryValidate(request, out var telemetryEvent, out var errors))
        {
            logger.LogWarning(
                "Rejected telemetry ingest for sensor {SensorId}: {Errors}",
                request.SensorId, string.Join("; ", errors.SelectMany(e => e.Value)));
            return new TelemetryIngestResult(false, null, errors);
        }

        var isHighValue = telemetryEvent!.Value > SensorThresholds.For(telemetryEvent.SensorType);
        store.Add(telemetryEvent, isHighValue);
        logger.LogDebug(
            "Ingested {SensorType} reading for sensor {SensorId} at {Timestamp}",
            telemetryEvent.SensorType, telemetryEvent.SensorId, telemetryEvent.Timestamp);
        return new TelemetryIngestResult(true, telemetryEvent, null);
    }

    public IReadOnlyList<TelemetryEvent> GetBySensor(string sensorId) => store.GetBySensor(sensorId);
}
