using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

public class FitCurve : CommandBaseWithData<FitCurveRequest, FitCurveResultData>
{
    protected override Task<Result<FitCurveResultData>> ExecuteCommandAsync(FitCurveRequest request)
    {
        throw new NotImplementedException();
    }
}
