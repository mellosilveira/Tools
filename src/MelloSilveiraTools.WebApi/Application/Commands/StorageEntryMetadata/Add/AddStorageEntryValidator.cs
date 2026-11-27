using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;

/// <summary>
/// Validates <see cref="AddStorageEntryRequest"/> payloads prior to storage creation.
/// </summary>
public sealed class AddStorageEntryValidator : IValidator<AddStorageEntryRequest>
{
    /// <inheritdoc/>
    public ValueTask<Result> ValidateAsync(AddStorageEntryRequest value, CancellationToken cancellationToken = default)
    {
        if (value.File is null || value.File.Length <= 0)
            return ValueTask.FromResult(Result.CreateBadRequest("A non-empty file must be provided."));

        if (string.IsNullOrWhiteSpace(value.File.FileName))
            return ValueTask.FromResult(Result.CreateBadRequest("File name must be provided."));

        if (string.IsNullOrWhiteSpace(value.Identifier) || ContainsPathTraversal(value.Identifier))
            return ValueTask.FromResult(Result.CreateBadRequest("A valid 'Identifier' without path traversal segments must be provided."));

        if (string.IsNullOrWhiteSpace(value.Context) || ContainsPathTraversal(value.Context))
            return ValueTask.FromResult(Result.CreateBadRequest("A valid 'Context' without path traversal segments must be provided."));

        if (ContainsPathTraversal(value.File.FileName))
            return ValueTask.FromResult(Result.CreateBadRequest("File name must not contain path traversal segments."));

        return ValueTask.FromResult(Result.CreateSuccessOk());
    }

    private static bool ContainsPathTraversal(string input)
    {
        string normalized = input.Replace('\\', '/');
        if (Path.IsPathRooted(input))
            return true;

        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "." or "..");
    }
}
