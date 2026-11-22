using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Commands;
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
    /// <returns>A <see cref="TokenValidationResponse"/> wrapped in <see cref="Result{T}"/>.</returns>
    [AllowAnonymous]
    [HttpPost("validate")]
    [ProducesResponseType(typeof(Result<TokenValidationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateAsync(
        [FromServices] ValidateAuthenticationTokenCommand command,
        [FromBody] ValidateTokenRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);
}
