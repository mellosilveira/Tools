namespace MelloSilveiraTools.WebApi.Application.Commands.Users.Register;

/// <summary>
/// Request payload for registering a new user account.
/// </summary>
public sealed record RegisterUserRequest
{
    /// <summary>
    /// Unique login username (minimum 3 characters).
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Plain-text password (minimum 6 characters).
    /// </summary>
    public required string Password { get; init; }
}
