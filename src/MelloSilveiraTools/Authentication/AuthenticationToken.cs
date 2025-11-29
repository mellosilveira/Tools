namespace MelloSilveiraTools.Authentication;

public readonly struct AuthenticationToken(string value, DateTimeOffset expiresOn)
{
    public string Value { get; } = value;

    public DateTimeOffset ExpiresOn { get; } = expiresOn;
}
