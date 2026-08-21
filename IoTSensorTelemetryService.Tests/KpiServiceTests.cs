using IoTSensorTelemetryService.Models;
using IoTSensorTelemetryService.Services;
using IoTSensorTelemetryService.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace IoTSensorTelemetryService.Tests;

public class KpiServiceTests
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private readonly InMemoryTelemetryStore _store = new();
    private readonly TelemetryService _telemetryService;
    private readonly KpiService _kpiService;

    public KpiServiceTests()
    {
        _telemetryService = new TelemetryService(_store, NullLogger<TelemetryService>.Instance);
        _kpiService = new KpiService(_store, NullLogger<KpiService>.Instance);
    }

    [Fact]
    public void ComputeForDate_CalculatesAverageAndHighValueCount()
    {
        // Temperature threshold is 30 (see SensorThresholds): one reading below it,
        // one above it.
        Ingest("sensor-1", SensorType.Temperature, 25, Day);
        Ingest("sensor-1", SensorType.Temperature, 35, Day);

        var kpis = _kpiService.ComputeForDate(Day);

        var temperature = Assert.Single(kpis);
        Assert.Equal(SensorType.Temperature, temperature.SensorType);
        Assert.Equal(2, temperature.ReadingCount);
        Assert.Equal(1, temperature.HighValueCount);
        Assert.Equal(30, temperature.AverageValue);
    }

    [Fact]
    public void ComputeForDate_ValueEqualToThreshold_IsNotCountedAsHigh()
    {
        // Threshold comparison is strictly-greater-than, so a reading exactly at the
        // threshold should not be flagged as a high reading.
        Ingest("sensor-1", SensorType.Temperature, 30, Day);

        var kpis = _kpiService.ComputeForDate(Day);

        var temperature = Assert.Single(kpis);
        Assert.Equal(0, temperature.HighValueCount);
    }

    [Fact]
    public void ComputeForDate_ExcludesSensorTypesWithNoReadings()
    {
        Ingest("sensor-1", SensorType.Temperature, 25, Day);

        var kpis = _kpiService.ComputeForDate(Day);

        Assert.DoesNotContain(kpis, k => k.SensorType == SensorType.Humidity);
        Assert.DoesNotContain(kpis, k => k.SensorType == SensorType.Pressure);
    }

    [Fact]
    public void ComputeForDate_ComputesEachSensorTypeIndependently()
    {
        Ingest("sensor-1", SensorType.Temperature, 20, Day);
        Ingest("sensor-2", SensorType.Humidity, 80, Day);

        var kpis = _kpiService.ComputeForDate(Day);

        Assert.Equal(2, kpis.Count);

        var temperature = kpis.Single(k => k.SensorType == SensorType.Temperature);
        Assert.Equal(1, temperature.ReadingCount);
        Assert.Equal(0, temperature.HighValueCount);
        Assert.Equal(20, temperature.AverageValue);

        var humidity = kpis.Single(k => k.SensorType == SensorType.Humidity);
        Assert.Equal(1, humidity.ReadingCount);
        Assert.Equal(1, humidity.HighValueCount); // Humidity threshold is 70.
        Assert.Equal(80, humidity.AverageValue);
    }

    [Fact]
    public void ComputeForDate_OnlyIncludesReadingsFromTheGivenDate()
    {
        var otherDay = Day.AddDays(1);
        Ingest("sensor-1", SensorType.Temperature, 25, Day);
        Ingest("sensor-1", SensorType.Temperature, 99, otherDay);

        var kpis = _kpiService.ComputeForDate(Day);

        var temperature = Assert.Single(kpis);
        Assert.Equal(1, temperature.ReadingCount);
        Assert.Equal(25, temperature.AverageValue);
    }

    [Fact]
    public void GetComputedForDate_ReturnsNull_WhenDateHasNeverBeenComputed()
    {
        var result = _kpiService.GetComputedForDate(Day, sensorTypeFilter: null);

        Assert.Null(result);
    }

    [Fact]
    public void GetComputedForDate_ReturnsFrozenSnapshot_UnaffectedByLaterIngests()
    {
        Ingest("sensor-1", SensorType.Temperature, 25, Day);
        _kpiService.ComputeForDate(Day);

        // A reading that arrives after ComputeForDate should not retroactively change
        // the cached KPI snapshot.
        Ingest("sensor-1", SensorType.Temperature, 999, Day);

        var cached = _kpiService.GetComputedForDate(Day, sensorTypeFilter: null);

        var temperature = Assert.Single(cached!);
        Assert.Equal(1, temperature.ReadingCount);
        Assert.Equal(25, temperature.AverageValue);
    }

    [Fact]
    public void GetComputedForDate_FiltersBySensorType()
    {
        Ingest("sensor-1", SensorType.Temperature, 25, Day);
        Ingest("sensor-2", SensorType.Humidity, 40, Day);
        _kpiService.ComputeForDate(Day);

        var filtered = _kpiService.GetComputedForDate(Day, SensorType.Humidity);

        var humidity = Assert.Single(filtered!);
        Assert.Equal(SensorType.Humidity, humidity.SensorType);
    }

    private void Ingest(string sensorId, SensorType sensorType, double value, DateOnly date)
    {
        var request = new TelemetryIngestRequest
        {
            SensorId = sensorId,
            SensorType = sensorType.ToString(),
            Value = value,
            Timestamp = date.ToDateTime(TimeOnly.MinValue)
        };

        var result = _telemetryService.Ingest(request);
        Assert.True(result.Success, string.Join("; ", result.Errors?.SelectMany(e => e.Value) ?? []));
    }
}
