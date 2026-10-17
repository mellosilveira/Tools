using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes HTTP endpoints for executing Parametric Sweep sensitivity analyses and streaming real-time pipeline progress.
/// File artifact queries and downloads are centralized in <see cref="StoredFilesController"/>.
/// </summary>
[ApiController]
[Route("api/v1/parametric-sweeps")]
public class ParametricSweepController(ExecuteParametricSweepCommand command) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Result<ExecuteParametricSweepResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExecuteParametricSweepResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ExecuteParametricSweepResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ExecuteParametricSweepResponse>>> ExecuteAsync(
        [FromBody] ExecuteParametricSweepRequest request,
        CancellationToken cancellationToken = default)
    {
        return await command.ExecuteAsync(request with { CancellationToken = cancellationToken }).BuildHttpResponseAsync().ConfigureAwait(false);
    }

    [HttpPost("stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task ExecuteStreamAsync(
        [FromBody] ExecuteParametricSweepRequest request,
        CancellationToken cancellationToken = default)
    {
        await PipelineProgressStreamWriter.StreamProgressAsync(
            HttpContext,
            (progress, ct) => command.ExecuteAsync(request with { Progress = progress, CancellationToken = ct }),
            cancellationToken).ConfigureAwait(false);
    }
}
