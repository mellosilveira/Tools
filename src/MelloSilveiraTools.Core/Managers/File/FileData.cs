namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// Contains metadata about a file, such as its directory URI and its file name.
/// </summary>
public readonly record struct FileData(string Uri, string Name)
{
    /// <summary>
    /// Initializes a new instance of <see cref="FileData"/> from a <see cref="FileInfo"/>.
    /// </summary>
    /// <param name="fileInfo">The file info to extract data from.</param>
    public FileData(FileInfo fileInfo) : this(fileInfo.DirectoryName!, fileInfo.Name) { }
}
