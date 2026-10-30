using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using Moq;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AcceptanceTests.Plugins.Helpers;

/// <summary>
/// High-fidelity in-memory test double and mock for <see cref="IFileStorageService"/>,
/// backing file operations across canonical partitions (incoming, loaded, archive, quarantine, cache)
/// in an in-memory thread-safe dictionary.
/// </summary>
public sealed class PluginTestStorageMock
{
    private readonly ConcurrentDictionary<string, (byte[] Data, DateTimeOffset CreatedAt)> _files = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the underlying Moq instance configured with in-memory storage semantics.
    /// </summary>
    public Mock<IFileStorageService> Mock { get; }

    /// <summary>
    /// Gets the storage provider technology simulated by this mock instance.
    /// </summary>
    public StorageProvider Provider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginTestStorageMock"/> class.
    /// </summary>
    /// <param name="provider">The storage provider technology to simulate.</param>
    public PluginTestStorageMock(StorageProvider provider = StorageProvider.Local)
    {
        Provider = provider;
        Mock = new Mock<IFileStorageService>();

        SetupMockOperations();
    }

    /// <summary>
    /// Seeds or overwrites a file directly into the specified context and partition.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="partition">Canonical storage partition name.</param>
    /// <param name="fileName">File name with extension.</param>
    /// <param name="content">Raw file payload bytes.</param>
    public void AddFile(string identifier, string partition, string fileName, byte[] content)
    {
        string location = ResolveLocationCore(identifier, partition, fileName);
        string normalized = NormalizeLocation(location);
        _files[normalized] = (content, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Checks whether a file exists at the specified context and partition.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="partition">Canonical storage partition name.</param>
    /// <param name="fileName">File name with extension.</param>
    /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    public bool FileExists(string identifier, string partition, string fileName)
    {
        string location = ResolveLocationCore(identifier, partition, fileName);
        return _files.ContainsKey(NormalizeLocation(location));
    }

    /// <summary>
    /// Retrieves file bytes for a specific file residing in the specified partition, or <c>null</c> if not found.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="partition">Canonical storage partition name.</param>
    /// <param name="fileName">File name with extension.</param>
    /// <returns>The raw byte array, or <c>null</c>.</returns>
    public byte[]? GetFileBytes(string identifier, string partition, string fileName)
    {
        string location = ResolveLocationCore(identifier, partition, fileName);
        return _files.TryGetValue(NormalizeLocation(location), out (byte[] Data, DateTimeOffset CreatedAt) item)
            ? item.Data
            : null;
    }

    /// <summary>
    /// Lists all file names currently stored in the given context and partition.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="partition">Canonical storage partition name.</param>
    /// <returns>A read-only list of file names.</returns>
    public IReadOnlyList<string> GetFilesInPartition(string identifier, string partition)
    {
        string normIdentifier = identifier.Replace('\\', '/').Trim('/');
        string normPartition = partition.Replace('\\', '/').Trim('/');
        string prefix = $"{normIdentifier}/{normPartition}/";

        List<string> names = [];
        foreach (string key in _files.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                names.Add(Path.GetFileName(key));
            }
        }

        return names;
    }

    /// <summary>
    /// Clears all stored files from memory.
    /// </summary>
    public void Clear()
    {
        _files.Clear();
    }

    private void SetupMockOperations()
    {
        Mock.SetupGet(s => s.Provider).Returns(Provider);

        Mock.Setup(s => s.ResolveLocation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string, string>((identifier, context, fileName) => ResolveLocationCore(identifier, context, fileName));

        Mock.Setup(s => s.OpenWriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((location, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                string normalized = NormalizeLocation(location);
                Stream stream = new CommitOnFlushMemoryStream(normalized, _files);
                return ValueTask.FromResult(stream);
            });

        Mock.Setup(s => s.CreateWriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, CancellationToken>((identifier, context, fileName, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                string location = ResolveLocationCore(identifier, context, fileName);
                string normalized = NormalizeLocation(location);
                Stream stream = new CommitOnFlushMemoryStream(normalized, _files);
                return ValueTask.FromResult((stream, location));
            });

        Mock.Setup(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((location, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                string normalized = NormalizeLocation(location);
                if (_files.TryGetValue(normalized, out (byte[] Data, DateTimeOffset CreatedAt) item))
                {
                    Stream stream = new MemoryStream(item.Data, writable: false);
                    return ValueTask.FromResult(stream);
                }

                throw new FileNotFoundException($"Storage file '{location}' was not found.");
            });

        Mock.Setup(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, CancellationToken>((identifier, context, fileName, ct) =>
            {
                string location = ResolveLocationCore(identifier, context, fileName);
                return Mock.Object.OpenReadAsync(location, ct);
            });

        Mock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((location, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                string normalized = NormalizeLocation(location);
                return ValueTask.FromResult(_files.ContainsKey(normalized));
            });

        Mock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, CancellationToken>((identifier, context, fileName, ct) =>
            {
                string location = ResolveLocationCore(identifier, context, fileName);
                return Mock.Object.ExistsAsync(location, ct);
            });

        Mock.Setup(s => s.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((location, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                string normalized = NormalizeLocation(location);
                if (_files.TryGetValue(normalized, out (byte[] Data, DateTimeOffset CreatedAt) item))
                {
                    string fileName = Path.GetFileName(normalized);
                    string directory = Path.GetDirectoryName(normalized)?.Replace('\\', '/') ?? string.Empty;
                    StorageEntryMetadata metadata = new(
                        Provider,
                        normalized,
                        directory,
                        fileName,
                        item.Data.Length,
                        item.CreatedAt);

                    return ValueTask.FromResult(metadata);
                }

                throw new FileNotFoundException($"Storage file '{location}' was not found.");
            });

        Mock.Setup(s => s.GetMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, CancellationToken>((identifier, context, fileName, ct) =>
            {
                string location = ResolveLocationCore(identifier, context, fileName);
                return Mock.Object.GetMetadataAsync(location, ct);
            });

        Mock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((location, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                string normalized = NormalizeLocation(location);
                bool removed = _files.TryRemove(normalized, out _);
                return ValueTask.FromResult(removed);
            });

        Mock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, CancellationToken>((identifier, context, fileName, ct) =>
            {
                string location = ResolveLocationCore(identifier, context, fileName);
                return Mock.Object.DeleteAsync(location, ct);
            });

        Mock.Setup(s => s.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string identifier, string context, CancellationToken ct) => ListFilesCore(identifier, context, ct));

        Mock.Setup(s => s.ListEntriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string identifier, string context, CancellationToken ct) => ListFilesCore(identifier, context, ct));

        Mock.Setup(s => s.Dispose());
        Mock.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    private async IAsyncEnumerable<StorageEntryMetadata> ListFilesCore(
        string identifier,
        string context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string normIdentifier = identifier.Replace('\\', '/').Trim('/');
        string normContext = context.Replace('\\', '/').Trim('/');
        string prefix = $"{normIdentifier}/{normContext}/";

        foreach (KeyValuePair<string, (byte[] Data, DateTimeOffset CreatedAt)> kvp in _files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(kvp.Key);
                string directory = $"{normIdentifier}/{normContext}";
                yield return new StorageEntryMetadata(
                    Provider,
                    kvp.Key,
                    directory,
                    fileName,
                    kvp.Value.Data.Length,
                    kvp.Value.CreatedAt);
            }
        }

        await Task.CompletedTask;
    }

    private static string NormalizeLocation(string location)
    {
        return location.Replace('\\', '/').TrimStart('/');
    }

    private static string ResolveLocationCore(string identifier, string context, string fileName)
    {
        string normIdentifier = identifier.Replace('\\', '/').Trim('/');
        string normContext = context.Replace('\\', '/').Trim('/');
        return $"{normIdentifier}/{normContext}/{fileName}";
    }

    private sealed class CommitOnFlushMemoryStream : MemoryStream
    {
        private readonly string _location;
        private readonly ConcurrentDictionary<string, (byte[] Data, DateTimeOffset CreatedAt)> _storage;

        public CommitOnFlushMemoryStream(
            string location,
            ConcurrentDictionary<string, (byte[] Data, DateTimeOffset CreatedAt)> storage)
        {
            _location = location;
            _storage = storage;
        }

        public override void Flush()
        {
            base.Flush();
            Commit();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await base.FlushAsync(cancellationToken).ConfigureAwait(false);
            Commit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Commit();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            Commit();
            await base.DisposeAsync().ConfigureAwait(false);
        }

        private void Commit()
        {
            byte[] bytes = ToArray();
            _storage.AddOrUpdate(
                _location,
                (bytes, DateTimeOffset.UtcNow),
                (_, existing) => (bytes, existing.CreatedAt));
        }
    }
}
