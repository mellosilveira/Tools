using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Pipeline step responsible for persisting processed experimental data points to a CSV file.
/// Writes the CSV header upon initialization and sequentially appends each data point line.
/// </summary>
public sealed class ExperimentalDataFileWriterStep : IAsyncPipelineStep<SegmentedDataPoint>
{
    private const int LargeFileBufferSize = 128 * 1024; // 128 KB buffer
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

    public ExperimentalDataFileWriterStep(IFileManager fileManager, string uri, string identifier)
    {
        FileInfo fileInfo = fileManager.BuildTimebasedFileInfo(uri, identifier, FileExtensions.CommaSeparatedValues);
        OutputFullFileName = fileInfo.FullName;

        FileStream stream = fileInfo.Open(LargeFileStreamOptions);
        _writer = new StreamWriter(stream, Utf8Encoding, LargeFileBufferSize);
    }

    /// <inheritdoc/>
    public string Name => "ExperimentalDataFileWriter";

    /// <summary>
    /// Gets the full name of the generated CSV file.
    /// </summary>
    public string OutputFullFileName { get; }

    /// <inheritdoc/>
    public async Task ExecuteAsync(SegmentedDataPoint input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_headerWritten)
        {
            await _writer.WriteLineAsync("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration").ConfigureAwait(false);
            _headerWritten = true;
        }

        ProcessedDataPoint point = input.ProcessedDataPoint;
        await _writer.WriteLineAsync($"{point.Time},{point.Strain},{point.StrainRate},{point.StrainAcceleration},{point.Stress},{point.StressRate},{point.StressAcceleration}").ConfigureAwait(false);
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

        await _writer.FlushAsync().ConfigureAwait(false);
        await _writer.DisposeAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }
}
