namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// Configuration settings governing internal folder layouts and root storage locations for the file storage service.
/// </summary>
public sealed record FileStorageSettings
{
    /// <summary>
    /// Gets the base directory or root storage path (defaults to "storage").
    /// </summary>
    public string BaseDirectory { get; init; } = "storage";

    public static FileStorageSettings Default => new();
}
