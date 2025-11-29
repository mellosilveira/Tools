namespace MelloSilveiraTools.Infrastructure.Services.Email;

public record SftpEmailSettings
{
    public string ApplicationEmail { get; init; }

    public string ApplicationPassword { get; init; }

    public string Host { get; init; }

    public int Port { get; init; }
}
