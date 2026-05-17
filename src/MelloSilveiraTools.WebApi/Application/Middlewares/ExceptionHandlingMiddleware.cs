using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Commands;
using MelloSilveiraTools.WebApi.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace MelloSilveiraTools.WebApi.Application.Middlewares;

/// <summary>
/// Middleware that converts unhandled exceptions raised by the pipeline into a standard <see cref="ProblemDetails"/> JSON response.
/// </summary>
/// <remarks>
/// Exception-to-status mapping performed by <see cref="InvokeAsync"/>:
/// <list type="bullet">
///   <item><description><see cref="UnauthorizedAccessException"/> → <see cref="StatusCode.Unauthorized"/> (401).</description></item>
///   <item><description><see cref="ArgumentException"/> or <see cref="InvalidOperationException"/> → <see cref="StatusCode.BadRequest"/> (400).</description></item>
///   <item><description><see cref="KeyNotFoundException"/> → <see cref="StatusCode.NotFound"/> (404).</description></item>
///   <item><description>Any other exception → <see cref="StatusCode.UnknownError"/> (500).</description></item>
/// </list>
/// <see cref="NdjsonException"/> receives special handling: it is logged via <paramref name="logger"/>
/// and the middleware returns without writing any response body, since NDJSON streaming may already
/// have sent partial output to the client and rewriting the response would corrupt the stream.
/// </remarks>
/// <param name="next">The next middleware in the request pipeline.</param>
/// <param name="logger">Logger used to record handled streaming exceptions.</param>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
{
    /// <summary>
    /// Invokes the next middleware in the pipeline and translates any unhandled exception into an HTTP error response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (ex is NdjsonException)
            {
                logger.Error("Error occurred while streaming NDJSON data.", ex);
                return;
            }

            var statusCode = ex switch
            {
                UnauthorizedAccessException => StatusCode.Unauthorized,
                ArgumentException or InvalidOperationException => StatusCode.BadRequest,
                KeyNotFoundException => StatusCode.NotFound,
                _ => StatusCode.UnknownError
            };

            context.Response.ContentType = MediaTypeNames.Application.Json;
            context.Response.StatusCode = (int)statusCode;

#if DEBUG
            string message = $"{ex}";
#else
            string message = "Ocorreu um erro interno durante o processamento da solicitação.";
#endif
            Dictionary<string, object?> logAdditionalData = new() { { "Request", context.Request } };
            logger.Error(message, ex, logAdditionalData);

            await context.Response.WriteAsJsonAsync(Result.CreateError(statusCode, message)).ConfigureAwait(false);
        }
    }
}
