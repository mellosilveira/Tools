using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.IdentityModel.Tokens;

namespace UnitTests.WebApi.Authentication;

public class AuthenticationJweTokenServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static JwtSettings CreateValidSettings(int expirationMinutes = 30, int clockSkewSeconds = 5) => new()
    {
        Issuer = "https://tools.mellosilveira.test",
        Audience = "tools-api-clients",
        EncryptionKey = "12345678901234567890123456789012",
        SigningKey = "super-secret-signing-key-32-bytes!",
        ClockSkewInSeconds = clockSkewSeconds,
        SecurityKeyType = SecurityKeyType.Symmetric,
        TokenExperitationTimeInMinutes = expirationMinutes
    };

    [Fact]
    public async Task Generate_And_IsValidAsync_WithLongAndStringIdentifiers_ProducesValidJweToken()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        FixedTimeProvider timeProvider = new(now);
        JwtSettings settings = CreateValidSettings(expirationMinutes: 20);
        AuthenticationJweTokenService service = new(timeProvider, settings);

        AuthenticationToken numericToken = service.Generate(42L);
        AuthenticationToken stringToken = service.Generate("user-abc");

        Assert.False(string.IsNullOrWhiteSpace(numericToken.Value));
        Assert.Equal(now.AddMinutes(20), numericToken.ExpiresOn);
        Assert.False(string.IsNullOrWhiteSpace(stringToken.Value));
        Assert.Equal(5, stringToken.Value.Split('.').Length);

        Assert.True(await service.IsValidAsync(numericToken.Value));
        Assert.True(await service.IsValidAsync(stringToken.Value));
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_IssuesNewTokenWithSameSubject()
    {
        DateTimeOffset t0 = DateTimeOffset.UtcNow;
        FixedTimeProvider timeProvider = new(t0);
        JwtSettings settings = CreateValidSettings();
        AuthenticationJweTokenService service = new(timeProvider, settings);

        AuthenticationToken original = service.Generate(999L);
        AuthenticationToken refreshed = await service.RefreshAsync(original.Value);

        Assert.False(string.IsNullOrWhiteSpace(refreshed.Value));
        Assert.True(await service.IsValidAsync(refreshed.Value));
    }

    [Fact]
    public async Task IsValidAsync_And_RefreshAsync_ExpiredOrTamperedToken_ReturnsFalseAndThrows()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        FixedTimeProvider timeProvider = new(now);
        JwtSettings expiredSettings = CreateValidSettings(expirationMinutes: -10, clockSkewSeconds: 0);
        AuthenticationJweTokenService expiredService = new(timeProvider, expiredSettings);

        AuthenticationToken expiredToken = expiredService.Generate("user-1");

        Assert.False(await expiredService.IsValidAsync(expiredToken.Value));
        await Assert.ThrowsAsync<SecurityTokenException>(() => expiredService.RefreshAsync(expiredToken.Value));
        await Assert.ThrowsAsync<SecurityTokenException>(() => expiredService.RefreshAsync("not-a-valid-jwt"));
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenHasNoSubject_ThrowsSecurityTokenException()
    {
        JwtSettings settings = CreateValidSettings();
        AuthenticationJweTokenService service = new(TimeProvider.System, settings);

        SymmetricSecurityKey signingKey = new(System.Text.Encoding.UTF8.GetBytes(settings.SigningKey));
        SymmetricSecurityKey encKey = new(System.Text.Encoding.UTF8.GetBytes(settings.EncryptionKey));
        Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler handler = new();

        SecurityTokenDescriptor descriptor = new()
        {
            Audience = settings.Audience,
            Issuer = settings.Issuer,
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            EncryptingCredentials = new EncryptingCredentials(encKey, SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512)
        };

        string tokenWithoutSubject = handler.CreateToken(descriptor);

        SecurityTokenException ex = await Assert.ThrowsAsync<SecurityTokenException>(() => service.RefreshAsync(tokenWithoutSubject));
        Assert.Contains("Subject", ex.Message);
    }

    [Fact]
    public void BuildTokenValidationParameters_WithUnsupportedSecurityKeyType_ThrowsArgumentOutOfRangeException()
    {
        JwtSettings invalidSettings = CreateValidSettings() with { SecurityKeyType = (SecurityKeyType)999 };

        Assert.Throws<ArgumentOutOfRangeException>(() => AuthenticationJweTokenService.BuildTokenValidationParameters(invalidSettings));
    }
}
