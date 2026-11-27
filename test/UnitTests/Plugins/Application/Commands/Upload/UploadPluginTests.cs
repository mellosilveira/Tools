using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Cryptography;

namespace UnitTests.Plugins.Application.Commands.Upload;

public sealed class UploadPluginTests
{
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly PluginSettings _settings = new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24),
        MaxPluginSizeBytes = 10L * 1024 * 1024,
        MaxPendingSubmissionsPerUser = 2
    };

    private static async IAsyncEnumerable<PluginSubmissionEntity> ToAsync(params PluginSubmissionEntity[] items)
    {
        foreach (PluginSubmissionEntity item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private PluginFileProcessor CreateFileProcessor() => new(
        NullLogger<PluginFileProcessor>.Instance,
        _storageMock.Object,
        new PluginCache(new InMemoryTwoLevelCache()),
        _settings);

    private UploadPlugin CreateUploadCommand(ICurrentUserProvider userProvider)
    {
        PluginFileProcessor fileProcessor = CreateFileProcessor();
        PluginSubmissionPersistence persistence = new(NullLogger<PluginSubmissionPersistence>.Instance, _storageMock.Object, _repositoryMock.Object, _settings);
        UploadPluginValidator validator = new(fileProcessor, persistence, _settings);
        return new UploadPlugin(
            NullLogger<UploadPlugin>.Instance,
            userProvider,
            TimeProvider.System,
            fileProcessor,
            persistence,
            validator);
    }

    private SubmitPlugin CreateSubmitCommand(ICurrentUserProvider userProvider)
    {
        PluginFileProcessor fileProcessor = CreateFileProcessor();
        PluginSubmissionPersistence persistence = new(NullLogger<PluginSubmissionPersistence>.Instance, _storageMock.Object, _repositoryMock.Object, _settings);
        UploadPluginValidator validator = new(fileProcessor, persistence, _settings);
        return new SubmitPlugin(
            NullLogger<SubmitPlugin>.Instance,
            userProvider,
            _repositoryMock.Object,
            TimeProvider.System,
            fileProcessor,
            persistence,
            validator,
            _settings);
    }

    [Fact]
    public async Task UploadPlugin_WhenSuperuserPublishesValidPlugin_AutoApprovesToIncomingPartitionWithSha256()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPlugin).Assembly.Location);
        string expectedSha256 = Convert.ToHexStringLower(SHA256.HashData(validAssemblyBytes));
        string fileName = "Contoso.Tools.v2.1.0.dll";
        string pendingLocation = "storage/plugins/pending/Contoso.Tools.v2.1.0.dll";
        string incomingLocation = "storage/plugins/incoming/Contoso.Tools.v2.1.0.dll";
        string loadedLocation = "storage/plugins/loaded/Contoso.Tools.v2.1.0.dll";

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(validAssemblyBytes));

        _storageMock.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Pending, fileName)).Returns(pendingLocation);
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, fileName)).Returns(incomingLocation);
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName)).Returns(loadedLocation);
        _storageMock
            .Setup(s => s.WriteAndHashAsync("plugins", PluginStoragePartitions.Incoming, fileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((incomingLocation, "storage/plugins/incoming", expectedSha256, (long)validAssemblyBytes.Length));

        StorageEntryMetadataEntity? capturedFile = null;
        PluginSubmissionEntity? capturedSubmission = null;
        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(() => ToAsync());
        _repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .Callback<StorageEntryMetadataEntity, CancellationToken>((file, _) => capturedFile = file)
            .ReturnsAsync(500L);
        _repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<PluginSubmissionEntity>(), It.IsAny<CancellationToken>()))
            .Callback<PluginSubmissionEntity, CancellationToken>((sub, _) => capturedSubmission = sub)
            .ReturnsAsync(77L);
        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity>(77L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedSubmission! with { Id = 77L });

        CurrentUserProvider userProvider = new();
        using (userProvider.BeginScope(1L, isSuperuser: true))
        {
            UploadPlugin command = CreateUploadCommand(userProvider);
            Result<PluginInfo> result = await command.ExecuteAsync(new UploadPluginRequest { File = fileMock.Object });

            Assert.True(result.Success);
            Assert.Equal(StatusCode.OK, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(77L, result.Data.EntityId);
            Assert.Equal(500L, result.Data.StorageEntryMetadataEntityId);
            Assert.Equal("Contoso.Tools", result.Data.Name);
            Assert.Equal(new PluginVersion(2, 1, 0), result.Data.Version);
            Assert.Equal(PluginSubmissionStatus.Approved, result.Data.Status);
            Assert.Equal(expectedSha256, result.Data.Sha256Hash);
            Assert.NotNull(capturedFile);
            Assert.Null(capturedFile.UserId);
            Assert.Equal(fileName, capturedFile.FileName);
            Assert.Equal(incomingLocation, capturedFile.Location);
            Assert.Equal(validAssemblyBytes.Length, capturedFile.SizeBytes);
            Assert.Equal("application/octet-stream", capturedFile.ContentType);
            _storageMock.Verify(s => s.WriteAndHashAsync("plugins", PluginStoragePartitions.Incoming, fileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task SubmitPlugin_WhenRegularUserSubmitsValidPlugin_StagesInPendingPartitionAndNeverTouchesIncoming()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPlugin).Assembly.Location);
        string expectedSha256 = Convert.ToHexStringLower(SHA256.HashData(validAssemblyBytes));
        string fileName = "Community.Model.v1.0.0.dll";
        string pendingLocation = "storage/plugins/pending/Community.Model.v1.0.0.dll";
        string incomingLocation = "storage/plugins/incoming/Community.Model.v1.0.0.dll";
        string loadedLocation = "storage/plugins/loaded/Community.Model.v1.0.0.dll";

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(validAssemblyBytes));

        _storageMock.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Pending, fileName)).Returns(pendingLocation);
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, fileName)).Returns(incomingLocation);
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName)).Returns(loadedLocation);
        _storageMock
            .Setup(s => s.WriteAndHashAsync("plugins", PluginStoragePartitions.Pending, fileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((pendingLocation, "storage/plugins/pending", expectedSha256, (long)validAssemblyBytes.Length));

        StorageEntryMetadataEntity? capturedFile = null;
        PluginSubmissionEntity? capturedSubmission = null;
        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(() => ToAsync());
        _repositoryMock
            .Setup(r => r.CountAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);
        _repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .Callback<StorageEntryMetadataEntity, CancellationToken>((file, _) => capturedFile = file)
            .ReturnsAsync(501L);
        _repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<PluginSubmissionEntity>(), It.IsAny<CancellationToken>()))
            .Callback<PluginSubmissionEntity, CancellationToken>((sub, _) => capturedSubmission = sub)
            .ReturnsAsync(88L);
        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity>(88L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedSubmission! with { Id = 88L });

        CurrentUserProvider userProvider = new();
        using (userProvider.BeginScope(42L, isSuperuser: false))
        {
            SubmitPlugin command = CreateSubmitCommand(userProvider);
            Result<PluginInfo> result = await command.ExecuteAsync(new UploadPluginRequest { File = fileMock.Object });

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(501L, result.Data.StorageEntryMetadataEntityId);
            Assert.Equal(PluginSubmissionStatus.Pending, result.Data.Status);
            Assert.Equal(42L, result.Data.UserEntityId);
            Assert.NotNull(capturedFile);
            Assert.Equal(42L, capturedFile.UserId);
            Assert.Equal(fileName, capturedFile.FileName);
            Assert.Equal(pendingLocation, capturedFile.Location);
            _storageMock.Verify(s => s.WriteAndHashAsync("plugins", PluginStoragePartitions.Pending, fileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
            _storageMock.Verify(s => s.WriteAndHashAsync("plugins", PluginStoragePartitions.Incoming, It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task SubmitPlugin_WhenNonOwnerAttemptsVersionHijacking_ReturnsForbidden()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPlugin).Assembly.Location);
        string fileName = "Fung.v999.0.0.dll";
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(validAssemblyBytes));

        _storageMock.Setup(s => s.ResolveLocation("plugins", It.IsAny<string>(), fileName)).Returns($"storage/{fileName}");
        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(() => ToAsync(new PluginSubmissionEntity
            {
                Id = 1L,
                UserId = 10L,
                PluginName = "Fung",
                PluginVersion = "v1.0.0",
                Status = PluginSubmissionStatus.Approved
            }));

        CurrentUserProvider attackerProvider = new();
        using (attackerProvider.BeginScope(99L, isSuperuser: false))
        {
            SubmitPlugin command = CreateSubmitCommand(attackerProvider);
            Result<PluginInfo> result = await command.ExecuteAsync(new UploadPluginRequest { File = fileMock.Object });

            Assert.False(result.Success);
            Assert.Equal(StatusCode.Forbidden, result.StatusCode);
            _storageMock.Verify(s => s.WriteAndHashAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task SubmitPlugin_WhenPendingSubmissionQuotaExceeded_ReturnsBadRequest()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPlugin).Assembly.Location);
        string fileName = "Quota.Model.v1.0.0.dll";
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(validAssemblyBytes));

        _storageMock.Setup(s => s.ResolveLocation("plugins", It.IsAny<string>(), fileName)).Returns($"storage/{fileName}");
        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(() => ToAsync());
        _repositoryMock
            .Setup(r => r.CountAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2L);

        CurrentUserProvider userProvider = new();
        using (userProvider.BeginScope(42L, isSuperuser: false))
        {
            SubmitPlugin command = CreateSubmitCommand(userProvider);
            Result<PluginInfo> result = await command.ExecuteAsync(new UploadPluginRequest { File = fileMock.Object });

            Assert.False(result.Success);
            Assert.Equal(StatusCode.BadRequest, result.StatusCode);
            _storageMock.Verify(s => s.WriteAndHashAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task UploadPlugin_WhenFileValidationFails_ReturnsBadRequestWithoutWritingToStorage()
    {
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(0L);
        fileMock.Setup(f => f.FileName).Returns("InvalidPlugin.txt");

        CurrentUserProvider superuserProvider = new();
        using (superuserProvider.BeginScope(1L, isSuperuser: true))
        {
            UploadPlugin command = CreateUploadCommand(superuserProvider);
            Result<PluginInfo> result = await command.ExecuteAsync(new UploadPluginRequest { File = fileMock.Object });

            Assert.False(result.Success);
            Assert.Equal(StatusCode.BadRequest, result.StatusCode);
            _storageMock.Verify(s => s.WriteAndHashAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task UploadPlugin_WhenPluginVersionAlreadyExistsInLoadedPartition_ReturnsConflictWithoutOverwriting()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPlugin).Assembly.Location);
        string fileName = "Contoso.Tools.v2.1.0.dll";
        string pendingLocation = "storage/plugins/pending/Contoso.Tools.v2.1.0.dll";
        string incomingLocation = "storage/plugins/incoming/Contoso.Tools.v2.1.0.dll";
        string loadedLocation = "storage/plugins/loaded/Contoso.Tools.v2.1.0.dll";

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(validAssemblyBytes));

        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Pending, fileName)).Returns(pendingLocation);
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, fileName)).Returns(incomingLocation);
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName)).Returns(loadedLocation);
        _storageMock.Setup(s => s.ExistsAsync(pendingLocation, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _storageMock.Setup(s => s.ExistsAsync(incomingLocation, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _storageMock.Setup(s => s.ExistsAsync(loadedLocation, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        CurrentUserProvider superuserProvider = new();
        using (superuserProvider.BeginScope(1L, isSuperuser: true))
        {
            UploadPlugin command = CreateUploadCommand(superuserProvider);
            Result<PluginInfo> result = await command.ExecuteAsync(new UploadPluginRequest { File = fileMock.Object });

            Assert.False(result.Success);
            Assert.Equal(StatusCode.Conflict, result.StatusCode);
            _storageMock.Verify(s => s.WriteAndHashAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task WriteAndPersistAsync_WhenStorageMetadataInsertFails_DeletesUploadedFileAndRethrows()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPlugin).Assembly.Location);
        string fileName = "Rollback.Metadata.v1.0.0.dll";
        string incomingLocation = "storage/plugins/incoming/Rollback.Metadata.v1.0.0.dll";

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(validAssemblyBytes));

        _storageMock.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        _storageMock
            .Setup(s => s.WriteAndHashAsync("plugins", PluginStoragePartitions.Incoming, fileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((incomingLocation, "storage/plugins/incoming", "abc123", (long)validAssemblyBytes.Length));
        _repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB metadata insert failure"));

        PluginSubmissionPersistence persistence = new(NullLogger<PluginSubmissionPersistence>.Instance, _storageMock.Object, _repositoryMock.Object, _settings);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => persistence.WriteAndPersistAsync(
            fileMock.Object,
            fileName,
            "Rollback.Metadata",
            new PluginVersion(1, 0, 0),
            PluginStoragePartitions.Incoming,
            storageOwnerUserId: null,
            (fileId, hash) => new PluginSubmissionEntity { StorageEntryMetadataId = fileId, PluginName = "Rollback.Metadata", PluginVersion = "v1.0.0", Sha256Hash = hash },
            TimeProvider.System.GetUtcNow()));

        Assert.Equal("DB metadata insert failure", ex.Message);
        _storageMock.Verify(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WriteAndPersistAsync_WhenSubmissionInsertFailsAndRollbackThrows_CleansBothAndPreservesOriginalException()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPlugin).Assembly.Location);
        string fileName = "Rollback.Submission.v1.0.0.dll";
        string incomingLocation = "storage/plugins/incoming/Rollback.Submission.v1.0.0.dll";

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(validAssemblyBytes));

        _storageMock.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        _storageMock
            .Setup(s => s.WriteAndHashAsync("plugins", PluginStoragePartitions.Incoming, fileName, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((incomingLocation, "storage/plugins/incoming", "def456", (long)validAssemblyBytes.Length));
        _storageMock
            .Setup(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Storage cleanup failed"));

        _repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(909L);
        _repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<PluginSubmissionEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB submission insert failure"));
        _repositoryMock
            .Setup(r => r.DeleteAsync<StorageEntryMetadataEntity>(909L, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB rollback failed"));

        PluginSubmissionPersistence persistence = new(NullLogger<PluginSubmissionPersistence>.Instance, _storageMock.Object, _repositoryMock.Object, _settings);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => persistence.WriteAndPersistAsync(
            fileMock.Object,
            fileName,
            "Rollback.Submission",
            new PluginVersion(1, 0, 0),
            PluginStoragePartitions.Incoming,
            storageOwnerUserId: 10L,
            (fileId, hash) => new PluginSubmissionEntity { StorageEntryMetadataId = fileId, PluginName = "Rollback.Submission", PluginVersion = "v1.0.0", Sha256Hash = hash },
            TimeProvider.System.GetUtcNow()));

        Assert.Equal("DB submission insert failure", ex.Message);
        _repositoryMock.Verify(r => r.DeleteAsync<StorageEntryMetadataEntity>(909L, It.IsAny<CancellationToken>()), Times.Once);
        _storageMock.Verify(s => s.DeleteAsync(incomingLocation, It.IsAny<CancellationToken>()), Times.Once);
    }
}
