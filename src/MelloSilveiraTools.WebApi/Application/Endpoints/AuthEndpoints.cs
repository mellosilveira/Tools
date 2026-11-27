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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MelloSilveiraTools.WebApi.Application.Endpoints;

/// <summary>
/// Maps the same JWE authentication surface as <see cref="Controllers.AuthController"/> using minimal APIs.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps the authentication endpoints (register, login, refresh, validate) under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/auth"</c>.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain metadata.</returns>
    public static RouteGroupBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/auth")
    {
        RouteGroupBuilder group = builder.MapGroup(pattern).WithTags("Auth");

        group
            .MapPost("/register", async (HttpContext httpContext, [FromServices] RegisterUserCommand command, [FromBody] RegisterUserRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .AllowAnonymous()
            .Produces<Result<AuthenticationToken>>(StatusCodes.Status201Created)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status409Conflict)
            .WithName("AuthRegister")
            .WithSummary("Registers a new user account and issues an initial signed and encrypted JWE token.");

        group
            .MapPost("/login", async (HttpContext httpContext, [FromServices] LoginCommand command, [FromBody] LoginRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .AllowAnonymous()
            .Produces<Result<AuthenticationToken>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status401Unauthorized)
            .WithName("AuthLogin")
            .WithSummary("Authenticates an existing user and issues a signed and encrypted JWE token.");

        group
            .MapPost("/refresh", async (HttpContext httpContext, [FromServices] RefreshAuthenticationTokenCommand command, [FromBody] RefreshTokenRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .AllowAnonymous()
            .Produces<Result<AuthenticationToken>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status401Unauthorized)
            .WithName("AuthRefresh")
            .WithSummary("Validates and refreshes an existing JWE token.");

        group
            .MapPost("/validate", async (HttpContext httpContext, [FromServices] ValidateAuthenticationTokenCommand command, [FromBody] ValidateTokenRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .AllowAnonymous()
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status401Unauthorized)
            .WithName("AuthValidate")
            .WithSummary("Validates whether a JWE token is authentic and active.");

        group
            .MapGet("/me", async (HttpContext httpContext, [FromServices] ICurrentUserProvider currentUserProvider, [FromServices] IRepository repository) =>
            {
                long? userId = currentUserProvider.GetCurrentUserId();
                if (userId is null)
                    return Result.CreateUnauthorized("Authentication required.").ToHttpResult();

                UserEntity? user = await repository.GetAsync<UserEntity>(userId.Value, httpContext.RequestAborted).ConfigureAwait(false);
                return user is null || !user.IsActive
                    ? Result.CreateUnauthorized("User account not found or inactive.").ToHttpResult()
                    : Result.CreateSuccessOk(new CurrentUserInfo(user.Id, user.Username, user.IsSuperuser)).ToHttpResult();
            })
            .RequireAuthorization()
            .Produces<Result<CurrentUserInfo>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status401Unauthorized)
            .WithName("AuthMe")
            .WithSummary("Returns the authenticated user's profile and live superuser status.");

        return group;
    }
}
