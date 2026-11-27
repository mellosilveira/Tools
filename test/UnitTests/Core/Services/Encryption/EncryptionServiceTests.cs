using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Encryption;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Core.Services.Encryption;

/// <summary>
/// Unit tests for <see cref="EncryptionService"/> and <see cref="EncryptionSettings"/> verifying
/// password hash generation, salt randomization, PBKDF2 hash verification, malformed input handling,
/// and dependency injection registration.
/// </summary>
public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _service;

    public EncryptionServiceTests()
    {
        _service = new EncryptionService(new EncryptionSettings
        {
            SaltSize = 16,
            HashSize = 32,
            Iterations = 100_000
        });
    }

    /// <summary>
    /// Verifies that GeneratePasswordHash produces a colon-separated salt and hash in Base64 format.
    /// </summary>
    [Fact]
    public void GeneratePasswordHash_ShouldReturnColonSeparatedBase64String()
    {
        // Arrange
        string password = "SecurePassword123!";

        // Act
        string hash = _service.GeneratePasswordHash(password);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
        string[] parts = hash.Split(':');
        Assert.Equal(2, parts.Length);

        byte[] saltBytes = Convert.FromBase64String(parts[0]);
        byte[] hashBytes = Convert.FromBase64String(parts[1]);

        Assert.Equal(16, saltBytes.Length);
        Assert.Equal(32, hashBytes.Length);
    }

    /// <summary>
    /// Verifies that hashing the same password twice yields different hashes due to unique cryptographic salts.
    /// </summary>
    [Fact]
    public void GeneratePasswordHash_SamePassword_ProducesUniqueHashesDueToSalt()
    {
        // Arrange
        string password = "MySuperSecretPassword";

        // Act
        string hash1 = _service.GeneratePasswordHash(password);
        string hash2 = _service.GeneratePasswordHash(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    /// <summary>
    /// Verifies that IsPasswordValid returns true when the correct password is provided.
    /// </summary>
    [Fact]
    public void IsPasswordValid_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        string password = "CorrectHorseBatteryStaple";
        string hash = _service.GeneratePasswordHash(password);

        // Act
        bool isValid = _service.IsPasswordValid(password, hash);

        // Assert
        Assert.True(isValid);
    }

    /// <summary>
    /// Verifies that IsPasswordValid returns false when an incorrect password is provided.
    /// </summary>
    [Fact]
    public void IsPasswordValid_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        string password = "CorrectPassword";
        string wrongPassword = "WrongPassword";
        string hash = _service.GeneratePasswordHash(password);

        // Act
        bool isValid = _service.IsPasswordValid(wrongPassword, hash);

        // Assert
        Assert.False(isValid);
    }

    /// <summary>
    /// Verifies that IsPasswordValid returns false for malformed hash strings without throwing exceptions.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("singlepartonly")]
    [InlineData("too:many:colons:here")]
    public void IsPasswordValid_WithMalformedHash_ReturnsFalse(string malformedHash)
    {
        // Act
        bool isValid = _service.IsPasswordValid("password", malformedHash);

        // Assert
        Assert.False(isValid);
    }

    /// <summary>
    /// Verifies that AddCoreServices registers IEncryptionService when encryptionSettings are provided.
    /// </summary>
    [Fact]
    public void KeyedRegistration_AddCoreServices_RegistersEncryptionService()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddCoreServices(
            new ResiliencePipelineSettings
            {
                BackoffType = Polly.DelayBackoffType.Constant,
                DelayInMilliseconds = 100,
                MaxRetryAttempts = 3,
                UseJitter = false
            },
            encryptionSettings: new EncryptionSettings
            {
                SaltSize = 16,
                HashSize = 32,
                Iterations = 100_000
            });
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IEncryptionService? service = provider.GetService<IEncryptionService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<EncryptionService>(service);
    }
}
