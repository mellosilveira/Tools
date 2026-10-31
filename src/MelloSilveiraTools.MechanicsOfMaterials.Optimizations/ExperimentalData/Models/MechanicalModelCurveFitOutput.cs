using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

/// <summary>
/// Output for mechanical model curve fitting steps containing optimized constitutive parameters and fit quality metrics.
/// </summary>
/// <param name="CurveSegments">The experimental curve segments associated with this fit (ignored during JSON serialization to prevent persisting massive raw point arrays in database payloads).</param>
/// <param name="ConstitutiveParameters">The optimized material constitutive parameters.</param>
/// <param name="AcceptedRange">The strain range within which the parameters are valid.</param>
/// <param name="LoadResponseRelationship">The physical load-response relationship (e.g. StressStrain).</param>
/// <param name="ViscoelasticEffect">The active viscoelastic regime (e.g. Relaxation or Creep).</param>
/// <param name="RampTimeConsideration">Strategy used for handling finite loading ramp times.</param>
/// <param name="RSquared">The coefficient of determination (R²) of the fit.</param>
/// <param name="FinalError">The sum of squared residuals (SSR) achieved by the optimization routine.</param>
/// <param name="Iterations">The total number of optimization iterations executed.</param>
public sealed record MechanicalModelCurveFitOutput(
    [property: JsonIgnore] IReadOnlyList<CurveSegment> CurveSegments,
    ConstitutiveParameters ConstitutiveParameters,
    AcceptedRange AcceptedRange,
    LoadResponseRelationship LoadResponseRelationship,
    ViscoelasticEffect ViscoelasticEffect,
    RampTimeConsideration RampTimeConsideration,
    double RSquared,
    double FinalError,
    int Iterations);
