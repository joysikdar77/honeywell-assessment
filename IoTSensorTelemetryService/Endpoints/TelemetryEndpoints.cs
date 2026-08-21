using IoTSensorTelemetryService.Models;
using IoTSensorTelemetryService.Services;

namespace IoTSensorTelemetryService.Endpoints;

public static class TelemetryEndpoints
{
    public static RouteGroupBuilder MapTelemetryEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", IngestTelemetry);
        group.MapGet("/{sensorId}", GetBySensor);
        return group;
    }

    private static IResult IngestTelemetry(TelemetryIngestRequest request, ITelemetryService telemetryService)
    {
        var result = telemetryService.Ingest(request);
        if (!result.Success)
        {
            return Results.ValidationProblem(result.Errors!);
        }

        var reading = result.Event!;
        return Results.Created($"/api/telemetry/{reading.SensorId}", reading);
    }

    private static IResult GetBySensor(string sensorId, ITelemetryService telemetryService)
    {
        var readings = telemetryService.GetBySensor(sensorId);
        return Results.Ok(readings);
    }
}
