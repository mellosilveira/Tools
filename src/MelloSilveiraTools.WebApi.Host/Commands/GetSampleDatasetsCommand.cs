using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Read;

namespace MelloSilveiraTools.WebApi.Host.Commands;

/// <summary>
/// Lists all pre-bundled sample dataset files stored via <c>AddStorageEntryCommand</c>,
/// returning a <see cref="StorageEntryResultData"/> entry for each sample file so callers can download them
/// directly through <c>GET /api/v1/storage-entry-metadata/{fileId}/download</c>.
/// </summary>
/// <param name="readStorageEntriesCommand">Command used to query stored file entries and metadata.</param>
public sealed class GetSampleDatasetsCommand(ReadStorageEntriesCommand readStorageEntriesCommand) : ListedCommandBaseWithoutRequest<StorageEntryResultData>
{
    /// <summary>
    /// External correlation identifier assigned to bundled sample datasets.
    /// </summary>
    public const string SampleIdentifier = "sample-datasets";

    /// <summary>
    /// Storage context partition under which bundled sample datasets are uploaded on startup.
    /// </summary>
    public const string SampleContext = "SamplesData";

    /// <inheritdoc/>
    protected override Task<ListedResult<StorageEntryResultData>> ExecuteCommandAsync(CancellationToken cancellationToken = default) => readStorageEntriesCommand.ExecuteAsync(new ReadStorageEntriesRequest
    {
        Identifier = SampleIdentifier,
        Context = SampleContext,
    }, cancellationToken);
}
