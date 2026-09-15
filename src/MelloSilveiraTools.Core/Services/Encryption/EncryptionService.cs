using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace MelloSilveiraTools.Core.Services.Encryption;

/// <inheritdoc cref="IEncryptionService"/>
/// <param name="settings">Settings that control the salt size, hash size and iteration count.</param>
public class EncryptionService(EncryptionSettings settings) : IEncryptionService
{
    private const char Separator = ':';

    /// <inheritdoc/>
    public string GeneratePasswordHash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(settings.SaltSize);
        byte[] hash = BuildDerivedKey(password, salt);
        return $"{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc/>
    public bool IsPasswordValid(string password, string storedPasswordHash)
    {
        string[] parts = storedPasswordHash.Split(Separator);
        if (parts.Length != 2)
            return false;

        byte[] storedSalt = Convert.FromBase64String(parts[0]);
        byte[] storedHash = Convert.FromBase64String(parts[1]);
        byte[] hash = BuildDerivedKey(password, storedSalt);

        return CryptographicOperations.FixedTimeEquals(storedHash, hash);
    }

    private byte[] BuildDerivedKey(string password, byte[] salt) => KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, settings.Iterations, settings.HashSize);
}