using MelloSilveiraTools.Application.Models;
using MelloSilveiraTools.Infrastructure.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mime;

namespace MelloSilveiraTools.Application.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
{
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
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                ArgumentException or InvalidOperationException => HttpStatusCode.BadRequest,
                KeyNotFoundException => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.ContentType = MediaTypeNames.Application.Json;
            context.Response.StatusCode = (int)statusCode;
            await context.Response
                .WriteAsJsonAsync(new ProblemDetails
                {
                    Status = (int)statusCode,
                    Title = "Erro na requisição.",
                    Detail = ex.Message ?? "Ocorreu um erro interno. Tente novamente mais tarde.",
                    Instance = context.Request.Path
                })
                .ConfigureAwait(false);
        }
    }
}