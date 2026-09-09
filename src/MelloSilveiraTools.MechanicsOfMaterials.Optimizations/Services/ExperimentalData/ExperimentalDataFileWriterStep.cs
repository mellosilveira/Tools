using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Pipeline step responsible for persisting processed experimental data points to a CSV file.
/// Writes the CSV header upon initialization and sequentially appends each data point line.
/// </summary>
/// <param name="writer">The writer instance to append CSV rows to.</param>
/// <param name="outputFilePath">The logical file path of the output file.</param>
/// <param name="leaveOpen">Whether to leave the underlying writer open upon disposal.</param>
public sealed class ExperimentalDataFileWriterStep(StreamWriter writer, string outputFilePath, bool leaveOpen = false) : IAsyncPipelineStep<SegmentedDataPoint, SegmentedDataPoint>
{
    private bool _headerWritten;
    private bool _disposed;

    /// <inheritdoc/>
    public string Name => "ExperimentalDataFileWriter";

    /// <summary>
    /// Gets the full path of the generated CSV file.
    /// </summary>
    public string OutputFilePath => outputFilePath;

    /// <inheritdoc/>
    public async Task<SegmentedDataPoint> ExecuteAsync(SegmentedDataPoint input, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(input.ProcessedDataPoint, cancellationToken).ConfigureAwait(false);
        return input;
    }

    /// <inheritdoc/>
    public async Task<ProcessedDataPoint> ExecuteAsync(ProcessedDataPoint input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_headerWritten)
        {
            await writer.WriteLineAsync("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration").ConfigureAwait(false);
            _headerWritten = true;
        }

        await writer.WriteLineAsync($"{input.Time},{input.Strain},{input.StrainRate},{input.StrainAcceleration},{input.Stress},{input.StressRate},{input.StressAcceleration}").ConfigureAwait(false);
        return input;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_headerWritten)
        {
            await writer.WriteLineAsync("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration").ConfigureAwait(false);
            _headerWritten = true;
        }

        await writer.FlushAsync().ConfigureAwait(false);
        
        if (!leaveOpen)
            await writer.DisposeAsync().ConfigureAwait(false);
    }
}
