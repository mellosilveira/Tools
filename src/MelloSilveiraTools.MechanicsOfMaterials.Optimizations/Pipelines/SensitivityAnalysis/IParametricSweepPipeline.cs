using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;

// TODO: CRIAR INTERFACE GENÉRICA A TODAS AS PIPELINES.

/// <summary>
/// Orchestrates the One-At-A-Time (OAAT) Parametric Sweep sensitivity analysis pipeline.
/// </summary>
public interface IParametricSweepPipeline
{
    /// <summary>
    /// Builds and executes the parametric sweep pipeline for a given analysis request.
    /// </summary>
    /// <param name="input">The parametric sweep input configuration.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the unique external identifier of the completed sweep.</returns>
    Task<Result<string>> ProcessAsync(ParametricSweepInput input, CancellationToken cancellationToken = default);
}
