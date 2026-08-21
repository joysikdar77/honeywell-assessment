using IoTSensorTelemetryService.Models;

namespace IoTSensorTelemetryService.Services;

public interface ITelemetryService
{
    TelemetryIngestResult Ingest(TelemetryIngestRequest request);

    IReadOnlyList<TelemetryEvent> GetBySensor(string sensorId);
}
