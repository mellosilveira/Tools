using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;

/// <summary>
/// Represents the data contract for a range of unique dimension mathematical function, f(x).
/// </summary>
/// <param name="InitialVariableValue">Initial value for variable.</param>
/// <param name="FinalVariableValue">Final value for variable.</param>
/// <param name="Types">List of <see cref="FunctionType"/>.</param>
/// <param name="Coefficients">List of <see cref="RangeParameters"/> for the scaling or proportionality factor of the variable in the expression.</param>
public record RangeFunction(double? InitialVariableValue, double? FinalVariableValue, List<FunctionType> Types, List<RangeParameters> Coefficients);
