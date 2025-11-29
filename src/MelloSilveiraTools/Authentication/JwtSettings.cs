namespace MelloSilveiraTools.Authentication;

/// <summary>
/// Settings to deal with JSON Web Tokens (JWT).
/// </summary>
public record JwtSettings
{
    public string Issuer { get; init; }

    public string Audience { get; init; }

    public string EncryptionKey { get; init; }

    public string SigningKey { get; init; }

    public int ClockSkewInSeconds { get; init; }

    public SecurityKeyType SecurityKeyType { get; init; }

    /// <summary>
    /// Expiration time in minutes for access token.
    /// </summary>
    public int TokenExperitationTimeInMinutes { get; init; }
}
