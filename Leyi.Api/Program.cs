using Leyi.Api.Endpoints.Identity;
using Leyi.Api.Endpoints.Onboarding;
using Leyi.Api.Endpoints.Today;
using Leyi.Api.ErrorHandling;
using Leyi.Api.Observability;
using Leyi.Api.RateLimiting;
using Leyi.Application;
using Leyi.Infrastructure;
using Leyi.Infrastructure.HealthChecks;
using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.Observability;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

const string serviceName = "leyi-api";
const string healthPathPrefix = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservability(builder.Configuration, serviceName);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation(options =>
        options.Filter = context => !context.Request.Path.StartsWithSegments(healthPathPrefix)))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// The API issues access tokens: fail at startup when the JWT signing configuration is missing or too weak.
builder.Services.AddOptions<JwtOptions>().ValidateOnStart();

// Bearer access tokens (logout, logout-all and later personal endpoints). Endpoints opt in with RequireAuthorization;
// register/login/refresh stay anonymous. 401/403 have empty bodies and become ProblemDetails in UseStatusCodePages.
builder.Services.AddJwtBearerAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsCodes.Apply);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
    options.GetLevel = (context, _, exception) =>
        exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : context.Request.Path.StartsWithSegments(healthPathPrefix)
                ? LogEventLevel.Verbose
                : LogEventLevel.Information);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapOnboardingEndpoints();
app.MapTodayEndpoints();

// Liveness: the process is running. No dependency checks.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});

// Readiness: SQL Server, Redis and RabbitMQ (checks tagged "ready", registered by Infrastructure/MassTransit).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
});

app.Run();
