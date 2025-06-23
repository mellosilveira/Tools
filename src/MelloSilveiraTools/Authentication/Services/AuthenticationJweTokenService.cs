using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
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
            SigningCredentials = new SigningCredentials(settings.SigningSecurityKey, SecurityAlgorithms.HmacSha256),
            EncryptingCredentials = new EncryptingCredentials(
                key: settings.EncryptionSecurityKey,
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
            throw new SecurityTokenException($"Invalid token during refresh.");

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
        TokenValidationParameters validationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            TokenDecryptionKey = settings.EncryptionSecurityKey,
            IssuerSigningKey = settings.SigningSecurityKey,
        };
        return new JsonWebTokenHandler().ValidateTokenAsync(token, validationParameters);
    }
}
