namespace SoftTissue.Infrastructure.Files.Managers;

public class FileManager : IFileManager
{
    public string BuildTimebasedFullName(string fileUri, string filePrefix, string fileExtension)
    {
        FileInfo fileInfo = BuildTimebasedFileInfo(fileUri, filePrefix, fileExtension);
        return fileInfo.FullName;
    }

    public FileData BuildTimebasedFile(string fileUri, string filePrefix, string fileExtension)
    {
        FileInfo fileInfo = BuildTimebasedFileInfo(fileUri, filePrefix, fileExtension);
        return new FileData(fileInfo);
    }

    public FileInfo BuildTimebasedFileInfo(string fileUri, string filePrefix, string fileExtension)
    {
        string fullFileName = Path.Combine(fileUri, $"{filePrefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{fileExtension}");
        FileInfo fileInfo = new(fullFileName);

        if (fileInfo.Directory?.Exists == false)
            fileInfo.Directory.Create();

        return fileInfo;
    }
}
