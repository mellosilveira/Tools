namespace MelloSilveiraTools.WebApi.Application.Commands.Users.Login;

/// <summary>
/// Request payload for authenticating an existing user account.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Registered login username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Plain-text password.
    /// </summary>
    public required string Password { get; init; }
}
