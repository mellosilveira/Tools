using System.Text;

namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// Encapsulates a buffered asynchronous CSV file writing session.
/// Manages the underlying FileStream, StreamWriter, row batching buffer, immediate header emission, and resource disposal.
/// </summary>
/// <param name="filePath">Target destination file path.</param>
/// <param name="header">Mandatory CSV header line written immediately upon session initialization.</param>
public sealed class CsvFileWriter(string filePath, string header) : IAsyncDisposable
{
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
    private readonly StreamWriter _writer = CreateWriter(filePath, header);
    private readonly StringBuilder _buffer = new(16 * 1024);
    private int _batchCount;
    private bool _disposed;

    /// <summary>
    /// Gets the destination file path.
    /// </summary>
    public string FilePath { get; } = filePath;

    /// <summary>
    /// Appends a formatted CSV row string and batch-flushes to disk when the threshold is reached.
    /// </summary>
    /// <param name="row">Formatted CSV row text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteRowAsync(string row, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _buffer.Append(row).Append(Environment.NewLine);

        if (++_batchCount >= 1000)
        {
            await _writer.WriteAsync(_buffer, cancellationToken).ConfigureAwait(false);
            _buffer.Clear();
            _batchCount = 0;
        }
    }

    /// <summary>
    /// Appends a CSV row directly into the internal buffer via an action, avoiding intermediate string allocations,
    /// and batch-flushes to disk when the threshold is reached.
    /// </summary>
    /// <param name="writeRow">Action writing column values into the provided row buffer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteRowAsync(Action<StringBuilder> writeRow, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writeRow(_buffer);
        _buffer.Append(Environment.NewLine);

        if (++_batchCount >= 1000)
        {
            await _writer.WriteAsync(_buffer, cancellationToken).ConfigureAwait(false);
            _buffer.Clear();
            _batchCount = 0;
        }
    }

    /// <summary>
    /// Disposes a collection of asynchronous sessions, accumulating exceptions and re-throwing as an <see cref="AggregateException"/> if any fault occurred.
    /// </summary>
    /// <param name="sessions">Collection of asynchronous disposable sessions.</param>
    public static async ValueTask DisposeAllAsync(IEnumerable<IAsyncDisposable> sessions)
    {
        List<Exception> exceptions = [];
        foreach (IAsyncDisposable session in sessions)
        {
            try
            {
                await session.DisposeAsync().ConfigureAwait(false);
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
    }

    private static StreamWriter CreateWriter(string filePath, string header)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        FileStreamOptions options = new()
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
            BufferSize = 128 * 1024
        };

        FileStream stream = new(filePath, options);
        StreamWriter writer = new(stream, Utf8Encoding, 128 * 1024);
        writer.WriteLine(header);
        return writer;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_buffer.Length > 0)
        {
            await _writer.WriteAsync(_buffer).ConfigureAwait(false);
            _buffer.Clear();
        }

        await _writer.FlushAsync().ConfigureAwait(false);
        await _writer.DisposeAsync().ConfigureAwait(false);
    }
}
