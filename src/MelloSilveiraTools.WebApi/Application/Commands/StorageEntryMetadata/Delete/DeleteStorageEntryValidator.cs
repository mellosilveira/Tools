using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.Core.Commands.StorageEntryMetadata.Delete;

/// <summary>
/// Validates <see cref="DeleteStorageEntryRequest"/> payloads prior to deletion.
/// </summary>
public sealed class DeleteStorageEntryValidator : IValidator<DeleteStorageEntryRequest>
{
    /// <inheritdoc/>
    public Result Validate(DeleteStorageEntryRequest value)
    {
        bool hasValidId = value.FileId.HasValue && value.FileId.Value > 0;
        bool hasLocation = !string.IsNullOrWhiteSpace(value.Location);

        if (!hasValidId && !hasLocation)
            return Result.CreateBadRequest("Either a positive 'FileId' or a non-empty 'Location' must be provided.");

        return Result.CreateSuccessOk();
    }
}
