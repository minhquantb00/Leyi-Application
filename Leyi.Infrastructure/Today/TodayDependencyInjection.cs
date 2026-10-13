using Leyi.Application.Features.Today.GetToday;
using Microsoft.Extensions.DependencyInjection;

namespace Leyi.Infrastructure.Today;

internal static class TodayDependencyInjection
{
    /// <summary>Adapters of the Today read model (GetToday).</summary>
    public static IServiceCollection AddToday(this IServiceCollection services)
    {
        services.AddScoped<ITodayReadStore, TodayReadStore>();
        return services;
    }
}
