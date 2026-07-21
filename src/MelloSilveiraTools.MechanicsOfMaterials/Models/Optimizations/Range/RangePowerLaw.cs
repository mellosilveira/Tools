namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations.Range;

/// <summary>
/// Represents the data contract for a power law function.
/// </summary>
/// <param name="InitialVariableValue">Initial value for variable.</param>
/// <param name="FinalVariableValue">Final value for variable.</param>
/// <param name="Coefficients">List of <see cref="RangeParameters"/> for coefficients used to calculate the power law function.</param>
public record RangePowerLaw(double? InitialVariableValue, double? FinalVariableValue, List<RangeParameters> Coefficients);