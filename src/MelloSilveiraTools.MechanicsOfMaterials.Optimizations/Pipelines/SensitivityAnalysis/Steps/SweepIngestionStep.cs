using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// Pipeline ingestion step that dynamically resolves the appropriate generator for the given mechanical model
/// and lazily yields permutations into the stream.
/// </summary>
public sealed class SweepIngestionStep(ServiceLocator serviceLocator) : IEnumerablePipelineStep<ParametricSweepInput, SweepSimulationEnvelope>
{
    /// <inheritdoc />
    public string Name => nameof(SweepIngestionStep);

    /// <inheritdoc />
    public IEnumerable<SweepSimulationEnvelope> Execute(ParametricSweepInput input, CancellationToken cancellationToken)
    {
        IParametricSweepGenerator generator = serviceLocator.GetRequiredKeyedService<IParametricSweepGenerator>(input.MechanicalModelName);
        foreach (SweepSimulationEnvelope envelope in generator.GenerateCombinations(input))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return envelope;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
