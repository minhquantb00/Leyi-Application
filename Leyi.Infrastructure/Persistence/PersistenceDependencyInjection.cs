using Leyi.Application.Abstractions.Persistence;
using Leyi.Infrastructure.Configuration;
using Leyi.Infrastructure.Persistence.Interceptors;
using Leyi.Infrastructure.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leyi.Infrastructure.Persistence;

internal static class PersistenceDependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddSingleton<TimestampInterceptor>();

        services.AddDbContext<LeyiDbContext>((serviceProvider, options) =>
        {
            var connectionStrings = serviceProvider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value;

            options.UseSqlServer(connectionStrings.SqlServer, ConfigureSqlServer)
                .AddInterceptors(serviceProvider.GetRequiredService<TimestampInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sqlServer)
    {
        sqlServer.MigrationsAssembly(typeof(LeyiDbContext).Assembly.GetName().Name);
        sqlServer.EnableRetryOnFailure();
    }
}
