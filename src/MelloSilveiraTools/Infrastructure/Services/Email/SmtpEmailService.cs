using MelloSilveiraTools.Domain.Services;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.ResiliencePipelines;
using System.Net;
using System.Net.Mail;

namespace MelloSilveiraTools.Infrastructure.Services.Email;

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
