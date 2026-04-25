namespace MelloSilveiraTools.WebApi.Authentication;

/// <summary>
/// Represents an issued authentication token along with its expiration timestamp.
/// </summary>
public readonly struct AuthenticationToken(string value, DateTimeOffset expiresOn)
{
    /// <summary>
    /// The serialized token value that clients must send on subsequent authenticated requests.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// The instant (in UTC offset) at which the token stops being valid.
    /// </summary>
    public DateTimeOffset ExpiresOn { get; } = expiresOn;
}
