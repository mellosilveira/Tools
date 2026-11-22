using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.IdentityModel.Tokens;

namespace MelloSilveiraTools.WebApi.Authentication.Commands;

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

/// <summary>
/// Validates <see cref="RegisterUserRequest"/> payloads prior to user creation.
/// </summary>
public sealed class RegisterUserValidator : IValidator<RegisterUserRequest>
{
    /// <inheritdoc/>
    public Result Validate(RegisterUserRequest value)
    {
        if (string.IsNullOrWhiteSpace(value.Username) || value.Username.Trim().Length < 3)
            return Result.CreateBadRequest("Username must contain at least 3 characters.");

        if (string.IsNullOrWhiteSpace(value.Password) || value.Password.Length < 6)
            return Result.CreateBadRequest("Password must contain at least 6 characters.");

        return Result.CreateSuccessOk();
    }
}

/// <summary>
/// Registers a new user in the database with a salted PBKDF2 password hash and issues a JWE token.
/// </summary>
/// <param name="validator">Validator for registration requests.</param>
/// <param name="repository">Relational repository for persisting <see cref="UserEntity"/> records.</param>
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

        UserEntity? existingUser = null;
        await foreach (UserEntity user in repository
            .GetAsync<UserEntity, UserFilter>(new UserFilter { Username = username }, null, cancellationToken)
            .ConfigureAwait(false))
        {
            existingUser = user;
            break;
        }

        if (existingUser is not null)
            return Result.Create<Result<AuthenticationToken>>(Result.CreateConflict($"Username '{username}' is already registered."));

        string passwordHash = encryptionService.GeneratePasswordHash(request.Password);
        UserEntity newUser = new()
        {
            Username = username,
            PasswordHash = passwordHash,
            IsActive = true
        };

        Result<long> insertResult = await repository.TryInsertAsync(newUser, cancellationToken).ConfigureAwait(false);
        if (!insertResult.Success)
            return Result.Create<Result<AuthenticationToken>>(Result.CreateError(insertResult.StatusCode, $"Failed to register user: {insertResult.JoinedMessages}"));

        AuthenticationToken token = tokenService.Generate(insertResult.Data);
        return Result.CreateSuccessCreated(token);
    }
}

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

/// <summary>
/// Validates <see cref="LoginRequest"/> payloads prior to authentication.
/// </summary>
public sealed class LoginValidator : IValidator<LoginRequest>
{
    /// <inheritdoc/>
    public Result Validate(LoginRequest value)
    {
        if (string.IsNullOrWhiteSpace(value.Username))
            return Result.CreateBadRequest("Username is required.");

        if (string.IsNullOrWhiteSpace(value.Password))
            return Result.CreateBadRequest("Password is required.");

        return Result.CreateSuccessOk();
    }
}

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
    protected override async Task<Result<AuthenticationToken>> ExecuteCommandAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        string username = request.Username.Trim();

        UserEntity? user = null;
        await foreach (UserEntity candidate in repository
            .GetAsync<UserEntity, UserFilter>(new UserFilter { Username = username, IsActive = true }, null, cancellationToken)
            .ConfigureAwait(false))
        {
            user = candidate;
            break;
        }

        if (user is null || !encryptionService.IsPasswordValid(request.Password, user.PasswordHash))
            return Result.Create<Result<AuthenticationToken>>(Result.CreateUnauthorized("Invalid username or password."));

        AuthenticationToken token = tokenService.Generate(user.Id);
        return Result.CreateSuccessOk(token);
    }
}

/// <summary>
/// Request payload for refreshing an active JWE token.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// Active JWE bearer token to refresh.
    /// </summary>
    public required string Token { get; init; }
}

/// <summary>
/// Validates and refreshes an existing JWE token.
/// </summary>
/// <param name="tokenService">Authentication token service.</param>
public sealed class RefreshAuthenticationTokenCommand(IAuthenticationTokenService tokenService)
    : CommandBase<RefreshTokenRequest, Result<AuthenticationToken>>
{
    /// <inheritdoc/>
    protected override async Task<Result<AuthenticationToken>> ExecuteCommandAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Create<Result<AuthenticationToken>>(Result.CreateBadRequest("Token cannot be empty."));

        try
        {
            AuthenticationToken refreshed = await tokenService.RefreshAsync(request.Token).ConfigureAwait(false);
            return Result.CreateSuccessOk(refreshed);
        }
        catch (SecurityTokenException ex)
        {
            return Result.Create<Result<AuthenticationToken>>(Result.CreateUnauthorized($"Invalid or expired token: {ex.Message}"));
        }
    }
}

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

/// <summary>
/// Response payload indicating whether a JWE token passed validation.
/// </summary>
public sealed record TokenValidationResponse
{
    /// <summary>
    /// Whether the token is valid and not expired.
    /// </summary>
    public required bool IsValid { get; init; }
}

/// <summary>
/// Validates whether a JWE token is authentic and active.
/// </summary>
/// <param name="tokenService">Authentication token service.</param>
public sealed class ValidateAuthenticationTokenCommand(IAuthenticationTokenService tokenService)
    : CommandBaseWithData<ValidateTokenRequest, TokenValidationResponse>
{
    /// <inheritdoc/>
    protected override async Task<Result<TokenValidationResponse>> ExecuteCommandAsync(
        ValidateTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Create<Result<TokenValidationResponse>>(Result.CreateBadRequest("Token cannot be empty."));

        bool isValid = await tokenService.IsValidAsync(request.Token).ConfigureAwait(false);
        return Result.CreateSuccessOk(new TokenValidationResponse { IsValid = isValid });
    }
}
