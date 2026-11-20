using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.WebApi.Host.Controllers;

/// <summary>
/// Accepts multipart CSV uploads from browser/HTTP clients and stores them into <see cref="IFileStorageService"/>
/// so they can be processed by the experimental data pipeline.
/// </summary>
[ApiController]
[Route("api/v1/experimental-data")]
public class ExperimentalDataUploadController(
    TimeProvider timeProvider,
    [FromKeyedServices(StorageProvider.Local)] IFileStorageService fileStorageService,
    IRepository repository) : ControllerBase
{
    private const long MaxCsvSizeBytes = 50 * 1024 * 1024;

    /// <summary>
    /// Response payload containing canonical storage locations and persisted metadata IDs for uploaded CSV files.
    /// </summary>
    public sealed record ExperimentalUploadResponse
    {
        /// <summary>
        /// Unique correlation identifier assigned to the uploaded pair.
        /// </summary>
        public required string UploadIdentifier { get; init; }

        /// <summary>
        /// Canonical storage location for the uploaded strain CSV.
        /// </summary>
        public required string StrainFileLocation { get; init; }

        /// <summary>
        /// Canonical storage location for the uploaded stress CSV.
        /// </summary>
        public required string StressFileLocation { get; init; }

        /// <summary>
        /// Database ID of the stored strain file metadata record.
        /// </summary>
        public required long StrainStorageEntryId { get; init; }

        /// <summary>
        /// Database ID of the stored stress file metadata record.
        /// </summary>
        public required long StressStorageEntryId { get; init; }
    }

    /// <summary>
    /// Uploads a pair of raw experimental CSV files (<c>strainFile</c> and <c>stressFile</c>) via <c>multipart/form-data</c>.
    /// </summary>
    /// <param name="strainFile">Raw experimental strain CSV file.</param>
    /// <param name="stressFile">Raw experimental stress CSV file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Storage locations and metadata IDs wrapped in <see cref="Result{T}"/>.</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxCsvSizeBytes * 2)]
    [ProducesResponseType(typeof(Result<ExperimentalUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAsync(
        IFormFile? strainFile,
        IFormFile? stressFile,
        CancellationToken cancellationToken = default)
    {
        Result validation = ValidateCsvPair(strainFile, stressFile);
        if (!validation.Success)
            return validation.BuildHttpResponse();

        string uploadId = $"upload-{Guid.NewGuid():N}";
        (bool strainOk, string strainLocation, long strainId, Result? strainError) = await SaveUploadedCsvAsync(uploadId, "strain.csv", strainFile!, cancellationToken).ConfigureAwait(false);
        if (!strainOk)
            return strainError!.BuildHttpResponse();

        (bool stressOk, string stressLocation, long stressId, Result? stressError) = await SaveUploadedCsvAsync(uploadId, "stress.csv", stressFile!, cancellationToken).ConfigureAwait(false);
        if (!stressOk)
        {
            await CleanupUploadedEntryAsync(strainLocation, strainId, cancellationToken).ConfigureAwait(false);
            return stressError!.BuildHttpResponse();
        }

        ExperimentalUploadResponse response = new()
        {
            UploadIdentifier = uploadId,
            StrainFileLocation = strainLocation,
            StressFileLocation = stressLocation,
            StrainStorageEntryId = strainId,
            StressStorageEntryId = stressId
        };

        return Result.CreateSuccessOk(response).BuildHttpResponse();
    }

    private static Result ValidateCsvPair(IFormFile? strainFile, IFormFile? stressFile)
    {
        if (strainFile is null || stressFile is null)
            return Result.CreateBadRequest("Both 'strainFile' and 'stressFile' must be provided.");

        if (strainFile.Length <= 0 || stressFile.Length <= 0)
            return Result.CreateBadRequest("Uploaded CSV files cannot be empty.");

        if (strainFile.Length > MaxCsvSizeBytes || stressFile.Length > MaxCsvSizeBytes)
            return Result.CreateBadRequest($"Each uploaded CSV file must not exceed {MaxCsvSizeBytes / (1024 * 1024)} MB.");

        if (!strainFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            || !stressFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return Result.CreateBadRequest("Only '.csv' files are supported.");

        return Result.CreateSuccessOk();
    }

    private async Task<(bool Success, string Location, long EntityId, Result? Error)> SaveUploadedCsvAsync(
        string uploadId,
        string targetFileName,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        (Stream targetStream, string location) = await fileStorageService
            .CreateWriteAsync(uploadId, "ExperimentalUploads", targetFileName, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await using (targetStream.ConfigureAwait(false))
            {
                await file.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await CleanupUploadedEntryAsync(location, 0L, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        StorageEntryMetadata metadata = new(
            StorageProvider.Local,
            location,
            Path.GetDirectoryName(location) ?? string.Empty,
            targetFileName,
            file.Length,
            timeProvider.GetUtcNow());

        StorageEntryMetadataEntity entity = StorageEntryMetadataEntity.Create(metadata, uploadId, "text/csv");
        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (!insertResult.Success)
        {
            await CleanupUploadedEntryAsync(location, 0L, cancellationToken).ConfigureAwait(false);
            return (false, string.Empty, 0L, Result.CreateError(insertResult.StatusCode, $"Failed to persist metadata for '{targetFileName}': {insertResult.JoinedMessages}"));
        }

        return (true, location, insertResult.Data, null);
    }

    private async Task CleanupUploadedEntryAsync(string location, long entityId, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(location))
                await fileStorageService.DeleteAsync(location, cancellationToken).ConfigureAwait(false);

            if (entityId > 0)
                await repository.DeleteAsync<StorageEntryMetadataEntity>(entityId, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
