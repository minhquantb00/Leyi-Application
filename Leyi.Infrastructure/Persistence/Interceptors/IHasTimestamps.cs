namespace Leyi.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Persistence models whose <c>CreatedAt</c>/<c>UpdatedAt</c> (UTC) are maintained by <see cref="TimestampInterceptor"/>.
/// </summary>
internal interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }
}
