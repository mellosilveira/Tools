namespace MelloSilveiraTools.Authentication.Services;

/// <summary>
/// Service that handles an authentication token.
/// </summary>
public interface IAuthenticationTokenService
{
    AuthenticationToken Generate(string userIdentifier);

    Task<AuthenticationToken> RefreshAsync(string token);

    Task<bool> IsValidAsync(string token);
}
