using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public interface IMathematicalFunctionCurveFitter
{
    SafeResult<CurveFitInput, CurveFitOutput> TryFit(int numberOfParameters, double[] independentVariable, double[] dependentVariable, bool zeroBased = false, double tolerance = CurveFittingConstants.Tolerance, int maxIterations = CurveFittingConstants.MaxIterations);
}