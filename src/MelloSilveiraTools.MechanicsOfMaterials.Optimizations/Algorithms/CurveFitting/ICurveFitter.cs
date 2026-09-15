using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;

public interface ICurveFitter<TConstitutiveParameters> 
    where TConstitutiveParameters : ConstitutiveParameters
{
    Result<CurveFitResultData<TConstitutiveParameters>> Fit(CurveFitInput<TConstitutiveParameters> input);
}
