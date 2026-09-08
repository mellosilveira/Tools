using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Pipeline step responsible for persisting processed experimental data points to a CSV file.
/// Writes the CSV header upon initialization and sequentially appends each data point line.
/// </summary>
public sealed class ExperimentalDataFileWriterStep : IPipelineStep<SegmentedDataPoint, SegmentedDataPoint>
{
    private readonly StreamWriter _writer;
    private readonly string _outputFilePath;
    private readonly bool _leaveOpen;
    private bool _headerWritten;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExperimentalDataFileWriterStep"/> class using file manager configuration.
    /// </summary>
    /// <param name="fileManager">The file manager used to resolve the time-based output file path and large file writer.</param>
    /// <param name="outputFileUri">The destination directory path for the output CSV file.</param>
    /// <param name="uniqueIdentifier">The prefix or identifier used to name the output file.</param>
    public ExperimentalDataFileWriterStep(IFileManager fileManager, string outputFileUri, string uniqueIdentifier)
    {
        ArgumentNullException.ThrowIfNull(fileManager);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFileUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(uniqueIdentifier);

        FileInfo outputFile = fileManager.BuildTimebasedFileInfo(outputFileUri, uniqueIdentifier, FileExtensions.CommaSeparatedValues);
        _outputFilePath = outputFile.FullName;
        _writer = fileManager.CreateLargeFileWriter(outputFile);
        _leaveOpen = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExperimentalDataFileWriterStep"/> class using an existing <see cref="StreamWriter"/>.
    /// </summary>
    /// <param name="writer">The writer instance to append CSV rows to.</param>
    /// <param name="outputFilePath">The logical file path of the output file.</param>
    /// <param name="leaveOpen">Whether to leave the underlying writer open upon disposal.</param>
    public ExperimentalDataFileWriterStep(StreamWriter writer, string outputFilePath, bool leaveOpen = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _outputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));
        _leaveOpen = leaveOpen;
    }

    /// <inheritdoc/>
    public string Name => "ExperimentalDataFileWriter";

    /// <summary>
    /// Gets the full path of the generated CSV file.
    /// </summary>
    public string OutputFilePath => _outputFilePath;

    /// <inheritdoc/>
    public async Task<SegmentedDataPoint> ExecuteAsync(SegmentedDataPoint input, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(input.ProcessedDataPoint, cancellationToken).ConfigureAwait(false);
        return input;
    }

    /// <inheritdoc/>
    public async Task<ProcessedDataPoint> ExecuteAsync(ProcessedDataPoint input, CancellationToken cancellationToken = default)
    {
        if (!_headerWritten)
        {
            await _writer.WriteLineAsync("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration").ConfigureAwait(false);
            _headerWritten = true;
        }

        await _writer.WriteLineAsync($"{input.Time},{input.Strain},{input.StrainRate},{input.StrainAcceleration},{input.Stress},{input.StressRate},{input.StressAcceleration}").ConfigureAwait(false);
        return input;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_headerWritten)
        {
            await _writer.WriteLineAsync("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration").ConfigureAwait(false);
            _headerWritten = true;
        }

        await _writer.FlushAsync().ConfigureAwait(false);

        if (!_leaveOpen)
        {
            await _writer.DisposeAsync().ConfigureAwait(false);
        }
    }
}
