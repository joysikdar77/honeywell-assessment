using System.Collections.Concurrent;
using IoTSensorTelemetryService.Models;

namespace IoTSensorTelemetryService.Storage;

// In-memory telemetry store. Two data structures are kept side by side, each
// optimized for the access pattern that needs it:
//
//  1. _readingsBySensor holds the raw events, grouped by sensor, so a fetch-by-sensor
//     request only touches that sensor's readings instead of scanning everything.
//
//  2. _dailyAggregates keeps a running (count, sum, highValueCount) per
//     (date, sensorType), updated incrementally as each reading arrives. KPI
//     computation then reads three numbers per sensor type instead of re-scanning
//     every stored reading for the day, so ingest stays O(1) and compute stays O(1)
//     per sensor type regardless of how much history has piled up.
public sealed class InMemoryTelemetryStore : ITelemetryStore
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TelemetryEvent>> _readingsBySensor = new();
    private readonly ConcurrentDictionary<(DateOnly Date, SensorType SensorType), KpiAccumulator> _dailyAggregates = new();

    public void Add(TelemetryEvent telemetryEvent, bool isHighValue)
    {
        var queue = _readingsBySensor.GetOrAdd(telemetryEvent.SensorId, _ => new ConcurrentQueue<TelemetryEvent>());
        queue.Enqueue(telemetryEvent);

        var date = DateOnly.FromDateTime(telemetryEvent.Timestamp);
        var key = (date, telemetryEvent.SensorType);
        var accumulator = _dailyAggregates.GetOrAdd(key, _ => new KpiAccumulator());
        accumulator.Add(telemetryEvent.Value, isHighValue);
    }

    public IReadOnlyList<TelemetryEvent> GetBySensor(string sensorId)
    {
        return _readingsBySensor.TryGetValue(sensorId, out var queue)
            ? queue.ToArray()
            : [];
    }

    public bool TryGetAggregate(DateOnly date, SensorType sensorType, out KpiAggregateSnapshot snapshot)
    {
        if (_dailyAggregates.TryGetValue((date, sensorType), out var accumulator))
        {
            snapshot = accumulator.Snapshot();
            return true;
        }

        snapshot = default;
        return false;
    }

    // Mutable running total for one (date, sensorType) bucket. Guarded by a lock
    // since concurrent ingests for the same day/sensor type are expected.
    private sealed class KpiAccumulator
    {
        private readonly object _gate = new();
        private long _count;
        private double _sum;
        private long _highValueCount;

        public void Add(double value, bool isHighValue)
        {
            lock (_gate)
            {
                _count++;
                _sum += value;
                if (isHighValue)
                {
                    _highValueCount++;
                }
            }
        }

        public KpiAggregateSnapshot Snapshot()
        {
            lock (_gate)
            {
                return new KpiAggregateSnapshot(_count, _sum, _highValueCount);
            }
        }
    }
}
