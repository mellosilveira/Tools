using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes HTTP endpoints for processing experimental raw data streams, streaming real-time pipeline progress,
/// and querying or downloading generated CSV artifacts by external identifier.
/// </summary>
[ApiController]
[Route("api/v1/experimental-data")]
public class ExperimentalDataController : ControllerBase
{
    /// <summary>
    /// Processes uploaded experimental strain and stress CSV files through the segmentation, curve fitting, and simulation pipeline.
    /// </summary>
    /// <param name="command">The experimental data processing command.</param>
    /// <param name="request">The multipart form request containing raw files and configuration.</param>
    /// <returns>A <see cref="Result{T}"/> containing the generated or supplied external identifier.</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(Result<ProcessExperimentalDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ProcessExperimentalDataResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ProcessExperimentalDataResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ProcessExperimentalDataResponse>>> ProcessAsync(
        [FromServices] ProcessExperimentalDataCommand command,
        [FromForm] ProcessExperimentalDataRequest request)
    {
        return await command.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Processes uploaded experimental strain and stress CSV files while streaming real-time <see cref="MelloSilveiraTools.Core.Pipelines.Models.PipelineProgressEvent"/>
    /// notifications as newline-delimited JSON (<c>application/x-ndjson</c>).
    /// </summary>
    /// <param name="command">The experimental data processing command.</param>
    /// <param name="request">The multipart form request containing raw files and configuration.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    [HttpPost("process/stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task ProcessStreamAsync(
        [FromServices] ProcessExperimentalDataCommand command,
        [FromForm] ProcessExperimentalDataRequest request,
        CancellationToken cancellationToken = default)
    {
        await PipelineProgressStreamWriter.StreamProgressAsync(
            HttpContext,
            (progress, _) => command.ExecuteAsync(request with { Progress = progress }),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all stored files associated with the specified experimental data processing external identifier.
    /// </summary>
    /// <param name="repository">The database repository.</param>
    /// <param name="externalIdentifier">The unique external identifier of the processing run.</param>
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
    /// Downloads a specific stored file associated with the given external identifier and file record ID.
    /// </summary>
    /// <param name="repository">The database repository.</param>
    /// <param name="fileStorageService">The file storage service used to open the physical file stream.</param>
    /// <param name="externalIdentifier">The unique external identifier of the processing run.</param>
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
