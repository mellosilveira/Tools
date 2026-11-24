using MelloSilveiraTools.Core.Files.Providers;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;

/// <summary>
/// Wraps a readable file <see cref="Stream"/> and automatically deletes the underlying artifact from
/// <see cref="IFileStorageService"/> immediately after the stream is closed/disposed (e.g., once an HTTP file download completes).
/// </summary>
/// <param name="logger">Logger used to report non-fatal cleanup failures.</param>
/// <param name="fileStorageService">Storage service managing the temporary artifact.</param>
/// <param name="innerStream">Readable stream of the generated file.</param>
/// <param name="location">Canonical storage location to delete upon disposal.</param>
internal sealed class DeleteOnDisposeStream(
    ILogger logger,
    IFileStorageService fileStorageService,
    Stream innerStream,
    string location) : Stream
{
    private const int MaxDeleteAttempts = 4;
    private int _disposed;

    /// <inheritdoc/>
    public override bool CanRead => innerStream.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => innerStream.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => innerStream.CanWrite;

    /// <inheritdoc/>
    public override long Length => innerStream.Length;

    /// <inheritdoc/>
    public override long Position
    {
        get => innerStream.Position;
        set => innerStream.Position = value;
    }

    /// <inheritdoc/>
    public override void Flush() => innerStream.Flush();

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken) => innerStream.FlushAsync(cancellationToken);

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer) => innerStream.Read(buffer);

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => innerStream.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => innerStream.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);

    /// <inheritdoc/>
    public override void SetLength(long value) => innerStream.SetLength(value);

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await innerStream.DisposeAsync().ConfigureAwait(false);
        await DeleteQuietlyAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            innerStream.Dispose();
            DeleteQuietlyAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }

    private async ValueTask DeleteQuietlyAsync()
    {
        for (int attempt = 1; attempt <= MaxDeleteAttempts; attempt++)
        {
            try
            {
                await fileStorageService.DeleteAsync(location).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < MaxDeleteAttempts)
            {
                int delayMs = 50 * attempt * attempt;
                logger.LogInformation(ex, "Transient failure deleting temporary file '{Location}' (attempt {Attempt}/{MaxAttempts}). Retrying in {DelayMs} ms.", location, attempt, MaxDeleteAttempts, delayMs);
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete temporary specimen analysis file at '{Location}' after {MaxAttempts} attempts.", location, MaxDeleteAttempts);
            }
        }
    }
}
