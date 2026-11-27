using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Login;
using MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Register;
using MelloSilveiraTools.WebApi.Application.Commands.Users.ValidateToken;
using MelloSilveiraTools.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.WebApi.Application.Controllers;

/// <summary>
/// Exposes JWE authentication endpoints for registering users, logging in, refreshing, and validating bearer tokens.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Registers a new user account and issues an initial signed and encrypted JWE token.
    /// </summary>
    /// <param name="command">Command that validates and registers the user.</param>
    /// <param name="request">User registration credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="AuthenticationToken"/> wrapped in <see cref="Result{T}"/>.</returns>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(Result<AuthenticationToken>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(
        [FromServices] RegisterUserCommand command,
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Authenticates an existing user and issues a signed and encrypted JWE token.
    /// </summary>
    /// <param name="command">Command that verifies user credentials and issues the token.</param>
    /// <param name="request">User login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="AuthenticationToken"/> wrapped in <see cref="Result{T}"/>.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(Result<AuthenticationToken>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync(
        [FromServices] LoginCommand command,
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Validates and refreshes an existing JWE token.
    /// </summary>
    /// <param name="command">Command that refreshes the token.</param>
    /// <param name="request">Payload containing the active JWE token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A refreshed <see cref="AuthenticationToken"/> wrapped in <see cref="Result{T}"/>.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(Result<AuthenticationToken>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync(
        [FromServices] RefreshAuthenticationTokenCommand command,
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Validates whether a JWE token is authentic and active.
    /// </summary>
    /// <param name="command">Command that validates the token.</param>
    /// <param name="request">Payload containing the JWE token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if token is valid.</returns>
    [AllowAnonymous]
    [HttpPost("validate")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateAsync(
        [FromServices] ValidateAuthenticationTokenCommand command,
        [FromBody] ValidateTokenRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Returns the authenticated user's profile and live superuser status.
    /// </summary>
    /// <param name="currentUserProvider">Provider holding the authenticated caller's context.</param>
    /// <param name="repository">Repository querying <see cref="UserEntity"/> records.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="CurrentUserInfo"/> wrapped in <see cref="Result{T}"/>.</returns>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(Result<CurrentUserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MeAsync(
        [FromServices] ICurrentUserProvider currentUserProvider,
        [FromServices] IRepository repository,
        CancellationToken cancellationToken = default)
    {
        long? userId = currentUserProvider.GetCurrentUserId();
        if (userId is null)
            return Result.CreateUnauthorized("Authentication required.").BuildHttpResponse();

        UserEntity? user = await repository.GetAsync<UserEntity>(userId.Value, cancellationToken).ConfigureAwait(false);
        return user is null || !user.IsActive
            ? Result.CreateUnauthorized("User account not found or inactive.").BuildHttpResponse()
            : Result.CreateSuccessOk(new CurrentUserInfo(user.Id, user.Username, user.IsSuperuser)).BuildHttpResponse();
    }
}
