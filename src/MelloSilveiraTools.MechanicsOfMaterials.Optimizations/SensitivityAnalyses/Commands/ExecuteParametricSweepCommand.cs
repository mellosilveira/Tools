using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;

/// <summary>
/// Application command that validates and executes a parametric sweep sensitivity analysis via <see cref="IParametricSweepPipeline"/>.
/// </summary>
/// <param name="pipeline">The parametric sweep pipeline.</param>
/// <param name="validator">Request validator for <see cref="ExecuteParametricSweepRequest"/>.</param>
public sealed class ExecuteParametricSweepCommand(
    IParametricSweepPipeline pipeline,
    IValidator<ExecuteParametricSweepRequest> validator)
    : CommandBaseWithData<ExecuteParametricSweepRequest, ExecuteParametricSweepResponse>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<ExecuteParametricSweepResponse>> ExecuteCommandAsync(ExecuteParametricSweepRequest request)
    {
        Result<string> pipelineResult = await pipeline.ProcessAsync(request.ToInput(), CancellationToken.None).ConfigureAwait(false);
        if (!pipelineResult.Success)
        {
            return new Result<ExecuteParametricSweepResponse>
            {
                Success = false,
                StatusCode = pipelineResult.StatusCode,
                Messages = pipelineResult.Messages
            };
        }

        return new ExecuteParametricSweepResponse(pipelineResult.Data!);
    }
}
