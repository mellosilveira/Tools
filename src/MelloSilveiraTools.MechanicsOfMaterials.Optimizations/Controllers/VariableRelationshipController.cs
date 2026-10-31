using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes HTTP endpoints for executing Variable Relationship and Iso-Stress explorations and streaming real-time pipeline progress.
/// File artifact queries and downloads are centralized in <see cref="StoredFilesController"/>.
/// </summary>
/// <param name="command">The variable relationship command handler.</param>
[ApiController]
[Route("api/v1/variable-relationships")]
public class VariableRelationshipController(ExecuteVariableRelationshipCommand command) : ControllerBase
{
    /// <summary>
    /// Executes a variable relationship exploration synchronously and returns the output payload.
    /// </summary>
    /// <param name="request">The variable relationship execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result containing parameter combinations and CSV metadata.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Result<ExecuteVariableRelationshipResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExecuteVariableRelationshipResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ExecuteVariableRelationshipResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ExecuteVariableRelationshipResponse>>> ExecuteAsync(
        [FromBody] ExecuteVariableRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        return await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a variable relationship exploration while streaming execution progress events as NDJSON.
    /// </summary>
    /// <param name="progressNotifier">The pipeline progress notifier instance.</param>
    /// <param name="timeProvider">The time provider instance.</param>
    /// <param name="request">The variable relationship execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task ExecuteStreamAsync(
        [FromServices] IPipelineProgressNotifier progressNotifier,
        [FromServices] TimeProvider timeProvider,
        [FromBody] ExecuteVariableRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        string identifier = Guid.NewGuid().ToString("N");
        await PipelineProgressStreamWriter.StreamProgressAsync(
            identifier,
            progressNotifier,
            timeProvider,
            HttpContext,
            ct => command.ExecuteAsync(request, ct),
            cancellationToken).ConfigureAwait(false);
    }
}
