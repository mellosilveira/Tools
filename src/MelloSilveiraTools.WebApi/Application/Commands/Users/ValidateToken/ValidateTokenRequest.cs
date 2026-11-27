namespace MelloSilveiraTools.WebApi.Application.Commands.Users.ValidateToken;

/// <summary>
/// Request payload for validating an existing JWE token.
/// </summary>
public sealed record ValidateTokenRequest
{
    /// <summary>
    /// JWE bearer token to validate.
    /// </summary>
    public required string Token { get; init; }
}
