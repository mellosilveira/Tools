namespace MelloSilveiraTools.Authentication;

/// <summary>
/// Settings to deal with JSON Web Tokens (JWT).
/// </summary>
public record JwtSettings
{
    /// <summary>
    /// The expected issuer (iss claim) of the token.
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// The expected audience (aud claim) of the token.
    /// </summary>
    public required string Audience { get; init; }

    /// <summary>
    /// Symmetric key used to encrypt the token payload (JWE).
    /// </summary>
    public required string EncryptionKey { get; init; }

    /// <summary>
    /// Symmetric key used to sign the token (JWS).
    /// </summary>
    public required string SigningKey { get; init; }

    /// <summary>
    /// Clock skew, in seconds, tolerated when validating token lifetime claims.
    /// </summary>
    public int ClockSkewInSeconds { get; init; }

    /// <summary>
    /// The kind of security key used by <see cref="EncryptionKey"/> and <see cref="SigningKey"/>.
    /// </summary>
    public SecurityKeyType SecurityKeyType { get; init; }

    /// <summary>
    /// Expiration time in minutes for access token.
    /// </summary>
    public int TokenExperitationTimeInMinutes { get; init; }
}
