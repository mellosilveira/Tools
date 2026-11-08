using MelloSilveiraTools.Core.Managers.File.Csv;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps;

/// <summary>
/// Pipeline step that partitions evaluated parametric sweep payloads by variable name
/// and serializes each variable's sensitivity results into a dedicated CSV file.
/// </summary>
/// <param name="fileStorageService">Storage service responsible for resolving paths and opening file streams.</param>
/// <param name="identifier">Mandatory unique business identifier partitioning the generated sensitivity CSV files.</param>
public sealed class ParametricSweepCsvWriterStep(
    IFileStorageService fileStorageService, 
    string identifier)
    : IAsyncPipelineStep<
        (string VariableName, RangeParameters VariableRange, IReadOnlyList<ParametricSweepOutput> Outputs), 
        (string VariableName, RangeParameters VariableRange, StorageEntryMetadata VariableOutputFile)>
{
    private const string Context = "SensitivityAnalysis";
    private const string CsvHeader = "VariableValue,InitialStress,FinalStress,AbsoluteDeltaStress,PercentageDeltaStress,AsymptoteTime";

    /// <inheritdoc />
    public string Name => nameof(ParametricSweepCsvWriterStep);

    /// <inheritdoc />
    public async Task<(string VariableName, RangeParameters VariableRange, StorageEntryMetadata VariableOutputFile)> ExecuteAsync(
        (string VariableName, RangeParameters VariableRange, IReadOnlyList<ParametricSweepOutput> Outputs) input,
        CancellationToken cancellationToken = default)
    {
        (Stream stream, string location) = await fileStorageService
            .CreateWriteTimebasedAsync(identifier, Context, input.VariableName, FileExtensions.CommaSeparatedValues, cancellationToken)
            .ConfigureAwait(false);

        await using (CsvFileWriter writer = new(stream, CsvHeader))
        {
            foreach (ParametricSweepOutput payload in input.Outputs)
            {
                await writer
                    .WriteRowAsync(cancellationToken, payload.VariableValue, payload.InitialStress, payload.FinalStress, payload.AbsoluteDeltaStress, payload.PercentageDeltaStress, payload.AsymptoteTime)
                    .ConfigureAwait(false);
            }
        }

        StorageEntryMetadata fileMetadata = await fileStorageService.GetMetadataAsync(location, cancellationToken).ConfigureAwait(false);
        return (input.VariableName, input.VariableRange, fileMetadata);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
