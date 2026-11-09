using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Controllers;

/// <summary>
/// Exposes HTTP endpoints for processing experimental raw data streams, polling step checkpoints,
/// retrying analyses from specific steps, and streaming real-time NDJSON progress notifications.
/// </summary>
[ApiController]
[Route("api/v1/experimental-data")]
public class ExperimentalDataController(ILogger logger) : ControllerBase
{
    [HttpPost("process")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Process(
        [FromServices] ExecuteExperimentalDataPipeline command, 
        [FromBody] ExecuteExperimentalDataPipelineRequest request, 
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    [HttpGet("{identifier}/stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task ProcessStreamAsync(
        [FromServices] IPipelineProgressNotifier progressNotifier,
        [FromRoute] string identifier,
        CancellationToken cancellationToken = default) 
        => await progressNotifier
            .ReadAllAsync(identifier, cancellationToken)
            .WriteNdjsonAsync(HttpContext, logger, "Progress for experimental data pipeline", cancellationToken)
            .ConfigureAwait(false);
}
