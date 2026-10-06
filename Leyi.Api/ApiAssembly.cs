using System.Reflection;

namespace Leyi.Api;

/// <summary>
/// Marker type used to locate the Leyi.Api assembly (architecture tests, WebApplicationFactory entry point).
/// </summary>
public sealed class ApiAssembly
{
    private ApiAssembly()
    {
    }

    public static Assembly Assembly { get; } = typeof(ApiAssembly).Assembly;
}
