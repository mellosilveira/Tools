using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Managers.File.Streams;
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
}
