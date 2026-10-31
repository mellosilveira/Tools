namespace MelloSilveiraTools.Core.Managers.File.Streams;

/// <summary>
/// Decorator stream that delegates all streaming operations to an underlying inner stream while binding
/// the lifetime of an associated <see cref="IDisposable"/> owner object (such as an HTTP response or SDK client handle)
/// to the lifetime of the stream.
/// </summary>
/// <remarks>
/// Guarantees that when consumers dispose of the stream via either <see cref="Dispose(bool)"/> or <see cref="DisposeAsync"/>,
/// both the inner stream and the owner object are deterministically disposed, preventing HTTP socket exhaustion and resource leaks.
/// </remarks>
/// <param name="innerStream">The actual payload data stream.</param>
/// <param name="owner">The parent resource owner responsible for underlying socket or connection handles.</param>
public sealed class DelegatingDisposableStream(Stream innerStream, IDisposable owner) : Stream
{
    private int _disposedState;

    /// <inheritdoc />
    public override bool CanRead => innerStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => innerStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => innerStream.CanWrite;

    /// <inheritdoc />
    public override long Length => innerStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => innerStream.Position;
        set => innerStream.Position = value;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        innerStream.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return innerStream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        return innerStream.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        return innerStream.Read(buffer);
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return innerStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return innerStream.ReadAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        return innerStream.Seek(offset, origin);
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        innerStream.SetLength(value);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        innerStream.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        innerStream.Write(buffer);
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return innerStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return innerStream.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposedState, 1) == 0)
        {
            innerStream.Dispose();
            owner.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposedState, 1) == 0)
        {
            await innerStream.DisposeAsync().ConfigureAwait(false);
            if (owner is IAsyncDisposable asyncOwner)
            {
                await asyncOwner.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                owner.Dispose();
            }
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
}
