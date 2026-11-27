using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Commands.Health;
using MelloSilveiraTools.WebApi.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.WebApi.Application.Controllers;

/// <summary>
/// Exposes the aggregated native ASP.NET Core health-check endpoint under <c>api/health</c>
/// for Controller-based hosts, delegating execution to <see cref="CheckHealthCommand"/>.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Evaluates all registered ASP.NET Core health checks and returns a structured
    /// <see cref="HealthCheckResultData"/> wrapped in <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="command">Command executing the registered health checks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>200 OK</c> when Healthy/Degraded or <c>503 Service Unavailable</c> when Unhealthy.</returns>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(Result<HealthCheckResultData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<HealthCheckResultData>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAsync(
        [FromServices] CheckHealthCommand command,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);
}
