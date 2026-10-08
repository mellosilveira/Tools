namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// File management service responsible for structuring and safely saving files and reports to disk.
/// Employs chronological naming to protect against inadvertent file loss and ensures target directories exist.
/// </summary>
public class FileManager : IFileManager
{
    /// <inheritdoc/>
    public string BuildTimebasedFullName(string fileUri, string filePrefix, string fileExtension) => Path.Combine(fileUri, BuildTimebasedName(filePrefix, fileExtension));

    /// <inheritdoc/>
    public FileData BuildTimebasedFile(string fileUri, string filePrefix, string fileExtension) => new(fileUri, BuildTimebasedName(filePrefix, fileExtension));

    /// <inheritdoc/>
    public FileInfo BuildTimebasedFileInfo(string fileUri, string filePrefix, string fileExtension)
    {
        string fullFileName = BuildTimebasedFullName(fileUri, filePrefix, fileExtension);
        FileInfo fileInfo = new(fullFileName);

        if (fileInfo.Directory?.Exists == false)
        {
            fileInfo.Directory.Create();
        }

        return fileInfo;
    }

    private static string BuildTimebasedName(string filePrefix, string fileExtension) => $"{filePrefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{fileExtension}";
}
