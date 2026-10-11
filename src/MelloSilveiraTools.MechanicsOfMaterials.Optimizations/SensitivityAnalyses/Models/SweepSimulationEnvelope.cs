using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

/// <summary>
/// Envelope used to propagate contextual metadata through the TPL Dataflow pipeline.
/// </summary>
public record SweepSimulationEnvelope(
    string VariableName,
    double VariableValue,
    GenericMechanicalModelInput SimulationInput);
