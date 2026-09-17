using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;

public interface ICurveFitter
{
    CurveFitOutput Fit(CurveFitInput input);
}
