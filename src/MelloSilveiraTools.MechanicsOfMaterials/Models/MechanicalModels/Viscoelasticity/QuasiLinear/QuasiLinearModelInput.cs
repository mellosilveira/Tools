namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// Contains the input data for quasi-linear viscoelastic model.
/// </summary>
/// <typeparam name="TReducedRelaxationFunction">The type of reduced relaxation function.</typeparam>
public abstract record QuasiLinearModelInput<TReducedRelaxationFunction> : MechanicalModelInput
{
    #region Elastic Response parameters

    /// <summary>
    /// Constant A.
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    public double ElasticStressConstant { get; init; }

    /// <summary>
    /// Constant B.
    /// Unit: dimensionless.
    /// </summary>
    public double ElasticPowerConstant { get; init; }

    #endregion

    #region Reduced Relaxation Function constants

    /// <summary>
    /// The constants for Reduced Relaxation Function.
    /// </summary>
    public TReducedRelaxationFunction? ReducedRelaxationFunction { get; init; }

    #endregion
}