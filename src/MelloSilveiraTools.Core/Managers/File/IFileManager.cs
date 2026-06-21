namespace MelloSilveiraTools.Core.Managers.File;

public interface IFileManager
{
    string BuildTimebasedFullName(string fileUri, string filePrefix, string fileExtension);

    FileData BuildTimebasedFile(string fileUri, string filePrefix, string fileExtension);

    FileInfo BuildTimebasedFileInfo(string fileUri, string filePrefix, string fileExtension);
}