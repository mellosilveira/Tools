namespace MelloSilveiraTools.Plugins.Infrastructure.Models;

/// <summary>
/// Represents a semantic version parsed from a plugin DLL filename.
/// </summary>
public readonly record struct PluginVersion(int Major, int Minor, int Patch) : IComparable<PluginVersion>
{
    /// <summary>
    /// String representation in the form <c>v{major}.{minor}.{patch}</c>.
    /// </summary>
    public string Name { get; } = $"v{Major}.{Minor}.{Patch}";

    /// <summary>
    /// The default "v0.0.0" version.
    /// </summary>
    public static PluginVersion Default => new(0, 0, 0);

    /// <summary>
    /// Compares this version to <paramref name="other"/> using major, then minor, then patch.
    /// </summary>
    public int CompareTo(PluginVersion other)
    {
        int cmp = Major.CompareTo(other.Major);
        if (cmp != 0)
            return cmp;

        cmp = Minor.CompareTo(other.Minor);
        return cmp != 0 ? cmp : Patch.CompareTo(other.Patch);
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is lower than <paramref name="right"/>.</summary>
    public static bool operator <(PluginVersion left, PluginVersion right) => left.CompareTo(right) < 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is greater than <paramref name="right"/>.</summary>
    public static bool operator >(PluginVersion left, PluginVersion right) => left.CompareTo(right) > 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is lower than or equal to <paramref name="right"/>.</summary>
    public static bool operator <=(PluginVersion left, PluginVersion right) => left.CompareTo(right) <= 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is greater than or equal to <paramref name="right"/>.</summary>
    public static bool operator >=(PluginVersion left, PluginVersion right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Parses a version string in the <c>v{major}.{minor}.{patch}</c> form (the leading <c>v</c> is optional).
    /// </summary>
    public static PluginVersion Parse(string version)
    {
        string[] parts = version.TrimStart('v').Split('.');
        return new PluginVersion(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    /// <summary>
    /// Attempts to parse a version string; returns <see langword="false"/> when the input is malformed.
    /// </summary>
    public static bool TryParse(string? version, out PluginVersion parsedVersion)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                parsedVersion = Default;
                return false;
            }

            parsedVersion = Parse(version);
            return true;
        }
        catch
        {
            parsedVersion = Default;
            return false;
        }
    }

    /// <summary>
    /// Parses a version string and returns <see langword="null"/> when the input is malformed.
    /// </summary>
    public static PluginVersion? SafeParse(string? version) => TryParse(version, out PluginVersion parsedVersion) ? parsedVersion : null;

    /// <inheritdoc/>
    public override string ToString() => Name;
}