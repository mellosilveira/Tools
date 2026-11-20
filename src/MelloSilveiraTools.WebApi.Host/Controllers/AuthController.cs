using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MelloSilveiraTools.WebApi.Host.Controllers;

/// <summary>
/// Exposes JWE authentication endpoints for issuing and refreshing quickstart demonstration tokens.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthenticationTokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Request payload for generating a demonstration JWE token.
    /// </summary>
    public sealed record DemoTokenRequest
    {
        /// <summary>
        /// Optional user identifier embedded as the JWT subject (<c>sub</c>) claim.
        /// </summary>
        public string? Username { get; init; }
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
    /// Issues a signed and encrypted JWE token for local quickstart, Swagger, and WebUi sessions.
    /// </summary>
    /// <param name="request">Optional demonstration user payload.</param>
    /// <returns>An <see cref="AuthenticationToken"/> wrapped in <see cref="Result{T}"/>.</returns>
    [AllowAnonymous]
    [HttpPost("demo-token")]
    [ProducesResponseType(typeof(Result<AuthenticationToken>), StatusCodes.Status200OK)]
    public IActionResult GenerateDemoToken([FromBody] DemoTokenRequest? request)
    {
        string userIdentifier = string.IsNullOrWhiteSpace(request?.Username)
            ? "quickstart-researcher"
            : request.Username.Trim();

        AuthenticationToken token = tokenService.Generate(userIdentifier);
        return Result.CreateSuccessOk(token).BuildHttpResponse();
    }

    /// <summary>
    /// Validates and refreshes an existing JWE token.
    /// </summary>
    /// <param name="request">Payload containing the active JWE token.</param>
    /// <returns>A refreshed <see cref="AuthenticationToken"/> wrapped in <see cref="Result{T}"/>.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(Result<AuthenticationToken>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.CreateBadRequest("Token cannot be empty.").BuildHttpResponse();

        try
        {
            AuthenticationToken refreshed = await tokenService.RefreshAsync(request.Token).ConfigureAwait(false);
            return Result.CreateSuccessOk(refreshed).BuildHttpResponse();
        }
        catch (SecurityTokenException ex)
        {
            return Result.CreateUnauthorized($"Invalid or expired token: {ex.Message}").BuildHttpResponse();
        }
    }
}
