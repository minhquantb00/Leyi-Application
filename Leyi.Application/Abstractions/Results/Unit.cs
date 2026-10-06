namespace Leyi.Application.Abstractions.Results;

/// <summary>Value of a successful <see cref="Result{TValue}"/> for a use case that returns nothing (HTTP 204).</summary>
public readonly record struct Unit
{
    public static Unit Value => default;
}
