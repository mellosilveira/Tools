using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// Step-by-step sequential search algorithm that samples an interval to detect the best matching equilibrium point.
/// Increments through the domain systematically, identifying where the target error threshold is minimized.
/// </summary>
public class StepByStepMethod : RootFinding
{
    /// <inheritdoc/>
    public override (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function)
    {
        double? previousPoint = null;
        double previousValue = 0;

        double initialPoint = input.InitialPoint;
        double finalPoint = input.FinalPoint;
        double step = (finalPoint - initialPoint) / input.MaxIterations;

        double value = 0;
        int i = 0;
        double point = initialPoint;

        // Integer counter avoids floating-point accumulation drift.
        // Loop condition includes a half-step margin so the final point is always reached despite
        // floating-point representation (e.g. 1199.9999 < 1200 would otherwise skip the last step).
        while (point <= finalPoint + step * 0.5)
        {
            value = function(point);
            if (value < input.Tolerance)
            {
                if (previousPoint != null && value >= previousValue)
                {
                    return (previousPoint.Value, previousValue);
                }

                previousValue = value;
                previousPoint = point;
            }

            i++;
            point = initialPoint + i * step;
        }

        if (previousPoint != null)
        {
            return (previousPoint.Value, previousValue);
        }

        throw GetNonConvergenceException(point, value);
    }
}