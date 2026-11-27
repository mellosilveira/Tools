using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;

/// <summary>
/// Unified request payload to execute a single deterministic specimen mechanical analysis
/// for any statically registered or dynamically loaded plugin constitutive model.
/// </summary>
public sealed record ExecuteSpecimenAnalysisRequest
{
    /// <summary>
    /// The registered mechanical model name (e.g. <c>"Elastic"</c>, <c>"Maxwell"</c>, <c>"Fung"</c>,
    /// <c>"SimplifiedFung"</c>, <c>"Schapery"</c>, <c>"ModifiedSuperpositionMethod"</c>, or a loaded plugin model name).
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Optional correlational identifier for this execution; a GUID is generated when omitted.
    /// </summary>
    public string? AnalysisIdentifier { get; init; }

    /// <summary>
    /// Primary control variable and initial dependent variable defining the boundary value problem.
    /// </summary>
    public required AnalysisParameter AnalysisParameter { get; init; }

    /// <summary>
    /// Optional reference geometry and initial kinematic state of the specimen (required for <see cref="LoadResponseRelationship.ForceDisplacement"/>).
    /// </summary>
    public SpecimenRequest? Specimen { get; init; }

    /// <summary>
    /// Optional validated strain bounds enforcing physical sanity checks.
    /// </summary>
    public AcceptedRange? AcceptedRange { get; init; }

    /// <summary>
    /// Defines the load-response relationship (<see cref="LoadResponseRelationship.StressStrain"/> or <see cref="LoadResponseRelationship.ForceDisplacement"/>).
    /// </summary>
    public LoadResponseRelationship LoadResponseRelationship { get; init; }

    /// <summary>
    /// Specifies the viscoelastic phenomenon to evaluate (<see cref="ViscoelasticEffect.Relaxation"/> or <see cref="ViscoelasticEffect.Creep"/>).
    /// </summary>
    public ViscoelasticEffect ViscoelasticEffect { get; init; }

    /// <summary>
    /// Defines whether the initial loading phase is modeled as an instantaneous step or a finite-time linear ramp.
    /// </summary>
    public RampTimeConsideration RampTimeConsideration { get; init; }

    /// <summary>
    /// Optional ramp duration in seconds (s) when using finite-time ramp loading.
    /// </summary>
    public double? RampTime { get; init; }

    /// <summary>
    /// Starting temporal boundary (t₀) in seconds (s); defaults to <c>0</c>.
    /// </summary>
    public double? InitialTime { get; init; }

    /// <summary>
    /// Discrete temporal integration increment (Δt) in seconds (s).
    /// </summary>
    public double TimeStep { get; init; }

    /// <summary>
    /// Terminating temporal boundary (tf) in seconds (s).
    /// </summary>
    public double FinalTime { get; init; }

    /// <summary>
    /// Consecutive identical-output step count required to flag asymptotic convergence (defaults to <c>10</c>).
    /// </summary>
    public int AsymptoticConvergenceThreshold { get; init; } = 10;

    /// <summary>
    /// JSON object containing the constitutive parameters specific to <see cref="MechanicalModelName"/>.
    /// Deserialized dynamically using the registered <c>IMechanicalModelTypeResolver.ConstitutiveParameters</c> type.
    /// </summary>
    public required JsonElement ConstitutiveParameters { get; init; }
}
