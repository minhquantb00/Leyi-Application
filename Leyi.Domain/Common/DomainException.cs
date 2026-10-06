namespace Leyi.Domain.Common;

/// <summary>
/// A business rule was violated. <see cref="Code"/> is a stable code from
/// <c>0.Document/05.API/ErrorCodeCatalog.md</c> (see <see cref="DomainErrorCodes"/>).
/// Programming errors (invalid arguments that no user action can produce) use <see cref="ArgumentException"/> instead.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException()
        : this(DomainErrorCodes.Conflict, "A business rule was violated.")
    {
    }

    public DomainException(string message)
        : this(DomainErrorCodes.Conflict, message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = DomainErrorCodes.Conflict;
    }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
