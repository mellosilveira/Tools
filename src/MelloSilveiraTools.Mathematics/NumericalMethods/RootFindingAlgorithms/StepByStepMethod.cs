using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// Step-by-step sequential search algorithm that samples an interval to detect the best matching equilibrium point.
/// Increments through the domain systematically, identifying where the target error threshold is minimized.
/// </summary>
/// <param name="logger">Optional logger for telemetry and failure reporting.</param>
public class StepByStepMethod(ILogger<StepByStepMethod>? logger = null) : RootFinding(logger)
{
    /// <inheritdoc/>
    public override (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function)
    {
        double? previousPoint = null;
        double previousValue = 0;
        double previousAbsValue = double.PositiveInfinity;

        double initialPoint = input.InitialPoint;
        double finalPoint = input.FinalPoint;
        double step = (finalPoint - initialPoint) / input.MaxIterations;

        double value = 0;
        double point = initialPoint;

        // Bounded integer iteration prevents infinite loops when initialPoint == finalPoint (step == 0).
        for (int i = 0; i <= input.MaxIterations; i++)
        {
            point = initialPoint + i * step;
            value = function(point);
            double absValue = Math.Abs(value);

            if (absValue < input.Tolerance)
            {
                if (previousPoint != null && absValue >= previousAbsValue)
                    return (previousPoint.Value, previousValue);

                previousValue = value;
                previousAbsValue = absValue;
                previousPoint = point;
            }
        }

        return previousPoint != null
            ? (previousPoint.Value, previousValue)
            : throw GetNonConvergenceException(point, value);
    }
}
