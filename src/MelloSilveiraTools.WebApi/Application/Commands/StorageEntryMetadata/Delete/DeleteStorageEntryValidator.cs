using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Delete;

/// <summary>
/// Validates <see cref="DeleteStorageEntryRequest"/> payloads prior to deletion.
/// </summary>
public sealed class DeleteStorageEntryValidator : IValidator<DeleteStorageEntryRequest>
{
    /// <inheritdoc/>
    public ValueTask<Result> ValidateAsync(DeleteStorageEntryRequest value, CancellationToken cancellationToken = default)
    {
        bool hasValidId = value.FileId.HasValue && value.FileId.Value > 0;
        bool hasLocation = !string.IsNullOrWhiteSpace(value.Location);

        if (!hasValidId && !hasLocation)
            return ValueTask.FromResult(Result.CreateBadRequest("Either a positive 'FileId' or a non-empty 'Location' must be provided."));

        if (hasLocation && ContainsPathTraversal(value.Location!))
            return ValueTask.FromResult(Result.CreateBadRequest("Storage location must not contain path traversal segments."));

        return ValueTask.FromResult(Result.CreateSuccessOk());
    }

    private static bool ContainsPathTraversal(string location)
    {
        string normalized = location.Replace('\\', '/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "." or "..");
    }
}
