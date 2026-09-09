using System.Buffers;
using System.Buffers.Text;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// High-performance, streaming numeric CSV reader powered by native <see cref="PipeReader"/> and <see cref="Utf8Parser"/>.
/// Reads delimited rows containing an arbitrary number of numeric (<see cref="double"/>) columns with minimal memory allocations.
/// </summary>
/// <param name="stream">The underlying stream to read from.</param>
/// <param name="delimiter">The column delimiter byte (default is comma <c>','</c>).</param>
/// <param name="leaveOpen">Whether to leave the underlying stream open after disposing the reader.</param>
/// <param name="bufferSize">The initial pipe buffer size.</param>
/// <param name="skipInvalidLines">Whether to skip unparseable lines (e.g. headers) instead of stopping.</param>
public class CsvStreamReader(
    Stream stream,
    byte delimiter = (byte)',',
    bool leaveOpen = true,
    int bufferSize = 4096,
    bool skipInvalidLines = false) : IAsyncDisposable
{
    private readonly PipeReader _pipeReader = PipeReader.Create(
        stream,
        new StreamPipeReaderOptions(
            pool: MemoryPool<byte>.Shared,
            bufferSize: bufferSize,
            minimumReadSize: 1024,
            leaveOpen: leaveOpen));

    private readonly byte _delimiter = delimiter;
    private readonly bool _skipInvalidLines = skipInvalidLines;

    /// <summary>
    /// Reads the next numerical row asynchronously from the CSV stream, parsing all delimited numeric columns.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the read operation.</param>
    /// <returns>
    /// An array of parsed <see cref="double"/> values representing each column in the row, or <see langword="null"/> if the end of the stream is reached or an unparseable line is encountered.
    /// </returns>
    public async ValueTask<double[]?> ReadNextRowAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            ReadResult result = await _pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = result.Buffer;
            SequencePosition? position = buffer.PositionOf((byte)'\n');

            if (position != null)
            {
                ReadOnlySequence<byte> line = buffer.Slice(0, position.Value);
                SequencePosition nextPosition = buffer.GetPosition(1, position.Value);

                if (IsLineEmpty(line))
                {
                    _pipeReader.AdvanceTo(nextPosition);
                    continue;
                }

                bool parsed = TryParseLine(line, out double[]? values);
                _pipeReader.AdvanceTo(nextPosition);

                if (parsed)
                    return values;

                if (_skipInvalidLines)
                    continue;

                return null;
            }

            if (result.IsCompleted)
            {
                if (!buffer.IsEmpty && !IsLineEmpty(buffer))
                {
                    bool parsed = TryParseLine(buffer, out double[]? values);
                    _pipeReader.AdvanceTo(buffer.End);
                    if (parsed)
                        return values;
                }
                else
                {
                    _pipeReader.AdvanceTo(buffer.End);
                }

                return null;
            }

            _pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    /// <summary>
    /// Asynchronously streams all parsed numeric rows from the CSV stream until the end of the stream or the first invalid row.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
    /// <returns>An async stream of <see cref="double"/> arrays, where each array contains the parsed column values.</returns>
    public async IAsyncEnumerable<double[]> ReadAllRowsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            double[]? values = await ReadNextRowAsync(cancellationToken).ConfigureAwait(false);
            if (values is null)
                break;

            yield return values;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _pipeReader.CompleteAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private static bool IsLineEmpty(ReadOnlySequence<byte> line)
    {
        if (line.IsEmpty)
            return true;

        foreach (ReadOnlyMemory<byte> segment in line)
        {
            ReadOnlySpan<byte> span = segment.Span;
            for (int i = 0; i < span.Length; i++)
            {
                byte b = span[i];
                if (b != (byte)'\r' && b != (byte)' ' && b != (byte)'\t')
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool TryParseLine(ReadOnlySequence<byte> line, out double[]? values)
    {
        values = null;

        if (line.IsEmpty)
            return false;

        if (line.IsSingleSegment)
            return TryParseSpan(line.FirstSpan, out values);

        if (line.Length <= 512)
        {
            Span<byte> stackBuffer = stackalloc byte[(int)line.Length];
            line.CopyTo(stackBuffer);
            return TryParseSpan(stackBuffer, out values);
        }

        byte[] rented = ArrayPool<byte>.Shared.Rent((int)line.Length);
        try
        {
            line.CopyTo(rented);
            return TryParseSpan(rented.AsSpan(0, (int)line.Length), out values);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private bool TryParseSpan(ReadOnlySpan<byte> span, out double[]? values)
    {
        values = null;

        span = span.Trim((byte)'\r').Trim((byte)' ').Trim((byte)'\t');
        if (span.IsEmpty)
            return false;

        int delimiterCount = span.Count(_delimiter);
        int maxColumns = delimiterCount + 1;

        double[]? rentedColumns = null;
        Span<double> columnSpan = maxColumns <= 32
            ? stackalloc double[maxColumns]
            : (rentedColumns = ArrayPool<double>.Shared.Rent(maxColumns)).AsSpan(0, maxColumns);

        try
        {
            int count = 0;
            ReadOnlySpan<byte> remaining = span;

            while (true)
            {
                int commaIndex = remaining.IndexOf(_delimiter);
                ReadOnlySpan<byte> token;

                if (commaIndex >= 0)
                {
                    token = remaining[..commaIndex].Trim((byte)' ').Trim((byte)'\t');
                    remaining = remaining[(commaIndex + 1)..];
                }
                else
                {
                    token = remaining.Trim((byte)' ').Trim((byte)'\t');
                    remaining = [];
                }

                if (token.IsEmpty || !Utf8Parser.TryParse(token, out double val, out int bytesConsumed) || bytesConsumed != token.Length)
                    return false;

                columnSpan[count++] = val;
                if (remaining.IsEmpty && commaIndex < 0)
                    break;
            }

            if (count == 0)
                return false;

            values = new double[count];
            columnSpan[..count].CopyTo(values);
            return true;
        }
        finally
        {
            if (rentedColumns != null)
                ArrayPool<double>.Shared.Return(rentedColumns);
        }
    }
}
