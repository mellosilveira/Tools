using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Morris;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// Pipeline step that runs the Morris Analyzer to calculate sensitivity indices.
/// </summary>
public sealed class MorrisAnalyzerStep(MorrisAnalyzer morrisAnalyzer, MorrisTrajectoryGenerator trajectoryGenerator) : ISyncPipelineStep<MorrisInput, MorrisOutput>
{
    /// <inheritdoc />
    public string Name => nameof(MorrisAnalyzerStep);

    /// <inheritdoc />
    public MorrisOutput Execute(MorrisInput input)
    {
        List<List<MorrisPoint>> trajectories = trajectoryGenerator.Generate(input);

        return morrisAnalyzer.Analyze(
            input.Levels,
            input.TargetOutputs.ToList(),
            input.Boundaries.Select(b => b.ParameterPath).ToList(),
            trajectories);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose.
    }
}
