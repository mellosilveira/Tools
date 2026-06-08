using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Contains the input data for a generic mechanical model.
/// </summary>
public record MechanicalModelInput
{
    /// <summary>
    /// Unique identifier for mechanical model in current analysis.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Unit: dimensionless.
    /// </summary>
    public AcceptedRange AcceptedStrainRange { get; init; } = AcceptedRange.Default;

    #region Mechanical parameters

    public MechanicalBehaviorType MechanicalBehaviorType { get; init; }

    /// <inheritdoc cref="Viscoelasticity.ViscoelasticEffect"/>
    public ViscoelasticEffect ViscoelasticEffect { get; init; }

    /// <inheritdoc cref="Viscoelasticity.RampTimeConsideration"/>
    public RampTimeConsideration RampTimeConsideration { get; init; }

    /// <summary>
    /// Represents the strain behavior over the time.
    /// </summary>
    public MechanicalParameter? Strain { get; init; }

    /// <summary>
    /// Represents the displacement behavior over the time.
    /// </summary>
    public MechanicalParameter? Displacement { get; init; }

    /// <summary>
    /// Represents the stress behavior over the time.
    /// </summary>
    public MechanicalParameter? Stress { get; init; }

    /// <summary>
    /// Represents the force behavior over the time.
    /// </summary>
    public MechanicalParameter? Force { get; init; }

    #endregion

    #region Specimen properties

    /// <inheritdoc cref="Models.SpecimenParameter"/>
    public SpecimenParameter? Specimen { get; init; }

    #endregion

    #region Time parameters

    /// <summary>
    /// Unit: s (second).
    /// </summary>
    public double TimeStep { get; init; }

    #endregion
}
