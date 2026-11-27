namespace MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;

/// <summary>
/// Request payload for refreshing an active JWE token.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// Active JWE bearer token to refresh.
    /// </summary>
    public required string Token { get; init; }
}
