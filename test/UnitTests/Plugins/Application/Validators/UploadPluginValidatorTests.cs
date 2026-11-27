using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Plugins.Application.Validators;

public sealed class UploadPluginValidatorTests
{
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly PluginSettings _defaultSettings = new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(1),
        MaxPluginSizeBytes = 10L * 1024 * 1024,
        MaxPendingSubmissionsPerUser = 5
    };

    public UploadPluginValidatorTests()
    {
        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(() => ToAsync());
    }

    private UploadPluginValidator CreateValidator(PluginSettings? settings = null)
    {
        PluginSettings activeSettings = settings ?? _defaultSettings;
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), activeSettings);
        PluginSubmissionPersistence persistence = new(NullLogger<PluginSubmissionPersistence>.Instance, _storageMock.Object, _repositoryMock.Object, activeSettings);
        return new UploadPluginValidator(fileProcessor, persistence, activeSettings);
    }

    private static async IAsyncEnumerable<PluginSubmissionEntity> ToAsync(params PluginSubmissionEntity[] items)
    {
        foreach (PluginSubmissionEntity item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    [Fact]
    public async Task Validate_WhenFileIsNull_ReturnsBadRequest()
    {
        UploadPluginValidator validator = CreateValidator();
        UploadPluginRequest request = new()
        {
            File = null!
        };

        Result result = await validator.ValidateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenFileIsEmpty_ReturnsBadRequest()
    {
        UploadPluginValidator validator = CreateValidator();
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(0L);
        fileMock.Setup(f => f.FileName).Returns("Plugin.v1.0.0.dll");

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = await validator.ValidateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenNotDllExtension_ReturnsBadRequest()
    {
        UploadPluginValidator validator = CreateValidator();
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(100L);
        fileMock.Setup(f => f.FileName).Returns("Plugin.v1.0.0.txt");

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = await validator.ValidateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenInvalidVersionPattern_ReturnsBadRequest()
    {
        UploadPluginValidator validator = CreateValidator();
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(100L);
        fileMock.Setup(f => f.FileName).Returns("InvalidPluginNaming.dll");

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = await validator.ValidateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenBinaryIsNotManagedAssembly_ReturnsBadRequest()
    {
        UploadPluginValidator validator = CreateValidator();
        byte[] fakeNativeBytes = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(fakeNativeBytes.Length);
        fileMock.Setup(f => f.FileName).Returns("NativeLib.v1.0.0.dll");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fakeNativeBytes));

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = await validator.ValidateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenValidManagedAssembly_ReturnsSuccess()
    {
        UploadPluginValidator validator = CreateValidator();
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPluginValidator).Assembly.Location);
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns("Contoso.Plugin.v1.0.0.dll");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(validAssemblyBytes));

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = await validator.ValidateAsync(request);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenStreamIsNotSeekable_CopiesAndValidatesSuccessfully()
    {
        UploadPluginValidator validator = CreateValidator();
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPluginValidator).Assembly.Location);
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns("Contoso.NonSeekable.v1.0.0.dll");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new NonSeekableMemoryStream(validAssemblyBytes));

        Result result = await validator.ValidateAsync(new UploadPluginRequest { File = fileMock.Object });

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenNativePeWithoutManagedMetadata_ReturnsBadRequest()
    {
        string nativeDllPath = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
        if (!File.Exists(nativeDllPath))
            return;

        UploadPluginValidator validator = CreateValidator();
        byte[] nativeBytes = File.ReadAllBytes(nativeDllPath);
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(nativeBytes.Length);
        fileMock.Setup(f => f.FileName).Returns("NativePe.v1.0.0.dll");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(nativeBytes));

        Result result = await validator.ValidateAsync(new UploadPluginRequest { File = fileMock.Object });

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenFileExceedsConfiguredMaxPluginSizeBytes_ReturnsBadRequestWithoutOpeningStream()
    {
        PluginSettings tightSettings = _defaultSettings with { MaxPluginSizeBytes = 1024 };
        UploadPluginValidator validator = CreateValidator(tightSettings);

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(2048L);
        fileMock.Setup(f => f.FileName).Returns("TooBig.v1.0.0.dll");

        Result result = await validator.ValidateAsync(new UploadPluginRequest { File = fileMock.Object });

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        fileMock.Verify(f => f.OpenReadStream(), Times.Never);
    }

    [Fact]
    public async Task Validate_WhenAssemblyExistsInActiveStoragePartition_ReturnsConflict()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPluginValidator).Assembly.Location);
        string fileName = "Existing.Plugin.v1.0.0.dll";
        string loadedLocation = $"storage/plugins/loaded/{fileName}";

        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Pending, fileName)).Returns($"storage/plugins/pending/{fileName}");
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, fileName)).Returns($"storage/plugins/incoming/{fileName}");
        _storageMock.Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Loaded, fileName)).Returns(loadedLocation);
        _storageMock.Setup(s => s.ExistsAsync(loadedLocation, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(validAssemblyBytes));

        UploadPluginValidator validator = CreateValidator();
        Result result = await validator.ValidateAsync(new UploadPluginRequest { File = fileMock.Object });

        Assert.False(result.Success);
        Assert.Equal(StatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task Validate_WhenPluginVersionAlreadyExistsInRepository_ReturnsConflict()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPluginValidator).Assembly.Location);
        string fileName = "Duplicate.Model.v2.0.0.dll";

        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(() => ToAsync(new PluginSubmissionEntity
            {
                Id = 10L,
                PluginName = "Duplicate.Model",
                PluginVersion = "v2.0.0",
                Status = PluginSubmissionStatus.Pending
            }));

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(validAssemblyBytes));

        UploadPluginValidator validator = CreateValidator();
        Result result = await validator.ValidateAsync(new UploadPluginRequest { File = fileMock.Object });

        Assert.False(result.Success);
        Assert.Equal(StatusCode.Conflict, result.StatusCode);
    }

    private sealed class NonSeekableMemoryStream(byte[] buffer) : MemoryStream(buffer)
    {
        public override bool CanSeek => false;
    }
}
