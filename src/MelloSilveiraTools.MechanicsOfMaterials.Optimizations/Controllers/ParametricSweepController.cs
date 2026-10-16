using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes HTTP endpoints for executing Parametric Sweep sensitivity analyses, streaming real-time pipeline progress,
/// and querying or downloading generated CSV artifacts by external identifier.
/// </summary>
[ApiController]
[Route("api/v1/parametric-sweeps")]
public class ParametricSweepController : ControllerBase
{
    /// <summary>
    /// Executes a Parametric Sweep sensitivity analysis and persists the resulting sweep metadata and CSV artifacts.
    /// </summary>
    /// <param name="command">The parametric sweep execution command.</param>
    /// <param name="request">The parametric sweep request payload.</param>
    /// <returns>A <see cref="Result{T}"/> containing the generated or supplied external identifier.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Result<ExecuteParametricSweepResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExecuteParametricSweepResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ExecuteParametricSweepResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ExecuteParametricSweepResponse>>> ExecuteAsync(
        [FromServices] ExecuteParametricSweepCommand command,
        [FromBody] ExecuteParametricSweepRequest request)
    {
        return await command.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a Parametric Sweep sensitivity analysis while streaming real-time <see cref="MelloSilveiraTools.Core.Pipelines.Models.PipelineProgressEvent"/>
    /// notifications as newline-delimited JSON (<c>application/x-ndjson</c>).
    /// </summary>
    /// <param name="command">The parametric sweep execution command.</param>
    /// <param name="request">The parametric sweep request payload.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    [HttpPost("stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task ExecuteStreamAsync(
        [FromServices] ExecuteParametricSweepCommand command,
        [FromBody] ExecuteParametricSweepRequest request,
        CancellationToken cancellationToken = default)
    {
        await PipelineProgressStreamWriter.StreamProgressAsync(
            HttpContext,
            (progress, _) => command.ExecuteAsync(request with { Progress = progress }),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all stored CSV files associated with the specified parametric sweep external identifier.
    /// </summary>
    /// <param name="repository">The database repository.</param>
    /// <param name="externalIdentifier">The unique external identifier of the parametric sweep run.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>A <see cref="ListedResult{T}"/> of <see cref="StoredFileEntity"/> records.</returns>
    [HttpGet("{externalIdentifier}/files")]
    [ProducesResponseType(typeof(ListedResult<StoredFileEntity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListedResult<StoredFileEntity>>> GetFilesAsync(
        [FromServices] IRepository repository,
        [FromRoute] string externalIdentifier,
        CancellationToken cancellationToken = default)
    {
        StoredFileFilter filter = new() { ExternalIdentifier = externalIdentifier };
        List<StoredFileEntity> files = [];
        await foreach (StoredFileEntity entity in repository.GetAsync<StoredFileEntity, StoredFileFilter>(filter, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            files.Add(entity);
        }

        ListedResult<StoredFileEntity> result = Result.CreateListedSuccessOk(files);
        return result.BuildHttpResponse();
    }

    /// <summary>
    /// Downloads a specific stored CSV file associated with the given external identifier and file record ID.
    /// </summary>
    /// <param name="repository">The database repository.</param>
    /// <param name="fileStorageService">The file storage service used to open the physical file stream.</param>
    /// <param name="externalIdentifier">The unique external identifier of the parametric sweep run.</param>
    /// <param name="fileId">The database primary key of the <see cref="StoredFileEntity"/>.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>A streaming file download response or 404 Not Found.</returns>
    [HttpGet("{externalIdentifier}/files/{fileId:long}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFileByIdAsync(
        [FromServices] IRepository repository,
        [FromServices] IFileStorageService fileStorageService,
        [FromRoute] string externalIdentifier,
        [FromRoute] long fileId,
        CancellationToken cancellationToken = default)
    {
        StoredFileEntity? entity = await repository.GetAsync<StoredFileEntity>(fileId, cancellationToken).ConfigureAwait(false);
        if (entity is null || !string.Equals(entity.ExternalIdentifier, externalIdentifier, StringComparison.Ordinal))
        {
            return Result.CreateNotFound($"Stored file '{fileId}' for identifier '{externalIdentifier}' was not found.").BuildHttpResponse();
        }

        Stream fileStream = fileStorageService.OpenRead(entity.Location);
        return File(fileStream, entity.ContentType, entity.FileName);
    }
}
