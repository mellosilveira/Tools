using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Services;
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
    /// <summary>
    /// Initiates background processing for experimental data, returning 202 Accepted immediately with polling instructions.
    /// </summary>
    /// <param name="request">Storage-backed analysis parameters and thresholds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An HTTP 202 Accepted response containing the analysis identifier and status endpoint URI.</returns>
    [HttpPost("process")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Process([FromServices] ProcessExperimentalDataCommand command, [FromBody] ProcessExperimentalDataRequest request, CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Queries the discrete step execution checkpoints and real-time status of an ongoing or completed analysis.
    /// </summary>
    /// <param name="stepExecutionManager">Step execution checkpoint manager.</param>
    /// <param name="identifier">Unique business identifier correlating the analysis.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of step checkpoints detailing execution state, item counts, and timestamps.</returns>
    [HttpGet("{identifier}/status")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<AnalysisStepExecutionEntity>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusAsync(
        [FromServices] IStepExecutionManager stepExecutionManager,
        [FromRoute] string identifier,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AnalysisStepExecutionEntity> steps = await stepExecutionManager.GetStepExecutionsAsync(identifier, cancellationToken).ConfigureAwait(false);
        return Result.CreateSuccessOk(steps).BuildHttpResponse();
    }

    /// <summary>
    /// Retries or restarts execution of an analysis from a designated pipeline step, purging downstream artifacts.
    /// </summary>
    /// <param name="command">Command executing step resume orchestration.</param>
    /// <param name="identifier">Unique business identifier correlating the analysis.</param>
    /// <param name="fromStep">The logical name of the step to resume from ("CurveFitter" or "Simulation").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the resumed analysis identifier.</returns>
    [HttpPost("{identifier}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RetryFromStepAsync(
        [FromServices] ResumeExperimentalDataStepCommand command,
        [FromRoute] string identifier,
        [FromQuery] string fromStep,
        CancellationToken cancellationToken = default)
    {
        Result<string> result = await command.ExecuteAsync(new ResumeExperimentalDataStepRequest { Identifier = identifier, FromStepName = fromStep }, cancellationToken).ConfigureAwait(false);
        return result.BuildHttpResponse();
    }

    /// <summary>
    /// Processes experimental raw data streams while streaming all emitted progress notifications in real time via NDJSON.
    /// </summary>
    /// <param name="progressNotifier">Pipeline progress notifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{identifier}/stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task ProcessStreamAsync(
        [FromServices] IPipelineProgressNotifier progressNotifier,
        [FromRoute] string identifier,
        CancellationToken cancellationToken = default)
    {
        await progressNotifier
            .ReadAllAsync(identifier, cancellationToken)
            .WriteNdjsonAsync(HttpContext, logger, "Progress for experimental data pipeline", cancellationToken)
            .ConfigureAwait(false);
    }
}
