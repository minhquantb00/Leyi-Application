using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Leyi.Infrastructure.Observability;

public static class ObservabilityDependencyInjection
{
    private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// Structured logging (Serilog, configured from the <c>Serilog</c> section; trace/span ids are captured from
    /// the current Activity) and OpenTelemetry tracing/metrics (HTTP client + MassTransit).
    /// Telemetry is exported over OTLP only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is configured.
    /// Hosts add their own instrumentation (e.g. ASP.NET Core) on top.
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.AddSerilog((serviceProvider, logger) => logger
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(serviceProvider)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName));

        var openTelemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddSource(DiagnosticHeaders.DefaultListenerName)
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter(InstrumentationOptions.MeterName)
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(configuration[OtlpEndpointKey]))
        {
            openTelemetry.UseOtlpExporter();
        }

        return services;
    }
}
