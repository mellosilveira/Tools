using MelloSilveiraTools.Mathematics.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// Van Wijngaarden-Dekker-Brent root-finding algorithm combining bisection, secant, and inverse quadratic interpolation.
/// Guarantees superlinear convergence for continuous functions within an interval that changes sign.
/// </summary>
/// <remarks>
/// <para>
/// <b>Algorithm Summary:</b>
/// Brent's method maintains three points in each iteration:
/// <list type="bullet">
/// <item><description><c>currentEstimate</c> (<c>b</c>): the best root approximation so far (lowest absolute value <c>|f(b)|</c>).</description></item>
/// <item><description><c>previousEstimate</c> (<c>a</c>): the estimate from the previous iteration.</description></item>
/// <item><description><c>contrapoint</c> (<c>c</c>): the opposite bracket endpoint, ensuring that <c>f(b)</c> and <c>f(c)</c> have opposite signs.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Formulations and Iteration Steps:</b>
/// </para>
/// <list type="number">
/// <item>
/// <term>Stopping Criteria:</term>
/// <description>
/// In each step, the half-bracket width is <c>halfBracket = (c - b) / 2</c>, and the effective tolerance is:
/// <code>effectiveTolerance = 2 * MachineEpsilon * |b| + tolerance / 2</code>
/// The search stops when <c>|halfBracket| &lt;= effectiveTolerance</c>, when <c>f(b) == 0</c>, or when <c>|f(b)| &lt; tolerance</c>.
/// </description>
/// </item>
/// <item>
/// <term>Step Calculation (Secant vs. Inverse Quadratic Interpolation):</term>
/// <description>
/// <para>
/// When only two distinct points exist (<c>a == c</c>), the Secant method calculates:
/// <code>
/// s = f(b) / f(a)
/// p = 2 * halfBracket * s
/// q = 1 - s
/// step = p / q
/// </code>
/// </para>
/// <para>
/// When three distinct points exist (<c>a != c</c>), Inverse Quadratic Interpolation (IQI) calculates:
/// <code>
/// s = f(b) / f(a)
/// q0 = f(a) / f(c)
/// r = f(b) / f(c)
/// p = s * (2 * halfBracket * q0 * (q0 - r) - (b - a) * (r - 1))
/// q = (q0 - 1) * (r - 1) * (s - 1)
/// step = p / q
/// </code>
/// </para>
/// </description>
/// </item>
/// <item>
/// <term>Interpolation Acceptance and Bisection Fallback:</term>
/// <description>
/// The interpolated step (<c>p / q</c>) is accepted only if it lands inside the bracket and contracts faster than bisection:
/// <code>
/// interpolationBound = 3 * halfBracket * q - |effectiveTolerance * q|
/// previousStepBound = |previousStepSize * q|
/// 
/// if (2 * |p| &lt; Math.Min(interpolationBound, previousStepBound))
///     step = p / q;
/// else
///     step = halfBracket; // fallback to bisection
/// </code>
/// </description>
/// </item>
/// <item>
/// <term>Estimate Update:</term>
/// <description>
/// The current estimate <c>b</c> is updated by adding <c>step</c>. If <c>|step| &lt;= effectiveTolerance</c>, it takes a minimal step of <c>effectiveTolerance</c> in the direction of <c>halfBracket</c> to prevent stagnation.
/// </description>
/// </item>
/// </list>
/// </remarks>
/// <param name="logger">Optional logger for telemetry and failure reporting.</param>
public class BrentMethod(ILogger<BrentMethod> logger) : RootFinding(logger)
{
    private const double MachineEpsilon = MathematicConstants.MachineEpsilonFloor * 2.0;

    /// <inheritdoc/>
    public override (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function)
    {
        double previousEstimate = input.InitialPoint;
        double currentEstimate = input.FinalPoint;
        double previousValue = function(previousEstimate);
        if (previousValue == 0.0)
            return (previousEstimate, previousValue);

        double currentValue = function(currentEstimate);
        if (currentValue == 0.0)
            return (currentEstimate, currentValue);

        if ((previousValue > 0.0 && currentValue > 0.0) || (previousValue < 0.0 && currentValue < 0.0))
            throw new InvalidOperationException("The Brent Method requires the function to have opposite signs at the initial and final points of the interval.");

        double contrapoint = currentEstimate;
        double contrapointValue = currentValue;
        double stepSize = currentEstimate - previousEstimate;
        double previousStepSize = stepSize;

        for (int iteration = 0; iteration < input.MaxIterations; iteration++)
        {
            if ((currentValue > 0.0 && contrapointValue > 0.0) || (currentValue < 0.0 && contrapointValue < 0.0))
            {
                contrapoint = previousEstimate;
                contrapointValue = previousValue;
                stepSize = currentEstimate - previousEstimate;
                previousStepSize = stepSize;
            }

            if (Math.Abs(contrapointValue) < Math.Abs(currentValue))
            {
                previousEstimate = currentEstimate;
                currentEstimate = contrapoint;
                contrapoint = previousEstimate;
                previousValue = currentValue;
                currentValue = contrapointValue;
                contrapointValue = previousValue;
            }

            double effectiveTolerance = 2.0 * MachineEpsilon * Math.Abs(currentEstimate) + 0.5 * input.Tolerance;
            double halfBracket = 0.5 * (contrapoint - currentEstimate);

            if (Math.Abs(halfBracket) <= effectiveTolerance || currentValue == 0.0 || Math.Abs(currentValue) < input.Tolerance)
                return (currentEstimate, currentValue);

            if (Math.Abs(previousStepSize) >= effectiveTolerance && Math.Abs(previousValue) > Math.Abs(currentValue))
            {
                double valueRatio = currentValue / previousValue;
                double numerator;
                double denominator;

                if (previousEstimate == contrapoint)
                {
                    numerator = 2.0 * halfBracket * valueRatio;
                    denominator = 1.0 - valueRatio;
                }
                else
                {
                    double contrapointRatio = previousValue / contrapointValue;
                    double currentContrapointRatio = currentValue / contrapointValue;
                    numerator = valueRatio * (2.0 * halfBracket * contrapointRatio * (contrapointRatio - currentContrapointRatio) - (currentEstimate - previousEstimate) * (currentContrapointRatio - 1.0));
                    denominator = (contrapointRatio - 1.0) * (currentContrapointRatio - 1.0) * (valueRatio - 1.0);
                }

                if (numerator > 0.0)
                    denominator = -denominator;

                numerator = Math.Abs(numerator);
                double interpolationBound = 3.0 * halfBracket * denominator - Math.Abs(effectiveTolerance * denominator);
                double previousStepBound = Math.Abs(previousStepSize * denominator);

                if (2.0 * numerator < Math.Min(interpolationBound, previousStepBound))
                {
                    previousStepSize = stepSize;
                    stepSize = numerator / denominator;
                }
                else
                {
                    stepSize = halfBracket;
                    previousStepSize = stepSize;
                }
            }
            else
            {
                stepSize = halfBracket;
                previousStepSize = stepSize;
            }

            previousEstimate = currentEstimate;
            previousValue = currentValue;
            currentEstimate += Math.Abs(stepSize) > effectiveTolerance ? stepSize : (halfBracket >= 0.0 ? effectiveTolerance : -effectiveTolerance);
            currentValue = function(currentEstimate);
        }

        throw GetNonConvergenceException(currentEstimate, currentValue);
    }
}
