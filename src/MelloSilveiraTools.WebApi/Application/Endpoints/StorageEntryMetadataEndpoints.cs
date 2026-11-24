using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Download;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Read;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MelloSilveiraTools.WebApi.Application.Endpoints;

/// <summary>
/// Maps the same unified HTTP CRUD and download surface as <see cref="Controllers.StorageEntryMetadataController"/>
/// using minimal APIs.
/// </summary>
public static class StorageEntryMetadataEndpoints
{
    /// <summary>
    /// Maps all stored-file metadata and binary download endpoints under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/storage-entry-metadata"</c>.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain authorization or tags.</returns>
    public static RouteGroupBuilder MapStorageEntryMetadataEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/storage-entry-metadata")
    {
        RouteGroupBuilder group = builder.MapGroup(pattern).WithTags("StorageEntryMetadata");

        group
            .MapPost("/", async (
                HttpContext httpContext,
                [FromServices] AddStorageEntryCommand command,
                IFormFile file,
                [FromForm] string identifier,
                [FromForm] string context,
                [FromForm] string? externalIdentifier,
                [FromForm] StorageProvider? storageProvider) => await command
                .ExecuteAsync(new AddStorageEntryRequest
                {
                    File = file,
                    Identifier = identifier,
                    Context = context,
                    ExternalIdentifier = externalIdentifier,
                    StorageProvider = storageProvider
                }, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .DisableAntiforgery()
            .Produces<Result<StorageEntryMetadataEntity>>(StatusCodes.Status201Created)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .WithName("CreateStorageEntryMetadata")
            .WithSummary("Stores a file in the requested (or default) storage provider and persists its metadata.");

        group
            .MapGet("/", async (HttpContext httpContext, [FromServices] ReadStorageEntriesCommand command, [AsParameters] ReadStorageEntriesRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<ListedResult<StorageEntryResultData>>(StatusCodes.Status200OK)
            .WithName("ReadStorageEntryMetadata")
            .WithSummary("Reads stored file metadata from storage and database, grouped by identifier, context, and file extension.");

        group
            .MapPut("/{fileId:long}", async (HttpContext httpContext, [FromServices] UpdateStorageEntryFileCommand command, long fileId, IFormFile file) => await command
                .ExecuteAsync(new UpdateStorageEntryFileRequest { FileId = fileId, File = file }, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .DisableAntiforgery()
            .Produces<Result<StorageEntryMetadataEntity>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .WithName("UpdateStorageEntryFile")
            .WithSummary("Overwrites the physical file for an existing entry, replacing the database metadata record.");

        group
            .MapDelete("/", async (HttpContext httpContext, [FromServices] DeleteStorageEntryCommand command, [FromBody] DeleteStorageEntryRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .WithName("DeleteStorageEntry")
            .WithSummary("Idempotently deletes a file from storage and/or database using the request payload.");

        group
            .MapDelete("/{fileId:long}", async (HttpContext httpContext, [FromServices] DeleteStorageEntryCommand command, long fileId, [FromBody] DeleteStorageEntryRequest? request) =>
            {
                DeleteStorageEntryRequest effectiveRequest = request is null
                    ? new DeleteStorageEntryRequest { FileId = fileId }
                    : request with { FileId = request.FileId ?? fileId };

                return await command
                    .ExecuteAsync(effectiveRequest, httpContext.RequestAborted)
                    .ToHttpResultAsync()
                    .ConfigureAwait(false);
            })
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .WithName("DeleteStorageEntryById")
            .WithSummary("Idempotently deletes a file from storage and/or database by its metadata identifier.");

        group
            .MapGet("/{fileId:long}/download", async (HttpContext httpContext, [FromServices] DownloadStoredFileCommand command, long fileId) =>
            {
                Result<DownloadStoredFileResultData> result = await command
                    .ExecuteAsync(new DownloadStoredFileRequest { FileId = fileId }, httpContext.RequestAborted)
                    .ConfigureAwait(false);

                return result.Success
                    ? Results.File(result.Data!.FileStream, result.Data.ContentType, result.Data.FileName)
                    : result.ToHttpResult();
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .WithName("DownloadStorageEntryById")
            .WithSummary("Streams a stored file by its metadata entity ID.");

        return group;
    }
}
