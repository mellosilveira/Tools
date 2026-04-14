using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace MelloSilveiraTools.Authentication.Services;

/// <summary>
/// Service that handles an encrypted and signed JSON Web token for authentication.
/// </summary>
public class AuthenticationJweTokenService : IAuthenticationTokenService
{
    private static readonly JsonWebTokenHandler _handler = new();

    private readonly JwtSettings _settings;
    private readonly SigningCredentials _signingCredentials;
    private readonly EncryptingCredentials _encryptingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public AuthenticationJweTokenService(JwtSettings settings)
    {
        _settings = settings;
        var signingKey = CreateSecurityKey(settings.SigningKey, settings.SecurityKeyType);
        var encryptionKey = CreateSecurityKey(settings.EncryptionKey, settings.SecurityKeyType);
        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        _encryptingCredentials = new EncryptingCredentials(encryptionKey, SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512);
        _validationParameters = BuildTokenValidationParameters(settings);
    }

    public AuthenticationToken Generate(long userIdentifier) => Generate(userIdentifier.ToString());

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

    public async Task<bool> IsValidAsync(string token)
    {
        TokenValidationResult result = await ValidateTokenAsync(token).ConfigureAwait(false);
        return result.IsValid;
    }

    private Task<TokenValidationResult> ValidateTokenAsync(string token)
        => _handler.ValidateTokenAsync(token, _validationParameters);

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
