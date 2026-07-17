using SoftTissue.Domain.Models.Optimization;

namespace SoftTissue.Domain.Optimizations.CurveFitting;

public interface ICurveFitter
{
    CurveFitResult Fit(CurveFitInput input);
}
