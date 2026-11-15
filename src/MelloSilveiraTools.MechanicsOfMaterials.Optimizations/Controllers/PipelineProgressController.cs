using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes HTTP endpoints for streaming real-time pipeline execution progress notifications.
/// </summary>
/// <param name="logger">Structured logger instance for telemetry.</param>
[ApiController]
[Route("api/v1/pipelines")]
public class PipelineProgressController(ILogger<PipelineProgressController> logger) : ControllerBase
{
    /// <summary>
    /// Streams real-time <see cref="PipelineProgressEvent"/> notifications for a specific pipeline execution
    /// formatted as an NDJSON (<c>application/x-ndjson</c>) response stream.
    /// </summary>
    /// <param name="progressNotifier">The progress notifier service resolving the pipeline execution channel.</param>
    /// <param name="pipelineId">The unique business identifier correlating the pipeline run.</param>
    /// <param name="cancellationToken">Cancellation token to terminate the response stream.</param>
    /// <returns>A task representing the ongoing NDJSON response write operation.</returns>
    [HttpGet("{pipelineId}/progress")]
    [HttpGet("{pipelineId}/progress/stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task StreamProgressAsync(
        [FromServices] IPipelineProgressNotifier progressNotifier,
        [FromRoute] string pipelineId,
        CancellationToken cancellationToken = default)
        => await progressNotifier
            .ReadAllAsync(pipelineId, cancellationToken)
            .WriteNdjsonAsync(HttpContext, logger, "Progress for pipeline execution", cancellationToken)
            .ConfigureAwait(false);
}
