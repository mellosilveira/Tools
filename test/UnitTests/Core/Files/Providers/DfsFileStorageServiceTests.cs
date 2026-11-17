using System.Text;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers.Dfs;

namespace UnitTests.Core.Files.Providers;

/// <summary>
/// Unit tests for <see cref="DfsFileStorageService"/> verifying DFS provider identity
/// and <see cref="FileShare.ReadWrite"/> concurrent read/write stream semantics.
/// </summary>
public sealed class DfsFileStorageServiceTests : IDisposable
{
    private readonly string _baseDirectory = Path.Combine(Path.GetTempPath(), $"DfsStorageTests_{Guid.NewGuid():N}");
    private readonly DfsFileStorageService _service;

    public DfsFileStorageServiceTests()
    {
        Directory.CreateDirectory(_baseDirectory);
        _service = new DfsFileStorageService(TimeProvider.System, new DfsStorageSettings { BaseDirectory = _baseDirectory });
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_baseDirectory))
        {
            try
            {
                Directory.Delete(_baseDirectory, true);
            }
            catch
            {
                // Best effort cleanup of temporary test directory
            }
        }
    }

    /// <summary>
    /// Verifies that Provider returns StorageProvider.Dfs and write/read shares permit concurrent read while write is open.
    /// </summary>
    [Fact]
    public async Task ProviderAndStreams_AllowConcurrentReadWhileWriteStreamIsOpen()
    {
        // Arrange
        Assert.Equal(StorageProvider.Dfs, _service.Provider);
        byte[] payload = Encoding.UTF8.GetBytes("dfs-shared-payload");

        // Act
        (Stream writeStream, string location) = await _service.CreateWriteAsync("dfs-run", "ctx", "shared.txt");
        await using (writeStream)
        {
            await writeStream.WriteAsync(payload);
            await writeStream.FlushAsync();

            // Open read stream while write stream is still open (requires FileShare.ReadWrite)
            await using Stream readStream = await _service.OpenReadAsync(location);
            using StreamReader reader = new(readStream);
            string readContent = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal("dfs-shared-payload", readContent);
        }

        Assert.True(await _service.ExistsAsync(location));
        Assert.True(await _service.DeleteAsync(location));
    }
}
