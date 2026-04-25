namespace MelloSilveiraTools.Authentication.Services;

/// <summary>
/// Service that handles an authentication token.
/// </summary>
public interface IAuthenticationTokenService
{
    /// <summary>
    /// Generates a new authentication token for the supplied numeric user identifier.
    /// </summary>
    /// <param name="userIdentifier">Numeric identifier of the subject the token is being issued for.</param>
    AuthenticationToken Generate(long userIdentifier);

    /// <summary>
    /// Generates a new authentication token for the supplied user identifier.
    /// </summary>
    /// <param name="userIdentifier">Identifier of the subject the token is being issued for.</param>
    AuthenticationToken Generate(string userIdentifier);

    /// <summary>
    /// Validates the given token and, if valid, issues a new token for the same subject.
    /// </summary>
    /// <param name="token">The previously issued token to be validated and refreshed.</param>
    /// <exception cref="Microsoft.IdentityModel.Tokens.SecurityTokenException">
    /// Thrown when <paramref name="token"/> is invalid, expired, tampered with, or does not contain a usable subject claim.
    /// </exception>
    Task<AuthenticationToken> RefreshAsync(string token);

    /// <summary>
    /// Returns whether the supplied token passes all validation parameters.
    /// </summary>
    /// <param name="token">The token to be validated.</param>
    /// <remarks>
    /// This method does not throw on validation failure: any validation error is captured and the method returns <c>false</c>.
    /// </remarks>
    Task<bool> IsValidAsync(string token);
}
