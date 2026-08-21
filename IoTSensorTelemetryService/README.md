# IoT Sensor Telemetry Service

A REST API for ingesting IoT sensor readings (Temperature, Humidity, Pressure),
storing them in memory, and computing daily KPI summaries per sensor type.

## Project layout

```
Models/       Data shapes: TelemetryEvent, DailyKpi, SensorType, request DTOs, thresholds
Storage/      ITelemetryStore + in-memory implementation (raw readings + running KPI aggregates)
Services/     Business logic: validation orchestration, ingestion, KPI computation/caching
Endpoints/    Minimal API route handlers, grouped by resource
Validation/   Request validation rules
```

Requests flow `Endpoint -> Service -> Store`. Endpoints only translate HTTP <-> C#
types and pick status codes; all business rules live in `Services`/`Validation`.

## Design notes

- **KPI computation is O(1) per sensor type, not O(n) over history.** Each ingest
  updates a running `(count, sum, highValueCount)` accumulator for that reading's
  `(date, sensorType)` bucket. "Compute KPIs for a date" just reads those three
  numbers per sensor type and divides — it never re-scans stored events, so cost
  stays flat no matter how much telemetry has piled up.
- **Raw readings are grouped by sensor ID** (`ConcurrentDictionary<string, ConcurrentQueue<TelemetryEvent>>`)
  so fetching one sensor's history only touches that sensor's data.
- **Computed KPIs are cached separately from the live aggregates.** A fetch
  always returns a stable, previously finalized snapshot rather than numbers
  that could still be changing from concurrent ingestion.
- All shared state uses lock-free or narrowly-locked concurrent structures
  (`ConcurrentDictionary`, `ConcurrentQueue`, a small per-bucket lock for the
  accumulator) since multiple sensors can post concurrently.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (this repo was built/tested against `10.0.400`)

There is no `.sln` file, so commands below are run from inside the
`IoTSensorTelemetryService/` project directory unless noted otherwise.

## Setup

Clone/copy the repo, then restore dependencies from the project directory:

```bash
cd IoTSensorTelemetryService
dotnet restore
```

## Running

```bash
cd IoTSensorTelemetryService
dotnet run
```

This uses the `http` launch profile in `Properties/launchSettings.json`,
which listens on **`http://localhost:5156`**. All examples below assume
that URL — adjust if your console output shows a different port.

To use a different port instead:

```bash
ASPNETCORE_URLS="http://localhost:5299" dotnet run
```

In `Development` (the default when run this way), Swagger UI is available
at [`http://localhost:5156/swagger`](http://localhost:5156/swagger) for
interactively exploring/calling the API.

## Running the tests

From the repository root (or from `IoTSensorTelemetryService.Tests/`):

```bash
dotnet test IoTSensorTelemetryService.Tests
```

Coverage is currently `KpiServiceTests.cs` (8 tests), exercising `KpiService`
directly: average/high-value-count computation against the sensor
thresholds, threshold boundary (equal-to-threshold is not "high"),
per-sensor-type isolation, date filtering, the "no KPIs computed yet"
(`null`) case, snapshot immutability against later ingests, and
`sensorType` filtering on fetch.

## API reference

### Ingest a reading
`POST /api/telemetry`

```bash
curl -X POST http://localhost:5156/api/telemetry \
  -H "Content-Type: application/json" \
  -d '{"sensorId":"sensor-1","sensorType":"Temperature","value":35.5,"timestamp":"2026-08-21T10:00:00"}'
```

Returns `201 Created` with the stored reading:

```json
{
  "sensorId": "sensor-1",
  "sensorType": "Temperature",
  "value": 35.5,
  "timestamp": "2026-08-21T10:00:00"
}
```

or `400` with field-level validation errors if `sensorId` is missing,
`sensorType` isn't one of `Temperature`/`Humidity`/`Pressure`, `value` isn't
a finite number, or `timestamp` is missing/invalid.

### Fetch a sensor's readings
`GET /api/telemetry/{sensorId}`

```bash
curl http://localhost:5156/api/telemetry/sensor-1
```

Returns `200` with an array of readings (empty array if the sensor has none):

```json
[
  {
    "sensorId": "sensor-1",
    "sensorType": "Temperature",
    "value": 35.5,
    "timestamp": "2026-08-21T10:00:00"
  }
]
```

### Compute daily KPIs
`POST /api/kpis/compute?date=yyyy-MM-dd`

```bash
curl -X POST "http://localhost:5156/api/kpis/compute?date=2026-08-21"
```

Finalizes and caches KPIs for that date from the current data. Returns `200`
with the computed KPIs (one entry per sensor type that had at least one
reading that day):

```json
[
  {
    "date": "2026-08-21",
    "sensorType": "Temperature",
    "readingCount": 12,
    "highValueCount": 3,
    "averageValue": 28.4
  }
]
```

Safe to call again later to refresh the cache as more data arrives.

### Fetch computed KPIs
`GET /api/kpis?date=yyyy-MM-dd&sensorType=Temperature` (sensorType is optional)

```bash
curl "http://localhost:5156/api/kpis?date=2026-08-21"
curl "http://localhost:5156/api/kpis?date=2026-08-21&sensorType=Pressure"
```

Returns `200` with the cached KPIs (same shape as above), or `404` if that
date hasn't been computed yet:

```json
{
  "message": "No KPIs have been computed for 2026-08-21. Trigger POST /api/kpis/compute?date=2026-08-21 first."
}
```

## KPI thresholds

| Sensor Type | High-value threshold |
|---|---|
| Temperature | > 30 |
| Humidity | > 70 |
| Pressure | > 1000 |

Each `DailyKpi` reports `readingCount`, `highValueCount`, and `averageValue`
for the given date and sensor type.
