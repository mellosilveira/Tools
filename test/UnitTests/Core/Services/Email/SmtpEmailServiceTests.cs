using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Core.Services.Email;

/// <summary>
/// Unit tests for <see cref="SmtpEmailService"/>, <see cref="EmailSettings"/>, and <see cref="SmtpResiliencePipeline"/>
/// verifying resilience error handling, graceful fallback on transport failures, and DI registration.
/// </summary>
public sealed class SmtpEmailServiceTests
{
    /// <summary>
    /// Verifies that SendAsync invokes the resilience fallback and returns false when SMTP transmission fails.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenHostUnreachable_InvokesFallbackAndReturnsFalseAsync()
    {
        // Arrange
        EmailSettings settings = new()
        {
            ApplicationEmail = "test@example.com",
            ApplicationPassword = "secret-password",
            SmtpHost = "127.0.0.1",
            SmtpPort = 65534,
            ResiliencePipelineSettings = new ResiliencePipelineSettings
            {
                BackoffType = Polly.DelayBackoffType.Constant,
                MaxRetryAttempts = 1,
                DelayInMilliseconds = 1,
                UseJitter = false
            }
        };

        SmtpResiliencePipeline pipeline = new(
            NullLogger<SmtpResiliencePipeline>.Instance,
            settings.ResiliencePipelineSettings);

        SmtpEmailService emailService = new(
            NullLogger<SmtpEmailService>.Instance,
            pipeline,
            settings);

        // Act
        bool result = await emailService.SendAsync(
            "recipient@example.com",
            "Test Subject",
            "<p>Test Body</p>",
            isBodyHtml: true);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that AddCoreServices registers IEmailService and SmtpResiliencePipeline when emailSettings are provided.
    /// </summary>
    [Fact]
    public void KeyedRegistration_AddCoreServices_RegistersEmailServiceAndPipeline()
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
            emailSettings: new EmailSettings
            {
                ApplicationEmail = "app@domain.com",
                ApplicationPassword = "pass",
                SmtpHost = "smtp.domain.com",
                SmtpPort = 587
            });
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IEmailService? service = provider.GetService<IEmailService>();
        SmtpResiliencePipeline? pipeline = provider.GetService<SmtpResiliencePipeline>();

        // Assert
        Assert.NotNull(service);
        Assert.NotNull(pipeline);
        Assert.IsType<SmtpEmailService>(service);
    }
}
