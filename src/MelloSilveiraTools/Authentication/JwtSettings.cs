using Microsoft.IdentityModel.Tokens;
using System.Text;

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

    public SymmetricSecurityKey SigningSecurityKey => new(Encoding.UTF8.GetBytes(SigningKey));

    public SymmetricSecurityKey EncryptionSecurityKey => new(Encoding.UTF8.GetBytes(EncryptionKey));


    /// <summary>
    /// Expiration time in minutes for access token.
    /// </summary>
    public int TokenExperitationTimeInMinutes { get; init; }
}
