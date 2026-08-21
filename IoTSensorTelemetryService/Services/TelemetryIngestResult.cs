using IoTSensorTelemetryService.Models;

namespace IoTSensorTelemetryService.Services;

// Outcome of an ingest attempt. Either Event is populated (success) or Errors is
// (validation failure) — callers branch on Success rather than null-checking both.
public sealed record TelemetryIngestResult(bool Success, TelemetryEvent? Event, Dictionary<string, string[]>? Errors);
