using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <summary>
/// Defines a unified structural and mathematical facade for all phenomenological mechanical model calculators.
/// </summary>
/// <remarks>
/// This interface fully encapsulates the underlying numerical complexity, constitutive tensor operations, 
/// and polymorphic material definitions, providing the macroscopic solver with a clean, strictly-typed 
/// contract to evaluate continuum mechanics states without exposing specific formulation details.
/// </remarks>
public interface IMechanicalModelCalculatorFacade
{
    /// <summary>
    /// Evaluates the complete, deterministic macroscopic and intensive state of the continuum at a specific integration step.
    /// </summary>
    /// <param name="time">The elapsed integration time ($t$). Unit: seconds (s).</param>
    /// <returns>An aggregated artifact encapsulating the instantaneously converged stress, strain, force, and displacement.</returns>
    MechanicalModelOutput Calculate(double time);

    /// <summary>
    /// Resolves the global kinetic response (force) evaluated against a generic macroscopic boundary condition.
    /// </summary>
    /// <param name="input">The generalized problem definition acting as a polymorphic parameter payload.</param>
    /// <param name="time">The integration time ($t$). Unit: seconds (s).</param>
    /// <param name="displacement">An optional kinematic override to bypass the intrinsic input history during iterative root-finding steps.</param>
    /// <returns>The resultant macroscopic force. Unit: Newtons (N).</returns>
    double CalculateForce(GenericMechanicalModelInput input, double time, double? displacement = null);

    /// <summary>
    /// Resolves the global kinematic response (displacement) derived from a generic kinetic boundary condition.
    /// </summary>
    /// <param name="input">The generalized problem definition acting as a polymorphic parameter payload.</param>
    /// <param name="time">The integration time ($t$). Unit: seconds (s).</param>
    /// <param name="force">An optional kinetic override to bypass the intrinsic input history.</param>
    /// <returns>The resultant macroscopic displacement. Unit: meters (m).</returns>
    double CalculateDisplacement(GenericMechanicalModelInput input, double time, double? force = null);

    /// <summary>
    /// Evaluates the intensive kinetic response (stress tensor magnitude) within the continuum domain.
    /// </summary>
    /// <param name="input">The generalized problem definition acting as a polymorphic parameter payload.</param>
    /// <param name="time">The integration time ($t$). Unit: seconds (s).</param>
    /// <param name="strain">An optional intensive kinematic override to bypass the intrinsic input history.</param>
    /// <returns>The resultant continuum stress. Unit: Megapascals (MPa).</returns>
    double CalculateStress(GenericMechanicalModelInput input, double time, double? strain = null);

    /// <summary>
    /// Evaluates the intensive kinematic response (strain tensor magnitude) within the continuum domain.
    /// </summary>
    /// <param name="input">The generalized problem definition acting as a polymorphic parameter payload.</param>
    /// <param name="time">The integration time ($t$). Unit: seconds (s).</param>
    /// <param name="stress">An optional intensive kinetic override to bypass the intrinsic input history.</param>
    /// <returns>The resultant continuum strain. Dimensionless.</returns>
    double CalculateStrain(GenericMechanicalModelInput input, double time, double? stress = null);
}