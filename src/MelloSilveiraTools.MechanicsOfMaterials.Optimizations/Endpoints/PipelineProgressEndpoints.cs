using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Endpoints;

/// <summary>
/// Maps the same real-time pipeline progress NDJSON streaming surface as
/// <see cref="Controllers.PipelineProgressController"/> using minimal APIs.
/// </summary>
public static class PipelineProgressEndpoints
{
    /// <summary>
    /// Maps pipeline progress streaming endpoints under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/pipelines"</c>.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain authorization or tags.</returns>
    public static RouteGroupBuilder MapPipelineProgressEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/pipelines")
    {
        RouteGroupBuilder group = builder.MapGroup(pattern).WithTags("Pipelines");

        group
            .MapGet("/{pipelineId}/progress", async (HttpContext httpContext, [FromServices] IPipelineProgressNotifier progressNotifier, [FromServices] ILoggerFactory loggerFactory, string pipelineId) =>
            {
                ILogger logger = loggerFactory.CreateLogger(typeof(PipelineProgressEndpoints));
                await progressNotifier
                    .ReadAllAsync(pipelineId, httpContext.RequestAborted)
                    .WriteNdjsonAsync(httpContext, logger, "Progress for pipeline execution", httpContext.RequestAborted)
                    .ConfigureAwait(false);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/x-ndjson")
            .WithName("StreamPipelineProgress")
            .WithSummary($"Streams real-time {nameof(PipelineProgressEvent)} notifications as an NDJSON response stream.");

        return group;
    }
}
