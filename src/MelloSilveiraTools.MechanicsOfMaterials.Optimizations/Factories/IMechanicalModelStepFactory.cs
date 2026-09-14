using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Abstractions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Factories;

/// <summary>
/// Factory responsible for creating the appropriate <see cref="IMechanicalModelCurveFitterStep"/>
/// based on the mechanical model name supplied at runtime.
/// </summary>
public interface IMechanicalModelStepFactory
{
    /// <summary>
    /// Creates a concrete <see cref="IMechanicalModelCurveFitterStep"/> implementation
    /// that matches the specified mechanical model.
    /// </summary>
    /// <param name="modelName">The mechanical model identifier (e.g. <c>"Schapery"</c>, <c>"Fung"</c>, <c>"SimplifiedFung"</c>).</param>
    /// <param name="targetSegments"></param>
    /// <returns>An instance of <see cref="IMechanicalModelCurveFitterStep"/> for the given model.</returns>
    IMechanicalModelCurveFitterStep Create(string modelName, IReadOnlyList<SegmentType> targetSegments);
}
