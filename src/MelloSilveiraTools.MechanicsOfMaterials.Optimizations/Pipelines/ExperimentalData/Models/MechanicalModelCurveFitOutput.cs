using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Output for mechanical model curve fitting steps containing optimized constitutive parameters and fit quality metrics.
/// </summary>
/// <param name="CurveSegments">The experimental curve segments associated with this fit.</param>
/// <param name="MechanicalModelName">The name of the mechanical model evaluated.</param>
/// <param name="ConstitutiveParameters">The optimized material constitutive parameters.</param>
/// <param name="AcceptedRange">The strain range within which the parameters are valid.</param>
/// <param name="MechanicalBehaviorType">The physical behavior classification (e.g. StressStrain).</param>
/// <param name="ViscoelasticEffect">The active viscoelastic regime (e.g. Relaxation or Creep).</param>
/// <param name="RampTimeConsideration">Strategy used for handling finite loading ramp times.</param>
/// <param name="RSquared">The coefficient of determination (R²) of the fit.</param>
/// <param name="FinalError">The sum of squared residuals (SSR) achieved by the optimization routine.</param>
/// <param name="Iterations">The total number of optimization iterations executed.</param>
public sealed record MechanicalModelCurveFitOutput(
    IReadOnlyList<CurveSegment> CurveSegments,
    string MechanicalModelName,
    ConstitutiveParameters ConstitutiveParameters,
    AcceptedRange AcceptedRange,
    MechanicalBehaviorType MechanicalBehaviorType,
    ViscoelasticEffect ViscoelasticEffect,
    RampTimeConsideration RampTimeConsideration,
    double RSquared,
    double FinalError,
    int Iterations);
