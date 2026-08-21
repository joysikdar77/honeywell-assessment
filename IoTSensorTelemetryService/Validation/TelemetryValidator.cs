using IoTSensorTelemetryService.Models;

namespace IoTSensorTelemetryService.Validation;

public static class TelemetryValidator
{
    // Validates a raw ingest request and, on success, produces the strongly-typed
    // event. Returns field-level error messages keyed by field name so the caller
    // can build a standard ValidationProblemDetails response.
    public static bool TryValidate(
        TelemetryIngestRequest request,
        out TelemetryEvent? telemetryEvent,
        out Dictionary<string, string[]> errors)
    {
        var fieldErrors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.SensorId))
        {
            AddError(fieldErrors, nameof(request.SensorId), "sensorId is required.");
        }

        SensorType? parsedSensorType = null;
        if (string.IsNullOrWhiteSpace(request.SensorType))
        {
            AddError(fieldErrors, nameof(request.SensorType), "sensorType is required.");
        }
        else if (!Enum.TryParse<SensorType>(request.SensorType, ignoreCase: true, out var sensorType)
                 || !Enum.IsDefined(sensorType))
        {
            AddError(fieldErrors, nameof(request.SensorType),
                "sensorType must be one of: Temperature, Humidity, Pressure.");
        }
        else
        {
            parsedSensorType = sensorType;
        }

        if (request.Value is null || double.IsNaN(request.Value.Value) || double.IsInfinity(request.Value.Value))
        {
            AddError(fieldErrors, nameof(request.Value), "value is required and must be a finite number.");
        }

        if (request.Timestamp is null || request.Timestamp.Value == default)
        {
            AddError(fieldErrors, nameof(request.Timestamp), "timestamp is required and must be a valid date-time.");
        }

        errors = fieldErrors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());

        if (errors.Count > 0)
        {
            telemetryEvent = null;
            return false;
        }

        telemetryEvent = new TelemetryEvent(
            request.SensorId!.Trim(),
            parsedSensorType!.Value,
            request.Value!.Value,
            request.Timestamp!.Value);
        return true;
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = [];
            errors[field] = list;
        }

        list.Add(message);
    }
}
