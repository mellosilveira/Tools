using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Controllers;

/// <summary>
/// Exposes HTTP endpoints for processing experimental raw data streams,
/// executing batch analyses across multiple groups, and streaming real-time NDJSON progress notifications.
/// </summary>
[ApiController]
[Route("api/v1/experimental-data")]
public class ExperimentalDataController(ILogger<ExperimentalDataController> logger) : ControllerBase
{
    [HttpPost("process")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Process(
        [FromServices] ExecuteExperimentalDataPipeline command, 
        [FromBody] ExecuteExperimentalDataPipelineRequest request, 
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    [HttpPost("process/batch")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessBatch(
        [FromServices] ExecuteBatchExperimentalDataPipeline command,
        [FromBody] ExecuteBatchExperimentalDataPipelineRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<IAsyncEnumerable<string>> result = await command.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return result.BuildHttpResponse();

        // Sets the HTTP SatatusCode as 202 Accepted to indicate that the request has been accepted for processing, but the processing has not been completed yet.
        HttpContext.Response.StatusCode = StatusCodes.Status202Accepted;
        await result.Data!.WriteNdjsonAsync(HttpContext, logger, "Progress for experimental data pipelines", cancellationToken).ConfigureAwait(false);
        return Result.CreateSuccessOk().BuildHttpResponse();
    }
}
