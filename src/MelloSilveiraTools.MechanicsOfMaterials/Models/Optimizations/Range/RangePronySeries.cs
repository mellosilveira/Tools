namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations.Range;

/// <summary>
/// Represents the data contract for a prony series.
/// </summary>
/// <param name="InitialVariableValue">Initial value for variable.</param>
/// <param name="FinalVariableValue">Final value for variable.</param>
/// <param name="IndependentParameter">Independent parameter represented by c.</param>
/// <param name="Coefficients">List of <see cref="RangeParameters"/> for iterator coefficients represented by a_n.</param>
public record RangePronySeries(
    double? InitialVariableValue,
    double? FinalVariableValue,
    RangeParameters IndependentParameter,
    List<RangeParameters> Coefficients);
// TODO: checar se é possível herdar de RangeFunction.
//: RangeFunction(InitialVariableValue, FinalVariableValue, [FunctionType.PronySeries], [IndependentParameter, ..IteratorCoefficients]);
