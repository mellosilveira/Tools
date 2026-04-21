namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Represents a semantic version parsed from a plugin DLL filename.
/// </summary>
public readonly record struct PluginVersion(int Major, int Minor, int Patch) : IComparable<PluginVersion>
{
    public string Name { get; } = $"v{Major}.{Minor}.{Patch}";

    public static PluginVersion Default => new(0, 0, 0);

    public int CompareTo(PluginVersion other)
    {
        int cmp = Major.CompareTo(other.Major);
        if (cmp != 0)
            return cmp;

        cmp = Minor.CompareTo(other.Minor);
        return cmp != 0 ? cmp : Patch.CompareTo(other.Patch);
    }

    public static bool operator <(PluginVersion left, PluginVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(PluginVersion left, PluginVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(PluginVersion left, PluginVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(PluginVersion left, PluginVersion right) => left.CompareTo(right) >= 0;

    public static PluginVersion Parse(string version)
    {
        string[] parts = version.TrimStart('v').Split('.');
        return new PluginVersion(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    public static bool TryParse(string version, out PluginVersion parsedVersion)
    {
        try
        {
            parsedVersion = Parse(version);
            return true;
        }
        catch 
        {
            parsedVersion = Default;
            return false; 
        }
    }

    public static PluginVersion? SafeParse(string version) => TryParse(version, out PluginVersion parsedVersion) ? parsedVersion : null;

    public override string ToString() => Name;
}