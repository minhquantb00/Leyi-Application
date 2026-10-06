using System.Reflection;

namespace Leyi.Worker;

/// <summary>
/// Marker type used to locate the Leyi.Worker assembly (architecture tests).
/// </summary>
public sealed class WorkerAssembly
{
    private WorkerAssembly()
    {
    }

    public static Assembly Assembly { get; } = typeof(WorkerAssembly).Assembly;
}
