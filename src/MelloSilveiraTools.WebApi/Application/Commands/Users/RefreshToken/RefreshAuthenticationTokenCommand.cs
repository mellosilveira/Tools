using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.IdentityModel.Tokens;

namespace MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;

/// <summary>
/// Validates and refreshes an existing JWE token.
/// </summary>
/// <param name="tokenService">Authentication token service.</param>
public sealed class RefreshAuthenticationTokenCommand(IAuthenticationTokenService tokenService) : CommandBase<RefreshTokenRequest, Result<AuthenticationToken>>
{
    /// <inheritdoc/>
    protected override async Task<Result<AuthenticationToken>> ExecuteCommandAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.CreateBadRequest("Token cannot be empty.");

        try
        {
            AuthenticationToken refreshed = await tokenService.RefreshAsync(request.Token).ConfigureAwait(false);
            return refreshed;
        }
        catch (SecurityTokenException ex)
        {
            return Result.CreateUnauthorized($"Invalid or expired token: {ex.Message}");
        }
    }
}
