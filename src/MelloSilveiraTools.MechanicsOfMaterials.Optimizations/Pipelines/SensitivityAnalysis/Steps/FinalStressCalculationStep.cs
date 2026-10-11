using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// A pure domain-specific calculation step that evaluates the continuum stress 
/// at the final integration time.
/// </summary>
public sealed class FinalStressCalculationStep(IMechanicalModelCalculatorFacade calculator, double finalTime) : ISyncPipelineStep<GenericMechanicalModelInput, double>
{
    /// <inheritdoc />
    public string Name => nameof(FinalStressCalculationStep);

    /// <inheritdoc />
    public double Execute(GenericMechanicalModelInput input) => calculator.CalculateStress(input, finalTime);

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
