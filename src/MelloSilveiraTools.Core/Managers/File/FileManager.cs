namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// Handles file operations such as writing streams to disk.
/// Registered as Singleton in DI. Thread-safe.
/// </summary>
public class FileManager : IFileManager
{
    public string BuildTimebasedFullName(string fileUri, string filePrefix, string fileExtension) => Path.Combine(fileUri, BuildTimebasedName(filePrefix, fileExtension));

    public FileData BuildTimebasedFile(string fileUri, string filePrefix, string fileExtension) => new(fileUri, BuildTimebasedName(filePrefix, fileExtension));

    public FileInfo BuildTimebasedFileInfo(string fileUri, string filePrefix, string fileExtension)
    {
        string fullFileName = BuildTimebasedFullName(fileUri, filePrefix, fileExtension);
        FileInfo fileInfo = new(fullFileName);

        if (fileInfo.Directory?.Exists == false)
            fileInfo.Directory.Create();

        return fileInfo;
    }

    private static string BuildTimebasedName(string filePrefix, string fileExtension) => $"{filePrefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{fileExtension}";
}
