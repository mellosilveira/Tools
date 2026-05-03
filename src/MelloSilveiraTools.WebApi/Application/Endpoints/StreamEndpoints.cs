using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Application.Models;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace MelloSilveiraTools.WebApi.Application.Endpoints;

/// <summary>
/// Helpers that expose the same NDJSON streaming behavior as <see cref="Controllers.CustomControllerBase"/>
/// so minimal-API endpoints can write newline-delimited JSON responses without inheriting from a controller.
/// </summary>
public static class StreamEndpoints
{
    /// <summary>
    /// Maps a <c>GET</c> endpoint at <paramref name="pattern"/> that streams entities matching <typeparamref name="TFilter"/>
    /// as NDJSON. Available standalone (for hosts that only need the streaming path) or composed by <c>CrudEndpoints.MapCrud</c>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being streamed.</typeparam>
    /// <typeparam name="TFilter">The filter type used to query the entity.</typeparam>
    /// <param name="builder">Endpoint route builder used to register the endpoint.</param>
    /// <param name="pattern">Route pattern (e.g. <c>"/stream"</c> when called from a route group, or a full path).</param>
    /// <param name="resourceName">Human-readable resource name used when reporting streaming failures.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> so the caller can chain metadata.</returns>
    public static RouteHandlerBuilder MapStream<TEntity, TFilter>(
        this IEndpointRouteBuilder builder,
        string pattern,
        string resourceName)
        where TEntity : EntityBase, new()
        where TFilter : FilterBase, new()
        => builder
            .MapGet(pattern, async (HttpContext httpContext, IRepository repository, ILogger logger, [AsParameters] TFilter filter) => repository
                .GetAsync<TEntity, TFilter>(filter, cancellationToken: httpContext.RequestAborted)
                .WriteNdjsonAsync(httpContext, logger, resourceName))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithName($"Stream_{resourceName}")
            .WithSummary($"Streams {resourceName} entities as newline-delimited JSON.");

    /// <summary>
    /// Writes <paramref name="entities"/> to the response body as newline-delimited JSON (NDJSON).
    /// Sets the proper content-type, the <c>X-Content-Type-Options: nosniff</c> header, and reports
    /// the final status through the <see cref="ApplicationConstants.StreamStatusTrailerName"/> trailer.
    /// </summary>
    /// <typeparam name="T">Type of the items being streamed.</typeparam>
    /// <param name="context">HTTP context that owns the response stream.</param>
    /// <param name="entities">Asynchronous sequence of items to be streamed.</param>
    /// <param name="logger">Logger used to record warnings (client cancellation) and errors.</param>
    /// <param name="resourceName">Human-readable resource name used when reporting streaming failures.</param>
    public static async Task WriteNdjsonAsync<T>(
        this IAsyncEnumerable<T> entities,
        HttpContext context,
        ILogger logger,
        string resourceName)
    {
        // The nosniff directive within the X-Content-Type-Options HTTP response header is a security measure designed to
        // prevent browsers from performing MIME type sniffing.
        context.Response.Headers.Append(HeaderNames.XContentTypeOptions, ApplicationConstants.NoSniffHeaderValue);
        context.Response.ContentType = ApplicationConstants.NdjsonContentType;

        try
        {
            await foreach (T entity in entities.WithCancellation(context.RequestAborted))
            {
                await context.Response.WriteLineAsNdJsonAsync(entity, context.RequestAborted);
            }

            context.Response.AppendTrailer(ApplicationConstants.StreamStatusTrailerName, ApplicationConstants.StreamSuccessfullyStatus);
        }
        catch (OperationCanceledException ex)
        {
            logger.Warn("A conexão foi fechada pelo cliente durante o streaming.", ex);
        }
        catch (Exception ex)
        {
            logger.Error($"Falha durante o streaming de {resourceName}.", ex);
        }
    }
}
