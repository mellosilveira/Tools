using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

public interface ICurveFitter
{
    CurveFitOutput Fit(CurveFitInput input);
}
