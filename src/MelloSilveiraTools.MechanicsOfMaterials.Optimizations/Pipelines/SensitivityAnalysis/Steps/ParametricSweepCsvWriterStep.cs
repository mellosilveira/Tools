using MelloSilveiraTools.Core.Managers.File.Csv;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// Pipeline step that partitions evaluated parametric sweep payloads by variable name
/// and serializes each variable's sensitivity results into a dedicated CSV file.
/// </summary>
/// <param name="fileStorageService">Storage service responsible for resolving paths and opening file streams.</param>
/// <param name="identifier">Mandatory unique business identifier partitioning the generated sensitivity CSV files.</param>
public sealed class ParametricSweepCsvWriterStep(IFileStorageService fileStorageService, string identifier)
    : IAsyncPipelineStep<IReadOnlyList<ParametricSweepFinalPayload>, IReadOnlyCollection<StorageEntryMetadata>>
{
    private const string Context = "SensitivityAnalysis";
    private const string CsvHeader = "VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress";

    /// <inheritdoc />
    public string Name => nameof(ParametricSweepCsvWriterStep);

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<StorageEntryMetadata>> ExecuteAsync(
        IReadOnlyList<ParametricSweepFinalPayload> input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<StorageEntryMetadata> metadataList = [];

        foreach (IGrouping<string, ParametricSweepFinalPayload> group in input.GroupBy(p => p.VariableName))
        {
            string fileName = $"{group.Key}_SensitivityAnalysis{FileExtensions.CommaSeparatedValues}";
            (Stream stream, string location) = await fileStorageService
                .CreateWriteAsync(Context, identifier, fileName, cancellationToken)
                .ConfigureAwait(false);

            await using (stream)
            await using (CsvFileWriter writer = new(stream, CsvHeader))
            {
                foreach (ParametricSweepFinalPayload payload in group)
                {
                    string row = FormattableString.Invariant($"{payload.VariableValue:G},{payload.InitialStress:G},{payload.FinalStress:G},{payload.AsymptoteTime:G},{payload.AsymptoteStress:G},{payload.AbsoluteDeltaStress:G},{payload.PercentageDeltaStress:G}");
                    await writer.WriteRowAsync(row, cancellationToken).ConfigureAwait(false);
                }
            }

            StorageEntryMetadata metadata = await fileStorageService.GetMetadataAsync(location, cancellationToken).ConfigureAwait(false);
            metadataList.Add(metadata);
        }

        return metadataList;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
