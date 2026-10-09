using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Morris;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;

/// <summary>
/// Defines the pipeline for executing a sensitivity analysis.
/// </summary>
public interface ISensitivityAnalysisPipeline
{
    /// <summary>
    /// Processes the sensitivity analysis for the given input.
    /// </summary>
    /// <param name="input">The input for the sensitivity analysis.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the sensitivity analysis output.</returns>
    Task<Result<MorrisOutput>> ProcessAsync(MorrisInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implements <see cref="ISensitivityAnalysisPipeline"/> using TPL Dataflow.
/// </summary>
public sealed class SensitivityAnalysisPipeline(
    ILogger<SensitivityAnalysisPipeline> logger,
    SensitivityAnalysisSettings settings,
    MorrisAnalyzerStep morrisAnalyzerStep) : ISensitivityAnalysisPipeline
{
    /// <inheritdoc />
    public async Task<Result<MorrisOutput>> ProcessAsync(MorrisInput input, CancellationToken cancellationToken = default)
    {
        MorrisOutput? finalOutput = null;

        await using IDataflowPipeline<MorrisInput> pipeline = PipelineFactory.StartDataflow<MorrisInput>(logger)
            .AddStep(morrisAnalyzerStep)
            .BuildTerminal("Capture Output", output => finalOutput = output);

        await pipeline.SendAsync(input, cancellationToken);
        pipeline.Complete();
        await pipeline.Completion;

        return finalOutput is not null 
            ? Result.CreateSuccessOk(finalOutput)
            : Result.CreateUnknownError("Pipeline completed but no output was captured.");
    }
}
