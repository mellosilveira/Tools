using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands;
using MelloSilveiraTools.Plugins.Application.Commands.Cache;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Load;
using MelloSilveiraTools.Plugins.Application.Commands.Reload;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Controllers;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Plugins.Application.Controllers;

public sealed class PluginControllerTests
{
    private readonly Mock<IPluginService> _serviceMock = new();
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly ICurrentUserProvider _superuserContext = Mock.Of<ICurrentUserProvider>(u => u.IsSuperuser());
    private readonly PluginValidator _validator = new();
    private readonly PluginSettings _settings = new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24),
        MaxPluginSizeBytes = 10L * 1024 * 1024,
        MaxPendingSubmissionsPerUser = 5
    };

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    [Fact]
    public async Task Get_ExecutesCommandAndReturnsOk()
    {
        PluginController controller = new();
        _serviceMock
            .Setup(s => s.GetPluginsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable<RegisteredPlugin>([]));

        GetPlugins command = new(_serviceMock.Object, new PluginValidator<GetPluginsRequest>());

        ActionResult<ListedResult<PluginInfo>> actionResult = await controller.Get(command, new GetPluginsRequest());

        Assert.NotNull(actionResult);
        JsonResult? jsonResult = actionResult.Result as JsonResult;
        Assert.NotNull(jsonResult);
        ListedResult<PluginInfo>? result = jsonResult.Value as ListedResult<PluginInfo>;
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Inspect_ExecutesCommandAndReturnsOk()
    {
        PluginController controller = new();
        _serviceMock
            .Setup(s => s.InspectPluginsAsync(null, null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable<PluginInfo>([]));

        InspectPlugins command = new(_serviceMock.Object, new PluginValidator<InspectPluginsRequest>());

        ActionResult<ListedResult<PluginInfo>> actionResult = await controller.Inspect(command, new InspectPluginsRequest());

        Assert.NotNull(actionResult);
        JsonResult? jsonResult = actionResult.Result as JsonResult;
        Assert.NotNull(jsonResult);
        ListedResult<PluginInfo>? result = jsonResult.Value as ListedResult<PluginInfo>;
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Load_And_LoadAll_ExecuteCommandAndReturnOk()
    {
        PluginController controller = new();
        _serviceMock
            .Setup(s => s.LoadPluginsOnRuntimeAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        LoadPlugins command = new(_serviceMock.Object, _validator);

        ActionResult<Result> loadResult = await controller.Load(command, new PluginsRequest());
        ActionResult<Result> loadAllResult = await controller.LoadAll(command);

        Assert.NotNull(loadResult.Result);
        Assert.NotNull(loadAllResult.Result);
        _serviceMock.Verify(s => s.LoadPluginsOnRuntimeAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Reload_And_ReloadAll_ExecuteCommandAndReturnOk()
    {
        PluginController controller = new();
        _serviceMock
            .Setup(s => s.ReloadPluginsOnRuntimeAsync(It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReloadPlugins command = new(_serviceMock.Object, new PluginValidator<ReloadPluginsRequest>());

        ActionResult<Result> reloadResult = await controller.Reload(command, new ReloadPluginsRequest());
        ActionResult<Result> reloadAllResult = await controller.ReloadAll(command);

        Assert.NotNull(reloadResult.Result);
        Assert.NotNull(reloadAllResult.Result);
        _serviceMock.Verify(s => s.ReloadPluginsOnRuntimeAsync(It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ClearCache_ExecutesCommandAndReturnsOk()
    {
        PluginController controller = new();
        _serviceMock.Setup(s => s.ClearAsync()).Returns(Task.CompletedTask);

        ClearPluginCache command = new(_serviceMock.Object);

        ActionResult<Result> actionResult = await controller.ClearCache(command);

        Assert.NotNull(actionResult.Result);
        _serviceMock.Verify(s => s.ClearAsync(), Times.Once);
    }

    [Fact]
    public async Task PersistCache_And_RestoreCache_ExecuteCommandsAndReturnOk()
    {
        PluginController controller = new();
        _serviceMock
            .Setup(s => s.PersistCacheAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _serviceMock
            .Setup(s => s.RestoreCacheAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        PersistPluginCache persistCommand = new(_serviceMock.Object, _validator);
        RestorePluginCache restoreCommand = new(_serviceMock.Object, _validator);

        ActionResult<Result> persistResult = await controller.PersistCache(persistCommand, new PluginsRequest());
        ActionResult<Result> restoreResult = await controller.RestoreCache(restoreCommand, new PluginsRequest());

        Assert.NotNull(persistResult.Result);
        Assert.NotNull(restoreResult.Result);
        _serviceMock.Verify(s => s.PersistCacheAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.RestoreCacheAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_Submit_And_GetSubmissions_ExecuteCommandsAndReturnResult()
    {
        PluginController controller = new();
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, _storageMock.Object, new PluginCache(new InMemoryTwoLevelCache()), _settings);
        PluginSubmissionPersistence submissionPersistence = new(NullLogger<PluginSubmissionPersistence>.Instance, _storageMock.Object, _repositoryMock.Object, _settings);
        UploadPluginValidator uploadValidator = new(fileProcessor, submissionPersistence, _settings);
        UploadPlugin uploadCommand = new(
            NullLogger<UploadPlugin>.Instance,
            _superuserContext,
            TimeProvider.System,
            fileProcessor,
            submissionPersistence,
            uploadValidator);
        SubmitPlugin submitCommand = new(
            NullLogger<SubmitPlugin>.Instance,
            _superuserContext,
            _repositoryMock.Object,
            TimeProvider.System,
            fileProcessor,
            submissionPersistence,
            uploadValidator,
            _settings);

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(0L);

        ActionResult<Result<PluginInfo>> actionResult = await controller.Upload(uploadCommand, fileMock.Object);
        Assert.NotNull(actionResult.Result);
        JsonResult? jsonResult = actionResult.Result as JsonResult;
        Assert.NotNull(jsonResult);
        Result<PluginInfo>? result = jsonResult.Value as Result<PluginInfo>;
        Assert.NotNull(result);
        Assert.False(result.Success);

        ActionResult<Result<PluginInfo>> submitActionResult = await controller.Submit(submitCommand, fileMock.Object);
        Assert.NotNull(submitActionResult.Result);

        _repositoryMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable<PluginSubmissionEntity>([]));
        GetPluginSubmissions submissionsCommand = new(_repositoryMock.Object);
        ActionResult<ListedResult<PluginInfo>> submissionsResult = await controller.GetSubmissions(submissionsCommand, new GetPluginSubmissionsRequest());
        Assert.NotNull(submissionsResult.Result);
    }
}
