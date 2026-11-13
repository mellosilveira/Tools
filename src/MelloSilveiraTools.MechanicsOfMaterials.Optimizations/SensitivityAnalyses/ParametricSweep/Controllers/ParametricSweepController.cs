using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Controllers;

/// <summary>
/// Exposes HTTP endpoints for executing Parametric Sweep sensitivity analyses and streaming real-time pipeline progress.
/// File artifact queries and downloads are centralized in <see cref="StoredFilesController"/>.
/// </summary>
[ApiController]
[Route("api/v1/parametric-sweeps")]
public class ParametricSweepController(ILogger<ParametricSweepController> logger) : ControllerBase
{
    [HttpPost("process")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Process(
        [FromServices] ExecuteParametricSweepPipeline command, 
        [FromBody] ExecuteParametricSweepPipelineRequest request, 
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    [HttpPost("process-from-experimental-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessFromExperimentalData(
        [FromServices] ExecuteParametricSweepFromExperimentalDataCommand command,
        [FromBody] ExecuteParametricSweepFromExperimentalDataRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<IAsyncEnumerable<string>> result = await command.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return result.BuildHttpResponse();

        // Sets the HTTP SatatusCode as 202 Accepted to indicate that the request has been accepted for processing, but the processing has not been completed yet.
        HttpContext.Response.StatusCode = StatusCodes.Status202Accepted;
        await result.Data!.WriteNdjsonAsync(HttpContext, logger, "Progress for parametric sweep pipelines", cancellationToken).ConfigureAwait(false);
        return Result.CreateSuccessOk().BuildHttpResponse();
    }

    [HttpGet("{identifier}/stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task ProcessStreamAsync(
        [FromServices] IPipelineProgressNotifier progressNotifier,
        [FromRoute] string identifier,
        CancellationToken cancellationToken = default) 
        => await progressNotifier
            .ReadAllAsync(identifier, cancellationToken)
            .WriteNdjsonAsync(HttpContext, logger, "Progress for parametric sweep pipeline", cancellationToken)
            .ConfigureAwait(false);
}
