using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Endpoints;

/// <summary>
/// Maps strongly-typed CRUD and NDJSON-streaming minimal API endpoints for the persisted entities of
/// <c>MelloSilveiraTools.MechanicsOfMaterials.Optimizations</c> without requiring hosts to hardcode
/// routes, resource names, or OpenAPI tags.
/// </summary>
public static class OptimizationsCrudEndpoints
{
    /// <summary>
    /// Maps CRUD and NDJSON-streaming endpoints for <see cref="MechanicalModelCurveFitOutputEntity"/>
    /// under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/curve-fit-outputs"</c>.</param>
    /// <param name="requireAuthForMutations">
    /// When <see langword="true"/> (default), requires authentication on <c>PUT</c> and <c>DELETE</c> routes.
    /// </param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> for additional metadata chaining.</returns>
    public static RouteGroupBuilder MapCurveFitOutputEndpoints(this IEndpointRouteBuilder builder, string pattern = "/api/v1/curve-fit-outputs", bool requireAuthForMutations = true)
        => builder.MapCrud<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(pattern, "Curve fit output", requireAuthForMutations, tag: "CurveFitOutputs");

    /// <summary>
    /// Maps CRUD and NDJSON-streaming endpoints for <see cref="MechanicalModelSimulationEntity"/>
    /// under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/mechanical-model-simulations"</c>.</param>
    /// <param name="requireAuthForMutations">
    /// When <see langword="true"/> (default), requires authentication on <c>PUT</c> and <c>DELETE</c> routes.
    /// </param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> for additional metadata chaining.</returns>
    public static RouteGroupBuilder MapMechanicalModelSimulationEndpoints(this IEndpointRouteBuilder builder, string pattern = "/api/v1/mechanical-model-simulations", bool requireAuthForMutations = true)
        => builder.MapCrud<MechanicalModelSimulationEntity, MechanicalModelSimulationFilter>(pattern, "Mechanical model simulation", requireAuthForMutations, tag: "MechanicalModelSimulations");

    /// <summary>
    /// Maps CRUD and NDJSON-streaming endpoints for <see cref="ParametricSweepEntity"/>
    /// under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/parametric-sweeps"</c>.</param>
    /// <param name="requireAuthForMutations">
    /// When <see langword="true"/> (default), requires authentication on <c>PUT</c> and <c>DELETE</c> routes.
    /// </param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> for additional metadata chaining.</returns>
    public static RouteGroupBuilder MapParametricSweepCrudEndpoints(this IEndpointRouteBuilder builder, string pattern = "/api/v1/parametric-sweeps", bool requireAuthForMutations = true)
        => builder.MapCrud<ParametricSweepEntity, ParametricSweepFilter>(pattern, "Parametric sweep", requireAuthForMutations, tag: "ParametricSweeps");

    /// <summary>
    /// Maps all persisted entity CRUD and NDJSON-streaming endpoints for
    /// <c>MelloSilveiraTools.MechanicsOfMaterials.Optimizations</c> (curve fit outputs, mechanical model simulations,
    /// and parametric sweeps) using their canonical routes and OpenAPI tags.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="requireAuthForMutations">
    /// When <see langword="true"/> (default), requires authentication on <c>PUT</c> and <c>DELETE</c> routes.
    /// </param>
    /// <returns>The same <paramref name="builder"/> instance for call chaining.</returns>
    public static IEndpointRouteBuilder MapOptimizationsCrudEndpoints(this IEndpointRouteBuilder builder, bool requireAuthForMutations = true) => builder
        .MapCurveFitOutputEndpoints(requireAuthForMutations: requireAuthForMutations)
        .MapMechanicalModelSimulationEndpoints(requireAuthForMutations: requireAuthForMutations)
        .MapParametricSweepCrudEndpoints(requireAuthForMutations: requireAuthForMutations);
}
