using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Streams;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Core.Managers.File;

public sealed class DiskSpoolUploadStreamBaseTests
{
    private sealed class TestDiskSpoolUploadStream : DiskSpoolUploadStreamBase
    {
        public bool UploadPayloadInvoked { get; private set; }
        public long UploadPayloadSizeBytes { get; private set; }

        public TestDiskSpoolUploadStream(
            IFileStorageService defaultStorage,
            Stream localWriteStream,
            string tempLocation,
            CancellationToken cancellationToken = default)
            : base(NullLogger.Instance, defaultStorage, localWriteStream, tempLocation, cancellationToken)
        {
        }

        protected override ValueTask UploadPayloadAsync(Stream readStream, long fileSizeBytes, CancellationToken cancellationToken)
        {
            UploadPayloadInvoked = true;
            UploadPayloadSizeBytes = fileSizeBytes;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task WriteAsync_BuffersToLocalStream()
    {
        Mock<IFileStorageService> defaultStorageMock = new();
        using MemoryStream localStream = new();
        string tempLocation = "storage/temp/upload_01/file.bin";

        TestDiskSpoolUploadStream stream = new(defaultStorageMock.Object, localStream, tempLocation);

        byte[] payload = [1, 2, 3, 4, 5];
        await stream.WriteAsync(payload);
        await stream.FlushAsync();

        Assert.Equal(5L, stream.Length);
        Assert.Equal(5L, localStream.Length);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotFaulted_UploadsAndCleansUp()
    {
        Mock<IFileStorageService> defaultStorageMock = new();
        MemoryStream localStream = new();
        string tempLocation = "storage/temp/upload_01/file.bin";

        defaultStorageMock
            .Setup(s => s.GetMetadataAsync(tempLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, tempLocation, "storage/temp/upload_01", "file.bin", 1024L, DateTimeOffset.UtcNow));

        defaultStorageMock
            .Setup(s => s.OpenReadAsync(tempLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([10, 20, 30]));

        defaultStorageMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));

        TestDiskSpoolUploadStream stream = new(defaultStorageMock.Object, localStream, tempLocation);

        await stream.DisposeAsync();

        Assert.True(stream.UploadPayloadInvoked);
        Assert.Equal(1024L, stream.UploadPayloadSizeBytes);
        string expectedDir = Path.GetDirectoryName(tempLocation)!;
        defaultStorageMock.Verify(s => s.DeleteAsync(tempLocation, It.IsAny<CancellationToken>()), Times.Once);
        defaultStorageMock.Verify(s => s.DeleteAsync(expectedDir, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenAborted_SkipsUploadAndCleansUp()
    {
        Mock<IFileStorageService> defaultStorageMock = new();
        MemoryStream localStream = new();
        string tempLocation = "storage/temp/upload_02/file.bin";

        defaultStorageMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));

        TestDiskSpoolUploadStream stream = new(defaultStorageMock.Object, localStream, tempLocation);
        stream.Abort();

        await stream.DisposeAsync();

        Assert.False(stream.UploadPayloadInvoked);
        string expectedDir = Path.GetDirectoryName(tempLocation)!;
        defaultStorageMock.Verify(s => s.DeleteAsync(tempLocation, It.IsAny<CancellationToken>()), Times.Once);
        defaultStorageMock.Verify(s => s.DeleteAsync(expectedDir, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_Synchronous_UploadsAndCleansUp()
    {
        Mock<IFileStorageService> defaultStorageMock = new();
        MemoryStream localStream = new();
        string tempLocation = "storage/temp/upload_03/file.bin";

        defaultStorageMock
            .Setup(s => s.GetMetadataAsync(tempLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, tempLocation, "storage/temp/upload_03", "file.bin", 512L, DateTimeOffset.UtcNow));

        defaultStorageMock
            .Setup(s => s.OpenReadAsync(tempLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2]));

        defaultStorageMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));

        TestDiskSpoolUploadStream stream = new(defaultStorageMock.Object, localStream, tempLocation);

        stream.Dispose();

        Assert.True(stream.UploadPayloadInvoked);
        Assert.Equal(512L, stream.UploadPayloadSizeBytes);
        string expectedDir = Path.GetDirectoryName(tempLocation)!;
        defaultStorageMock.Verify(s => s.DeleteAsync(tempLocation, It.IsAny<CancellationToken>()), Times.Once);
        defaultStorageMock.Verify(s => s.DeleteAsync(expectedDir, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PropertiesSeekSetLengthWriteOverloadsAndUnsupportedReads_BehaveCorrectly()
    {
        Mock<IFileStorageService> defaultStorageMock = new();
        defaultStorageMock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(true));
        using MemoryStream localStream = new();
        await using TestDiskSpoolUploadStream stream = new(defaultStorageMock.Object, localStream, "temp/u/f.bin");
        stream.Abort();

        Assert.False(stream.CanRead);
        Assert.True(stream.CanSeek);
        Assert.True(stream.CanWrite);

        stream.Write([1, 2], 0, 2);
        stream.Write([3, 4]);
        await stream.WriteAsync([5, 6], 0, 2, CancellationToken.None);
        stream.Flush();

        Assert.Equal(6L, stream.Length);
        stream.Position = 2L;
        Assert.Equal(2L, stream.Position);
        Assert.Equal(4L, stream.Seek(2L, SeekOrigin.Current));
        stream.SetLength(4L);
        Assert.Equal(4L, stream.Length);

        byte[] readBuf = new byte[4];
        Assert.Throws<NotSupportedException>(() => stream.Read(readBuf, 0, 4));
        Assert.Throws<NotSupportedException>(() => stream.Read(readBuf.AsSpan()));
        await Assert.ThrowsAsync<NotSupportedException>(() => stream.ReadAsync(readBuf, 0, 4, CancellationToken.None));
        await Assert.ThrowsAsync<NotSupportedException>(() => stream.ReadAsync(readBuf.AsMemory()).AsTask());
    }

    private sealed class FailingStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new IOException("disk full"); }
        public override void Flush() => throw new IOException("disk full");
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new IOException("disk full");
        public override void SetLength(long value) => throw new IOException("disk full");
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("disk full");
        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("disk full");
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromException(new IOException("disk full"));
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.FromException(new IOException("disk full"));
    }

    [Fact]
    public async Task WriteFaultAndCleanupExceptions_MarkFaultedAndDoNotThrowDuringDispose()
    {
        Mock<IFileStorageService> defaultStorageMock = new();
        defaultStorageMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new IOException("cleanup failed"));
        FailingStream failingStream = new();

        TestDiskSpoolUploadStream stream = new(defaultStorageMock.Object, failingStream, "temp/u/fault.bin");

        Assert.Throws<IOException>(() => stream.SetLength(10));
        Assert.Throws<IOException>(() => stream.Write([1], 0, 1));
        Assert.Throws<IOException>(() => stream.Write([1]));
        await Assert.ThrowsAsync<IOException>(() => stream.WriteAsync([1], 0, 1, CancellationToken.None));
        await Assert.ThrowsAsync<IOException>(() => stream.WriteAsync(new ReadOnlyMemory<byte>([1])).AsTask());

        await stream.DisposeAsync();
        Assert.False(stream.UploadPayloadInvoked);
    }
}
