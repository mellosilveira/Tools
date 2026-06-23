using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Defines the macroscopic boundary value problem and physical payload required to execute 
/// a numerical continuum mechanics simulation.
/// </summary>
/// <remarks>
/// This base record acts as the foundational structural blueprint, aggregating kinematic histories, 
/// kinetic boundaries, and the temporal domain prior to the injection of specific material constitutive laws.
/// </remarks>
public record MechanicalModelInput
{
    public MechanicalModelInput() { }

    public MechanicalModelInput(MechanicalModelInput original)
    {
        Identifier = original.Identifier;
        MechanicalModelName = original.MechanicalModelName;
        AcceptedStrainRange = original.AcceptedStrainRange;
        MechanicalBehaviorType = original.MechanicalBehaviorType;
        ViscoelasticEffect = original.ViscoelasticEffect;
        RampTimeConsideration = original.RampTimeConsideration;
        Strain = original.Strain;
        Displacement = original.Displacement;
        Stress = original.Stress;
        Force = original.Force;
        Specimen = original.Specimen;
        TimeStep = original.TimeStep;
    }

    /// <summary>
    /// An optional correlational identifier used to track this specific boundary value problem 
    /// configuration within batch executions or experimental logs.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// The unique registry string defining the phenomenological or structurally based formulation 
    /// (e.g., 'Fung', 'Maxwell', 'Schapery') that the solver factory must instantiate.
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// The validated operational limits enforcing thermodynamic or structural sanity checks 
    /// (e.g., preventing compression in tension-only cable elements).
    /// </summary>
    public AcceptedRange AcceptedStrainRange { get; init; } = AcceptedRange.Default;

    #region Mechanical parameters

    /// <summary>
    /// Defines the overarching phenomenological continuum framework governing the stress-strain relationship 
    /// (e.g., Elastic, Hyperelastic, Damage mechanics) targeted by the solver.
    /// </summary>
    public MechanicalBehaviorType MechanicalBehaviorType { get; init; }

    /// <summary>
    /// Specifies the temporal dependency of the material's response, determining the specific viscoelastic 
    /// phenomena to be integrated (e.g., Creep, Stress Relaxation, Constant Strain Rate).
    /// </summary>
    public ViscoelasticEffect ViscoelasticEffect { get; init; }

    /// <summary>
    /// Defines the integration strategy for the initial loading phase, determining whether the applied boundary 
    /// conditions are evaluated as an instantaneous Heaviside step or a finite-time linear ramp.
    /// </summary>
    public RampTimeConsideration RampTimeConsideration { get; init; }

    /// <summary>
    /// The intensive kinematic boundary condition history applied to the continuum domain.
    /// </summary>
    public MechanicalParameter? Strain { get; init; }

    /// <summary>
    /// The extensive macroscopic kinematic boundary condition history applied to the geometric boundaries.
    /// </summary>
    public MechanicalParameter? Displacement { get; init; }

    /// <summary>
    /// The intensive macroscopic kinetic boundary condition (traction) history applied to the continuum domain.
    /// </summary>
    public MechanicalParameter? Stress { get; init; }

    /// <summary>
    /// The extensive macroscopic kinetic boundary condition history applied to the geometric boundaries.
    /// </summary>
    public MechanicalParameter? Force { get; init; }

    #endregion

    #region Specimen properties

    /// <summary>
    /// The reference geometric and spatial initial state of the analyzed continuum body.
    /// </summary>
    public SpecimenParameter? Specimen { get; init; }

    #endregion

    #region Time parameters

    /// <summary>
    /// The discrete temporal integration increment utilized by the time-marching numerical solver.
    /// </summary>
    /// <value>Unit: seconds (s).</value>
    public double TimeStep { get; init; }

    #endregion
}

/// <summary>
/// Enforces a strict, type-safe coupling between the macroscopic boundary conditions and a specifically defined material constitutive law.
/// </summary>
/// <typeparam name="TConstitutiveParameters">The strictly typed constitutive parameters defining the material's physical response.</typeparam>
public record MechanicalModelInput<TConstitutiveParameters> : MechanicalModelInput
    where TConstitutiveParameters : ConstitutiveParameters
{
    public MechanicalModelInput() { }

    public MechanicalModelInput(MechanicalModelInput input, TConstitutiveParameters constitutiveParameters) : base(input) => ConstitutiveParameters = constitutiveParameters;

    /// <summary>
    /// The explicitly defined material constants, scalar moduli, and phenomenological tensors 
    /// governing the mathematical formulation assigned to this simulation.
    /// </summary>
    public required TConstitutiveParameters ConstitutiveParameters { get; init; }
}

/// <summary>
/// Provides a non-generic bridging construct, allowing polymorphically diverse boundary value problems 
/// to be routed through generalized processing pipelines prior to type specialization.
/// </summary>
public sealed record GenericMechanicalModelInput : MechanicalModelInput<ConstitutiveParameters>
{
    public GenericMechanicalModelInput() { }

    public GenericMechanicalModelInput(MechanicalModelInput input, ConstitutiveParameters constitutiveParameters) : base(input, constitutiveParameters) { }
}