using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands;

/// <summary>
/// Request payload for retrying or resuming an experimental data analysis from a specific pipeline step.
/// </summary>
public record ResumeExperimentalDataStepRequest : ResumeStepRequest;
