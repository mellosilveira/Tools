namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// Defines the constitutive parameters for the Quasi-Linear Viscoelastic (QLV) model.
/// </summary>
/// <typeparam name="TReducedRelaxationFunction">The specific type of the reduced relaxation function used in the time-dependent formulation.</typeparam>
public abstract record QuasiLinearConstitutiveParameters<TReducedRelaxationFunction> : ConstitutiveParameters
{
    #region Elastic Response parameters

    /// <summary>
    /// Gets the linear stress scaling constant (often denoted as A).
    /// </summary>
    /// <value>Unit: MPa (Mega-Pascal).</value>
    public double ElasticStressConstant { get; init; }

    /// <summary>
    /// Gets the non-linear stiffening coefficient (often denoted as B), which governs the exponential elastic response.
    /// </summary>
    /// <value>Unit: dimensionless.</value>
    public double ElasticPowerConstant { get; init; }

    #endregion

    #region Reduced Relaxation Function constants

    /// <summary>
    /// Gets the parameters that govern the time-dependent reduced relaxation function (often denoted as G(t)).
    /// </summary>
    public TReducedRelaxationFunction? ReducedRelaxationFunction { get; init; }

    #endregion
}