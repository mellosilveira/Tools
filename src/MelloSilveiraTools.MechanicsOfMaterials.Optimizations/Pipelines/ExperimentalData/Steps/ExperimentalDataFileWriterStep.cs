using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.Common;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Globalization;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step responsible for persisting processed experimental data points to a CSV file.
/// Writes the CSV header upon initialization and sequentially appends each data point line in batches.
/// </summary>
public sealed class ExperimentalDataFileWriterStep : BaseCsvWriterStep, IAsyncPipelineStep<SegmentedDataPoint>
{
    private const string CsvHeader = "Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration";

    private readonly string _outputFullFileName;
    private readonly StreamWriter _writer;
    private readonly StringBuilder _buffer = new(BufferCapacity);
    private readonly int _batchThreshold;
    private int _batchCount;
    private bool _headerWritten;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExperimentalDataFileWriterStep"/> class.
    /// </summary>
    /// <param name="fileManager">File manager used to resolve the destination file path.</param>
    /// <param name="uri">Target destination directory URI.</param>
    /// <param name="identifier">Unique business identifier prefix for the generated file.</param>
    /// <param name="bufferSize">Internal buffer size for the underlying stream. Defaults to 128 KB.</param>
    /// <param name="batchThreshold">Row accumulation threshold before batch-flushing. Defaults to 1000.</param>
    public ExperimentalDataFileWriterStep(
        IFileManager fileManager,
        string uri,
        string identifier,
        int bufferSize = BufferSize,
        int batchThreshold = BatchThreshold)
    {
        FileInfo fileInfo = fileManager.BuildTimebasedFileInfo(uri, identifier, FileExtensions.CommaSeparatedValues);
        _outputFullFileName = fileInfo.FullName;
        _writer = CreateWriter(_outputFullFileName, bufferSize);
        _batchThreshold = batchThreshold;
    }

    /// <inheritdoc />
    public override string Name => "ExperimentalDataFileWriter";

    /// <summary>
    /// Gets the full name of the generated CSV file.
    /// </summary>
    public override string OutputFullFileName => _outputFullFileName;

    /// <inheritdoc />
    public async Task ExecuteAsync(SegmentedDataPoint input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_headerWritten)
        {
            await _writer.WriteLineAsync(CsvHeader).ConfigureAwait(false);
            _headerWritten = true;
        }

        ProcessedDataPoint p = input.ProcessedDataPoint;
        string row = string.Format(
            CultureInfo.InvariantCulture,
            "{0},{1},{2},{3},{4},{5},{6}",
            p.Time,
            p.Strain,
            p.StrainRate,
            p.StrainAcceleration,
            p.Stress,
            p.StressRate,
            p.StressAcceleration);

        _batchCount = await WriteRowAsync(_writer, _buffer, _batchCount, row, cancellationToken, _batchThreshold).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await FlushAndDisposeAsync(_writer, _buffer, CsvHeader, _headerWritten).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
