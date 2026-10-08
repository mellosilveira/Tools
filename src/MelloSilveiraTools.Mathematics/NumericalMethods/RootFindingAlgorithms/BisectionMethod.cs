using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// Bisection search algorithm for finding equilibrium points within an interval of opposite signs.
/// Halves the search range iteratively to guarantee convergence towards the exact target condition.
/// </summary>
public class BisectionMethod : RootFinding
{
    /// <inheritdoc/>
    public override (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function)
    {
        double initialPoint = input.InitialPoint;
        double finalPoint = input.FinalPoint;

        double initialValue = function(input.InitialPoint);
        double finalValue = function(input.FinalPoint);

        if (initialValue * finalValue >= 0)
        {
            throw new InvalidOperationException("The Bisection Method requires the function to have opposite signs at the initial and final points of the interval.");
        }

        double middlePoint = 0;
        double middleValue = 0;
        for (int i = 0; i < input.MaxIterations; i++)
        {
            middlePoint = CustomMath.Average(initialPoint, finalPoint);
            middleValue = function(middlePoint);

            if (Math.Abs(middleValue) < input.Tolerance || Math.Abs(finalPoint - initialPoint) / 2 < input.Tolerance)
            {
                return (middlePoint, middleValue);
            }

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
