using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Download;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Read;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.WebApi.Application.Controllers;

/// <summary>
/// Exposes unified HTTP CRUD and download endpoints for stored files (<see cref="StorageEntryMetadataEntity"/>)
/// and their underlying physical/cloud storage entries.
/// </summary>
[ApiController]
[Route("api/v1/storage-entry-metadata")]
public class StorageEntryMetadataController : ControllerBase
{
    /// <summary>
    /// Stores a file in the requested (or default) <see cref="Core.Files.Providers.IFileStorageService"/> and persists its metadata.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<StorageEntryMetadataEntity>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromServices] AddStorageEntryCommand command,
        [FromForm] AddStorageEntryRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Reads stored file metadata from storage and database, grouped by identifier, context, and file extension.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListedResult<StorageEntryResultData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReadAsync(
        [FromServices] ReadStorageEntriesCommand command,
        [FromQuery] ReadStorageEntriesRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Overwrites only the physical file for an existing entry, replacing the database metadata record.
    /// </summary>
    [HttpPut("{fileId:long}")]
    [ProducesResponseType(typeof(Result<StorageEntryMetadataEntity>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        [FromServices] UpdateStorageEntryFileCommand command,
        [FromRoute] long fileId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
        => await command
            .ExecuteAsync(new UpdateStorageEntryFileRequest { FileId = fileId, File = file }, cancellationToken)
            .BuildHttpResponseAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// Idempotently deletes a file from storage and/or database, failing only if absent from both.
    /// </summary>
    [HttpDelete]
    [HttpDelete("{fileId:long}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromServices] DeleteStorageEntryCommand command,
        [FromBody] DeleteStorageEntryRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Streams a stored file by its metadata entity ID.
    /// </summary>
    [HttpGet("{fileId:long}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadByIdAsync(
        [FromServices] DownloadStoredFileCommand command,
        [FromRoute] long fileId,
        CancellationToken cancellationToken = default)
    {
        Result<DownloadStoredFileResultData> result = await command.ExecuteAsync(new DownloadStoredFileRequest { FileId = fileId }, cancellationToken).ConfigureAwait(false);
        return result.Success
            ? File(result.Data!.FileStream, result.Data.ContentType, result.Data.FileName)
            : result.BuildHttpResponse();
    }
}
