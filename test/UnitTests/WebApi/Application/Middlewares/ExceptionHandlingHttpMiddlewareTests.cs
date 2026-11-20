using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Middlewares;
using MelloSilveiraTools.WebApi.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Mime;
using System.Text;

namespace UnitTests.WebApi.Application.Middlewares;

public class ExceptionHandlingHttpMiddlewareTests
{
    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasAlreadyStarted_LogsAndReturnsWithoutWriting()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("after start");
        ExceptionHandlingHttpMiddleware middleware = new(next);
        DefaultHttpContext context = new();
        StartedResponseFeature responseFeature = new();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        Mock<ILogger<ExceptionHandlingHttpMiddleware>> loggerMock = new();

        await middleware.InvokeAsync(context, loggerMock.Object);

        Assert.Equal(0, responseFeature.Body.Length);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestBodyIsNotSeekable_SkipsReadingRequestBody()
    {
        RequestDelegate next = _ => throw new ArgumentException("bad");
        ExceptionHandlingHttpMiddleware middleware = new(next);
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        context.Request.ContentLength = 10;
        context.Request.Body = new NonSeekableStream();
        Mock<ILogger<ExceptionHandlingHttpMiddleware>> loggerMock = new();

        await middleware.InvokeAsync(context, loggerMock.Object);

        Assert.Equal((int)StatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_CompletesWithoutModifyingResponse()
    {
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        };

        ExceptionHandlingHttpMiddleware middleware = new(next);
        DefaultHttpContext context = new();
        Mock<ILogger<ExceptionHandlingHttpMiddleware>> loggerMock = new();

        await middleware.InvokeAsync(context, loggerMock.Object);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenNdjsonException_LogsErrorAndDoesNotRewriteResponse()
    {
        RequestDelegate next = _ => throw new NdjsonException(new InvalidOperationException("stream fail"));
        ExceptionHandlingHttpMiddleware middleware = new(next);

        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        Mock<ILogger<ExceptionHandlingHttpMiddleware>> loggerMock = new();

        await middleware.InvokeAsync(context, loggerMock.Object);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<NdjsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(ExceptionMappingCases))]
    public async Task InvokeAsync_MapsExceptionToExpectedStatusCodeAndReadsSeekableBody(Exception thrownException, int expectedStatusCode, bool includeBody)
    {
        RequestDelegate next = _ => throw thrownException;
        ExceptionHandlingHttpMiddleware middleware = new(next);

        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/items";
        context.Request.QueryString = new QueryString("?q=1");

        if (includeBody)
        {
            byte[] payload = Encoding.UTF8.GetBytes("{\"id\":1}");
            context.Request.Body = new MemoryStream(payload);
            context.Request.ContentLength = payload.Length;
        }

        Mock<ILogger<ExceptionHandlingHttpMiddleware>> loggerMock = new();

        await middleware.InvokeAsync(context, loggerMock.Object);

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.StartsWith(MediaTypeNames.Application.Json, context.Response.ContentType);

        context.Response.Body.Position = 0;
        string responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains(expectedStatusCode.ToString(), responseJson);
    }

    public static TheoryData<Exception, int, bool> ExceptionMappingCases() => new()
    {
        { new UnauthorizedAccessException("Unauth"), (int)StatusCode.Unauthorized, false },
        { new ArgumentException("Bad arg"), (int)StatusCode.BadRequest, true },
        { new InvalidOperationException("Invalid op"), (int)StatusCode.BadRequest, false },
        { new KeyNotFoundException("Not found"), (int)StatusCode.NotFound, true },
        { new TimeoutException("Unhandled"), (int)StatusCode.UnknownError, false }
    };
}
