using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public interface IFunctionCurveFitter
{
    SafeResult<CurveFitInput, CurveFitOutput> TryFit(MathematicalCurveFitInput input);
}