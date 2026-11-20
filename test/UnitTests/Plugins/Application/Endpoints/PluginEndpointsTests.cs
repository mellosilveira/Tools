using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Application.Endpoints;
using MelloSilveiraTools.Plugins.Infrastructure;
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
    public void MapPluginEndpoints_WithDefaultAndCustomPatterns_RegistersRouteGroup()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        using WebApplication app = builder.Build();

        RouteGroupBuilder defaultGroup = app.MapPluginEndpoints();
        RouteGroupBuilder customGroup = app.MapPluginEndpoints("/custom/plugins");

        Assert.NotNull(defaultGroup);
        Assert.NotNull(customGroup);
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
                PreviousVersionRetention = TimeSpan.FromHours(1)
            };

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>();
            builder.Services.AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>();
            builder.Services.AddSingleton<IDynamicServiceProvider>(Mock.Of<IDynamicServiceProvider>());
            builder.Services.AddSingleton(Mock.Of<IRepository>());
            builder.Services.AddKeyedSingleton<IFileStorageService>(
                StorageProvider.Local,
                (_, _) => new LocalFileStorageService(TimeProvider.System, new LocalFileStorageSettings { BaseDirectory = tempRoot }));
            builder.Services.AddSingleton<IFileStorageService>(sp => sp.GetRequiredKeyedService<IFileStorageService>(StorageProvider.Local));
            builder.Services.AddPluginServices(settings);

            await using WebApplication app = builder.Build();
            app.MapPluginEndpoints("/api/v1/plugins");
            await app.StartAsync();

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/plugins")).StatusCode);
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

            await app.StopAsync();
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
