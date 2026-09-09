using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Abstractions;

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
    /// <param name="modelName">
    /// The mechanical model identifier (e.g. <c>"Schapery"</c>, <c>"Fung"</c>,
    /// <c>"SimplifiedFungRelaxation"</c>). Matching is case-insensitive.
    /// </param>
    /// <returns>An instance of <see cref="IMechanicalModelCurveFitterStep"/> for the given model.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="modelName"/> does not match any known model.</exception>
    IMechanicalModelCurveFitterStep Create(string modelName);
}
