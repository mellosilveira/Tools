using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// A pure domain-specific calculation step that evaluates the theoretical limit 
/// (asymptote) of the continuum stress as time approaches infinity.
/// </summary>
public sealed class AsymptoteCalculationStep(IMechanicalModelCalculatorFacade calculator) : ISyncPipelineStep<GenericMechanicalModelInput, (double AsymptoteTime, double Stress)>
{
    /// <inheritdoc />
    public string Name => nameof(AsymptoteCalculationStep);

    /// <inheritdoc />
    public (double AsymptoteTime, double Stress) Execute(GenericMechanicalModelInput input)
    {
        // For a true mathematical asymptote, we approximate by evaluating at a sufficiently large time.
        // A robust simulation could analyze the constitutive constants (e.g., maximum relaxation time * factor).
        // Since the interface facade calculates stress at a given time, we use a placeholder large time.
        // In a real scenario, this might call a specific limit evaluation method if the facade supports it.
        double asymptoteTime = 1e8;
        double stress = calculator.CalculateStress(input, asymptoteTime);

        return (asymptoteTime, stress);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
