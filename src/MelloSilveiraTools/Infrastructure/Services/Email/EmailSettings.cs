namespace MelloSilveiraTools.Infrastructure.Services.Email;

public record EmailSettings
{
    public string ApplicationEmail { get; init; }

    public string ApplicationPassword { get; init; }

    public string SmtpHost { get; init; }

    public int SmtpPort { get; init; }
}
