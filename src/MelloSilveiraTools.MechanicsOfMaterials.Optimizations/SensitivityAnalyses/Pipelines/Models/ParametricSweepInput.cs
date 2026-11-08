using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

/// <summary>
/// Envelope used to propagate contextual metadata through the TPL Dataflow pipeline.
/// </summary>
public record ParametricSweepInput(
    string VariableName,
    double VariableValue,
    GenericMechanicalModelInput SimulationInput);
