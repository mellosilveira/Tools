using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Authentication.Services;

namespace MelloSilveiraTools.WebApi.Application.Commands.Users.ValidateToken;

/// <summary>
/// Validates whether a JWE token is authentic and active.
/// </summary>
/// <param name="tokenService">Authentication token service.</param>
public sealed class ValidateAuthenticationTokenCommand(IAuthenticationTokenService tokenService) : CommandBaseWithDefaultResponse<ValidateTokenRequest>
{
    /// <inheritdoc/>
    protected override async Task<Result> ExecuteCommandAsync(ValidateTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.CreateBadRequest("Token cannot be empty.");

        bool isValid = await tokenService.IsValidAsync(request.Token).ConfigureAwait(false);
        return isValid ? Result.CreateSuccessOk() : Result.CreateUnauthorized("Invalid token.");
    }
}
