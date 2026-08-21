using IoTSensorTelemetryService.Models;

namespace IoTSensorTelemetryService.Validation;

// Validates query parameters for the KPI endpoints. Kept alongside TelemetryValidator
// so all input validation follows the same convention: parsed here, not inline in the
// endpoint handlers.
public static class KpiRequestValidator
{
    public static bool TryValidateDate(string date, out DateOnly parsedDate, out Dictionary<string, string[]> errors)
    {
        if (DateOnly.TryParse(date, out parsedDate))
        {
            errors = [];
            return true;
        }

        parsedDate = default;
        errors = new Dictionary<string, string[]>
        {
            ["date"] = ["date is required and must be a valid date (yyyy-MM-dd)."]
        };
        return false;
    }

    public static bool TryValidateSensorTypeFilter(
        string? sensorType,
        out SensorType? parsed,
        out Dictionary<string, string[]> errors)
    {
        parsed = null;
        errors = [];

        if (string.IsNullOrWhiteSpace(sensorType))
        {
            return true;
        }

        if (!Enum.TryParse<SensorType>(sensorType, ignoreCase: true, out var parsedType) || !Enum.IsDefined(parsedType))
        {
            errors = new Dictionary<string, string[]>
            {
                ["sensorType"] = ["sensorType must be one of: Temperature, Humidity, Pressure."]
            };
            return false;
        }

        parsed = parsedType;
        return true;
    }
}
