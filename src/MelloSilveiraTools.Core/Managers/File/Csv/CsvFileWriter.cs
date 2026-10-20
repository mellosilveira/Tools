using System.Text;

namespace MelloSilveiraTools.Core.Managers.File.Csv;

/// <summary>
/// Encapsulates a buffered asynchronous CSV file writing session over an underlying output stream.
/// Manages row batching buffer, immediate header emission, and resource disposal.
/// </summary>
/// <param name="stream">Writable output stream.</param>
/// <param name="header">Mandatory CSV header line written immediately upon session initialization.</param>
public sealed class CsvFileWriter(Stream stream, string header) : IAsyncDisposable
{
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
    private readonly StreamWriter _writer = CreateWriter(stream, header);
    private readonly StringBuilder _buffer = new(16 * 1024);
    private int _batchCount;
    private bool _disposed;

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

    private static StreamWriter CreateWriter(Stream stream, string header)
    {
        StreamWriter writer = new(stream, Utf8Encoding, 128 * 1024);
        writer.WriteLine(header);
        return writer;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

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
