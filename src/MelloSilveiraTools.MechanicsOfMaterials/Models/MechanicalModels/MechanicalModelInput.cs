using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Represents the comprehensive input dataset required to execute a mechanical model simulation.
/// </summary>
/// <typeparam name="TConstitutiveParameters">The specific type of constitutive parameters governing the model.</typeparam>
public record MechanicalModelInput<TConstitutiveParameters> where TConstitutiveParameters : ConstitutiveParameters
{
    /// <summary>
    /// Unique identifier for the mechanical model in the current analysis.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// The descriptive name or classification of the mechanical model.
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <inheritdoc cref="MechanicalModels.ConstitutiveParameters"/>
    public required TConstitutiveParameters ConstitutiveParameters { get; init; }

    /// <inheritdoc cref="AcceptedRange"/>
    public AcceptedRange AcceptedStrainRange { get; init; } = AcceptedRange.Default;

    #region Mechanical parameters

    /// <inheritdoc cref="MechanicalModels.MechanicalBehaviorType"/>
    public MechanicalBehaviorType MechanicalBehaviorType { get; init; }

    /// <inheritdoc cref="Viscoelasticity.ViscoelasticEffect"/>
    public ViscoelasticEffect ViscoelasticEffect { get; init; }

    /// <inheritdoc cref="Viscoelasticity.RampTimeConsideration"/>
    public RampTimeConsideration RampTimeConsideration { get; init; }

    /// <summary>
    /// Represents the strain applied or measured over time.
    /// </summary>
    public MechanicalParameter? Strain { get; init; }

    /// <summary>
    /// Represents the displacement applied or measured over time.
    /// </summary>
    public MechanicalParameter? Displacement { get; init; }

    /// <summary>
    /// Represents the stress applied or measured over time.
    /// </summary>
    public MechanicalParameter? Stress { get; init; }

    /// <summary>
    /// Represents the force applied or measured over time.
    /// </summary>
    public MechanicalParameter? Force { get; init; }

    #endregion

    #region Specimen properties

    /// <inheritdoc cref="Models.SpecimenParameter"/>
    public SpecimenParameter? Specimen { get; init; }

    #endregion

    #region Time parameters

    /// <summary>
    /// The time increment used for numerical integration or step-by-step analysis.
    /// </summary>
    /// <value>Unit: s (second).</value>
    public double TimeStep { get; init; }

    #endregion
}

/// <summary>
/// Represents the generic input dataset required to execute a mechanical model, decoupled from a specific constitutive formulation.
/// </summary>
public sealed record MechanicalModelInput : MechanicalModelInput<ConstitutiveParameters>;