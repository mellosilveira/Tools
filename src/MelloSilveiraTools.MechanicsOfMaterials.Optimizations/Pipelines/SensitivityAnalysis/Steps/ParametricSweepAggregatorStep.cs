using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// Aggregates the natively joined results from the 3-way Fork back into a unified metric structure.
/// </summary>
public sealed class ParametricSweepAggregatorStep : ISyncPipelineStep<
    Tuple<(string VariableName, double VariableValue, double InitialStress), double, (double AsymptoteTime, double AsymptoteStress)>,
    (string VariableName, double VariableValue, SimulationMetrics Metrics)>
{
    /// <inheritdoc />
    public string Name => nameof(ParametricSweepAggregatorStep);

    /// <inheritdoc />
    public (string VariableName, double VariableValue, SimulationMetrics Metrics) Execute(
        Tuple<(string VariableName, double VariableValue, double InitialStress), double, (double AsymptoteTime, double AsymptoteStress)> input)
    {
        string varName = input.Item1.VariableName;
        double varValue = input.Item1.VariableValue;
        
        SimulationMetrics metrics = new(
            InitialStress: input.Item1.InitialStress,
            FinalStress: input.Item2,
            AsymptoteTime: input.Item3.AsymptoteTime,
            AsymptoteStress: input.Item3.AsymptoteStress
        );

        return (varName, varValue, metrics);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
