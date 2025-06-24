using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace MelloSilveiraTools.Authentication.Services;

/// <summary>
/// Service that handles an encrypted and signed JSON Web token for authentication.
/// </summary>
public class AuthenticationJweTokenService(JwtSettings settings) : IAuthenticationTokenService
{
    public AuthenticationToken Generate(string userIdentifier)
    {
        var utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset expires = utcNow.AddMinutes(settings.TokenExperitationTimeInMinutes);

        SecurityTokenDescriptor descriptor = new()
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userIdentifier),
                new Claim(JwtRegisteredClaimNames.Iat, utcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            ]),
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(CreateSecurityKey(settings.SigningKey, settings.SecurityKeyType), SecurityAlgorithms.HmacSha256),
            EncryptingCredentials = new EncryptingCredentials(
                key: CreateSecurityKey(settings.EncryptionKey, settings.SecurityKeyType),
                alg: SecurityAlgorithms.Aes256KW,
                enc: SecurityAlgorithms.Aes256CbcHmacSha512
            )
        };

        string token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AuthenticationToken(token, expires);
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
    {
        TokenValidationParameters validationParameters = BuildTokenValidationParameters(settings);
        return new JsonWebTokenHandler().ValidateTokenAsync(token, validationParameters);
    }

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
