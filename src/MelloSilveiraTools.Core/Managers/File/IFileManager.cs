namespace SoftTissue.Infrastructure.Files.Managers;

public interface IFileManager
{
    string BuildTimebasedFullName(string fileUri, string filePrefix, string fileExtension);

    FileData BuildTimebasedFile(string fileUri, string filePrefix, string fileExtension);

    FileInfo BuildTimebasedFileInfo(string fileUri, string filePrefix, string fileExtension);
}