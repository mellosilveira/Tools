using MelloSilveiraTools.Mathematics.Functions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

/// <summary>
/// Defines the constitutive parameters for the Modified Superposition Method (MSM) non-linear viscoelastic model.
/// </summary>
public sealed record ModifiedSuperpositionMethodConstitutiveParameters : ConstitutiveParameters
{
    /// <summary>
    /// Gets the function representing the instantaneous elastic stiffness (Young's Modulus).
    /// </summary>
    /// <remarks>
    /// In this non-linear formulation, the initial modulus is not a constant, but a function that varies depending on the applied strain or stress level.
    /// </remarks>
    public Function? InitialYoungModulus { get; init; }

    /// <summary>
    /// Gets the polynomial function representing the rate of stress relaxation.
    /// </summary>
    /// <remarks>
    /// Dictates how the viscous decay accelerates or decelerates depending on the magnitude of the strain.
    /// </remarks>
    public PolynomialFunction? StressRelaxationRate { get; init; }
}