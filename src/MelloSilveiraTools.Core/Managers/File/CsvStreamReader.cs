using System.Buffers;
using System.Buffers.Text;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// High-performance, streaming numeric CSV reader powered by native <see cref="PipeReader"/> and <see cref="Utf8Parser"/>.
/// Reads delimited rows containing an arbitrary number of numeric (<see cref="double"/>) columns with minimal memory allocations.
/// </summary>
public class CsvStreamReader : IAsyncDisposable
{
    private readonly PipeReader _pipeReader;
    private readonly byte _delimiter;
    private readonly bool _skipInvalidLines;
    private double[] _rowBuffer = null!; // Initialized safely via CreateAsync

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream">The underlying stream to read from.</param>
    /// <param name="delimiter">The column delimiter byte (default is comma <c>','</c>).</param>
    /// <param name="leaveOpen">Whether to leave the underlying stream open after disposing the reader.</param>
    /// <param name="bufferSize">The initial pipe buffer size.</param>
    /// <param name="skipInvalidLines">Whether to skip unparseable lines (e.g. headers) instead of stopping.</param>
    private CsvStreamReader(Stream stream, byte delimiter, bool leaveOpen, int bufferSize, bool skipInvalidLines)
    {
        _pipeReader = PipeReader.Create(
            stream,
            new StreamPipeReaderOptions(
                pool: MemoryPool<byte>.Shared,
                bufferSize: bufferSize,
                minimumReadSize: 1024,
                leaveOpen: leaveOpen));

        _delimiter = delimiter;
        _skipInvalidLines = skipInvalidLines;
    }

    /// <summary>
    /// Asynchronously creates a new instance of <see cref="CsvStreamReader"/>, reads the header line to initialize internal buffers, and advances the stream to the first data row.
    /// </summary>
    public static async Task<CsvStreamReader> CreateAsync(
        Stream stream,
        byte delimiter = (byte)',',
        bool leaveOpen = true,
        int bufferSize = 4096,
        bool skipInvalidLines = false,
        CancellationToken cancellationToken = default)
    {
        CsvStreamReader reader = new(stream, delimiter, leaveOpen, bufferSize, skipInvalidLines);
        await reader.ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
        return reader;
    }

    private async Task ReadHeaderAsync(CancellationToken cancellationToken)
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

                if (!IsLineEmpty(line))
                {
                    _rowBuffer = ArrayPool<double>.Shared.Rent(CountColumns(line));
                    _pipeReader.AdvanceTo(nextPosition);
                    return;
                }

                _pipeReader.AdvanceTo(nextPosition);
                continue;
            }

            if (result.IsCompleted)
            {
                if (!buffer.IsEmpty && !IsLineEmpty(buffer))
                {
                    _rowBuffer = ArrayPool<double>.Shared.Rent(CountColumns(buffer));
                    _pipeReader.AdvanceTo(buffer.End);
                }
                else
                {
                    _rowBuffer = ArrayPool<double>.Shared.Rent(2);
                    _pipeReader.AdvanceTo(buffer.End);
                }

                return;
            }

            _pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    /// <summary>
    /// Reads the next numerical row asynchronously from the CSV stream, parsing all delimited numeric columns directly into an internal buffer.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the read operation.</param>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the parsed columns, or an empty memory if the end of the stream is reached or an unparseable line is encountered.</returns>
    public async ValueTask<ReadOnlyMemory<double>> ReadNextRowAsync(CancellationToken cancellationToken = default)
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

                int count = TryParseLine(line);
                _pipeReader.AdvanceTo(nextPosition);

                if (count > 0)
                    return _rowBuffer.AsMemory(0, count);

                if (_skipInvalidLines)
                    continue;

                return default;
            }

            if (result.IsCompleted)
            {
                if (!buffer.IsEmpty && !IsLineEmpty(buffer))
                {
                    int count = TryParseLine(buffer);
                    _pipeReader.AdvanceTo(buffer.End);
                    if (count > 0)
                        return _rowBuffer.AsMemory(0, count);
                }
                else
                {
                    _pipeReader.AdvanceTo(buffer.End);
                }

                return default;
            }

            _pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    /// <summary>
    /// Asynchronously streams all parsed numeric rows from the CSV stream until the end of the stream or the first invalid row.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
    /// <returns>An async stream of <see cref="ReadOnlyMemory{T}"/>, where each memory contains the parsed column values.</returns>
    public async IAsyncEnumerable<ReadOnlyMemory<double>> ReadAllRowsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ReadOnlyMemory<double> values = await ReadNextRowAsync(cancellationToken).ConfigureAwait(false);
            if (values.IsEmpty)
                break;

            yield return values;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _pipeReader.CompleteAsync().ConfigureAwait(false);
        if (_rowBuffer != null)
        {
            ArrayPool<double>.Shared.Return(_rowBuffer);
            _rowBuffer = null!;
        }
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

    private int CountColumns(ReadOnlySequence<byte> line)
    {
        int count = 1;
        foreach (ReadOnlyMemory<byte> segment in line)
        {
            ReadOnlySpan<byte> span = segment.Span;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == _delimiter)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private int TryParseLine(ReadOnlySequence<byte> line)
    {
        if (line.IsEmpty)
            return 0;

        if (line.IsSingleSegment)
            return TryParseSpan(line.FirstSpan);

        if (line.Length <= 512)
        {
            Span<byte> stackBuffer = stackalloc byte[(int)line.Length];
            line.CopyTo(stackBuffer);
            return TryParseSpan(stackBuffer);
        }

        byte[] rented = ArrayPool<byte>.Shared.Rent((int)line.Length);
        try
        {
            line.CopyTo(rented);
            return TryParseSpan(rented.AsSpan(0, (int)line.Length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private int TryParseSpan(ReadOnlySpan<byte> span)
    {
        span = span.Trim((byte)'\r').Trim((byte)' ').Trim((byte)'\t');
        if (span.IsEmpty)
            return 0;

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
                return 0;

            if (count >= _rowBuffer!.Length)
            {
                double[] newBuffer = ArrayPool<double>.Shared.Rent(_rowBuffer.Length * 2);
                _rowBuffer.AsSpan().CopyTo(newBuffer);
                ArrayPool<double>.Shared.Return(_rowBuffer);
                _rowBuffer = newBuffer;
            }

            _rowBuffer[count++] = val;

            if (remaining.IsEmpty && commaIndex < 0)
                break;
        }

        return count;
    }
}
