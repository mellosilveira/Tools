using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

/// <summary>
/// Contains the input data for Schapery's model.
/// </summary>
public sealed record SchaperyModelInput : NonLinearModelInput
{
    #region Contansts for strain calculation

    /// <summary>
    /// The letter G and the index numbers indicate the Gibb’s free energy dependence and its order.
    /// Material constant based on thermodynamic concepts that depend on stress and Gibb’s free energy.
    /// Non-linear elastic response that calculates the instantaneous change in stiffness.
    /// Unit: .
    /// </summary>
    public double G0 { get; init; }

    /// <summary>
    /// The letter G and the index numbers indicate the Gibb’s free energy dependence and its order.
    /// Material constant based on thermodynamic concepts that depend on stress and Gibb’s free energy.
    /// Non-linear transient response.
    /// Unit: .
    /// </summary>
    public double G1 { get; init; }

    /// <summary>
    /// The letter G and the index numbers indicate the Gibb’s free energy dependence and its order.
    /// Material constant based on thermodynamic concepts that depend on stress and Gibb’s free energy.
    /// The parameter that measures load rate effects in creep.
    /// Unit: .
    /// </summary>
    public double G2 { get; init; }

    /// <summary>
    /// The state in equilibrium of creep compliance.
    /// Unit: .
    /// </summary>
    public double J0 { get; init; }

    #endregion

    #region Constants for stress calculation

    /// <summary>
    /// Material constant that depend on strain and Helmoltz free energy for the state in equilibrium.
    /// Unit: dimensionless.
    /// </summary>
    public Function? He { get; init; }

    /// <summary>
    /// Material constant that depend on strain and Helmoltz free energy.
    /// First order of dependence on the Helmoltz free energy.
    /// Unit: dimensionless.
    /// </summary>
    public Function? H1 { get; init; }

    /// <summary>
    /// Material constant that depend on strain and Helmoltz free energy.
    /// Second order of dependence on the Helmoltz free energy.
    /// Unit: dimensionless.
    /// </summary>
    public Function? H2 { get; init; }

    /// <summary>
    /// Stress dependent coefficients based on thermodynamic concepts. 
    /// The state in equilibrium of relaxation function.
    /// Unit: MPa (Mega-pascal).
    /// </summary>
    public double Ge { get; init; }

    #endregion

    #region Constants for Relaxation Function

    /// <summary>
    /// An <see cref="Function"/> that represents the transient relaxation function.
    /// </summary>
    public PowerLaw? TransientRelaxationFunction { get; init; }

    #endregion

    #region Constants for Creep Compliance

    /// <summary>
    /// An <see cref="Expression"/> that represents the transient creep compliance.
    /// </summary>
    public PronySeries? TransientCreepCompliance { get; init; }

    #endregion
}
