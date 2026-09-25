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
    int Iterations)
{
    public string Identifier { get; init; } = Guid.NewGuid().ToString("N");
    public double? RampTime { get; init; }
    public double[] ExperimentalStress { get; init; } = [];
    public double TimeStep { get; init; } = 0.01;
    public double[] TimePoints { get; init; } = [];
    public MechanicalModelSimulationOutput? Simulation { get; init; }
}
