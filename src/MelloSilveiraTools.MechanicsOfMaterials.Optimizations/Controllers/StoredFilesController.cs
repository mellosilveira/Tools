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
public class StoredFilesController(IRepository repository, IFileStorageService fileStorageService) : ControllerBase
{
    [HttpGet("{externalIdentifier}")]
    [ProducesResponseType(typeof(ListedResult<StoredFileEntity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListedResult<StoredFileEntity>>> GetByExternalIdentifierAsync(
        [FromRoute] string externalIdentifier,
        [FromQuery] string? context = null,
        CancellationToken cancellationToken = default)
    {
        StoredFileFilter filter = new() { ExternalIdentifier = externalIdentifier, Context = context };
        List<StoredFileEntity> files = await repository
            .GetAsync<StoredFileEntity, StoredFileFilter>(filter, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.CreateListedSuccessOk(files).BuildHttpResponse();
    }

    [HttpGet("{externalIdentifier}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadByExternalIdentifierAsync(
        [FromRoute] string externalIdentifier,
        [FromQuery] string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        StoredFileFilter filter = new() { ExternalIdentifier = externalIdentifier, FileName = fileName };
        StoredFileEntity? entity = await repository.GetFirstOrDefaultAsync<StoredFileEntity, StoredFileFilter>(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return Result.CreateNotFound($"No stored file found for external identifier '{externalIdentifier}'.").BuildHttpResponse();
        }

        return CreateFileResult(entity);
    }

    [HttpGet("{externalIdentifier}/{fileId:long}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadByIdAsync(
        [FromRoute] string externalIdentifier,
        [FromRoute] long fileId,
        CancellationToken cancellationToken = default)
    {
        StoredFileEntity? entity = await repository.GetAsync<StoredFileEntity>(fileId, cancellationToken).ConfigureAwait(false);
        if (entity is null || !string.Equals(entity.ExternalIdentifier, externalIdentifier, StringComparison.Ordinal))
        {
            return Result.CreateNotFound($"Stored file '{fileId}' for external identifier '{externalIdentifier}' was not found.").BuildHttpResponse();
        }

        return CreateFileResult(entity);
    }

    private IActionResult CreateFileResult(StoredFileEntity entity)
    {
        if (!fileStorageService.Exists(entity.Location))
        {
            return Result.CreateNotFound($"Physical file for '{entity.FileName}' was not found on storage.").BuildHttpResponse();
        }

        return File(fileStorageService.OpenRead(entity.Location), entity.ContentType, entity.FileName);
    }
}
