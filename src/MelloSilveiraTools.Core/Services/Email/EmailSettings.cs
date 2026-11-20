using MelloSilveiraTools.Core.ResiliencePipelines;

namespace MelloSilveiraTools.Core.Services.Email;

/// <summary>
/// Settings used to send e-mails through an SMTP server.
/// </summary>
public record EmailSettings
{
    /// <summary>
    /// E-mail address that the application uses as sender and to authenticate against the SMTP server.
    /// </summary>
    public required string ApplicationEmail { get; init; }

    /// <summary>
    /// Password used together with <see cref="ApplicationEmail"/> to authenticate against the SMTP server.
    /// </summary>
    public required string ApplicationPassword { get; init; }

    /// <summary>
    /// Host name or IP address of the SMTP server.
    /// </summary>
    public required string SmtpHost { get; init; }

    /// <summary>
    /// Port number of the SMTP server.
    /// </summary>
    public required int SmtpPort { get; init; }

    /// <summary>
    /// Optional resilience pipeline settings for retry and circuit breaker policies during SMTP operations.
    /// </summary>
    public ResiliencePipelineSettings? ResiliencePipelineSettings { get; init; }
}
