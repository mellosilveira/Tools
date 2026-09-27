using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Input payload for the <see cref="Steps.SimulationOrchestratorStep"/>, combining curve fitting results with numerical solver inputs.
/// </summary>
/// <param name="CurveFitIdentifier">Unique identifier associated with the curve fitting entity.</param>
/// <param name="CurveFit">The fitted mechanical model parameters and metadata.</param>
/// <param name="MechanicalModelInput">Prepared generic model input containing strain/stress functions and material properties.</param>
/// <param name="ExperimentalTimePoints">Ordered time points at which the forward numerical simulation should evaluate the model.</param>
public record MechanicalModelSimulationStepInput(
    string CurveFitIdentifier,
    MechanicalModelCurveFitOutput CurveFit,
    GenericMechanicalModelInput MechanicalModelInput,
    double[] ExperimentalTimePoints
);
