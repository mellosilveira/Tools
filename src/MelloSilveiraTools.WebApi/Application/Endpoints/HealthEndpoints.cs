using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Commands.Health;
using MelloSilveiraTools.WebApi.Application.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MelloSilveiraTools.WebApi.Application.Endpoints;

/// <summary>
/// Maps the same aggregated native ASP.NET Core health-check surface as <see cref="Controllers.HealthController"/>
/// using minimal APIs, delegating execution to <see cref="CheckHealthCommand"/>.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps an anonymous health-check endpoint at <paramref name="pattern"/> backed by <see cref="CheckHealthCommand"/>
    /// and ASP.NET Core's native <see cref="HealthCheckService"/>. Returns <c>200 OK</c> for
    /// <see cref="HealthStatus.Healthy"/> / <see cref="HealthStatus.Degraded"/> and <c>503 Service Unavailable</c>
    /// for <see cref="HealthStatus.Unhealthy"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoint.</param>
    /// <param name="pattern">Route path; defaults to <c>"/api/health"</c>.</param>
    /// <param name="endpointName">Optional unique OpenAPI/routing endpoint name. Derived automatically from <paramref name="pattern"/> when omitted.</param>
    /// <returns>The created <see cref="RouteHandlerBuilder"/> for optional metadata chaining.</returns>
    public static RouteHandlerBuilder MapHealthEndpoint(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/health",
        string? endpointName = null)
    {
        string effectiveName = !string.IsNullOrWhiteSpace(endpointName)
            ? endpointName
            : string.Equals(pattern, "/api/health", StringComparison.OrdinalIgnoreCase)
                ? "HealthCheck"
                : $"HealthCheck_{pattern.Trim('/').Replace('/', '_')}";

        return builder
            .MapGet(pattern, async (HttpContext httpContext, [FromServices] CheckHealthCommand command) => await command
                .ExecuteAsync(httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .AllowAnonymous()
            .Produces<Result<HealthCheckResultData>>(StatusCodes.Status200OK)
            .Produces<Result<HealthCheckResultData>>(StatusCodes.Status503ServiceUnavailable)
            .WithTags("Health")
            .WithName(effectiveName)
            .WithSummary("Evaluates all registered ASP.NET Core health checks and returns structured health status.");
    }
}
