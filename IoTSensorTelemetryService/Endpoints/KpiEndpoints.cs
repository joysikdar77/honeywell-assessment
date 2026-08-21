using IoTSensorTelemetryService.Services;
using IoTSensorTelemetryService.Validation;

namespace IoTSensorTelemetryService.Endpoints;

public static class KpiEndpoints
{
    public static RouteGroupBuilder MapKpiEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/compute", ComputeKpis);
        group.MapGet("/", GetKpis);
        return group;
    }

    private static IResult ComputeKpis(string date, IKpiService kpiService)
    {
        if (!KpiRequestValidator.TryValidateDate(date, out var parsedDate, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var kpis = kpiService.ComputeForDate(parsedDate);
        return Results.Ok(kpis);
    }

    private static IResult GetKpis(string date, string? sensorType, IKpiService kpiService)
    {
        if (!KpiRequestValidator.TryValidateDate(date, out var parsedDate, out var dateErrors))
        {
            return Results.ValidationProblem(dateErrors);
        }

        if (!KpiRequestValidator.TryValidateSensorTypeFilter(sensorType, out var filter, out var filterErrors))
        {
            return Results.ValidationProblem(filterErrors);
        }

        var kpis = kpiService.GetComputedForDate(parsedDate, filter);
        if (kpis is null)
        {
            return Results.NotFound(new
            {
                message = $"No KPIs have been computed for {parsedDate:yyyy-MM-dd}. " +
                           $"Trigger POST /api/kpis/compute?date={parsedDate:yyyy-MM-dd} first."
            });
        }

        return Results.Ok(kpis);
    }
}
