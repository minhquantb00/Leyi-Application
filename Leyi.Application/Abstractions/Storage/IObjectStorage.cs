namespace Leyi.Application.Abstractions.Storage;

/// <summary>
/// Stores binary objects (audio, images, recordings) in S3-compatible object storage.
/// SQL Server keeps only metadata and the object key.
/// </summary>
public interface IObjectStorage
{
    Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
