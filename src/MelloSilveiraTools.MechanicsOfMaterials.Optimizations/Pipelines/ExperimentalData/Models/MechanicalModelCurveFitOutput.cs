using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Output for mechanical model curve fitting steps.
/// </summary>
public record MechanicalModelCurveFitOutput(
    string MechanicalModelName,
    ConstitutiveParameters ConstitutiveParameters,
    AcceptedRange AcceptedRange,
    MechanicalBehaviorType MechanicalBehaviorType,
    ViscoelasticEffect ViscoelasticEffect,
    RampTimeConsideration RampTimeConsideration,
    double Precision,
    double FinalError,
    int Iterations);
