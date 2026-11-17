using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Application.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http.Json;
using UnitTests.WebApi.Fixtures;

namespace UnitTests.WebApi.Application.Controllers;

public class CrudControllerAndEndpointsTests
{
    private sealed class FakeResponseTrailersFeature : IHttpResponseTrailersFeature
    {
        public IHeaderDictionary Trailers { get; set; } = new HeaderDictionary();
    }

    [Fact]
    public async Task CrudController_AllActions_ExecuteExpectedCommandsAndStreamNdjson()
    {
        Mock<IRepository> repositoryMock = new();
        WebApiTestEntity entity = new() { Id = 12L, Name = "Entity12" };

        repositoryMock.Setup(r => r.InsertAsync(It.IsAny<WebApiTestEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(12L);
        repositoryMock.Setup(r => r.GetAsync<WebApiTestEntity>(12L, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repositoryMock.Setup(r => r.CountAsync<WebApiTestEntity, WebApiTestFilter>(It.IsAny<WebApiTestFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        repositoryMock
            .Setup(r => r.GetAsync<WebApiTestEntity, WebApiTestFilter>(It.IsAny<WebApiTestFilter>(), It.IsAny<Pagination?>(), It.IsAny<CancellationToken>()))
            .Returns(StreamItems([entity]));
        repositoryMock.Setup(r => r.TryUpdateAsync(It.IsAny<WebApiTestEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repositoryMock.Setup(r => r.DeleteAsync<WebApiTestEntity>(12L, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<ILogger<WebApiTestController>> loggerMock = new();
        WebApiTestController controller = new(loggerMock.Object);
        DefaultHttpContext httpContext = new();
        MemoryStream responseStream = new();
        httpContext.Response.Body = responseStream;
        FakeResponseTrailersFeature trailersFeature = new();
        httpContext.Features.Set<IHttpResponseTrailersFeature>(trailersFeature);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        ActionResult<AddResult> addResult = await controller.Add(new AddEntity<WebApiTestEntity>(repositoryMock.Object), entity);
        JsonResult addJson = Assert.IsType<JsonResult>(addResult.Result);
        Assert.Equal(StatusCodes.Status201Created, addJson.StatusCode);

        ActionResult<Result<WebApiTestEntity>> readByIdResult = await controller.Read(new ReadEntityById<WebApiTestEntity>(repositoryMock.Object), 12L);
        JsonResult readByIdJson = Assert.IsType<JsonResult>(readByIdResult.Result);
        Assert.Equal(StatusCodes.Status200OK, readByIdJson.StatusCode);

        ActionResult<PagedResult<WebApiTestEntity>> readPagedResult = await controller.Read(
            new ReadEntityPaged<WebApiTestEntity, WebApiTestFilter>(repositoryMock.Object),
            new WebApiTestFilter(),
            new Pagination());
        JsonResult readPagedJson = Assert.IsType<JsonResult>(readPagedResult.Result);
        Assert.Equal(StatusCodes.Status200OK, readPagedJson.StatusCode);

        ActionResult<Result> updateResult = await controller.Update(new UpdateEntity<WebApiTestEntity>(repositoryMock.Object), 12L, entity);
        JsonResult updateJson = Assert.IsType<JsonResult>(updateResult.Result);
        Assert.Equal(StatusCodes.Status201Created, updateJson.StatusCode);

        ActionResult<Result> deleteResult = await controller.Delete(new DeleteEntity<WebApiTestEntity>(repositoryMock.Object), 12L);
        JsonResult deleteJson = Assert.IsType<JsonResult>(deleteResult.Result);
        Assert.Equal(StatusCodes.Status200OK, deleteJson.StatusCode);

        await controller.Stream(repositoryMock.Object, new WebApiTestFilter());
        responseStream.Position = 0;
        string ndjsonPayload = await new StreamReader(responseStream).ReadToEndAsync();
        Assert.Contains("Entity12", ndjsonPayload);
        Assert.Equal(ApplicationConstants.NdjsonContentType, httpContext.Response.ContentType);
    }

    [Fact]
    public async Task MapCrud_RoutesAllEndpointsOverHttp()
    {
        Mock<IRepository> repositoryMock = new();
        WebApiTestEntity stored = new() { Id = 7L, Name = "MinimalApiItem" };

        repositoryMock.Setup(r => r.InsertAsync(It.IsAny<WebApiTestEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(7L);
        repositoryMock.Setup(r => r.GetAsync<WebApiTestEntity>(7L, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        repositoryMock.Setup(r => r.CountAsync<WebApiTestEntity, WebApiTestFilter>(It.IsAny<WebApiTestFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        repositoryMock
            .Setup(r => r.GetAsync<WebApiTestEntity, WebApiTestFilter>(It.IsAny<WebApiTestFilter>(), It.IsAny<Pagination?>(), It.IsAny<CancellationToken>()))
            .Returns(StreamItems([stored]));
        repositoryMock.Setup(r => r.TryUpdateAsync(It.IsAny<WebApiTestEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repositoryMock.Setup(r => r.DeleteAsync<WebApiTestEntity>(7L, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        ResiliencePipelineSettings settings = WebApiTestSettingsFactory.CreateResilienceSettings();
        builder.Services.AddSingleton(repositoryMock.Object);
        builder.Services.AddWebApiServices(settings);

        await using WebApplication app = builder.Build();
        app.MapCrud<WebApiTestEntity, WebApiTestFilter>("/api/test-items", "test item");
        await app.StartAsync();

        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

        HttpResponseMessage postResp = await client.PostAsJsonAsync("/api/test-items", stored);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        HttpResponseMessage getByIdResp = await client.GetAsync("/api/test-items/7");
        Assert.Equal(HttpStatusCode.OK, getByIdResp.StatusCode);

        HttpResponseMessage getPagedResp = await client.GetAsync("/api/test-items");
        Assert.Equal(HttpStatusCode.OK, getPagedResp.StatusCode);

        HttpResponseMessage putResp = await client.PutAsJsonAsync("/api/test-items/7", stored);
        Assert.Equal(HttpStatusCode.Created, putResp.StatusCode);

        HttpResponseMessage deleteResp = await client.DeleteAsync("/api/test-items/7");
        Assert.Equal(HttpStatusCode.OK, deleteResp.StatusCode);

        HttpResponseMessage streamResp = await client.GetAsync("/api/test-items/stream");
        Assert.Equal(HttpStatusCode.OK, streamResp.StatusCode);
        string streamBody = await streamResp.Content.ReadAsStringAsync();
        Assert.Contains("MinimalApiItem", streamBody);

        await app.StopAsync();
    }

    [Fact]
    public async Task WriteNdjsonAsync_HandlesCancellationAndUnexpectedExceptionsByLogging()
    {
        Mock<ILogger> loggerMock = new();
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        await CancelledStream().WriteNdjsonAsync(context, loggerMock.Object, "test-resource");
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<OperationCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        await FaultingStream().WriteNdjsonAsync(context, loggerMock.Object, "test-resource");
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static async IAsyncEnumerable<WebApiTestEntity> StreamItems(WebApiTestEntity[] items)
    {
        foreach (WebApiTestEntity item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private static async IAsyncEnumerable<WebApiTestEntity> CancelledStream()
    {
        await Task.Yield();
        throw new OperationCanceledException("Client disconnected");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<WebApiTestEntity> FaultingStream()
    {
        await Task.Yield();
        throw new InvalidOperationException("Database exploded");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
