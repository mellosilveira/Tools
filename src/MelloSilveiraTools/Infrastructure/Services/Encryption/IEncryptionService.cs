namespace MelloSilveiraTools.Infrastructure.Services.Encryption;

public interface IEncryptionService
{
    string GeneratePasswordHash(string password);

    bool IsPasswordValid(string password, string storedPasswordHash);
}
