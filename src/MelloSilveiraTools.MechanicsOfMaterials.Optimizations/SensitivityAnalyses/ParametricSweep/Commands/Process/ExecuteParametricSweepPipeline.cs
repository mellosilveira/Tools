using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Commands;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;

/// <summary>
/// Application command that validates and executes a parametric sweep sensitivity analysis via <see cref="IPipeline{TInput, TOutput}"/>.
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="pipeline">The parametric sweep pipeline.</param>
/// <param name="validator">Request validator for <see cref="ExecuteParametricSweepPipelineRequest"/>.</param>
public sealed class ExecuteParametricSweepPipeline(
    ILogger<ExecuteParametricSweepPipeline> logger,
    IPipeline<ParametricSweepPipelineInput, string> pipeline,
    IValidator<ExecuteParametricSweepPipelineRequest> validator)
    : ExecutePipeline<ExecuteParametricSweepPipelineRequest, ParametricSweepPipelineInput, string>(logger, pipeline, validator)
{
    /// <inheritdoc/>
    protected override ParametricSweepPipelineInput MapToInput(ExecuteParametricSweepPipelineRequest request) => new()
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
        RootFindingAlgorithm = request.RootFindingAlgorithm,
        BaseRootFindingInput = request.BaseRootFindingInput,
    };
}
