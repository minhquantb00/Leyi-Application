using System.Reflection;

namespace Leyi.Infrastructure;

/// <summary>
/// Marker type used to locate the Leyi.Infrastructure assembly (architecture tests, assembly scanning).
/// </summary>
public sealed class InfrastructureAssembly
{
    private InfrastructureAssembly()
    {
    }

    public static Assembly Assembly { get; } = typeof(InfrastructureAssembly).Assembly;
}
