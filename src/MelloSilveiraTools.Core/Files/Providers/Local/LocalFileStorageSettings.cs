namespace MelloSilveiraTools.Core.Files.Providers.Local;

/// <summary>
/// Configuration settings governing internal folder layouts and root storage locations for the local file storage service.
/// Supports leading <c>~</c> (expanded to the current user's profile directory) and environment variable placeholders.
/// </summary>
public sealed record LocalFileStorageSettings
{
    private readonly string _baseDirectory = string.Empty;

    /// <summary>
    /// Gets the base directory or root storage path.
    /// </summary>
    public required string BaseDirectory
    {
        get => _baseDirectory;
        init => _baseDirectory = ResolvePath(value);
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        string expanded = Environment.ExpandEnvironmentVariables(path);
        if (expanded == "~")
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (expanded.StartsWith("~/", StringComparison.Ordinal) || expanded.StartsWith("~\\", StringComparison.Ordinal))
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, expanded[2..]);
        }

        return expanded;
    }
}
