using MelloSilveiraTools.Core.Managers.File.Csv;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps;

/// <summary>
/// Pipeline step responsible for persisting processed experimental data points and their physical segment classifications to a CSV file.
/// Writes the CSV header upon initialization and sequentially appends each data point line in batches.
/// </summary>
public sealed class ExperimentalDataFileWriterStep : IAsyncPipelineStep<SegmentedDataPoint>
{
    private const string Context = "ExperimentalData";
    private const string CsvHeader = "Time,Strain,Stress,SegmentType,StrainRate,StrainAcceleration,StressRate,StressAcceleration";
    private readonly CsvFileWriter _writer;

    private ExperimentalDataFileWriterStep(string location, CsvFileWriter writer)
    {
        Location = location;
        _writer = writer;
    }

    /// <summary>
    /// Gets the persistent storage location where the segmented CSV file is stored.
    /// </summary>
    public string Location { get; }

    /// <inheritdoc />
    public string Name => nameof(ExperimentalDataFileWriterStep);

    /// <summary>
    /// Asynchronously creates a new <see cref="ExperimentalDataFileWriterStep"/> instance initialized with a timestamped writable storage stream.
    /// </summary>
    /// <param name="fileStorageService">The file storage service used to allocate and open the target CSV stream.</param>
    /// <param name="pipelineIdentifier">Unique business identifier prefix for the created storage artifact.</param>
    /// <returns>A tuple containing the resolved storage location and the initialized writer step.</returns>
    public static async Task<(string Location, ExperimentalDataFileWriterStep Step)> CreateAsync(IFileStorageService fileStorageService, string pipelineIdentifier)
    {
        (Stream stream, string location) = await fileStorageService.CreateWriteTimebasedAsync(Context, pipelineIdentifier, FileExtensions.CommaSeparatedValues).ConfigureAwait(false);
        CsvFileWriter writer = new(stream, CsvHeader);
        return (location, new ExperimentalDataFileWriterStep(location, writer));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(SegmentedDataPoint input, CancellationToken cancellationToken = default)
    {
        ProcessedDataPoint point = input.ProcessedDataPoint;
        string row = FormattableString.Invariant($"{point.Time:G7},{point.Strain:G7},{point.Stress:G7},{(int)input.SegmentType},{point.StrainRate:G7},{point.StrainAcceleration:G7},{point.StressRate:G7},{point.StressAcceleration:G7}");
        await _writer.WriteRowAsync(row, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
