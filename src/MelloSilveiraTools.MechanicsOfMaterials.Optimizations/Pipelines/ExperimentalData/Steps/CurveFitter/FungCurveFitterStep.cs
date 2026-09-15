using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Fung model (full curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class FungCurveFitterStep : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(FungCurveFitterStep);

    /// <inheritdoc />
    public ConstitutiveParameters[] Execute(CurveSegment[] input)
    {
        // TODO: Implement specific numerical solver for Fung full curve fitting.
        return Array.Empty<ConstitutiveParameters>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged resources to release.
    }
}
