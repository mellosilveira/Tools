using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Upload;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Endpoints;

/// <summary>
/// Maps the same experimental data upload, processing, and NDJSON batch streaming surface as
/// <see cref="Controllers.ExperimentalDataController"/> using minimal APIs.
/// </summary>
public static class ExperimentalDataEndpoints
{
    /// <summary>
    /// Maps all experimental data endpoints under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/experimental-data"</c>.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain authorization or tags.</returns>
    public static RouteGroupBuilder MapExperimentalDataEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/experimental-data")
    {
        RouteGroupBuilder group = builder.MapGroup(pattern).WithTags("ExperimentalData");

        group
            .MapPost("/upload", async (HttpContext httpContext, [FromServices] UploadExperimentalDataCommand command, IFormFile strainFile, IFormFile stressFile) => await command
                .ExecuteAsync(new UploadExperimentalDataRequest { StrainFile = strainFile, StressFile = stressFile }, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .DisableAntiforgery()
            .Produces<Result<UploadExperimentalDataResultData>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .WithName("UploadExperimentalData")
            .WithSummary("Uploads a pair of raw experimental CSV files (strainFile and stressFile) via multipart/form-data.");

        group
            .MapPost("/process", async (HttpContext httpContext, [FromServices] ExecuteExperimentalDataPipeline command, [FromBody] ExecuteExperimentalDataPipelineRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces(StatusCodes.Status202Accepted)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .WithName("ProcessExperimentalData")
            .WithSummary("Starts an asynchronous experimental data constitutive fitting pipeline.");

        group
            .MapPost("/process/batch", async (HttpContext httpContext, [FromServices] ExecuteBatchExperimentalDataPipeline command, [FromServices] ILoggerFactory loggerFactory, [FromBody] ExecuteBatchExperimentalDataPipelineRequest request) =>
            {
                Result<IAsyncEnumerable<string>> result = await command.ExecuteAsync(request, httpContext.RequestAborted).ConfigureAwait(false);
                if (!result.Success)
                    return result.ToHttpResult();

                ILogger logger = loggerFactory.CreateLogger(typeof(ExperimentalDataEndpoints));
                httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
                await result.Data!.WriteNdjsonAsync(httpContext, logger, "Progress for experimental data pipelines", httpContext.RequestAborted).ConfigureAwait(false);
                return Results.Empty;
            })
            .Produces(StatusCodes.Status202Accepted, contentType: "application/x-ndjson")
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .WithName("ProcessExperimentalDataBatch")
            .WithSummary("Starts a batch of experimental data pipelines and streams real-time NDJSON progress notifications.");

        return group;
    }
}
