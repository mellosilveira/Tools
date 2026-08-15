using System.Text;

namespace MelloSilveiraTools.Core.Managers.File;

public class FileManager : IFileManager
{
    private const int LargeFileBufferSize = 128 * 1024; // 128 KB buffer
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
    private static readonly FileStreamOptions LargeFileStreamOptions = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
        BufferSize = LargeFileBufferSize
    };

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

    public StreamWriter CreateTimebasedFileWriter(string fileUri, string filePrefix, string fileExtension)
    {
        FileInfo fileInfo = BuildTimebasedFileInfo(fileUri, filePrefix, fileExtension);
        FileStream stream = fileInfo.Open(LargeFileStreamOptions);
        return new StreamWriter(stream, Utf8Encoding, LargeFileBufferSize);
    }
}
