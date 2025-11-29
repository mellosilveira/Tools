namespace MelloSilveiraTools.Infrastructure.Services.Encryption;

public record EncryptionSettings
{
    public int SaltSize { get; init; }
    public int HashSize { get; init; }
    public int Iterations { get; init; }
}