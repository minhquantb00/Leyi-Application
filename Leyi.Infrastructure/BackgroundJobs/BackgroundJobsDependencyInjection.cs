using Hangfire;
using Hangfire.SqlServer;
using Leyi.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leyi.Infrastructure.BackgroundJobs;

public static class BackgroundJobsDependencyInjection
{
    /// <summary>
    /// Hangfire storage (SQL Server) and processing server. Called only by the Worker host:
    /// scheduled jobs run in Leyi.Worker, never in the API. No recurring jobs are registered yet.
    /// </summary>
    public static IServiceCollection AddBackgroundJobServer(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<HangfireOptions>()
            .Bind(configuration.GetSection(HangfireOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SchemaName), "Hangfire:SchemaName is required.")
            .Validate(options => options.WorkerCount is null or > 0, "Hangfire:WorkerCount must be positive.")
            .ValidateOnStart();

        services.AddHangfire((serviceProvider, hangfire) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.SqlServer;
            var options = serviceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value;

            hangfire
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    () => new SqlConnection(connectionString),
                    new SqlServerStorageOptions
                    {
                        SchemaName = options.SchemaName,
                        PrepareSchemaIfNecessary = options.PrepareSchemaIfNecessary,
                    });
        });

        services.AddHangfireServer((serviceProvider, server) =>
        {
            var workerCount = serviceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value.WorkerCount;
            if (workerCount is not null)
            {
                server.WorkerCount = workerCount.Value;
            }
        });

        return services;
    }
}
