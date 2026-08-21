namespace IoTSensorTelemetryService.Models;

// Fixed "high reading" thresholds per sensor type, used by the KPI aggregation.
// A dictionary keeps this in one place instead of scattering magic numbers
// through the ingestion/aggregation code.
public static class SensorThresholds
{
    private static readonly Dictionary<SensorType, double> Values = new()
    {
        [SensorType.Temperature] = 30,
        [SensorType.Humidity] = 70,
        [SensorType.Pressure] = 1000
    };

    public static double For(SensorType sensorType) => Values[sensorType];
}
