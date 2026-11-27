using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Steps.Generators;

/// <summary>
/// Core contract for specific constitutive mechanical model sweep generators, serving as a pipeline step.
/// Inheritors must implement the One-At-A-Time (OAAT) variation logic specific to their parameter type.
/// </summary>
public interface IParametricSweepGeneratorStep : IEnumerablePipelineStep<ParametricSweepPipelineInput, (string VariableName, RangeParameters VariableRange, IEnumerable<ParametricSweepInput> Inputs)>;