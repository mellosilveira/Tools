using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Upload;

/// <summary>
/// Validates, stores, and registers metadata for a pair of uploaded experimental strain and stress CSV files.
/// </summary>
/// <param name="validator">Validator for uploaded CSV file requests.</param>
/// <param name="fileStorageService">Local file storage service.</param>
/// <param name="repository">Relational repository for file metadata persistence.</param>
public sealed class UploadExperimentalDataCommand(
    IValidator<UploadExperimentalDataRequest> validator, 
    IFileStorageService fileStorageService, 
    IRepository repository)
    : CommandBaseWithData<UploadExperimentalDataRequest, UploadExperimentalDataResultData>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<UploadExperimentalDataResultData>> ExecuteCommandAsync(UploadExperimentalDataRequest request, CancellationToken cancellationToken = default)
    {
        string uploadIdentifier = $"upload-{Guid.NewGuid():N}";

        Result<(long EntityId, StorageEntryMetadata Metadata)> strainResult = await SaveUploadedCsvAsync(uploadIdentifier, "strain.csv", request.StrainFile, cancellationToken).ConfigureAwait(false);
        if (!strainResult.Success)
            return Result.CreateError(strainResult.StatusCode, $"Failed to save uploaded strain file: {strainResult.JoinedMessages}");

        Result<(long EntityId, StorageEntryMetadata Metadata)> stressResult = await SaveUploadedCsvAsync(uploadIdentifier, "stress.csv", request.StrainFile, cancellationToken).ConfigureAwait(false);
        if (!strainResult.Success)
        {
            (long strainEntityId, StorageEntryMetadata strainMetadata) = strainResult.Data;
            await CleanupUploadedEntryAsync(strainMetadata.Location, strainEntityId, cancellationToken).ConfigureAwait(false);
            return Result.CreateError(strainResult.StatusCode, $"Failed to save uploaded stress file: {strainResult.JoinedMessages}");
        }

        return Result.CreateSuccessOk(new UploadExperimentalDataResultData
        {
            UploadIdentifier = uploadIdentifier,
            StrainFileMetadata = strainResult.Data.Metadata,
            StrainStorageEntryId = strainResult.Data.EntityId,
            StressFileMetadata = stressResult.Data.Metadata,
            StressStorageEntryId = stressResult.Data.EntityId,
        });
    }

    private async Task<Result<(long EntityId, StorageEntryMetadata Metadata)>> SaveUploadedCsvAsync(
        string uploadIdentifier,
        string targetFileName,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        (Stream targetStream, string location) = await fileStorageService
            .CreateWriteAsync(uploadIdentifier, "ExperimentalUploads", $"{file.FileName}_{targetFileName}", cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await using (targetStream.ConfigureAwait(false))
            {
                await file.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);
            }

            StorageEntryMetadata metadata = await fileStorageService.GetMetadataAsync(location, cancellationToken).ConfigureAwait(false);
            StorageEntryMetadataEntity entity = StorageEntryMetadataEntity.Create(metadata, uploadIdentifier);

            Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
            if (insertResult.Success || insertResult.IsConflict)
                return (insertResult.Data, metadata);

            await CleanupUploadedEntryAsync(location, null, cancellationToken).ConfigureAwait(false);
            return Result.CreateError(insertResult.StatusCode, $"Failed to persist metadata for '{targetFileName}': {insertResult.JoinedMessages}");
        }
        catch
        {
            await CleanupUploadedEntryAsync(location, null, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task CleanupUploadedEntryAsync(string location, long? entityId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(location))
            await fileStorageService.DeleteAsync(location, cancellationToken).ConfigureAwait(false);

        if (entityId is not null)
            await repository.DeleteAsync<StorageEntryMetadataEntity>(entityId.Value, cancellationToken).ConfigureAwait(false);
    }
}
