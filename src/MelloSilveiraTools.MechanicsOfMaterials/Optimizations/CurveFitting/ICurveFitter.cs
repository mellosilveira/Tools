using MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting;

public interface ICurveFitter
{
    CurveFitResult Fit(CurveFitInput input);
}
