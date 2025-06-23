namespace MelloSilveiraTools.Authentication;

public readonly struct AuthenticationToken(string value, DateTimeOffset expires)
{
    public string Value { get; } = value;

    public DateTimeOffset Expires { get; } = expires;
}
