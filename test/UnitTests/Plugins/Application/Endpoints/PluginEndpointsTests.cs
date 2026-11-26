using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Application.Controllers;
using MelloSilveiraTools.Plugins.Application.Endpoints;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Headers;

namespace UnitTests.Plugins.Application.Endpoints;

public sealed class PluginEndpointsTests
{
    [Fact]
    public void MapPluginEndpoints_WithDefaultAndCustomPatterns_RegistersFailClosedAuthMetadata()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        using WebApplication app = builder.Build();

        RouteGroupBuilder defaultGroup = app.MapPluginEndpoints();
        RouteGroupBuilder customGroup = app.MapPluginEndpoints("/custom/plugins");

        Assert.NotNull(defaultGroup);
        Assert.NotNull(customGroup);

        List<RouteEndpoint> defaultEndpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? string.Empty).StartsWith("/api/v1/plugins", StringComparison.Ordinal))
            .ToList();

        RouteEndpoint getPluginsEndpoint = defaultEndpoints.Single(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "GetPlugins");
        Assert.NotNull(getPluginsEndpoint.Metadata.GetMetadata<IAllowAnonymous>());

        string[] authenticatedNames = ["SubmitPlugin", "GetPluginSubmissions", "CancelPluginSubmission"];
        foreach (string name in authenticatedNames)
        {
            RouteEndpoint endpoint = defaultEndpoints.Single(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
            IAuthorizeData? auth = endpoint.Metadata.GetMetadata<IAuthorizeData>();
            Assert.NotNull(auth);
            Assert.Null(auth.Policy);
        }

        string[] superuserNames =
        [
            "InspectPlugins",
            "UploadPlugin",
            "ApprovePluginSubmission",
            "RejectPluginSubmission",
            "LoadPlugins",
            "LoadAllPlugins",
            "ReloadPlugins",
            "ReloadAllPlugins",
            "ClearPluginCache",
            "PersistPluginCache",
            "RestorePluginCache"
        ];
        foreach (string name in superuserNames)
        {
            RouteEndpoint endpoint = defaultEndpoints.Single(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
            IAuthorizeData? auth = endpoint.Metadata.GetMetadata<IAuthorizeData>();
            Assert.NotNull(auth);
            Assert.Equal(AuthorizationPolicies.SuperuserOnly, auth.Policy);
        }
    }

    [Fact]
    public async Task MapPluginEndpoints_ExecutesAllMappedRoutesOverHttp()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "mst-plugin-endpoints", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            PluginSettings settings = new()
            {
                StorageProvider = StorageProvider.Local,
                Directory = "plugins",
                DefaultCacheTarget = "file",
                PollInterval = TimeSpan.FromMinutes(10),
                PreviousVersionRetention = TimeSpan.FromHours(1),
                MaxPluginSizeBytes = 10L * 1024 * 1024,
                MaxPendingSubmissionsPerUser = 5
            };

            Mock<IRepository> repoMock = new();
            repoMock
                .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
                .Returns(EmptyAsync<PluginSubmissionEntity>());

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton(Mock.Of<ICurrentUserProvider>(u => u.IsSuperuser()));
            builder.Services.AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>();
            builder.Services.AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>();
            builder.Services.AddSingleton<IDynamicServiceProvider>(Mock.Of<IDynamicServiceProvider>());
            builder.Services.AddSingleton(repoMock.Object);
            builder.Services.AddKeyedSingleton<IFileStorageService>(
                StorageProvider.Local,
                (_, _) => new LocalFileStorageService(TimeProvider.System, new LocalFileStorageSettings { BaseDirectory = tempRoot }));
            builder.Services.AddSingleton<IFileStorageService>(sp => sp.GetRequiredKeyedService<IFileStorageService>(StorageProvider.Local));
            builder.Services.AddPluginServices(settings);

            await using WebApplication app = builder.Build();
            app.MapPluginEndpoints("/api/v1/plugins", managementAuthorizationPolicy: null, requireAuthForSubmissions: false);
            await app.StartAsync();

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/plugins")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/plugins/inspect")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/plugins/submissions")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/v1/plugins/submissions/999/approve", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/v1/plugins/submissions/999/reject", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/v1/plugins/submissions/999")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins/load?Name=Missing", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins/load/all", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins/reload?Force=false", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins/reload/all", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/v1/plugins/cache")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins/cache/file/persist", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins/cache/file/restore", null)).StatusCode);

            using MultipartFormDataContent uploadContent = new();
            ByteArrayContent invalidFile = new([1, 2, 3]);
            invalidFile.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            uploadContent.Add(invalidFile, "file", "invalid.txt");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/plugins/upload", uploadContent)).StatusCode);

            using MultipartFormDataContent submitContent = new();
            ByteArrayContent invalidSubmitFile = new([1, 2, 3]);
            invalidSubmitFile.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            submitContent.Add(invalidSubmitFile, "file", "invalid.txt");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/plugins/submissions", submitContent)).StatusCode);

            await app.StopAsync();
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task PluginAuthorizationBoundary_BlocksAnonymousAndNonSuperusersBeforeEnteringControllerOrEndpoint()
    {
        JwtSettings jwtSettings = new()
        {
            Issuer = "MelloSilveiraTools.Tests",
            Audience = "MelloSilveiraTools.Tests",
            EncryptionKey = "MelloSilveiraToolsQuickstartKey!",
            SigningKey = "MelloSilveiraToolsSigningKey2026!!",
            ClockSkewInSeconds = 30,
            SecurityKeyType = SecurityKeyType.Symmetric,
            TokenExperitationTimeInMinutes = 60
        };

        Mock<IPluginService> pluginServiceMock = new();
        pluginServiceMock
            .Setup(s => s.GetPluginsAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()))
            .Returns(EmptyAsync<RegisteredPlugin>());
        pluginServiceMock
            .Setup(s => s.InspectPluginsAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(EmptyAsync<PluginInfo>());
        pluginServiceMock
            .Setup(s => s.LoadPluginsOnRuntimeAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pluginServiceMock
            .Setup(s => s.ClearAsync())
            .Returns(Task.CompletedTask);

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetAsync<UserEntity>(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity { Id = 1L, Username = "admin", PasswordHash = "h", IsActive = true, IsSuperuser = true });
        repoMock
            .Setup(r => r.GetAsync<UserEntity>(42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity { Id = 42L, Username = "researcher", PasswordHash = "h", IsActive = true, IsSuperuser = false });
        repoMock
            .Setup(r => r.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(It.IsAny<PluginSubmissionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsync<PluginSubmissionEntity>());

        PluginSettings pluginSettings = new()
        {
            StorageProvider = StorageProvider.Local,
            Directory = "plugins",
            DefaultCacheTarget = "file",
            PollInterval = TimeSpan.FromMinutes(10),
            PreviousVersionRetention = TimeSpan.FromHours(1),
            MaxPluginSizeBytes = 10L * 1024 * 1024,
            MaxPendingSubmissionsPerUser = 5
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();
        builder.Services.AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>();
        builder.Services.AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>();
        builder.Services.AddSingleton<IDynamicServiceProvider>(Mock.Of<IDynamicServiceProvider>());
        builder.Services.AddSingleton(repoMock.Object);
        builder.Services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, Mock.Of<IFileStorageService>());
        builder.Services.AddSingleton<IFileStorageService>(Mock.Of<IFileStorageService>());
        builder.Services.AddPluginServices(pluginSettings);
        builder.Services.AddScoped(_ => pluginServiceMock.Object);
        builder.Services.AddJweAuthentication(jwtSettings);
        builder.Services.AddControllers().AddApplicationPart(typeof(PluginController).Assembly);

        await using WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseUserContext();
        app.UseAuthorization();
        app.MapControllers();
        app.MapPluginEndpoints("/api/v1/plugins-minimal");
        await app.StartAsync();

        AuthenticationJweTokenService tokenService = new(TimeProvider.System, jwtSettings);
        string regularToken = tokenService.Generate(42L).Value;
        string superuserToken = tokenService.Generate(1L).Value;

        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

        // 1. Public discovery works without authentication on both Controller and Minimal API
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/V1/plugins")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/plugins-minimal")).StatusCode);

        // 2. Anonymous requests are blocked with 401 BEFORE reaching controller actions or minimal handlers
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/V1/plugins/inspect")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/plugins-minimal/inspect")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/V1/plugins/submissions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/plugins-minimal/submissions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/V1/plugins/submissions", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/plugins-minimal/submissions", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/V1/plugins/upload", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/plugins-minimal/upload", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/V1/plugins/load/all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/plugins-minimal/load/all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/V1/plugins/cache")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/v1/plugins-minimal/cache")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/V1/plugins/submissions/1/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/plugins-minimal/submissions/1/approve", null)).StatusCode);

        // 3. Regular authenticated user (User 42) is blocked with 403 BEFORE reaching any superuser controller/endpoint (including GET /inspect and POST /upload)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regularToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/V1/plugins/inspect")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/plugins-minimal/inspect")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/V1/plugins/upload", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/plugins-minimal/upload", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/V1/plugins/load/all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/plugins-minimal/load/all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/V1/plugins/cache")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/v1/plugins-minimal/cache")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/V1/plugins/submissions/1/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/plugins-minimal/submissions/1/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/V1/plugins/submissions/1/reject", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/plugins-minimal/submissions/1/reject", null)).StatusCode);

        // Proof: zero calls ever entered InspectPlugins, LoadPlugins, ClearPluginCache, ApprovePluginSubmission, or RejectPluginSubmission
        pluginServiceMock.Verify(s => s.InspectPluginsAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Never);
        pluginServiceMock.Verify(s => s.ClearAsync(), Times.Never);
        repoMock.Verify(r => r.GetAsync<PluginSubmissionEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);

        // Regular authenticated user CAN access [Authorize] submissions listing
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/V1/plugins/submissions")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/plugins-minimal/submissions")).StatusCode);

        // 4. Superuser (User 1) passes SuperuserOnly middleware and executes controller/endpoint commands cleanly
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", superuserToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/V1/plugins/inspect")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/plugins-minimal/inspect")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/V1/plugins/load/all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins-minimal/load/all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/V1/plugins/cache")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/v1/plugins-minimal/cache")).StatusCode);

        pluginServiceMock.Verify(s => s.InspectPluginsAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        pluginServiceMock.Verify(s => s.LoadPluginsOnRuntimeAsync(It.IsAny<string?>(), It.IsAny<PluginVersion?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        pluginServiceMock.Verify(s => s.ClearAsync(), Times.Exactly(2));

        await app.StopAsync();
    }

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}
