namespace MelloSilveiraTools.Core.Managers.File.Providers.Local;

/// <summary>
/// Configuration settings governing internal folder layouts and root storage locations for the local file storage service.
/// </summary>
public sealed record LocalFileStorageSettings
{
    /// <summary>
    /// Gets the base directory or root storage path.
    /// </summary>
    public required string BaseDirectory { get; init; }
}
