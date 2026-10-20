namespace MelloSilveiraTools.Core.Services.Encryption;

/// <summary>
/// Settings that parameterize password hashing performed by <see cref="IEncryptionService"/>.
/// </summary>
public record EncryptionSettings
{
    /// <summary>
    /// Size, in bytes, of the random salt generated for each password.
    /// </summary>
    public required int SaltSize { get; init; }

    /// <summary>
    /// Size, in bytes, of the derived key produced by the hashing algorithm.
    /// </summary>
    public required int HashSize { get; init; }

    /// <summary>
    /// Number of iterations used by the key derivation function.
    /// </summary>
    public required int Iterations { get; init; }
}
