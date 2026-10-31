using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Common abstract controller for pipeline-driven APIs providing agnostic status polling.
/// </summary>
/// <param name="logger">Structured logger instance.</param>
public abstract class PipelineControllerBase(ILogger logger) : ControllerBase
{
    /// <summary>
    /// Structured logger instance for the controller.
    /// </summary>
    protected ILogger Logger { get; } = logger;

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
}

/// <summary>
/// Common abstract controller for pipeline-driven APIs, providing agnostic status polling
/// and step-level retry/resume endpoints.
/// </summary>
/// <typeparam name="TResumeCommand">The concrete application command type executing step resume orchestration.</typeparam>
/// <param name="logger">Structured logger instance.</param>
public abstract class PipelineControllerBase<TResumeCommand>(ILogger logger) : PipelineControllerBase(logger)
    where TResumeCommand : CommandBase<ResumeStepRequest, Result<string>>
{
    /// <summary>
    /// Retries or restarts execution of an analysis from a designated pipeline step, purging downstream artifacts.
    /// </summary>
    /// <param name="command">Command executing step resume orchestration.</param>
    /// <param name="identifier">Unique business identifier correlating the analysis.</param>
    /// <param name="fromStep">The logical name of the step to resume from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the resumed analysis identifier.</returns>
    [HttpPost("{identifier}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RetryFromStepAsync(
        [FromServices] TResumeCommand command,
        [FromRoute] string identifier,
        [FromQuery] string fromStep,
        CancellationToken cancellationToken = default)
    {
        Result<string> result = await command.ExecuteAsync(new ResumeStepRequest { Identifier = identifier, FromStepName = fromStep }, cancellationToken).ConfigureAwait(false);
        return result.BuildHttpResponse();
    }
}
