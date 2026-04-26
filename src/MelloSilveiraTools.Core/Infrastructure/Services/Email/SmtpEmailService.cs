using MelloSilveiraTools.Core.Domain.Services;
using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Core.Infrastructure.ResiliencePipelines;
using System.Net;
using System.Net.Mail;

namespace MelloSilveiraTools.Core.Infrastructure.Services.Email;

/// <summary>
/// Implementation of <see cref="IEmailService"/> that sends messages through an SMTP server
/// using the provided <see cref="EmailSettings"/> and the SMTP resilience pipeline.
/// </summary>
/// <param name="logger">Logger used to record failures while sending e-mails.</param>
/// <param name="smtpResiliencePipeline">Resilience pipeline applied to the SMTP send operation.</param>
/// <param name="emailSettings">SMTP server configuration and sender credentials.</param>
public class SmtpEmailService(
    ILogger logger,
    SmtpResiliencePipeline smtpResiliencePipeline,
    EmailSettings emailSettings)
    : IEmailService
{
    /// <inheritdoc/>
    public async Task<bool> SendAsync(string recipient, string subject, string body, bool isBodyHtml = true)
    {
        try
        {
            return await smtpResiliencePipeline.ExecuteAsync(async _ =>
            {
                NetworkCredential credentials = new(emailSettings.ApplicationEmail, emailSettings.ApplicationPassword);
                using SmtpClient smtpClient = new(emailSettings.SmtpHost, emailSettings.SmtpPort) { EnableSsl = true, Credentials = credentials };

                using MailMessage mailMessage = new(emailSettings.ApplicationEmail, recipient, subject, body) { IsBodyHtml = isBodyHtml };
                await smtpClient.SendMailAsync(mailMessage).ConfigureAwait(false);

                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Dictionary<string, object?> logAdditionalData = new()
            {
                { "Recipient", recipient },
                { "Subject", subject },
                { "Body", body },
                { "IsBodyHtml", isBodyHtml },
            };
            logger.Error("Falha ao enviar email.", ex, logAdditionalData);

            return false;
        }
    }
}
