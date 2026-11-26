using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;

/// <summary>
/// Validates <see cref="UpdateStorageEntryFileRequest"/> payloads prior to overwriting a stored file.
/// </summary>
public sealed class UpdateStorageEntryFileValidator : IValidator<UpdateStorageEntryFileRequest>
{
    /// <inheritdoc/>
    public ValueTask<Result> ValidateAsync(UpdateStorageEntryFileRequest value, CancellationToken cancellationToken = default)
    {
        if (value.FileId <= 0)
            return ValueTask.FromResult(Result.CreateBadRequest("FileId must be greater than zero."));

        if (value.File is null || value.File.Length <= 0)
            return ValueTask.FromResult(Result.CreateBadRequest("A non-empty file must be provided."));

        return ValueTask.FromResult(Result.CreateSuccessOk());
    }
}
