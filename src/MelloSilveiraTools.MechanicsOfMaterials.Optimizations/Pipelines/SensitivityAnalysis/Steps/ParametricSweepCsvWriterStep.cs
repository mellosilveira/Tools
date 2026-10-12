using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.Common;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using System.Globalization;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// A terminal I/O boundary step that consumes strictly formatted payloads and partitions 
/// the numerical results into segregated CSV files based on the evaluated variable.
/// Maintains open stream buffers during the execution for high performance.
/// </summary>
public sealed class ParametricSweepCsvWriterStep : BaseCsvWriterStep, IAsyncPipelineStep<ParametricSweepFinalPayload>
{
    private const string CsvHeader = "VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress";

    private readonly string _outputDirectory;
    private readonly Dictionary<string, SweepFileWriter> _writers = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParametricSweepCsvWriterStep"/> class.
    /// </summary>
    /// <param name="outputDirectory">Directory path where segregated sensitivity CSV files will be written.</param>
    public ParametricSweepCsvWriterStep(string outputDirectory = "SensitivityResults")
    {
        _outputDirectory = outputDirectory;
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    /// <inheritdoc />
    public override string Name => nameof(ParametricSweepCsvWriterStep);

    /// <summary>
    /// Gets the primary generated CSV file path, or the first generated path, or empty string if none have been created yet.
    /// </summary>
    public override string OutputFullFileName => _writers.Values.FirstOrDefault()?.FilePath ?? string.Empty;

    /// <inheritdoc />
    public override IReadOnlyCollection<string> GeneratedFilePaths => [.. _writers.Values.Select(w => w.FilePath)];

    /// <summary>
    /// Gets a dictionary mapping evaluated variable names to their corresponding generated CSV file paths.
    /// </summary>
    public IReadOnlyDictionary<string, string> GeneratedFilePathsByVariable => _writers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FilePath);

    /// <inheritdoc />
    public async Task ExecuteAsync(ParametricSweepFinalPayload input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_writers.TryGetValue(input.VariableName, out SweepFileWriter? fileWriter))
        {
            string filePath = Path.Combine(_outputDirectory, $"{input.VariableName}_Sensitivity.csv");
            StreamWriter writer = CreateWriter(filePath);
            await writer.WriteLineAsync(CsvHeader).ConfigureAwait(false);
            fileWriter = new SweepFileWriter(filePath, writer);
            _writers[input.VariableName] = fileWriter;
        }

        string row = string.Format(
            CultureInfo.InvariantCulture,
            "{0:G},{1:G},{2:G},{3:G},{4:G},{5:G},{6:G}",
            input.VariableValue,
            input.InitialStress,
            input.FinalStress,
            input.AsymptoteTime,
            input.AsymptoteStress,
            input.AbsoluteDeltaStress,
            input.PercentageDeltaStress);

        fileWriter.BatchCount = await WriteRowAsync(fileWriter.Writer, fileWriter.Buffer, fileWriter.BatchCount, row, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        List<Exception> exceptions = [];
        foreach (SweepFileWriter fileWriter in _writers.Values)
        {
            try
            {
                await fileWriter.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }

        GC.SuppressFinalize(this);
    }

    private sealed class SweepFileWriter(string filePath, StreamWriter writer) : IAsyncDisposable
    {
        public string FilePath => filePath;
        public StreamWriter Writer => writer;
        public StringBuilder Buffer { get; } = new(BufferCapacity);
        public int BatchCount;

        public async ValueTask DisposeAsync()
        {
            await FlushAndDisposeAsync(writer, Buffer).ConfigureAwait(false);
        }
    }
}
