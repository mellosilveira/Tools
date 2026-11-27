using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Endpoints;

/// <summary>
/// Maps the same Parametric Sweep execution and NDJSON progress streaming surface as
/// <see cref="Controllers.ParametricSweepController"/> using minimal APIs.
/// </summary>
public static class ParametricSweepEndpoints
{
    /// <summary>
    /// Maps all Parametric Sweep endpoints under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/parametric-sweeps"</c>.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain authorization or tags.</returns>
    public static RouteGroupBuilder MapParametricSweepEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/parametric-sweeps")
    {
        RouteGroupBuilder group = builder.MapGroup(pattern).WithTags("ParametricSweeps");

        group
            .MapPost("/process", async (HttpContext httpContext, [FromServices] ExecuteParametricSweepPipeline command, [FromBody] ExecuteParametricSweepPipelineRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces(StatusCodes.Status202Accepted)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .WithName("ProcessParametricSweep")
            .WithSummary("Starts an asynchronous OAAT parametric sweep sensitivity analysis pipeline.");

        group
            .MapPost("/process/experimental-data", async (HttpContext httpContext, [FromServices] ExecuteParametricSweepFromExperimentalDataCommand command, [FromServices] ILoggerFactory loggerFactory, [FromBody] ExecuteParametricSweepFromExperimentalDataRequest request) =>
            {
                Result<IAsyncEnumerable<string>> result = await command.ExecuteAsync(request, httpContext.RequestAborted).ConfigureAwait(false);
                if (!result.Success)
                    return result.ToHttpResult();

                ILogger logger = loggerFactory.CreateLogger(typeof(ParametricSweepEndpoints));
                httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
                await result.Data!.WriteNdjsonAsync(httpContext, logger, "Progress for parametric sweep pipelines", httpContext.RequestAborted).ConfigureAwait(false);
                return Results.Empty;
            })
            .Produces(StatusCodes.Status202Accepted, contentType: "application/x-ndjson")
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .WithName("ProcessParametricSweepFromExperimentalData")
            .WithSummary("Starts parametric sweep pipelines from stored experimental data and streams NDJSON progress.");

        return group;
    }
}
