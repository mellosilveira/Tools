using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;

/// <summary>
/// Orchestrates the One-At-A-Time (OAAT) Parametric Sweep sensitivity analysis pipeline.
/// </summary>
public interface IParametricSweepPipeline : IPipeline<ParametricSweepInput, string>;
