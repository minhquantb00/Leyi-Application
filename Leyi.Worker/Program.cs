using Leyi.Application;
using Leyi.Infrastructure;
using Leyi.Infrastructure.BackgroundJobs;
using Leyi.Infrastructure.Observability;

const string serviceName = "leyi-worker";

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddObservability(builder.Configuration, serviceName);
builder.Services.AddApplication();

// Message consumers are registered through the configureConsumers callback when the first
// business consumer exists. The Worker also hosts the Hangfire server (no recurring jobs yet).
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBackgroundJobServer(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
