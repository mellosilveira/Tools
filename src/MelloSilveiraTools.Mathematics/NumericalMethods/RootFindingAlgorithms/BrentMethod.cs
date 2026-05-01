using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <inheritdoc cref="RootFindingAlgorithm.BrentMethod"/>
public class BrentMethod : RootFinding
{
    /// <inheritdoc/>
    public override (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function)
    {
        throw new NotImplementedException();
    }
}
