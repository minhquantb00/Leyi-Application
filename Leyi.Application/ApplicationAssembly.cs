using System.Reflection;

namespace Leyi.Application;

/// <summary>
/// Marker type used to locate the Leyi.Application assembly (architecture tests, assembly scanning).
/// </summary>
public sealed class ApplicationAssembly
{
    private ApplicationAssembly()
    {
    }

    public static Assembly Assembly { get; } = typeof(ApplicationAssembly).Assembly;
}
