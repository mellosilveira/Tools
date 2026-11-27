using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Host.Commands;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.WebApi.Host.Endpoints;

/// <summary>
/// Maps the same pre-bundled sample dataset surface as <see cref="Controllers.SamplesController"/>
/// using minimal APIs.
/// </summary>
public static class SamplesEndpoints
{
    /// <summary>
    /// Maps the bundled sample dataset listing endpoint under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/samples"</c>.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain authorization or tags.</returns>
    public static RouteGroupBuilder MapSamplesEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/samples")
    {
        RouteGroupBuilder group = builder.MapGroup(pattern).WithTags("Samples");

        group
            .MapGet("/", async (HttpContext httpContext, [FromServices] GetSampleDatasetsCommand command) => await command
                .ExecuteAsync(httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<ListedResult<StorageEntryResultData>>(StatusCodes.Status200OK)
            .WithName("GetSampleDatasets")
            .WithSummary("Lists all pre-bundled sample dataset files as StorageEntryResultData entries.");

        return group;
    }
}
