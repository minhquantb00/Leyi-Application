using System.Reflection;

namespace Leyi.Domain;

/// <summary>
/// Marker type used to locate the Leyi.Domain assembly (architecture tests, assembly scanning).
/// </summary>
public sealed class DomainAssembly
{
    private DomainAssembly()
    {
    }

    public static Assembly Assembly { get; } = typeof(DomainAssembly).Assembly;
}
