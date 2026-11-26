using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.MechanicalModels.Get;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MelloSilveiraTools.MechanicsOfMaterials.Endpoints;

/// <summary>
/// Maps high-performance, low-allocation Minimal API endpoints exposing <c>MelloSilveiraTools.MechanicsOfMaterials</c>
/// capabilities (registered constitutive model discovery and Goodman/Marin fatigue analysis).
/// </summary>
public static class MechanicsOfMaterialsEndpoints
{
    /// <summary>
    /// Registers all Mechanics of Materials Minimal API endpoints under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/mechanics"</c>.</param>
    /// <returns>The configured <see cref="RouteGroupBuilder"/>.</returns>
    public static RouteGroupBuilder MapMechanicsOfMaterialsEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/mechanics")
    {
        RouteGroupBuilder group = builder
            .MapGroup(pattern)
            .WithTags("MechanicsOfMaterials");

        group
            .MapGet("/models", async (HttpContext httpContext, [FromServices] GetMechanicalModelsCommand command) => await command
                .ExecuteAsync(httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<ListedResult<MechanicalModelMetadata>>(StatusCodes.Status200OK)
            .WithName("GetAvailableMechanicalModels")
            .WithSummary("Lists every registered mechanical model resolver with its calculator type and JSON structures for constitutive parameters and output.");

        group
            .MapPost("/specimen-analysis/execute", async (HttpContext httpContext, [FromServices] ExecuteSpecimenAnalysisCommand command, [FromBody] ExecuteSpecimenAnalysisRequest request) => await command
                .ExecuteAsync(request, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<Result<ExecuteSpecimenAnalysisResultData>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .WithName("ExecuteSpecimenAnalysis")
            .WithSummary("Executes a deterministic specimen mechanical analysis, persists the generated CSV file, and returns delta metrics and metadata ID.");

        group
            .MapPost("/fatigue/calculate", ([FromServices] IFatigueCalculator calculator, [FromBody] FatigueInput input) =>
                Result.CreateSuccessOk(calculator.Calculate(input)).ToHttpResult())
            .Produces<Result<FatigueOutput>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .WithName("CalculateFatigue")
            .WithSummary("Runs a complete high-cycle Goodman/Marin fatigue analysis.");

        group
            .MapPost("/fatigue/calculate/endurance-limit", ([FromServices] IFatigueCalculator calculator, [FromBody] FatigueInput input) =>
                Result.CreateSuccessOk(calculator.CalculateModifiedFatigueStress(input)).ToHttpResult())
            .Produces<Result<double>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .WithName("CalculateModifiedEnduranceLimit")
            .WithSummary("Calculates the Marin-corrected endurance limit Se (MPa).");

        return group;
    }
}
