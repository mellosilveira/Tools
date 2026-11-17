using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;

/// <summary>
/// Validates <see cref="ExecuteParametricSweepFromExperimentalDataRequest"/> payloads prior to parametric sweep execution.
/// </summary>
public sealed class ExecuteParametricSweepFromExperimentalDataValidator : ExecuteParametricSweepValidatorBase<ExecuteParametricSweepFromExperimentalDataRequest>
{
    /// <inheritdoc/>
    protected override void ValidateComplementary(ExecuteParametricSweepFromExperimentalDataRequest value, List<string> errors)
    {
        errors
            .FluentAddIf(string.IsNullOrWhiteSpace(value.ExperimentalDataIdentifier), "ExperimentalDataIdentifier is required.")
            .FluentAddIf(!Enum.IsDefined(value.ExecutionMode), "A valid ExecutionMode must be specified.")
            .FluentAddIf(value.MaxDegreeOfParallelism is <= 0, "MaxDegreeOfParallelism must be greater than zero when specified.");
    }
}
