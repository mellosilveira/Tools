using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace MelloSilveiraTools.WebApi.Application.Middlewares;

/// <summary>
/// A safety net that catches unexpected system failures during operations, ensuring users receive a clear, standardized error message rather than a complete application crash.
/// It acts as a polite translator, turning complex technical issues into predictable responses so client applications can handle them gracefully.
/// </summary>
/// <remarks>
/// Exception-to-status mapping performed by <see cref="InvokeAsync"/>:
/// <list type="bullet">
///   <item><description><see cref="UnauthorizedAccessException"/> → <see cref="StatusCode.Unauthorized"/> (401).</description></item>
///   <item><description><see cref="ArgumentException"/> or <see cref="InvalidOperationException"/> → <see cref="StatusCode.BadRequest"/> (400).</description></item>
///   <item><description><see cref="KeyNotFoundException"/> → <see cref="StatusCode.NotFound"/> (404).</description></item>
///   <item><description>Any other exception → <see cref="StatusCode.UnknownError"/> (500).</description></item>
/// </list>
/// <see cref="NdjsonException"/> receives special handling: it is logged via <see cref="ILogger{ExceptionHandlingHttpMiddleware}"/>
/// and the middleware returns without writing any response body, since NDJSON streaming may already have sent partial output to the 
/// client and rewriting the response would corrupt the stream.
/// </remarks>
/// <param name="next">The next middleware in the request pipeline.</param>
public class ExceptionHandlingHttpMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the next middleware in the pipeline and translates any unhandled exception into an HTTP error response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="logger">Logger used to record handled streaming exceptions.</param>
    public async Task InvokeAsync(HttpContext context, ILogger<ExceptionHandlingHttpMiddleware> logger)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (ex is NdjsonException)
            {
                logger.LogError(ex, "Error occurred while streaming NDJSON data.");
                return;
            }

            StatusCode statusCode = ex switch
            {
                UnauthorizedAccessException => StatusCode.Unauthorized,
                ArgumentException or InvalidOperationException => StatusCode.BadRequest,
                KeyNotFoundException => StatusCode.NotFound,
                _ => StatusCode.UnknownError
            };

            context.Response.ContentType = MediaTypeNames.Application.Json;
            context.Response.StatusCode = (int)statusCode;
#if DEBUG
            string responseMessage = $"{ex}";
#else        
            string responseMessage = "An internal error occurred while processing the request.";
#endif

            string requestBody = string.Empty;
            if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;

                // leaveOpen: true ensures we don't accidentally kill the stream
                using StreamReader reader = new(context.Request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
            }

            logger.LogError(
                ex,
                "Request failed. Method: {Method}, Path: {Path}, Query: {QueryString}, Body: {RequestBody}, Status: {StatusCode}, SentMessage: {ResponseMessage}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value,
                requestBody,
                statusCode,
                responseMessage);

            Result result = Result.CreateError(statusCode, responseMessage);
            await context.Response.WriteAsJsonAsync(result).ConfigureAwait(false);
        }
    }
}
