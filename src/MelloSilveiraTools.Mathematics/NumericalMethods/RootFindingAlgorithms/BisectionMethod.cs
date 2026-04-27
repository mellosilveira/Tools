using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <inheritdoc cref="RootFindingAlgorithm.BisectionMethod"/>
public class BisectionMethod : RootFinding
{
    /// <inheritdoc/>
    public override (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function)
    {
        var initialPoint = input.InitialPoint;
        var finalPoint = input.FinalPoint;

        var initialValue = function(input.InitialPoint);
        var finalValue = function(input.FinalPoint);

        if (initialValue * finalValue >= 0)
            throw new InvalidOperationException("The Bisection Method requires the function to have opposite signs at the initial and final points of the interval.");

        double middlePoint = 0;
        double middleValue = 0;
        for (var i = 0; i < input.MaxIterations; i++)
        {
            middlePoint = CustomMath.Average(initialPoint, finalPoint);
            middleValue = function(middlePoint);

            if (Math.Abs(middleValue) < input.Tolerance || Math.Abs(finalPoint - initialPoint) / 2 < input.Tolerance)
                return (middlePoint, middleValue);

            if (initialValue * middleValue < 0)
            {
                finalPoint = middlePoint;
            }
            else
            {
                initialPoint = middlePoint;
                initialValue = middleValue;
            }
        }

        throw GetNonConvergenceException(middlePoint, middleValue);
    }
}
