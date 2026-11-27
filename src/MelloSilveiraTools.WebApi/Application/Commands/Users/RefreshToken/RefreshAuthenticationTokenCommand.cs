using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;

/// <summary>
/// Validates an existing JWE token, verifies that the owning <see cref="UserEntity"/> still exists
/// and remains active in the database, and issues a fresh signed and encrypted JWE token.
/// </summary>
/// <param name="tokenService">Authentication token service.</param>
/// <param name="repository">Optional relational repository used to verify active user status before refreshing.</param>
public sealed class RefreshAuthenticationTokenCommand(IAuthenticationTokenService tokenService, IRepository repository) : CommandBase<RefreshTokenRequest, Result<AuthenticationToken>>
{
    /// <inheritdoc/>
    protected override async Task<Result<AuthenticationToken>> ExecuteCommandAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.CreateBadRequest("Token cannot be empty.");

        try
        {
            string? subject = await tokenService.GetValidatedSubjectAsync(request.Token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(subject) || !long.TryParse(subject, NumberStyles.Integer, CultureInfo.InvariantCulture, out long userId) || userId <= 0L)
                return Result.CreateUnauthorized("Token does not contain a valid user identifier.");

            UserEntity? user = await repository.GetAsync<UserEntity>(userId, cancellationToken).ConfigureAwait(false);
            if (user is null || !user.IsActive)
                return Result.CreateUnauthorized("User account is inactive or no longer exists.");

            return await tokenService.RefreshAsync(request.Token).ConfigureAwait(false);
        }
        catch (SecurityTokenException ex)
        {
            return Result.CreateUnauthorized($"Invalid or expired token: {ex.Message}");
        }
    }
}
