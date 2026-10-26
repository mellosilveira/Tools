using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;

/// <summary>
/// Factory contract responsible for constructing <see cref="IMechanicalModelCurveFitterStep"/> implementations
/// using the <c>new()</c> directive and configuring the user-specified curve fitting algorithm and mandatory target R².
/// </summary>
public interface IMechanicalModelStepFactory
{
    /// <summary>
    /// Creates a concrete <see cref="IMechanicalModelCurveFitterStep"/> implementation
    /// that matches the specified mechanical model, instantiating the step and its dependencies directly with <c>new()</c>.
    /// </summary>
    /// <param name="modelName">The mechanical model identifier (e.g. <c>"Schapery"</c>, <c>"Fung"</c>, <c>"SimplifiedFung"</c>).</param>
    /// <param name="targetSegments">The segment types to fit.</param>
    /// <param name="curveFitterType">The curve fitting algorithm type to instantiate (e.g., <see cref="CurveFitterType.Alglib"/> or <see cref="CurveFitterType.MathNet"/>).</param>
    /// <param name="targetRSquared">Mandatory target R² determination coefficient requested by the user.</param>
    /// <returns>An instance of <see cref="IMechanicalModelCurveFitterStep"/> for the given model.</returns>
    IMechanicalModelCurveFitterStep CreateCurveFitterStep(string modelName, IReadOnlyList<SegmentType> targetSegments, CurveFitterType curveFitterType, double targetRSquared);
}
