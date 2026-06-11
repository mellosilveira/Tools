namespace SoftTissue.Infrastructure.Files.Managers;

public record FileData
{
    public FileData(string uri, string name)
    {
        Uri = uri;
        Name = name;
    }

    public FileData(FileInfo fileInfo)
    {
        Uri = fileInfo.DirectoryName!;
        Name = fileInfo.Name;
    }

    public string Uri { get; init; }

    public string Name { get; init; }
}
