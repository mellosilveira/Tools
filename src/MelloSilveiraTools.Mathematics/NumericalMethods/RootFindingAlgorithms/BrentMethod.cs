using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <inheritdoc cref="RootFindingAlgorithm.BrentMethod"/>
/// <param name="logger">Optional logger for telemetry and failure reporting.</param>
public class BrentMethod(ILogger<BrentMethod>? logger = null) : RootFinding(logger)
{
    /// <inheritdoc/>
    public override (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function)
    {
        throw new NotImplementedException();
    }
}
