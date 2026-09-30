using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <summary>
/// Factory contract responsible for constructing <see cref="IMechanicalModelCalculatorFacade"/> instances configured for mechanical model simulations.
/// </summary>
public interface IMechanicalModelCalculatorFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="IMechanicalModelCalculatorFacade"/> for the specified mechanical model.
    /// </summary>
    /// <param name="mechanicalModelName">The unique registry name of the mechanical model.</param>
    /// <returns>A configured <see cref="IMechanicalModelCalculatorFacade"/> instance.</returns>
    IMechanicalModelCalculatorFacade CreateCalculatorFacade(string mechanicalModelName);

    /// <summary>
    /// Creates a new instance of <see cref="IMechanicalModelCalculatorFacade"/> for the specified mechanical model and input.
    /// </summary>
    /// <param name="mechanicalModelName">The unique registry name of the mechanical model.</param>
    /// <param name="input">The generic mechanical model input containing parameters, boundary conditions, and time configuration.</param>
    /// <returns>A configured <see cref="IMechanicalModelCalculatorFacade"/> instance.</returns>
    IMechanicalModelCalculatorFacade CreateCalculatorFacade(string mechanicalModelName, GenericMechanicalModelInput input);

    /// <summary>
    /// Creates a new instance of <see cref="IMechanicalModelCalculatorFacade"/> for the provided generic input.
    /// </summary>
    /// <param name="input">The generic mechanical model input containing parameters, boundary conditions, and time configuration.</param>
    /// <returns>A configured <see cref="IMechanicalModelCalculatorFacade"/> instance.</returns>
    IMechanicalModelCalculatorFacade CreateCalculatorFacade(GenericMechanicalModelInput input);
}
