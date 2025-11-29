using MelloSilveiraTools.Domain.Services;
using MelloSilveiraTools.Infrastructure.Logger;
using System.Net;
using System.Net.Mail;

namespace MelloSilveiraTools.Infrastructure.Services.Email;

public class SmtpEmailService(
    ILogger logger,
    SftpEmailSettings emailSettings)
    : IEmailService
{
    /// <inheritdoc/>
    public async Task<bool> SendAsync(string recipient, string subject, string body, bool isBodyHtml = true)
    {
        using SmtpClient smtpClient = new(emailSettings.Host, emailSettings.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(emailSettings.ApplicationEmail, emailSettings.ApplicationPassword)
        };

        try
        {
            MailMessage mailMessage = new(emailSettings.ApplicationEmail, recipient, subject, body) { IsBodyHtml = isBodyHtml };
            await smtpClient.SendMailAsync(mailMessage).ConfigureAwait(false);

            return true;
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
