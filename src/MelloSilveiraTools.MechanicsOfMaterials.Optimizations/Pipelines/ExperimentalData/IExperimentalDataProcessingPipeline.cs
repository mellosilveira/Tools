using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Handles the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
public interface IExperimentalDataProcessingPipeline : IPipeline<ExperimentalDataProcessingInput, string>;
