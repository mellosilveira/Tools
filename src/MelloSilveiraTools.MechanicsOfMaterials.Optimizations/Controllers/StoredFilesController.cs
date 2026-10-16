using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes unified HTTP endpoints for querying and downloading stored files (<see cref="StoredFileEntity"/>)
/// by their <c>ExternalIdentifier</c>.
/// </summary>
[ApiController]
[Route("api/v1/stored-files")]
public class StoredFilesController : ControllerBase
{
    /// <summary>
    /// Lists all stored files associated with the specified external identifier, optionally filtered by context.
    /// </summary>
    /// <param name="repository">The database repository.</param>
    /// <param name="externalIdentifier">The external identifier correlating files to an execution run.</param>
    /// <param name="context">Optional domain context filter (e.g., "ExperimentalDataRawProcessed", "MechanicalModelSimulation", "SensitivityAnalysis").</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>A <see cref="ListedResult{T}"/> containing all matching <see cref="StoredFileEntity"/> records.</returns>
    [HttpGet("{externalIdentifier}")]
    [ProducesResponseType(typeof(ListedResult<StoredFileEntity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListedResult<StoredFileEntity>>> GetByExternalIdentifierAsync(
        [FromServices] IRepository repository,
        [FromRoute] string externalIdentifier,
        [FromQuery] string? context = null,
        CancellationToken cancellationToken = default)
    {
        StoredFileFilter filter = new()
        {
            ExternalIdentifier = externalIdentifier,
            Context = context
        };

        List<StoredFileEntity> files = [];
        await foreach (StoredFileEntity entity in repository.GetAsync<StoredFileEntity, StoredFileFilter>(filter, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            files.Add(entity);
        }

        ListedResult<StoredFileEntity> result = Result.CreateListedSuccessOk(files);
        return result.BuildHttpResponse();
    }

    /// <summary>
    /// Streams and downloads a stored file identified by its external identifier and file name (or the first matching file if <paramref name="fileName"/> is omitted).
    /// </summary>
    /// <param name="repository">The database repository.</param>
    /// <param name="fileStorageService">The file storage service used to open the physical file stream.</param>
    /// <param name="externalIdentifier">The external identifier correlating the file to an execution run.</param>
    /// <param name="fileName">Optional file name when multiple files share the same external identifier.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>A streaming file download response or 404 Not Found.</returns>
    [HttpGet("{externalIdentifier}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadByExternalIdentifierAsync(
        [FromServices] IRepository repository,
        [FromServices] IFileStorageService fileStorageService,
        [FromRoute] string externalIdentifier,
        [FromQuery] string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        StoredFileFilter filter = new()
        {
            ExternalIdentifier = externalIdentifier,
            FileName = fileName
        };

        StoredFileEntity? entity = await repository.GetFirstOrDefaultAsync<StoredFileEntity, StoredFileFilter>(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return Result.CreateNotFound($"No stored file found for external identifier '{externalIdentifier}'.").BuildHttpResponse();
        }

        Stream fileStream = fileStorageService.OpenRead(entity.Location);
        return File(fileStream, entity.ContentType, entity.FileName);
    }

    /// <summary>
    /// Streams and downloads a stored file identified by its external identifier and database record ID.
    /// </summary>
    /// <param name="repository">The database repository.</param>
    /// <param name="fileStorageService">The file storage service used to open the physical file stream.</param>
    /// <param name="externalIdentifier">The external identifier correlating the file to an execution run.</param>
    /// <param name="fileId">The database primary key of the <see cref="StoredFileEntity"/>.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>A streaming file download response or 404 Not Found.</returns>
    [HttpGet("{externalIdentifier}/{fileId:long}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadByIdAsync(
        [FromServices] IRepository repository,
        [FromServices] IFileStorageService fileStorageService,
        [FromRoute] string externalIdentifier,
        [FromRoute] long fileId,
        CancellationToken cancellationToken = default)
    {
        StoredFileEntity? entity = await repository.GetAsync<StoredFileEntity>(fileId, cancellationToken).ConfigureAwait(false);
        if (entity is null || !string.Equals(entity.ExternalIdentifier, externalIdentifier, StringComparison.Ordinal))
        {
            return Result.CreateNotFound($"Stored file '{fileId}' for external identifier '{externalIdentifier}' was not found.").BuildHttpResponse();
        }

        Stream fileStream = fileStorageService.OpenRead(entity.Location);
        return File(fileStream, entity.ContentType, entity.FileName);
    }
}
