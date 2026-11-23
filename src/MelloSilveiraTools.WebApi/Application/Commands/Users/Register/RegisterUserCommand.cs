using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;

namespace MelloSilveiraTools.WebApi.Application.Commands.Users.Register;

/// <summary>
/// Registers a new user in the database with a salted PBKDF2 password hash, provisions dedicated
/// PostgreSQL partitions for the user's tenant-isolated tables, and issues a JWE token.
/// </summary>
/// <param name="validator">Validator for registration requests.</param>
/// <param name="repository">Relational repository for persisting <see cref="UserEntity"/> records and provisioning user partitions.</param>
/// <param name="encryptionService">Password hashing service.</param>
/// <param name="tokenService">Authentication token service.</param>
public sealed class RegisterUserCommand(
    IValidator<RegisterUserRequest> validator,
    IRepository repository,
    IEncryptionService encryptionService,
    IAuthenticationTokenService tokenService)
    : CommandBase<RegisterUserRequest, Result<AuthenticationToken>>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<AuthenticationToken>> ExecuteCommandAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        string username = request.Username.Trim();

        UserEntity? existingUser = await repository.GetFirstOrDefaultAsync<UserEntity, UserFilter>(new UserFilter { Username = username }, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (existingUser is not null)
            return Result.CreateError(StatusCode.Conflict, $"Username '{username}' is already registered.");

        string passwordHash = encryptionService.GeneratePasswordHash(request.Password);
        UserEntity newUser = new()
        {
            Username = username,
            PasswordHash = passwordHash,
            IsActive = true
        };

        Result<long> insertResult = await repository.TryInsertAsync(newUser, cancellationToken).ConfigureAwait(false);
        if (!insertResult.Success)
            return Result.CreateError(insertResult.StatusCode, $"Failed to register user: {insertResult.JoinedMessages}");

        await repository.EnsureUserPartitionsAsync(insertResult.Data, cancellationToken).ConfigureAwait(false);

        AuthenticationToken token = tokenService.Generate(insertResult.Data);
        return token;
    }
}
