using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using System.Collections.Concurrent;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// A terminal I/O boundary step that consumes strictly formatted payloads and partitions 
/// the numerical results into segregated CSV files based on the evaluated variable.
/// Maintains open stream buffers during the execution for high performance.
/// </summary>
public sealed class ParametricSweepCsvWriterStep : IAsyncPipelineStep<ParametricSweepFinalPayload>
{
    private readonly string _outputDirectory;
    private readonly ConcurrentDictionary<string, CsvFileContext> _contexts = new();
    private bool _disposed;

    public ParametricSweepCsvWriterStep(string outputDirectory = "SensitivityResults")
    {
        _outputDirectory = outputDirectory;
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    /// <inheritdoc />
    public string Name => nameof(ParametricSweepCsvWriterStep);

    /// <inheritdoc />
    public async Task ExecuteAsync(ParametricSweepFinalPayload input, CancellationToken ct)
    {
        if (_disposed) return;

        CsvFileContext context = _contexts.GetOrAdd(input.VariableName, key => new CsvFileContext(key, _outputDirectory));

        await context.WritePayloadAsync(input, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var context in _contexts.Values)
        {
            await context.DisposeAsync().ConfigureAwait(false);
        }

        _contexts.Clear();
    }

    private sealed class CsvFileContext : IAsyncDisposable
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

        private readonly StreamWriter _writer;
        private readonly StringBuilder _buffer;
        private readonly SemaphoreSlim _semaphore;
        private int _batchCount;
        private bool _headerWritten;
        private bool _disposed;

        public CsvFileContext(string variableName, string outputDirectory)
        {
            string filePath = Path.Combine(outputDirectory, "${variableName}_Sensitivity.csv");
            FileStream stream = new(filePath, LargeFileStreamOptions);
            _writer = new StreamWriter(stream, Utf8Encoding, LargeFileBufferSize);
            _buffer = new StringBuilder(1024 * 16);
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task WritePayloadAsync(ParametricSweepFinalPayload input, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!_headerWritten)
                {
                    await _writer.WriteLineAsync("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress").ConfigureAwait(false);
                    _headerWritten = true;
                }

                _buffer.Append("${input.VariableValue:G},${input.InitialStress:G},${input.FinalStress:G},${input.AsymptoteTime:G},${input.AsymptoteStress:G},${input.AbsoluteDeltaStress:G},${input.PercentageDeltaStress:G}" + Environment.NewLine);

                if (++_batchCount >= BatchThreshold)
                {
                    await _writer.WriteAsync(_buffer, ct).ConfigureAwait(false);
                    _buffer.Clear();
                    _batchCount = 0;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_buffer.Length > 0)
                {
                    await _writer.WriteAsync(_buffer).ConfigureAwait(false);
                    _buffer.Clear();
                }

                await _writer.FlushAsync().ConfigureAwait(false);
                await _writer.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
        }
    }
}
