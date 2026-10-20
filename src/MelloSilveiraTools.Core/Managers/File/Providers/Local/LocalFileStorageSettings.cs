namespace MelloSilveiraTools.Core.Managers.File.Providers.Local;

/// <summary>
/// Configuration settings governing internal folder layouts and root storage locations for the local file storage service.
/// </summary>
public sealed record LocalFileStorageSettings
{
    /// <summary>
    /// Gets the base directory or root storage path. Defaults to the system temporary directory.
    /// </summary>
    public string BaseDirectory { get; init; } = Path.GetTempPath();
}
