namespace MelloSilveiraTools.Core.Files.Providers;

/// <summary>
/// Abstract base implementation for cloud object storage providers (such as Azure Blob Storage and AWS S3),
/// providing forward-slash virtual directory prefix construction and cloud object key path extraction.
/// </summary>
/// <param name="timeProvider">The time provider instance used for temporal operations.</param>
/// <param name="basePrefix">Mandatory root virtual prefix configured for the storage container or bucket.</param>
public abstract class CloudFileStorageServiceBase(
    TimeProvider timeProvider,
    string basePrefix)
    : FileStorageServiceBase(timeProvider)
{
    private readonly string _normalizedBasePrefix = basePrefix.Trim('/');

    /// <inheritdoc />
    public override string ResolveLocation(string identifier, string context, string fileName) => BuildDirectoryPrefix(identifier, context) + fileName;

    /// <inheritdoc />
    public override (string Identifier, string Context, string FileName) ParseLocation(string location)
    {
        string relative = location.TrimStart('/');
        if (!string.IsNullOrEmpty(_normalizedBasePrefix))
        {
            string requiredPrefix = $"{_normalizedBasePrefix}/";
            if (!relative.StartsWith(requiredPrefix, StringComparison.Ordinal))
                throw new FormatException($"Storage location '{location}' does not conform to the canonical layout for provider '{Provider}'.");

            relative = relative[requiredPrefix.Length..];
        }

        string[] segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3 || location.EndsWith('/'))
            throw new FormatException($"Storage location '{location}' does not conform to the canonical layout for provider '{Provider}'.");

        return (segments[0], segments[1], segments[2]);
    }

    /// <summary>
    /// Builds a standardized forward-slash delimited virtual directory prefix for the active cloud provider,
    /// applying the configured base prefix.
    /// </summary>
    /// <param name="identifier">The unique identifier.</param>
    /// <param name="context">The contextual domain or category.</param>
    /// <returns>A formatted directory prefix ending with a forward slash.</returns>
    protected string BuildDirectoryPrefix(string identifier, string context)
    {
        string[] parts =
        [
            _normalizedBasePrefix,
            identifier.Trim('/'),
            context.Trim('/')
        ];

        string joined = string.Join('/', parts.Where(part => !string.IsNullOrEmpty(part)));
        return string.IsNullOrEmpty(joined) ? string.Empty : $"{joined}/";
    }

    /// <summary>
    /// Deconstructs a cloud object key or URI into its forward-slash delimited virtual directory path and file name.
    /// Ensures consistent behavior across Windows and Unix platforms without backslash replacement.
    /// </summary>
    /// <param name="location">The cloud object key or URI.</param>
    /// <returns>A tuple containing the virtual directory prefix (without trailing slash) and the file name (or <c>null</c> if the key represents a directory or ends with a slash).</returns>
    protected static (string Directory, string? FileName) ExtractCloudPath(string location)
    {
        if (string.IsNullOrEmpty(location))
            return (string.Empty, null);

        if (location.EndsWith('/'))
        {
            string normalizedDir = location.TrimEnd('/');
            int slashIndex = normalizedDir.LastIndexOf('/');
            string parentDirectory = slashIndex >= 0 ? normalizedDir[..slashIndex] : string.Empty;
            return (parentDirectory, null);
        }

        int lastSlash = location.LastIndexOf('/');
        if (lastSlash < 0)
            return (string.Empty, location);

        string directory = location[..lastSlash];
        string fileName = location[(lastSlash + 1)..];
        return (directory, string.IsNullOrEmpty(fileName) ? null : fileName);
    }
}
