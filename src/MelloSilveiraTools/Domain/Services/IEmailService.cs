namespace MelloSilveiraTools.Domain.Services;

public interface IEmailService
{
    Task<bool> SendAsync(string recipient, string subject, string body, bool isBodyHtml = true);
}