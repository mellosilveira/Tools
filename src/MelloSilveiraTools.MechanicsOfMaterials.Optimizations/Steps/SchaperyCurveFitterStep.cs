using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Abstractions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Steps;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Schapery model (full curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SchaperyCurveFitterStep : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(SchaperyCurveFitterStep);

    /// <inheritdoc />
    public ConstitutiveParameters[] Execute(CurveSegment[] input)
    {
        // TODO: Implement specific numerical solver for Schapery full curve fitting.
        return Array.Empty<ConstitutiveParameters>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged resources to release.
    }
}
