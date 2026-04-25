namespace MelloSilveiraTools.Infrastructure.Services.Encryption;

/// <summary>
/// Provides cryptographic operations for password hashing and verification.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Generates a salted hash for the specified password.
    /// </summary>
    /// <param name="password">Plain-text password to be hashed.</param>
    /// <returns>A string that combines the salt and the derived hash.</returns>
    string GeneratePasswordHash(string password);

    /// <summary>
    /// Checks whether the specified password matches a previously generated hash.
    /// </summary>
    /// <param name="password">Plain-text password provided by the caller.</param>
    /// <param name="storedPasswordHash">Hash previously produced by <see cref="GeneratePasswordHash"/>.</param>
    /// <returns><c>true</c> when the password is valid for the stored hash; otherwise, <c>false</c>.</returns>
    bool IsPasswordValid(string password, string storedPasswordHash);
}
