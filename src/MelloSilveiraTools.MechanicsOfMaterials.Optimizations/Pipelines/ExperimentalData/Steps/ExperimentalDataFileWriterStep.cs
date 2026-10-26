using MelloSilveiraTools.Core.Managers.File.Csv;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step responsible for persisting processed experimental data points to a CSV file.
/// Writes the CSV header upon initialization and sequentially appends each data point line in batches.
/// </summary>
public sealed class ExperimentalDataFileWriterStep : IAsyncPipelineStep<SegmentedDataPoint>
{
    private const string Context = "ExperimentalData";
    private const string CsvHeader = "Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration";
    private readonly CsvFileWriter _writer;

    private ExperimentalDataFileWriterStep(CsvFileWriter writer)
    {
        _writer = writer;
    }

    /// <inheritdoc />
    public string Name => nameof(ExperimentalDataFileWriterStep);

    public static async Task<(string Location, ExperimentalDataFileWriterStep Step)> CreateAsync(IFileStorageService fileStorageService, string pipelineIdentifier )
    {
        (Stream stream, string location) = await fileStorageService.CreateWriteTimebasedAsync(Context, pipelineIdentifier, FileExtensions.CommaSeparatedValues);
        CsvFileWriter writer = new(stream, CsvHeader);
        return (location, new ExperimentalDataFileWriterStep(writer));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(SegmentedDataPoint input, CancellationToken cancellationToken = default)
    {
        ProcessedDataPoint point = input.ProcessedDataPoint;
        string row = FormattableString.Invariant($"{point.Time:G7},{point.Strain:G7},{point.StrainRate:G7},{point.StrainAcceleration:G7},{point.Stress:G7},{point.StressRate:G7},{point.StressAcceleration:G7}");
        await _writer.WriteRowAsync(row, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
