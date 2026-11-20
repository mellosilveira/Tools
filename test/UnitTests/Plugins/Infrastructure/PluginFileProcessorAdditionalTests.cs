using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Buffers.Binary;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace UnitTests.Plugins.Infrastructure;

public sealed class PluginFileProcessorAdditionalTests
{
    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }

    private static PluginSettings CreateSettings() => new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24)
    };

    [Fact]
    public async Task ScanAsync_HandlesNonDllCorruptPeNonManagedAndNonSeekableAndFilters()
    {
        byte[] validBytes = File.ReadAllBytes(typeof(PluginFileProcessor).Assembly.Location);
        string nativeDllPath = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
        byte[]? nativeBytes = File.Exists(nativeDllPath) ? File.ReadAllBytes(nativeDllPath) : null;

        List<StorageEntryMetadata> incomingEntries =
        [
            new(StorageProvider.Local, "p/readme.txt", "p", "readme.txt", 10L, DateTimeOffset.UtcNow),
            new(StorageProvider.Local, "p/Corrupt.v1.0.0.dll", "p", "Corrupt.v1.0.0.dll", 4L, DateTimeOffset.UtcNow),
            new(StorageProvider.Local, "p/Other.v1.0.0.dll", "p", "Other.v1.0.0.dll", validBytes.Length, DateTimeOffset.UtcNow),
            new(StorageProvider.Local, "p/Target.v1.0.0.dll", "p", "Target.v1.0.0.dll", validBytes.Length, DateTimeOffset.UtcNow),
            new(StorageProvider.Local, "p/Target.v2.0.0.dll", "p", "Target.v2.0.0.dll", validBytes.Length, DateTimeOffset.UtcNow)
        ];

        if (nativeBytes is not null)
        {
            incomingEntries.Insert(2, new(StorageProvider.Local, "p/Native.v1.0.0.dll", "p", "Native.v1.0.0.dll", nativeBytes.Length, DateTimeOffset.UtcNow));
        }

        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable(incomingEntries));
        storageMock
            .Setup(s => s.OpenReadAsync("p/Corrupt.v1.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream([0x01, 0x02, 0x03, 0x04]));
        if (nativeBytes is not null)
        {
            storageMock
                .Setup(s => s.OpenReadAsync("p/Native.v1.0.0.dll", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(nativeBytes));
        }
        storageMock
            .Setup(s => s.OpenReadAsync("p/Other.v1.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonSeekableStream(validBytes));
        storageMock
            .Setup(s => s.OpenReadAsync("p/Target.v1.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonSeekableStream(validBytes));
        storageMock
            .Setup(s => s.OpenReadAsync("p/Target.v2.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(validBytes));
        storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Quarantine, It.IsAny<string>()))
            .Returns<string, string, string>((_, _, f) => $"quarantine/{f}");
        storageMock
            .Setup(s => s.OpenWriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());
        storageMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), CreateSettings());

        List<DiscoveredPlugin> matches = await processor.ScanAsync("Target", new PluginVersion(2, 0, 0)).ToListAsync();

        Assert.Single(matches);
        Assert.Equal(new PluginVersion(2, 0, 0), matches[0].Version);
        storageMock.Verify(s => s.ResolveLocation("plugins", PluginStoragePartitions.Quarantine, "Corrupt.v1.0.0.dll"), Times.Once);
        if (nativeBytes is not null)
        {
            storageMock.Verify(s => s.ResolveLocation("plugins", PluginStoragePartitions.Quarantine, "Native.v1.0.0.dll"), Times.Once);
        }
    }

    [Fact]
    public async Task ScanLoadedAsync_FiltersInvalidNamesAndMatchesByNameAndVersion()
    {
        StorageEntryMetadata[] entries =
        [
            new(StorageProvider.Local, "l/notes.txt", "l", "notes.txt", 5L, DateTimeOffset.UtcNow),
            new(StorageProvider.Local, "l/badname.dll", "l", "badname.dll", 5L, DateTimeOffset.UtcNow),
            new(StorageProvider.Local, "l/Other.v1.0.0.dll", "l", "Other.v1.0.0.dll", 5L, DateTimeOffset.UtcNow),
            new(StorageProvider.Local, "l/Target.v1.0.0.dll", "l", "Target.v1.0.0.dll", 5L, DateTimeOffset.UtcNow),
            new(StorageProvider.Local, "l/Target.v2.0.0.dll", "l", "Target.v2.0.0.dll", 5L, DateTimeOffset.UtcNow)
        ];

        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Loaded, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable(entries));

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), CreateSettings());

        List<DiscoveredPlugin> matches = await processor.ScanLoadedAsync("Target", new PluginVersion(1, 0, 0)).ToListAsync();
        Assert.Single(matches);
        Assert.Equal("Target", matches[0].Name);
        Assert.Equal(new PluginVersion(1, 0, 0), matches[0].Version);
    }

    [Fact]
    public async Task CreateShadowCopyAsync_And_CleanShadowCopies_CopyOptionalDepsJsonAndPurgeFolders()
    {
        byte[] dllBytes = File.ReadAllBytes(typeof(PluginFileProcessor).Assembly.Location);
        byte[] depsBytes = "{}"u8.ToArray();
        string dllLoc = "loaded/MyMod.v1.0.0.dll";
        string depsLoc = "loaded/MyMod.v1.0.0.deps.json";

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.OpenReadAsync(dllLoc, It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream(dllBytes));
        storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, "MyMod.v1.0.0.deps.json")).Returns(depsLoc);
        storageMock.Setup(s => s.ExistsAsync(depsLoc, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storageMock.Setup(s => s.OpenReadAsync(depsLoc, It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream(depsBytes));

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), CreateSettings());
        DiscoveredPlugin plugin = new("MyMod", new PluginVersion(1, 0, 0), dllLoc, DateTimeOffset.UtcNow);

        (string shadowDll, string shadowDir) = await processor.CreateShadowCopyAsync(plugin);
        Assert.True(File.Exists(shadowDll));
        Assert.True(File.Exists(Path.Combine(shadowDir, "MyMod.v1.0.0.deps.json")));

        processor.CleanShadowCopies();
        Assert.False(Directory.Exists(shadowDir));
    }

    [Fact]
    public async Task MoveToLoadedBackup_ReplaceInLoaded_And_SameLocationMove_ExerciseAllLifecyclePaths()
    {
        string loadedLoc = "loaded/Mod.v1.0.0.dll";
        string backupLoc = "loaded/Mod.v1.0.0.dll.old";
        string incomingLoc = "incoming/Mod.v1.0.0.dll";

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, "Mod.v1.0.0.dll")).Returns(loadedLoc);
        storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, "Mod.v1.0.0.dll.old")).Returns(backupLoc);
        storageMock.Setup(s => s.ExistsAsync(loadedLoc, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storageMock.Setup(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream([1, 2]));
        storageMock.Setup(s => s.OpenWriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream());
        storageMock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), CreateSettings());

        await processor.MoveToLoadedFolderAsync(new DiscoveredPlugin("Mod", new PluginVersion(1, 0, 0), incomingLoc, DateTimeOffset.UtcNow));
        storageMock.Verify(s => s.OpenWriteAsync(backupLoc, It.IsAny<CancellationToken>()), Times.Once);

        await processor.ReplaceInLoadedFolderAsync(new DiscoveredPlugin("Mod", new PluginVersion(1, 0, 0), incomingLoc, DateTimeOffset.UtcNow));
        storageMock.Verify(s => s.DeleteAsync(loadedLoc, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        // Moving to same partition location returns early without reading/deleting
        storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Archive, "Mod.v1.0.0.dll")).Returns("archive/Mod.v1.0.0.dll");
        storageMock.Invocations.Clear();
        await processor.MoveToArchiveFolderAsync(new DiscoveredPlugin("Mod", new PluginVersion(1, 0, 0), "archive/Mod.v1.0.0.dll", DateTimeOffset.UtcNow));
        storageMock.Verify(s => s.OpenReadAsync("archive/Mod.v1.0.0.dll", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void IsArchitectureCompatible_ChecksManagedAndNativeBinaries()
    {
        using FileStream managedStream = File.OpenRead(typeof(PluginFileProcessor).Assembly.Location);
        using PEReader managedReader = new(managedStream);
        Assert.True(PluginFileProcessor.IsArchitectureCompatible(managedReader, out string okReason));
        Assert.Equal(string.Empty, okReason);

        string nativeDllPath = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
        if (File.Exists(nativeDllPath))
        {
            using FileStream nativeStream = File.OpenRead(nativeDllPath);
            using PEReader nativeReader = new(nativeStream);
            Assert.False(PluginFileProcessor.IsArchitectureCompatible(nativeReader, out string failReason));
            Assert.False(string.IsNullOrEmpty(failReason));
        }
    }

    [Fact]
    public async Task ArchitectureChecks_Cover32BitFlagsMachineMismatchesMetadataAndQuarantine()
    {
        byte[] baseBytes = File.ReadAllBytes(typeof(PluginFileProcessor).Assembly.Location);

        byte[] requires32BitBytes = CreatePatchedPe(baseBytes, Machine.I386, CorFlags.ILOnly | CorFlags.Requires32Bit);
        byte[] amd64Bytes = CreatePatchedPe(baseBytes, Machine.Amd64, CorFlags.ILOnly);
        byte[] arm64Bytes = CreatePatchedPe(baseBytes, Machine.Arm64, CorFlags.ILOnly);
        byte[] i386NoIlBytes = CreatePatchedPe(baseBytes, Machine.I386, 0);

        using PEReader anyCpuReader = new(new MemoryStream(baseBytes));
        using PEReader req32Reader = new(new MemoryStream(requires32BitBytes));
        using PEReader amd64Reader = new(new MemoryStream(amd64Bytes));
        using PEReader arm64Reader = new(new MemoryStream(arm64Bytes));
        using PEReader i386NoIlReader = new(new MemoryStream(i386NoIlBytes));

        // AnyCPU passes everywhere
        PluginAssemblyProcessor.ValidateAssemblyArchitecture(anyCpuReader, "AnyCpu");
        Assert.True(PluginFileProcessor.IsArchitectureCompatible(anyCpuReader, out _));

        // Native binary (no managed metadata) throws BadImageFormatException
        string nativeDllPath = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
        if (File.Exists(nativeDllPath))
        {
            using PEReader nativeReader = new(File.OpenRead(nativeDllPath));
            Assert.Throws<BadImageFormatException>(() => PluginAssemblyProcessor.ValidateAssemblyArchitecture(nativeReader, "Native"));
        }

        Architecture hostArch = RuntimeInformation.ProcessArchitecture;
        if (hostArch is Architecture.X64 or Architecture.Arm64)
        {
            Assert.Throws<BadImageFormatException>(() => PluginAssemblyProcessor.ValidateAssemblyArchitecture(req32Reader, "Req32"));
            Assert.False(PluginFileProcessor.IsArchitectureCompatible(req32Reader, out string req32Reason));
            Assert.Contains("32-bit", req32Reason, StringComparison.OrdinalIgnoreCase);
        }

        if (hostArch == Architecture.X64)
        {
            PluginAssemblyProcessor.ValidateAssemblyArchitecture(amd64Reader, "Amd64");
            Assert.True(PluginFileProcessor.IsArchitectureCompatible(amd64Reader, out _));

            Assert.Throws<BadImageFormatException>(() => PluginAssemblyProcessor.ValidateAssemblyArchitecture(arm64Reader, "Arm64"));
            Assert.False(PluginFileProcessor.IsArchitectureCompatible(arm64Reader, out _));

            Assert.Throws<BadImageFormatException>(() => PluginAssemblyProcessor.ValidateAssemblyArchitecture(i386NoIlReader, "I386NoIl"));
            Assert.False(PluginFileProcessor.IsArchitectureCompatible(i386NoIlReader, out _));
        }

        // ScanAsync quarantines an incoming binary rejected by IsArchitectureCompatible
        string badEntryLocation = "p/WrongArch.v1.0.0.dll";
        byte[] rejectedBytes = hostArch == Architecture.X86 ? arm64Bytes : requires32BitBytes;
        StorageEntryMetadata badEntry = new(StorageProvider.Local, badEntryLocation, "p", "WrongArch.v1.0.0.dll", rejectedBytes.Length, DateTimeOffset.UtcNow);

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.ListFilesAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()))
            .Returns(() => CreateAsyncEnumerable([badEntry]));
        storageMock.Setup(s => s.OpenReadAsync(badEntryLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(rejectedBytes));
        storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Quarantine, "WrongArch.v1.0.0.dll"))
            .Returns("quarantine/WrongArch.v1.0.0.dll");
        storageMock.Setup(s => s.OpenWriteAsync("quarantine/WrongArch.v1.0.0.dll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());
        storageMock.Setup(s => s.DeleteAsync(badEntryLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        PluginFileProcessor processor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), CreateSettings());
        List<DiscoveredPlugin> scanned = await processor.ScanAsync().ToListAsync();

        Assert.Empty(scanned);
        storageMock.Verify(s => s.OpenWriteAsync("quarantine/WrongArch.v1.0.0.dll", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static byte[] CreatePatchedPe(byte[] sourceBytes, Machine machine, CorFlags corFlags)
    {
        byte[] patched = (byte[])sourceBytes.Clone();
        using PEReader reader = new(new MemoryStream(patched));
        int coffStart = reader.PEHeaders.CoffHeaderStartOffset;
        int corStart = reader.PEHeaders.CorHeaderStartOffset;

        BinaryPrimitives.WriteUInt16LittleEndian(patched.AsSpan(coffStart, 2), (ushort)machine);
        BinaryPrimitives.WriteUInt32LittleEndian(patched.AsSpan(corStart + 16, 4), (uint)corFlags);

        return patched;
    }

    private static async IAsyncEnumerable<StorageEntryMetadata> CreateAsyncEnumerable(IEnumerable<StorageEntryMetadata> items)
    {
        foreach (StorageEntryMetadata item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
