namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

/// <summary>
/// Represents the result data of a curve fitting operation.
/// </summary>
/// <param name="FinalError">The final optimization error (sum of squared residuals).</param>
/// <param name="ParameterGroups">The optimized parameter groups with their validity ranges and events.</param>
public record FitCurveResultData(double FinalError, ParameterGroupResultData[] ParameterGroups);
