using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;

/// <summary>
/// Validates <see cref="UpdateStorageEntryFileRequest"/> payloads prior to overwriting a stored file.
/// </summary>
public sealed class UpdateStorageEntryFileValidator : IValidator<UpdateStorageEntryFileRequest>
{
    /// <inheritdoc/>
    public Result Validate(UpdateStorageEntryFileRequest value)
    {
        if (value.FileId <= 0)
            return Result.CreateBadRequest("FileId must be greater than zero.");

        if (value.File is null || value.File.Length <= 0)
            return Result.CreateBadRequest("A non-empty file must be provided.");

        return Result.CreateSuccessOk();
    }
}
