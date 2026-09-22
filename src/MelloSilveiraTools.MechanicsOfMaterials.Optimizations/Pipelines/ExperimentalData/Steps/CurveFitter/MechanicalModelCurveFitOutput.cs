using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Output for mechanical model curve fitting steps.
/// </summary>
public record MechanicalModelCurveFitOutput(
    ConstitutiveParameters ConstitutiveParameters,
    double FinalError,
    int Iterations,
    AcceptedRange AcceptedStrainRange);
