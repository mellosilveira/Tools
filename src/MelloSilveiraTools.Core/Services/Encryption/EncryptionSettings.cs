namespace MelloSilveiraTools.Core.Services.Encryption;

/// <summary>
/// Settings that parameterize password hashing performed by <see cref="IEncryptionService"/>.
/// </summary>
public record EncryptionSettings
{
    /// <summary>
    /// Size, in bytes, of the random salt generated for each password.
    /// </summary>
    public int SaltSize { get; init; }

    /// <summary>
    /// Size, in bytes, of the derived key produced by the hashing algorithm.
    /// </summary>
    public int HashSize { get; init; }

    /// <summary>
    /// Number of iterations used by the key derivation function.
    /// </summary>
    public int Iterations { get; init; }
}
