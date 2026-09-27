using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step responsible for persisting processed experimental data points to a CSV file.
/// Writes the CSV header upon initialization and sequentially appends each data point line in batches.
/// </summary>
public sealed class ExperimentalDataFileWriterStep : IAsyncPipelineStep<SegmentedDataPoint>
{
    private const int LargeFileBufferSize = 128 * 1024; // 128 KB buffer
    private const int BatchThreshold = 1000;
    
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
    private static readonly FileStreamOptions LargeFileStreamOptions = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
        BufferSize = LargeFileBufferSize
    };

    private bool _headerWritten;
    private bool _disposed;
    private readonly StreamWriter _writer;
    private readonly StringBuilder _buffer;
    private int _batchCount;

    public ExperimentalDataFileWriterStep(IFileManager fileManager, string uri, string identifier)
    {
        FileInfo fileInfo = fileManager.BuildTimebasedFileInfo(uri, identifier, FileExtensions.CommaSeparatedValues);
        OutputFullFileName = fileInfo.FullName;

        FileStream stream = fileInfo.Open(LargeFileStreamOptions);
        _writer = new StreamWriter(stream, Utf8Encoding, LargeFileBufferSize);
        _buffer = new StringBuilder(1024 * 16);
    }

    /// <inheritdoc/>
    public string Name => "ExperimentalDataFileWriter";

    /// <summary>
    /// Gets the full name of the generated CSV file.
    /// </summary>
    public string OutputFullFileName { get; }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(SegmentedDataPoint input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_headerWritten)
        {
            await _writer.WriteLineAsync("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration").ConfigureAwait(false);
            _headerWritten = true;
        }

        ProcessedDataPoint p = input.ProcessedDataPoint;
        _buffer.Append($"{p.Time},{p.Strain},{p.StrainRate},{p.StrainAcceleration},{p.Stress},{p.StressRate},{p.StressAcceleration}{Environment.NewLine}");

        if (++_batchCount >= BatchThreshold)
        {
            await _writer.WriteAsync(_buffer, cancellationToken).ConfigureAwait(false);
            _buffer.Clear();
            _batchCount = 0;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_headerWritten)
        {
            await _writer.WriteLineAsync("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration").ConfigureAwait(false);
            _headerWritten = true;
        }

        if (_buffer.Length > 0)
        {
            await _writer.WriteAsync(_buffer).ConfigureAwait(false);
            _buffer.Clear();
        }

        await _writer.FlushAsync().ConfigureAwait(false);
        await _writer.DisposeAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }
}
