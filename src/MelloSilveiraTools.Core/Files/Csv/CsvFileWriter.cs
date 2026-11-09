using MelloSilveiraTools.Core.Models;
using System.Globalization;
using System.Text;

namespace MelloSilveiraTools.Core.Files.Csv;

/// <summary>
/// Encapsulates a buffered asynchronous CSV file writing session over an underlying output stream.
/// Manages row batching buffer, immediate header emission, and resource disposal.
/// Standardizes all line endings to CRLF (<c>\n</c>, RFC 4180) to eliminate file size discrepancies and SHA-256 checksum mismatches across operating systems.
/// </summary>
/// <param name="stream">Writable output stream.</param>
/// <param name="header">Mandatory CSV header line written immediately upon session initialization.</param>
public sealed class CsvFileWriter(Stream stream, string header) : IAsyncDisposable
{
    private const string CsvNewLine = "\n";
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
    private readonly StreamWriter _writer = CreateWriter(stream, header);
    private readonly StringBuilder _buffer = new(16 * 1024);
    private int _batchCount;
    private bool _disposed;

    /// <summary>
    /// Appends a formatted CSV row string and batch-flushes to disk when the threshold is reached.
    /// Delimited with standard CRLF (<c>\n</c>) per RFC 4180.
    /// </summary>
    /// <param name="row">Formatted CSV row text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteRowAsync(string row, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _buffer.Append(row).Append(CsvNewLine);

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
    /// Delimited with standard CRLF (<c>\n</c>) per RFC 4180.
    /// </summary>
    /// <param name="writeRow">Action writing column values into the provided row buffer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteRowAsync(Action<StringBuilder> writeRow, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writeRow(_buffer);
        _buffer.Append(CsvNewLine);

        if (++_batchCount >= 1000)
        {
            await _writer.WriteAsync(_buffer, cancellationToken).ConfigureAwait(false);
            _buffer.Clear();
            _batchCount = 0;
        }
    }

    /// <summary>
    /// Appends a CSV row by formatting a sequence of numeric values with invariant culture,
    /// delimiting them with commas and batch-flushing to disk when the threshold is reached.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="values">Sequence of double-precision floating-point values to format and append as columns.</param>
    public Task WriteRowAsync(CancellationToken cancellationToken, params ReadOnlySpan<double> values)
        => WriteRowAsync(values, format: "G7", cancellationToken);

    /// <summary>
    /// Appends a CSV row by formatting a sequence of numeric values with invariant culture,
    /// delimiting them with commas and batch-flushing to disk when the threshold is reached.
    /// </summary>
    /// <param name="values">Sequence of double-precision floating-point values to format and append as columns.</param>
    public Task WriteRowAsync(params ReadOnlySpan<double> values)
        => WriteRowAsync(values, format: "G7", cancellationToken: default);

    /// <summary>
    /// Appends a CSV row by formatting a sequence of numeric values with a custom format and invariant culture,
    /// delimiting them with commas and batch-flushing to disk when the threshold is reached.
    /// </summary>
    /// <param name="values">Sequence of double-precision floating-point values to format and append as columns.</param>
    /// <param name="format">Optional numeric format string (e.g. "G7", "G", "F4"). If null, standard invariant representation is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task WriteRowAsync(ReadOnlySpan<double> values, string? format = "G7", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Span<char> span = stackalloc char[32];
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                _buffer.Append(CsvSeparators.Main);

            if (values[i].TryFormat(span, out int charsWritten, format, CultureInfo.InvariantCulture))
                _buffer.Append(span[..charsWritten]);
            else
                _buffer.Append(values[i].ToString(format, CultureInfo.InvariantCulture));
        }

        _buffer.Append(CsvNewLine);

        if (++_batchCount >= 1000)
            return FlushBufferAsync(cancellationToken);

        return Task.CompletedTask;
    }

    private async Task FlushBufferAsync(CancellationToken cancellationToken)
    {
        await _writer.WriteAsync(_buffer, cancellationToken).ConfigureAwait(false);
        _buffer.Clear();
        _batchCount = 0;
    }

    private static StreamWriter CreateWriter(Stream stream, string header)
    {
        StreamWriter writer = new(stream, Utf8Encoding, 128 * 1024) { NewLine = CsvNewLine };
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
