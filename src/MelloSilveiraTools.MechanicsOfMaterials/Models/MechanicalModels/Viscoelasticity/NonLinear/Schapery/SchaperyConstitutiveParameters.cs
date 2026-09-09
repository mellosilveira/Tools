using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

/// <summary>
/// Defines the constitutive parameters for the Schapery non-linear viscoelastic model.
/// </summary>
public sealed record SchaperyConstitutiveParameters : ConstitutiveParameters
{
    #region Constants for strain calculation (Creep)

    /// <summary>
    /// Material constant based on thermodynamic concepts that depends on stress and Gibbs free energy.
    /// Represents the non-linear elastic response that calculates the instantaneous change in compliance.
    /// </summary>
    /// <value>Unit: dimensionless.</value>
    public double G0 { get; init; }

    /// <summary>
    /// Material constant based on thermodynamic concepts that depends on stress and Gibbs free energy.
    /// Represents the non-linear transient response.
    /// </summary>
    /// <value>Unit: dimensionless.</value>
    public double G1 { get; init; }

    /// <summary>
    /// Material constant based on thermodynamic concepts that depends on stress and Gibbs free energy.
    /// Parameter that measures load rate effects in creep.
    /// </summary>
    /// <value>Unit: dimensionless.</value>
    public double G2 { get; init; }

    /// <summary>
    /// The instantaneous or equilibrium state of creep compliance.
    /// </summary>
    /// <value>Unit: /MPa (per Mega-Pascal).</value>
    public double J0 { get; init; }

    #endregion

    #region Constants for stress calculation (Relaxation)

    /// <summary>
    /// Strain-dependent material function based on Helmholtz free energy for the state in equilibrium.
    /// </summary>
    /// <value>Unit: dimensionless.</value>
    public Function? He { get; init; }

    /// <summary>
    /// Strain-dependent material function based on Helmholtz free energy.
    /// First order of dependence on the Helmholtz free energy.
    /// </summary>
    /// <value>Unit: dimensionless.</value>
    public Function? H1 { get; init; }

    /// <summary>
    /// Strain-dependent material function based on Helmholtz free energy.
    /// Second order of dependence on the Helmholtz free energy.
    /// </summary>
    /// <value>Unit: dimensionless.</value>
    public Function? H2 { get; init; }

    /// <summary>
    /// Strain-dependent coefficient based on thermodynamic concepts. 
    /// The state in equilibrium of the relaxation function.
    /// </summary>
    /// <value>Unit: MPa (Mega-Pascal).</value>
    public double Ge { get; init; }

    #endregion

    #region Constants for Relaxation Function

    /// <summary>
    /// Represents the transient relaxation function using a Power Law formulation.
    /// </summary>
    public PowerLaw? TransientRelaxationFunction { get; init; }

    #endregion

    #region Constants for Creep Compliance

    /// <summary>
    /// Represents the transient creep compliance using a Prony Series formulation.
    /// </summary>
    public PronySeries? TransientCreepCompliance { get; init; }

    #endregion
}