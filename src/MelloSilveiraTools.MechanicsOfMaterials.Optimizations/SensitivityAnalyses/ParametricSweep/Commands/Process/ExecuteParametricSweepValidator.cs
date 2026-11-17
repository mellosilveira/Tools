using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;

/// <summary>
/// Validates <see cref="ExecuteParametricSweepPipelineRequest"/> payloads prior to parametric sweep execution.
/// </summary>
public sealed class ExecuteParametricSweepValidator : ExecuteParametricSweepValidatorBase<ExecuteParametricSweepPipelineRequest>
{
    /// <inheritdoc/>
    protected override void ValidateComplementary(ExecuteParametricSweepPipelineRequest value, List<string> errors)
    {
        errors.FluentAddIf(value.Baseline is null, "Baseline constitutive parameters are required.");
    }
}
