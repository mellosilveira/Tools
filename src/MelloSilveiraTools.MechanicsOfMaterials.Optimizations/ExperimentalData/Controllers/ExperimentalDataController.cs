using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Upload;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Controllers;

/// <summary>
/// Exposes HTTP endpoints for uploading raw experimental CSV pairs, processing experimental data streams,
/// executing batch analyses across multiple groups, and streaming real-time NDJSON progress notifications.
/// </summary>
[ApiController]
[Route("api/v1/experimental-data")]
public class ExperimentalDataController(ILogger<ExperimentalDataController> logger) : ControllerBase
{
    /// <summary>
    /// Uploads a pair of raw experimental CSV files (<c>strainFile</c> and <c>stressFile</c>) via <c>multipart/form-data</c>.
    /// </summary>
    /// <param name="command">Command that validates, stores, and registers the uploaded CSV pair.</param>
    /// <param name="strainFile">Raw experimental strain CSV file.</param>
    /// <param name="stressFile">Raw experimental stress CSV file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Storage locations and metadata IDs wrapped in <see cref="Result{T}"/>.</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(Result<UploadExperimentalDataResultData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAsync(
        [FromServices] UploadExperimentalDataCommand command,
        IFormFile strainFile,
        IFormFile stressFile,
        CancellationToken cancellationToken = default)
        => await command
            .ExecuteAsync(new UploadExperimentalDataRequest { StrainFile = strainFile, StressFile = stressFile }, cancellationToken)
            .BuildHttpResponseAsync()
            .ConfigureAwait(false);

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

        // Sets the HTTP StatusCode as 202 Accepted to indicate that the request has been accepted for processing, but the processing has not been completed yet.
        HttpContext.Response.StatusCode = StatusCodes.Status202Accepted;
        await result.Data!.WriteNdjsonAsync(HttpContext, logger, "Progress for experimental data pipelines", cancellationToken).ConfigureAwait(false);
        return Empty;
    }
}
