namespace MelloSilveiraTools.Core.Services;

/// <summary>
/// Service responsible for sending e-mail messages.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an e-mail message to the specified recipient.
    /// </summary>
    /// <param name="recipient">The destination e-mail address.</param>
    /// <param name="subject">The subject line of the message.</param>
    /// <param name="body">The body content of the message.</param>
    /// <param name="isBodyHtml">Whether the body should be interpreted as HTML. Defaults to <c>true</c>.</param>
    /// <returns><c>true</c> when the message was successfully dispatched; otherwise <c>false</c>.</returns>
    Task<bool> SendAsync(string recipient, string subject, string body, bool isBodyHtml = true);
}
