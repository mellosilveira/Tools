using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Application.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes unified HTTP endpoints for querying and downloading stored files (<see cref="StoredFileEntity"/>)
/// by their <c>ExternalIdentifier</c>.
/// </summary>
[ApiController]
[Route("api/v1/stored-files")]
public class StoredFilesController(ILogger<StoredFilesController> logger) : CrudController<StoredFileEntity, StoredFileFilter>(logger)
{
    protected override string ResourceName => "stored file";

    [HttpGet("{fileId:long}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadByIdAsync(
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] IRepository repository,
        [FromRoute] long fileId,
        CancellationToken cancellationToken = default)
    {
        StoredFileEntity? entity = await repository.GetAsync<StoredFileEntity>(fileId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return Result.CreateNotFound($"Stored file with id '{fileId}' was not found.").BuildHttpResponse();

        IFileStorageService? fileStorageService = serviceProvider.GetKeyedService<IFileStorageService>(entity.StorageProvider);
        if (fileStorageService is null)
            return Result.CreateNotFound($"Storage provider '{entity.StorageProvider}' is not configured.").BuildHttpResponse();

        bool exists = await fileStorageService.ExistsAsync(entity.Location, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result.CreateNotFound($"Physical file for '{entity.FileName}' was not found on storage.").BuildHttpResponse();

        Stream fileStream = await fileStorageService.OpenReadAsync(entity.Location, cancellationToken).ConfigureAwait(false);
        return File(fileStream, entity.ContentType, entity.FileName);
    }
}
