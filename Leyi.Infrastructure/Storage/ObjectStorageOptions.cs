namespace Leyi.Infrastructure.Storage;

/// <summary>
/// Bound to the <c>ObjectStorage</c> section (MinIO in development, S3-compatible in production).
/// <see cref="SecretKey"/> must come from user-secrets or environment variables.
/// No <c>IObjectStorage</c> implementation exists yet; the SDK is added with the first feature that stores objects.
/// </summary>
public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public string Endpoint { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; } = true;
}
