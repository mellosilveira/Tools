using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Cancel;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Review;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Controllers;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using System.Security.Cryptography;

namespace UnitTests.Plugins.Application.Commands.Submissions;

public sealed class PluginSubmissionsAndSecurityTests
{
    private readonly PluginSettings _settings = new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24),
        MaxPluginSizeBytes = 10L * 1024 * 1024,
        MaxPendingSubmissionsPerUser = 5,
        AllowUnmanagedDlls = false
    };

    private static async IAsyncEnumerable<PluginSubmissionEntity> ToAsync(params PluginSubmissionEntity[] items)
    {
        foreach (PluginSubmissionEntity item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    [Fact]
    public async Task ApprovePluginSubmission_VerifiesSha256PromotesToIncomingUpdatesDbAndLoadsImmediately()
    {
        byte[] binaryBytes = [0x10, 0x20, 0x30, 0x40];
        string expectedHash = Convert.ToHexStringLower(SHA256.HashData(binaryBytes));
        string pendingLoc = "storage/plugins/pending/Community.v1.0.0.dll";
        string incomingLoc = "storage/plugins/incoming/Community.v1.0.0.dll";

        PluginSubmissionEntity pending = new()
        {
            Id = 55L,
            UserId = 42L,
            StorageEntryMetadataId = 500L,
            PluginName = "Community",
            PluginVersion = "v1.0.0",
            Sha256Hash = expectedHash,
            Status = PluginSubmissionStatus.Pending
        };
        StorageEntryMetadataEntity pendingFile = new()
        {
            Id = 500L,
            UserId = 42L,
            StorageProvider = StorageProvider.Local,
            Location = pendingLoc,
            Directory = "storage/plugins/pending",
            FileName = "Community.v1.0.0.dll",
            SizeBytes = binaryBytes.Length,
            ContentType = "application/octet-stream"
        };

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.ExistsAsync(pendingLoc, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storageMock.Setup(s => s.ComputeSha256Async(pendingLoc, It.IsAny<CancellationToken>())).ReturnsAsync(expectedHash);
        storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, "Community.v1.0.0.dll")).Returns(incomingLoc);
        storageMock.Setup(s => s.ExtractDirectory(incomingLoc, PluginStoragePartitions.Incoming)).Returns("storage/plugins/incoming");

        StorageEntryMetadataEntity? promotedFile = null;
        Mock<IRepository> repoMock = new();
        repoMock.Setup(r => r.GetAsync<PluginSubmissionEntity>(55L, It.IsAny<CancellationToken>())).ReturnsAsync(pending);
        repoMock.Setup(r => r.GetAsync<StorageEntryMetadataEntity>(500L, It.IsAny<CancellationToken>())).ReturnsAsync(pendingFile);
        repoMock.Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(() => ToAsync());
        repoMock.Setup(r => r.TryUpdateAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .Callback<StorageEntryMetadataEntity, CancellationToken>((file, _) => promotedFile = file)
            .ReturnsAsync(true);
        repoMock.Setup(r => r.TryUpdateAsync(It.IsAny<PluginSubmissionEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Mock<IPluginService> pluginServiceMock = new();
        pluginServiceMock
            .Setup(s => s.LoadPluginsOnRuntimeAsync("Community", new PluginVersion(1, 0, 0), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), _settings);
        PluginSubmissionPersistence submissionPersistence = new(NullLogger<PluginSubmissionPersistence>.Instance, storageMock.Object, repoMock.Object, _settings);
        CurrentUserProvider currentUser = new();

        ApprovePluginSubmission command = new(
            NullLogger<ApprovePluginSubmission>.Instance,
            storageMock.Object,
            currentUser,
            repoMock.Object,
            pluginServiceMock.Object,
            TimeProvider.System,
            fileProcessor,
            submissionPersistence);

        // Superuser approves: SHA-256 passes -> moved to incoming/ -> storage_entry_metadata promoted to UserId=null -> DB updated -> runtime load invoked immediately
        using (currentUser.BeginScope(1L, isSuperuser: true))
        {
            Result<PluginInfo> approved = await command.ExecuteAsync(new ReviewPluginSubmissionRequest { Id = 55L, ReviewNotes = "Audited OK" });
            Assert.True(approved.Success);
            Assert.NotNull(approved.Data);
            Assert.Equal(500L, approved.Data.StorageEntryMetadataEntityId);
            Assert.Equal(42L, approved.Data.UserEntityId);
            Assert.Equal(PluginSubmissionStatus.Approved, approved.Data.Status);
            Assert.Equal(1L, approved.Data.ReviewedByUserEntityId);
            Assert.Equal("Audited OK", approved.Data.ReviewNotes);
            Assert.NotNull(promotedFile);
            Assert.Null(promotedFile.UserId);
            Assert.Equal(incomingLoc, promotedFile.Location);
            Assert.Equal("storage/plugins/incoming", promotedFile.Directory);
            pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync("Community", new PluginVersion(1, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task ApprovePluginSubmission_WhenStoredBinaryTampered_RejectsWithConflictAndDoesNotLoad()
    {
        byte[] originalBytes = [0x01, 0x02, 0x03];
        byte[] tamperedBytes = [0x99, 0x99, 0x99];
        string pendingLoc = "storage/plugins/pending/Tampered.v1.0.0.dll";

        PluginSubmissionEntity pending = new()
        {
            Id = 56L,
            UserId = 42L,
            StorageEntryMetadataId = 501L,
            PluginName = "Tampered",
            PluginVersion = "v1.0.0",
            Sha256Hash = Convert.ToHexStringLower(SHA256.HashData(originalBytes)),
            Status = PluginSubmissionStatus.Pending
        };
        StorageEntryMetadataEntity pendingFile = new()
        {
            Id = 501L,
            UserId = 42L,
            Location = pendingLoc,
            FileName = "Tampered.v1.0.0.dll",
            SizeBytes = originalBytes.Length
        };

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.ExistsAsync(pendingLoc, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storageMock.Setup(s => s.ComputeSha256Async(pendingLoc, It.IsAny<CancellationToken>())).ReturnsAsync(Convert.ToHexStringLower(SHA256.HashData(tamperedBytes)));

        Mock<IRepository> repoMock = new();
        repoMock.Setup(r => r.GetAsync<PluginSubmissionEntity>(56L, It.IsAny<CancellationToken>())).ReturnsAsync(pending);
        repoMock.Setup(r => r.GetAsync<StorageEntryMetadataEntity>(501L, It.IsAny<CancellationToken>())).ReturnsAsync(pendingFile);
        repoMock.Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(() => ToAsync());

        Mock<IPluginService> pluginServiceMock = new();
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), _settings);
        PluginSubmissionPersistence submissionPersistence = new(NullLogger<PluginSubmissionPersistence>.Instance, storageMock.Object, repoMock.Object, _settings);
        CurrentUserProvider currentUser = new();

        ApprovePluginSubmission command = new(
            NullLogger<ApprovePluginSubmission>.Instance,
            storageMock.Object,
            currentUser,
            repoMock.Object,
            pluginServiceMock.Object,
            TimeProvider.System,
            fileProcessor,
            submissionPersistence);

        using (currentUser.BeginScope(1L, isSuperuser: true))
        {
            Result<PluginInfo> result = await command.ExecuteAsync(new ReviewPluginSubmissionRequest { Id = 56L });
            Assert.False(result.Success);
            Assert.Equal(StatusCode.Conflict, result.StatusCode);
            pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task RejectPluginSubmission_MovesToRejectedPartitionAndUpdatesStatus()
    {
        string pendingLoc = "storage/plugins/pending/Bad.v1.0.0.dll";
        string rejectedLoc = "storage/plugins/rejected/Bad.v1.0.0.dll";

        PluginSubmissionEntity pending = new()
        {
            Id = 57L,
            UserId = 42L,
            StorageEntryMetadataId = 502L,
            PluginName = "Bad",
            PluginVersion = "v1.0.0",
            Sha256Hash = "abc",
            Status = PluginSubmissionStatus.Pending
        };
        StorageEntryMetadataEntity pendingFile = new()
        {
            Id = 502L,
            UserId = 42L,
            Location = pendingLoc,
            Directory = "storage/plugins/pending",
            FileName = "Bad.v1.0.0.dll",
            SizeBytes = 10L
        };

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.ExistsAsync(pendingLoc, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Rejected, "Bad.v1.0.0.dll")).Returns(rejectedLoc);
        storageMock.Setup(s => s.ExtractDirectory(rejectedLoc, PluginStoragePartitions.Rejected)).Returns("storage/plugins/rejected");

        StorageEntryMetadataEntity? archivedFile = null;
        Mock<IRepository> repoMock = new();
        repoMock.Setup(r => r.GetAsync<PluginSubmissionEntity>(57L, It.IsAny<CancellationToken>())).ReturnsAsync(pending);
        repoMock.Setup(r => r.GetAsync<StorageEntryMetadataEntity>(502L, It.IsAny<CancellationToken>())).ReturnsAsync(pendingFile);
        repoMock.Setup(r => r.TryUpdateAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .Callback<StorageEntryMetadataEntity, CancellationToken>((file, _) => archivedFile = file)
            .ReturnsAsync(true);
        repoMock.Setup(r => r.TryUpdateAsync(It.IsAny<PluginSubmissionEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), _settings);
        CurrentUserProvider currentUser = new();

        RejectPluginSubmission command = new(
            NullLogger<RejectPluginSubmission>.Instance,
            storageMock.Object,
            currentUser,
            repoMock.Object,
            TimeProvider.System,
            fileProcessor);

        using (currentUser.BeginScope(1L, isSuperuser: true))
        {
            Result<PluginInfo> result = await command.ExecuteAsync(new ReviewPluginSubmissionRequest { Id = 57L, ReviewNotes = "Unsafe IO" });
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(PluginSubmissionStatus.Rejected, result.Data.Status);
            Assert.Equal("Unsafe IO", result.Data.ReviewNotes);
            Assert.NotNull(archivedFile);
            Assert.Equal(1L, archivedFile.UserId);
            Assert.Equal(rejectedLoc, archivedFile.Location);
            Assert.Equal("storage/plugins/rejected", archivedFile.Directory);
            storageMock.Verify(s => s.MoveAsync(pendingLoc, rejectedLoc, true, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task CancelPluginSubmission_AllowsAuthorWhilePending_BlocksOtherUsersAndApprovedPlugins()
    {
        string pendingLoc = "storage/plugins/pending/Own.v1.0.0.dll";
        PluginSubmissionEntity pending = new()
        {
            Id = 60L,
            UserId = 42L,
            StorageEntryMetadataId = 503L,
            PluginName = "Own",
            PluginVersion = "v1.0.0",
            Status = PluginSubmissionStatus.Pending
        };
        PluginSubmissionEntity approved = pending with { Id = 61L, Status = PluginSubmissionStatus.Approved };
        StorageEntryMetadataEntity pendingFile = new()
        {
            Id = 503L,
            UserId = 42L,
            Location = pendingLoc,
            FileName = "Own.v1.0.0.dll"
        };

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.ExistsAsync(pendingLoc, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storageMock.Setup(s => s.DeleteAsync(pendingLoc, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Mock<IRepository> repoMock = new();
        repoMock.Setup(r => r.GetAsync<PluginSubmissionEntity>(60L, It.IsAny<CancellationToken>())).ReturnsAsync(pending);
        repoMock.Setup(r => r.GetAsync<PluginSubmissionEntity>(61L, It.IsAny<CancellationToken>())).ReturnsAsync(approved);
        repoMock.Setup(r => r.GetAsync<StorageEntryMetadataEntity>(503L, It.IsAny<CancellationToken>())).ReturnsAsync(pendingFile);

        CurrentUserProvider currentUser = new();
        CancelPluginSubmission command = new(NullLogger<CancelPluginSubmission>.Instance, storageMock.Object, currentUser, repoMock.Object);

        // Other user cannot cancel User 42's pending submission
        using (currentUser.BeginScope(99L, isSuperuser: false))
        {
            Result forbidden = await command.ExecuteAsync(new CancelPluginSubmissionRequest { Id = 60L });
            Assert.Equal(StatusCode.Forbidden, forbidden.StatusCode);
        }

        // Author CANNOT cancel an already Approved submission
        using (currentUser.BeginScope(42L, isSuperuser: false))
        {
            Result conflict = await command.ExecuteAsync(new CancelPluginSubmissionRequest { Id = 61L });
            Assert.Equal(StatusCode.Conflict, conflict.StatusCode);

            // Author CAN cancel own Pending submission (deletes child submission, physical file, and linked storage_entry_metadata row)
            Result ok = await command.ExecuteAsync(new CancelPluginSubmissionRequest { Id = 60L });
            Assert.True(ok.Success);
            storageMock.Verify(s => s.DeleteAsync(pendingLoc, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.DeleteAsync<PluginSubmissionEntity>(60L, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.DeleteAsync<StorageEntryMetadataEntity>(503L, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public void PluginController_EnforcesExplicitAuthorizeAttributesOnAllMutatingAndSubmissionActions()
    {
        Type controllerType = typeof(PluginController);

        Assert.NotNull(controllerType.GetMethod(nameof(PluginController.Get))!.GetCustomAttribute<AllowAnonymousAttribute>());

        string[] authenticatedActions = [nameof(PluginController.Submit), nameof(PluginController.GetSubmissions), nameof(PluginController.CancelSubmission)];
        foreach (string action in authenticatedActions)
        {
            AuthorizeAttribute? auth = controllerType.GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(auth);
            Assert.Null(auth.Policy);
        }

        string[] superuserActions =
        [
            nameof(PluginController.Inspect),
            nameof(PluginController.Upload),
            nameof(PluginController.ApproveSubmission),
            nameof(PluginController.RejectSubmission),
            nameof(PluginController.Load),
            nameof(PluginController.LoadAll),
            nameof(PluginController.Reload),
            nameof(PluginController.ReloadAll),
            nameof(PluginController.ClearCache),
            nameof(PluginController.PersistCache),
            nameof(PluginController.RestoreCache)
        ];
        foreach (string action in superuserActions)
        {
            AuthorizeAttribute? auth = controllerType.GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(auth);
            Assert.Equal(AuthorizationPolicies.SuperuserOnly, auth.Policy);
        }
    }
}
