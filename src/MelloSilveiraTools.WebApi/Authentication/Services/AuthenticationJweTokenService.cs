using MelloSilveiraTools.WebApi.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace MelloSilveiraTools.WebApi.Authentication.Services;

/// <summary>
/// Service that handles an encrypted and signed JSON Web token for authentication.
/// </summary>
/// <remarks>
/// The configured <see cref="JwtSettings.EncryptionKey"/> is consumed as UTF-8 bytes and used to wrap the
/// content-encryption key with AES-256 KW. The underlying AES-256 algorithm therefore requires a key of
/// exactly 32 bytes (256 bits) in UTF-8. A shorter or longer encryption key will cause the constructor
/// to throw when the underlying cryptographic provider rejects the key length.
/// </remarks>
public class AuthenticationJweTokenService : IAuthenticationTokenService
{
    private static readonly JsonWebTokenHandler _handler = new();

    private readonly JwtSettings _settings;
    private readonly SigningCredentials _signingCredentials;
    private readonly EncryptingCredentials _encryptingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    /// <summary>
    /// Initializes the service with the provided JWT settings, building the signing and encrypting credentials
    /// along with the token validation parameters used to validate incoming tokens.
    /// </summary>
    /// <param name="settings">JWT settings carrying the signing key, encryption key, issuer, audience and lifetime configuration.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="JwtSettings.EncryptionKey"/> or <see cref="JwtSettings.SigningKey"/> has a length
    /// that is not compatible with the underlying symmetric algorithms (AES-256 KW requires 32 UTF-8 bytes for the encryption key).
    /// </exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown when the cryptographic provider rejects the supplied key material while building the encrypting credentials.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="JwtSettings.SecurityKeyType"/> is not a supported value.
    /// </exception>
    public AuthenticationJweTokenService(JwtSettings settings)
    {
        _settings = settings;
        var signingKey = CreateSecurityKey(settings.SigningKey, settings.SecurityKeyType);
        var encryptionKey = CreateSecurityKey(settings.EncryptionKey, settings.SecurityKeyType);
        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        _encryptingCredentials = new EncryptingCredentials(encryptionKey, SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512);
        _validationParameters = BuildTokenValidationParameters(settings);
    }

    /// <inheritdoc/>
    public AuthenticationToken Generate(long userIdentifier) => Generate(userIdentifier.ToString());

    /// <inheritdoc/>
    public AuthenticationToken Generate(string userIdentifier)
    {
        var utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset expiresOn = utcNow.AddMinutes(_settings.TokenExperitationTimeInMinutes);

        SecurityTokenDescriptor descriptor = new()
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userIdentifier),
                new Claim(JwtRegisteredClaimNames.Iat, utcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            ]),
            Audience = _settings.Audience,
            Issuer = _settings.Issuer,
            Expires = expiresOn.UtcDateTime,
            SigningCredentials = _signingCredentials,
            EncryptingCredentials = _encryptingCredentials
        };

        string token = _handler.CreateToken(descriptor);
        return new AuthenticationToken(token, expiresOn);
    }

    /// <inheritdoc/>
    public async Task<AuthenticationToken> RefreshAsync(string token)
    {
        TokenValidationResult result = await ValidateTokenAsync(token).ConfigureAwait(false);
        if (!result.IsValid)
            throw new SecurityTokenException("Invalid token during refresh.");

        var jwt = result.SecurityToken as JsonWebToken;
        string? userIdentifier = jwt?.Subject;

        if (string.IsNullOrEmpty(userIdentifier))
            throw new SecurityTokenException("Subject not found in token.");

        return Generate(userIdentifier);
    }

    /// <inheritdoc/>
    public async Task<bool> IsValidAsync(string token)
    {
        TokenValidationResult result = await ValidateTokenAsync(token).ConfigureAwait(false);
        return result.IsValid;
    }

    private Task<TokenValidationResult> ValidateTokenAsync(string token)
        => _handler.ValidateTokenAsync(token, _validationParameters);

    /// <summary>
    /// Builds the <see cref="TokenValidationParameters"/> used to validate tokens based on the supplied settings.
    /// </summary>
    public static TokenValidationParameters BuildTokenValidationParameters(JwtSettings jwtSettings) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewInSeconds),
        TokenDecryptionKey = CreateSecurityKey(jwtSettings.EncryptionKey, jwtSettings.SecurityKeyType),
        IssuerSigningKey = CreateSecurityKey(jwtSettings.SigningKey, jwtSettings.SecurityKeyType),
    };

    private static SecurityKey CreateSecurityKey(string key, SecurityKeyType securityKeyType) => securityKeyType switch
    {
        SecurityKeyType.Symmetric => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        _ => throw new ArgumentOutOfRangeException(nameof(securityKeyType))
    };
}
