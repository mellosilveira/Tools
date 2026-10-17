using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

/// <summary>
/// Request payload for executing a standalone mechanical model curve fitting command over segmented experimental data.
/// </summary>
public record FitCurveRequest : RequestBase
{
    /// <summary>
    /// The name of the mechanical model to fit (e.g., "Fung", "SimplifiedFung", "Schapery").
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// The segmented experimental curves to be fitted.
    /// </summary>
    public required IReadOnlyList<CurveSegment> Segments { get; init; }

    /// <summary>
    /// Optional target segment types to fit. When empty, all segment types supported by the model are fitted.
    /// </summary>
    public IReadOnlyList<SegmentType> TargetSegments { get; init; } = [];

    /// <summary>
    /// The curve fitting algorithm type to execute (e.g., <see cref="CurveFitterType.Alglib"/> or <see cref="CurveFitterType.MathNet"/>).
    /// </summary>
    public CurveFitterType CurveFitterType { get; init; } = CurveFitterType.Alglib;

    /// <summary>
    /// Target R² determination coefficient requested for the optimization. Defaults to 0.99.
    /// </summary>
    public double TargetRSquared { get; init; } = 0.99;

    /// <summary>
    /// Optional optimization options override.
    /// </summary>
    public OptimizationOptionsRequest? Options { get; init; }

    /// <summary>
    /// Optional cooperative cancellation token for terminating fitting prematurely.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public CancellationToken CancellationToken { get; init; } = default;
}
