using MelloSilveiraTools.Core.Managers.File.Streams;

namespace UnitTests.Core.Managers.File;

public sealed class DelegatingDisposableStreamTests
{
    private sealed class DisposableSpy : IDisposable, IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        public bool IsAsyncDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            IsAsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void Properties_ForwardToInnerStreamCorrectly()
    {
        byte[] data = [1, 2, 3, 4, 5];
        using MemoryStream innerStream = new(data);
        using DisposableSpy owner = new();
        using DelegatingDisposableStream stream = new(innerStream, owner);

        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);
        Assert.True(stream.CanWrite);
        Assert.Equal(5L, stream.Length);
        Assert.Equal(0L, stream.Position);

        stream.Position = 2L;
        Assert.Equal(2L, innerStream.Position);
        Assert.Equal(2L, stream.Position);
    }

    [Fact]
    public void ReadAndWrite_ForwardToInnerStreamCorrectly()
    {
        using MemoryStream innerStream = new();
        using DisposableSpy owner = new();
        using DelegatingDisposableStream stream = new(innerStream, owner);

        byte[] writeData = [10, 20, 30];
        stream.Write(writeData, 0, writeData.Length);
        stream.Flush();

        Assert.Equal(3L, stream.Length);

        stream.Seek(0, SeekOrigin.Begin);
        byte[] readBuffer = new byte[3];
        int bytesRead = stream.Read(readBuffer, 0, 3);

        Assert.Equal(3, bytesRead);
        Assert.Equal(writeData, readBuffer);
    }

    [Fact]
    public async Task ReadAsyncAndWriteAsync_ForwardToInnerStreamCorrectly()
    {
        using MemoryStream innerStream = new();
        using DisposableSpy owner = new();
        await using DelegatingDisposableStream stream = new(innerStream, owner);

        byte[] writeData = [40, 50, 60];
        await stream.WriteAsync(writeData, 0, writeData.Length);
        await stream.FlushAsync();

        Assert.Equal(3L, stream.Length);

        stream.Seek(0, SeekOrigin.Begin);
        byte[] readBuffer = new byte[3];
        int bytesRead = await stream.ReadAsync(readBuffer, 0, 3);

        Assert.Equal(3, bytesRead);
        Assert.Equal(writeData, readBuffer);
    }

    [Fact]
    public void Dispose_DisposesBothInnerStreamAndOwner()
    {
        MemoryStream innerStream = new([1, 2, 3]);
        DisposableSpy owner = new();
        DelegatingDisposableStream stream = new(innerStream, owner);

        stream.Dispose();

        Assert.True(owner.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => innerStream.ReadByte());
    }

    [Fact]
    public async Task DisposeAsync_DisposesBothInnerStreamAndOwnerAsync()
    {
        MemoryStream innerStream = new([1, 2, 3]);
        DisposableSpy owner = new();
        DelegatingDisposableStream stream = new(innerStream, owner);

        await stream.DisposeAsync();

        Assert.True(owner.IsAsyncDisposed);
        Assert.Throws<ObjectDisposedException>(() => innerStream.ReadByte());
    }
}
