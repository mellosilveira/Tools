using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.Core.Commands.StorageEntryMetadata.Create;

/// <summary>
/// Validates <see cref="AddStorageEntryRequest"/> payloads prior to storage creation.
/// </summary>
public sealed class AddStorageEntryValidator : IValidator<AddStorageEntryRequest>
{
    /// <inheritdoc/>
    public Result Validate(AddStorageEntryRequest value)
    {
        if (value.File is null || value.File.Length <= 0)
            return Result.CreateBadRequest("A non-empty file must be provided.");

        if (string.IsNullOrWhiteSpace(value.File.FileName))
            return Result.CreateBadRequest("File name must be provided.");

        return Result.CreateSuccessOk();
    }
}
