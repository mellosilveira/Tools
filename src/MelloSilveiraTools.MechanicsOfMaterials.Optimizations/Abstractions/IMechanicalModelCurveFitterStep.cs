using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Abstractions;

/// <summary>
/// Represents a pipeline step that fits constitutive model parameters from the full
/// set of <see cref="CurveSegment"/> objects collected by the pipeline.
/// Receives all accumulated segments as a single array after the grouping/accumulation step
/// and returns the fitted <see cref="ConstitutiveParameters"/> for the entire experiment.
/// Inherits <see cref="IPipelineStep"/> for telemetry and tracing.
/// </summary>
public interface IMechanicalModelCurveFitterStep : ISyncPipelineStep<CurveSegment[], ConstitutiveParameters[]>;