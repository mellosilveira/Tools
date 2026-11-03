using MelloSilveiraTools.Core.ResiliencePipelines;

namespace MelloSilveiraTools.Core.Managers.File.Providers.BlobStorage;

/// <summary>
/// Configuration settings governing connection parameters, container names, and virtual folder layouts for Azure Blob Storage.
/// </summary>
public sealed record BlobStorageSettings
{
    /// <summary>
    /// Gets the Azure Blob Storage connection string or account endpoint.
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Gets the target container name for storing files.
    /// </summary>
    public required string ContainerName { get; init; }

    /// <summary>
    /// Gets an optional base prefix or virtual folder hierarchy within the container.
    /// </summary>
    public string? BasePrefix { get; init; }

    /// <summary>
    /// Gets an optional custom service URL for local or Azure-compatible emulator endpoints (e.g. Azurite).
    /// </summary>
    public string? ServiceUrl { get; init; }

    /// <summary>
    /// Gets an optional local staging directory used for buffering uploads. If omitted, the default file storage service temporary location is used.
    /// </summary>
    public string? StagingDirectory { get; init; }

    /// <summary>
    /// Gets the optional resilience pipeline settings governing retries, backoff, and circuit breaker for Azure Blob Storage operations.
    /// </summary>
    public ResiliencePipelineSettings? ResiliencePipelineSettings { get; init; }
}
