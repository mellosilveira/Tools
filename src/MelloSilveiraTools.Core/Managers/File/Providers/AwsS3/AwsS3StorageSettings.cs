using MelloSilveiraTools.Core.ResiliencePipelines;

namespace MelloSilveiraTools.Core.Managers.File.Providers.AwsS3;

/// <summary>
/// Configuration settings governing bucket names, regions, credentials, and virtual folder layouts for Amazon Simple Storage Service (AWS S3).
/// </summary>
public sealed record AwsS3StorageSettings
{
    /// <summary>
    /// Gets the target Amazon S3 bucket name.
    /// </summary>
    public required string BucketName { get; init; }

    /// <summary>
    /// Gets the AWS region endpoint.
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    /// Gets the optional AWS access key identifier.
    /// </summary>
    public string? AccessKey { get; init; }

    /// <summary>
    /// Gets the optional AWS secret access key.
    /// </summary>
    public string? SecretKey { get; init; }

    /// <summary>
    /// Gets the optional custom service URL for S3-compatible object storage providers (e.g., MinIO, LocalStack).
    /// </summary>
    public string? ServiceUrl { get; init; }

    /// <summary>
    /// Gets an optional base prefix or virtual folder hierarchy within the bucket.
    /// </summary>
    public string? BasePrefix { get; init; }

    /// <summary>
    /// Gets the optional resilience pipeline settings governing retries, backoff, and circuit breaker for AWS S3 operations.
    /// </summary>
    public ResiliencePipelineSettings? ResiliencePipelineSettings { get; init; }
}
