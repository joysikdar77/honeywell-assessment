using System.Text.Json.Serialization;
using IoTSensorTelemetryService.Endpoints;
using IoTSensorTelemetryService.Services;
using IoTSensorTelemetryService.Storage;

var builder = WebApplication.CreateBuilder(args);

// Serialize enums (SensorType) as their names rather than numeric values so API
// responses stay self-explanatory without a lookup table.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Singleton lifetime: the store and services hold no per-request state, and the
// in-memory data needs to survive across requests for the lifetime of the process.
builder.Services.AddSingleton<ITelemetryStore, InMemoryTelemetryStore>();
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
builder.Services.AddSingleton<IKpiService, KpiService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        app.Logger.LogError(exception,
            "Unhandled exception while processing {Method} {Path}",
            context.Request.Method, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem("An unexpected error occurred while processing the request.")
            .ExecuteAsync(context);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new { service = "IoT Sensor Telemetry Service", status = "running" }));

app.MapGroup("/api/telemetry").MapTelemetryEndpoints();
app.MapGroup("/api/kpis").MapKpiEndpoints();

app.Run();
