using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Managers.File.Streams;

/// <summary>
/// Abstract base disk-backed stream buffer that implements the Disk Spooling Pattern for remote cloud object storage.
/// Intercepts write operations to a local temporary file stream, and upon completion/disposal, opens a read stream
/// from disk and delegates to the derived class for cloud transmission before deterministically deleting the temporary file and directory.
/// </summary>
/// <remarks>
/// <para>
/// <b>Architectural Rationale: Disk Spooling vs. In-Memory Buffering</b>
/// </para>
/// <para>
/// Buffering outgoing payloads on local disk ensures <c>O(1)</c> memory allocation, protects the Large Object Heap (LOH) from
/// fragmentation, decouples producer computational performance from WAN latency/throttling, and prevents corrupted partial objects
/// in cloud buckets when operations abort or fail mid-execution.
/// </para>
/// </remarks>
/// <param name="logger">Logger used to record progress and diagnostics.</param>
/// <param name="defaultStorage">Local file storage service used to manage the temporary spooling file.</param>
/// <param name="localWriteStream">Writable stream pointing to the local temporary file.</param>
/// <param name="tempLocation">Local path or identifier of the temporary spooling file.</param>
/// <param name="cancellationToken">Cancellation token observing operation abort signals.</param>
public abstract class DiskSpoolUploadStreamBase(
    ILogger logger,
    IFileStorageService defaultStorage,
    Stream localWriteStream,
    string tempLocation,
    CancellationToken cancellationToken = default) : Stream
{
    private int _disposedState;
    private bool _uploaded;
    private bool _faulted;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => logger;

    /// <summary>
    /// Gets the resolved temporary file location on disk.
    /// </summary>
    protected string TempLocation => tempLocation;

    /// <summary>
    /// Gets the underlying local storage service.
    /// </summary>
    protected IFileStorageService DefaultStorage => defaultStorage;

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => localWriteStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => localWriteStream.CanWrite;

    /// <inheritdoc />
    public override long Length => localWriteStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => localWriteStream.Position;
        set => localWriteStream.Position = value;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        localWriteStream.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return localWriteStream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("DiskSpoolUploadStream is write-only during spooling.");
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        throw new NotSupportedException("DiskSpoolUploadStream is write-only during spooling.");
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("DiskSpoolUploadStream is write-only during spooling.");
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("DiskSpoolUploadStream is write-only during spooling.");
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        return localWriteStream.Seek(offset, origin);
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        try
        {
            localWriteStream.SetLength(value);
        }
        catch
        {
            _faulted = true;
            throw;
        }
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        try
        {
            localWriteStream.Write(buffer, offset, count);
        }
        catch
        {
            _faulted = true;
            throw;
        }
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        try
        {
            localWriteStream.Write(buffer);
        }
        catch
        {
            _faulted = true;
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        try
        {
            await localWriteStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _faulted = true;
            throw;
        }
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        try
        {
            await localWriteStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _faulted = true;
            throw;
        }
    }

    /// <summary>
    /// Explicitly aborts the upload, preventing any transmission to remote storage upon disposal.
    /// </summary>
    public void Abort()
    {
        _faulted = true;
    }

    /// <summary>
    /// Executes the remote cloud upload when invoked upon stream disposal.
    /// </summary>
    /// <param name="readStream">Readable stream opened from the spooled local file.</param>
    /// <param name="fileSizeBytes">The total size of the spooled payload in bytes.</param>
    /// <param name="cancellationToken">Cancellation token observing operation abort signals.</param>
    /// <returns>A value task representing the asynchronous upload operation.</returns>
    protected abstract ValueTask UploadPayloadAsync(Stream readStream, long fileSizeBytes, CancellationToken cancellationToken);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposedState, 1) == 0)
        {
            try
            {
                localWriteStream.Dispose();

                if (!_uploaded && !_faulted && !cancellationToken.IsCancellationRequested)
                {
                    _uploaded = true;
                    long fileSizeBytes = 0;
                    try
                    {
                        StorageEntryMetadata metadata = defaultStorage.GetMetadataAsync(tempLocation).AsTask().GetAwaiter().GetResult();
                        fileSizeBytes = metadata.SizeBytes;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to retrieve temporary file metadata before upload: {TempLocation}", tempLocation);
                    }

                    using (Stream readStream = defaultStorage.OpenReadAsync(tempLocation).AsTask().GetAwaiter().GetResult())
                    {
                        UploadPayloadAsync(readStream, fileSizeBytes, cancellationToken).AsTask().GetAwaiter().GetResult();
                    }
                }
            }
            finally
            {
                DestroyCreatedStructuresAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposedState, 1) == 0)
        {
            try
            {
                await localWriteStream.DisposeAsync().ConfigureAwait(false);

                if (!_uploaded && !_faulted && !cancellationToken.IsCancellationRequested)
                {
                    _uploaded = true;
                    long fileSizeBytes = 0;
                    try
                    {
                        StorageEntryMetadata metadata = await defaultStorage.GetMetadataAsync(tempLocation).ConfigureAwait(false);
                        fileSizeBytes = metadata.SizeBytes;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to retrieve temporary file metadata before upload: {TempLocation}", tempLocation);
                    }

                    await using (Stream readStream = await defaultStorage.OpenReadAsync(tempLocation).ConfigureAwait(false))
                    {
                        await UploadPayloadAsync(readStream, fileSizeBytes, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await DestroyCreatedStructuresAsync().ConfigureAwait(false);
            }
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }

    private async ValueTask DestroyCreatedStructuresAsync()
    {
        logger.LogDebug("Destroying temporary on-disk structures. File: {TempLocation}", tempLocation);

        try
        {
            await defaultStorage.DeleteAsync(tempLocation).ConfigureAwait(false);
            logger.LogDebug("Temporary file deleted successfully: {TempLocation}", tempLocation);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error attempting to delete temporary file: {TempLocation}", tempLocation);
        }

        string? tempDirectory = Path.GetDirectoryName(tempLocation);
        if (!string.IsNullOrEmpty(tempDirectory))
        {
            try
            {
                await defaultStorage.DeleteAsync(tempDirectory).ConfigureAwait(false);
                logger.LogDebug("Temporary upload directory destroyed successfully: {TempDirectory}", tempDirectory);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error attempting to delete temporary directory: {TempDirectory}", tempDirectory);
            }
        }
    }
}
