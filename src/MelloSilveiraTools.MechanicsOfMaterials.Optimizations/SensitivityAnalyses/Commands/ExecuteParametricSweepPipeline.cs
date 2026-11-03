using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Commands;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;

/// <summary>
/// Application command that validates and executes a parametric sweep sensitivity analysis via <see cref="IPipeline{TInput, TOutput}"/>.
/// </summary>
/// <param name="pipeline">The parametric sweep pipeline.</param>
/// <param name="validator">Request validator for <see cref="ExecuteParametricSweepPipelineRequest"/>.</param>
public sealed class ExecuteParametricSweepPipeline(
    ILogger<ExecuteParametricSweepPipeline> logger,
    IPipeline<ParametricSweepInput, string> pipeline,
    IValidator<ExecuteParametricSweepPipelineRequest> validator)
    : ExecutePipeline<ExecuteParametricSweepPipelineRequest, ParametricSweepInput, string>(logger, pipeline, validator)
{
    protected override ParametricSweepInput MapToInput(ExecuteParametricSweepPipelineRequest request) => new()
    {
        Identifier = string.IsNullOrWhiteSpace(request.Identifier) ? Guid.NewGuid().ToString("N") : request.Identifier,
        MechanicalModelName = request.MechanicalModelName,
        Baseline = request.Baseline,
        ConstantVariables = request.ConstantVariables,
        ArrayVariables = request.ArrayVariables,
        BaseStrain = request.BaseStrain,
        StrainRange = request.StrainRange,
        RampTimeConsideration = request.RampTimeConsideration,
        RampTime = request.RampTime,
        InitialTime = request.InitialTime,
        TimeStep = request.TimeStep,
        FinalTime = request.FinalTime,
        AsymptoteTolerance = request.AsymptoteTolerance,
        BinarySearchIterations = request.BinarySearchIterations,
        MaxBracketIterations = request.MaxBracketIterations,
    };
}
