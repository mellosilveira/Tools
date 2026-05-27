using MelloSilveiraTools.Mathematics.Functions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

/// <summary>
/// Contains the input data for Modified Superposition Method.
/// </summary>
public sealed record ModifiedSuperpositionMethodInput : NonLinearModelInput
{
    /// <summary>
    /// A <see cref="Function"/> that represents the initial Young Modulus.
    /// </summary>
    public Function? InitialYoungModulus { get; init; }

    /// <summary>
    /// A <see cref="PolynomialFunction"/> that represents the stress relaxation rate.
    /// Strain-dependent rate of stress relaxation.
    /// </summary>
    public PolynomialFunction? StressRelaxationRate { get; init; }
}
