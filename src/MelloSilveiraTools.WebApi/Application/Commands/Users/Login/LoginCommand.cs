using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;

namespace MelloSilveiraTools.WebApi.Application.Commands.Users.Login;

/// <summary>
/// Authenticates a user against persisted credentials and issues a signed and encrypted JWE token.
/// </summary>
/// <param name="validator">Validator for login requests.</param>
/// <param name="repository">Relational repository for looking up <see cref="UserEntity"/> records.</param>
/// <param name="encryptionService">Password verification service.</param>
/// <param name="tokenService">Authentication token service.</param>
public sealed class LoginCommand(
    IValidator<LoginRequest> validator,
    IRepository repository,
    IEncryptionService encryptionService,
    IAuthenticationTokenService tokenService)
    : CommandBase<LoginRequest, Result<AuthenticationToken>>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<AuthenticationToken>> ExecuteCommandAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        string username = request.Username.Trim();

        UserEntity? user = await repository.GetFirstOrDefaultAsync<UserEntity, UserFilter>(new UserFilter { Username = username, IsActive = true }, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (user is null || !encryptionService.IsPasswordValid(request.Password, user.PasswordHash))
            return Result.CreateUnauthorized("Invalid username or password.");

        AuthenticationToken token = tokenService.Generate(user.Id);
        return token;
    }
}
