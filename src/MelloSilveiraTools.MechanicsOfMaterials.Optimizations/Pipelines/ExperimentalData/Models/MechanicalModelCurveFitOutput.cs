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
    double FinalError,
    int Iterations,
    string Identifier = "",
    double[]? TimePoints = null,
    double[]? ExperimentalStrain = null,
    double[]? ExperimentalStress = null,
    double TimeStep = 0,
    double? RampTime = null,
    MechanicalModelSimulationOutput? Simulation = null);
