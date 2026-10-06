namespace Leyi.Application.Abstractions.Results;

/// <summary>
/// Outcome of a use case: either a value or an expected <see cref="Results.Error"/>.
/// Unexpected failures are exceptions and are handled by the API's global exception handler.
/// </summary>
public sealed class Result<TValue>
{
    private readonly TValue? value;
    private readonly Error? error;

    private Result(TValue value)
    {
        this.value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        this.error = error;
    }

    public bool IsSuccess { get; }

    public TValue Value => IsSuccess ? value! : throw new InvalidOperationException("A failed result has no value.");

    public Error Error => IsSuccess ? throw new InvalidOperationException("A successful result has no error.") : error!;

    public static Result<TValue> Success(TValue value) => new(value);

    public static Result<TValue> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>(error);
    }

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
