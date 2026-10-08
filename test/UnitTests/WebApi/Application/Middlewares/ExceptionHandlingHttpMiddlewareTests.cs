using MelloSilveiraTools.WebApi.Application.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Mime;

namespace UnitTests.WebApi.Application.Middlewares;

public class ExceptionHandlingHttpMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) => throw new UnauthorizedAccessException("Test");
        ExceptionHandlingHttpMiddleware middleware = new(next);

        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        Mock<ILogger<ExceptionHandlingHttpMiddleware>> loggerMock = new();

        // Act
        await middleware.InvokeAsync(context, loggerMock.Object);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, context.Response.ContentType);
    }
}
