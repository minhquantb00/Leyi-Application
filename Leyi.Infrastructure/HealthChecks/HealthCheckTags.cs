namespace Leyi.Infrastructure.HealthChecks;

/// <summary>
/// Tags used to group health checks. Checks tagged <see cref="Ready"/> are evaluated by <c>/health/ready</c>.
/// </summary>
public static class HealthCheckTags
{
    public const string Ready = "ready";
}
