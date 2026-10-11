using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// A pure domain-specific calculation step that evaluates the continuum stress 
/// at the initial state (typically the end of the loading ramp).
/// </summary>
public sealed class InitialStressCalculationStep(IMechanicalModelCalculatorFacade calculator, double initialTime) : ISyncPipelineStep<GenericMechanicalModelInput, double>
{
    /// <inheritdoc />
    public string Name => nameof(InitialStressCalculationStep);

    /// <inheritdoc />
    public double Execute(GenericMechanicalModelInput input) => calculator.CalculateStress(input, initialTime);

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
