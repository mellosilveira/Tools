namespace MelloSilveiraTools.Mathematics.Functions;

using MelloSilveiraTools.Mathematics.Models;

/// <summary>
/// Represents the exponential integral function, defined as:
/// <c>E_1(x) = \int_x^\infty (e^(-t) / t) dt</c> for <c>x &gt; 0</c>.
/// Evaluates the single analytical integral function without multiple summation terms.
/// </summary>
/// <remarks>
/// <para>
/// <b>Numerical Methods and Domain Partition (<c>x &lt;= 1.0</c> vs <c>x &gt; 1.0</c>):</b>
/// The calculation employs two distinct analytical and numerical formulations partitioned at the optimal threshold <c>x = 1.0</c>
/// (Abramowitz &amp; Stegun §5.1, Gautschi 1973):
/// </para>
/// <list type="number">
/// <item>
/// <term>Power Series Expansion (<c>0 &lt; x &lt;= 1.0</c>):</term>
/// <description>
/// <c>E_1(x) = -\gamma - \ln(x) - \sum_{k=1}^\infty \frac{(-x)^k}{k \cdot k!}</c>, where <c>\gamma</c> is the Euler-Mascheroni constant (<see cref="MathematicConstants.EulerMascheroniConstant"/>).
/// For <c>x \in (0, 1]</c>, the factorial denominator ensures rapid super-exponential convergence to the requested tolerance in ~10-15 iterations,
/// while explicitly capturing the logarithmic singularity <c>-\ln(x)</c> as <c>x \to 0^+</c>.
/// Using this series for <c>x &gt;&gt; 1</c> is numerically forbidden due to <i>catastrophic cancellation</i>: alternating terms become exceedingly large
/// before decaying (e.g. <c>|term| &gt; 10^7</c> for <c>x = 20</c>), destroying all double-precision significant digits when subtracting large numbers
/// to produce a tiny result.
/// </description>
/// </item>
/// <item>
/// <term>Continued Fraction Representation (<c>x &gt; 1.0</c>):</term>
/// <description>
/// <c>E_1(x) = e^(-x) \cdot \cfrac{1}{x + \cfrac{1}{1 + \cfrac{1}{x + \cfrac{2}{1 + \cfrac{2}{x + \dots}}}}}</c> (Gautschi / Cephes algorithm).
/// For <c>x &gt; 1.0</c>, the continued fraction convergents converge monotonically and rapidly without subtraction of large numbers,
/// completely avoiding catastrophic cancellation. The exponential damping factor <c>e^(-x)</c> is factored out and applied at the end.
/// As <c>x \to 0^+</c>, the continued fraction converges very slowly and cannot handle the logarithmic singularity, making it unsuitable for small <c>x</c>.
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Cutoff Threshold:</b>
/// For <c>x &gt; MaximumAcceptedVariableValue = -\ln(\text{Tolerance})</c>, <c>e^(-x) &lt; \text{Tolerance}</c>,
/// allowing the function to immediately return <c>0.0</c> without numerical evaluation.
/// </para>
/// </remarks>
public sealed class ExponentialIntegralFunction : Function
{
    private const double RescalingThreshold = 1.44115188075855872e17;
    private readonly double _effectiveTolerance;
    public readonly double _maximumAcceptedVariableValue;

    public ExponentialIntegralFunction(
        double tolerance = MathematicConstants.Tolerance,
        double? initialVariableValue = null,
        double? finalVariableValue = null)
        : base(FunctionType.ExponentialIntegral, [], initialVariableValue, finalVariableValue)
    {
        _effectiveTolerance = Math.Max(tolerance, MathematicConstants.MachineEpsilonFloor);
        _maximumAcceptedVariableValue = -Math.Log(_effectiveTolerance);
    }

    /// <inheritdoc/>
    public override double Calculate(double variableValue) => EvaluateE1(variableValue);

    /// <summary>
    /// Calculates the definite exponential integral over the interval [<paramref name="lowerLimit"/>, <paramref name="upperLimit"/>]:
    /// <c>\int_{lowerLimit}^{upperLimit} (e^(-t) / t) dt = E_1(lowerLimit) - E_1(upperLimit)</c>.
    /// </summary>
    /// <param name="lowerLimit">The lower integration limit (a &gt;= 0).</param>
    /// <param name="upperLimit">The upper integration limit (b &gt;= 0).</param>
    /// <returns>The calculated definite integral value.</returns>
    /// <remarks>
    /// <para>
    /// Exposing a 2-parameter definite integral method offers decisive numerical and computational advantages over evaluating separate single-variable calls:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Singularity &amp; NaN Cancellation (<c>lowerLimit, upperLimit \to 0</c>):</term>
    /// In physical viscoelastic models (such as Fung quasi-linear viscoelasticity), the relaxation integral evaluates limits of the form <c>a = t / \tau_2</c> and <c>b = t / \tau_1</c>.
    /// When <c>t \to 0^+</c>, evaluating <c>E_1(a) - E_1(b)</c> individually produces <c>\infty - \infty = \text{NaN}</c>.
    /// This method resolves the singularity analytically: when both limits are below <see cref="_effectiveTolerance"/>,
    /// <c>\lim_{a,b \to 0^+} [E_1(a) - E_1(b)] = \ln(b / a)</c>.
    /// </item>
    /// <item>
    /// <term>Algebraic Constant Elimination (<c>upperLimit &lt;= 1.0</c>):</term>
    /// When both limits fall within the power series domain, the Euler-Mascheroni constant cancels analytically,
    /// and the logarithms combine into a single <c>\ln(b / a)</c>, saving transcendental function evaluations and reducing rounding accumulation.
    /// </item>
    /// <item>
    /// <term>Early Short-Circuiting:</term>
    /// Returns <c>0.0</c> immediately if <paramref name="lowerLimit"/> equals <paramref name="upperLimit"/> (within <see cref="_effectiveTolerance"/>),
    /// or if <paramref name="lowerLimit"/> &gt;= <see cref="_maximumAcceptedVariableValue"/>, bypassing numerical iteration entirely.
    /// </item>
    /// <item>
    /// <term>Limit Inversion:</term>
    /// Transparently satisfies <c>\int_a^b = -\int_b^a</c> when <paramref name="lowerLimit"/> &gt; <paramref name="upperLimit"/>.
    /// </item>
    /// </list>
    /// </remarks>
    public double Calculate(double lowerLimit, double upperLimit)
    {
        if (lowerLimit < 0.0 || upperLimit < 0.0)
            return double.NaN;

        if (lowerLimit == upperLimit)
            return 0.0;

        // Both limits below tolerance: analytic limit ln(upperLimit / lowerLimit) avoids infinity - infinity = NaN
        // Note: near zero, \int_a^b (1/t) dt = ln(b/a), which is finite and non-zero even for tiny intervals.
        if (upperLimit <= _effectiveTolerance)
            return lowerLimit <= 0.0 ? double.PositiveInfinity : Math.Log(upperLimit / lowerLimit);

        if (lowerLimit <= _effectiveTolerance)
            return double.PositiveInfinity;

        if (Math.Abs(lowerLimit - upperLimit) <= _effectiveTolerance)
            return 0.0;

        // Both limits beyond cutoff: e^(-t)/t has negligible contribution (< Tolerance)
        if (lowerLimit >= _maximumAcceptedVariableValue)
            return 0.0;

        // Optimization: when both limits are in the power series regime (<= 1.0),
        // Euler-Mascheroni constant cancels out and logarithms combine: -ln(a) - (-ln(b)) = ln(b/a).
        return upperLimit <= 1.0
            ? Math.Log(upperLimit / lowerLimit) - (EvaluateSeriesSum(lowerLimit) - EvaluateSeriesSum(upperLimit))
            : EvaluateE1(lowerLimit) - EvaluateE1(upperLimit);
    }

    /// <inheritdoc/>
    protected override Function CreateDerivative() => new GenericFunction(InitialVariableValue, FinalVariableValue, variableValue =>
    {
        if (variableValue <= _effectiveTolerance)
            return double.NegativeInfinity;

        if (variableValue > _maximumAcceptedVariableValue)
            return 0.0;

        return -Math.Exp(-variableValue) / variableValue;
    });

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">The antiderivative of the exponential integral is not supported.</exception>
    protected override Function CreateIntegral()
    {
        throw new NotSupportedException($"Antiderivative of '{nameof(ExponentialIntegralFunction)}' is not supported.");
    }

    /// <summary>
    /// Evaluates the exponential integral <c>E_1(x) = \int_x^\infty (e^(-t) / t) dt</c> for a positive real argument <paramref name="variableValue"/>.
    /// </summary>
    /// <param name="variableValue">The independent variable value <c>x</c>.</param>
    /// <returns>
    /// The evaluated value of <c>E_1(x)</c>:
    /// <list type="bullet">
    /// <item><description><see cref="double.NaN"/> if <c>x &lt; 0</c>.</description></item>
    /// <item><description><see cref="double.PositiveInfinity"/> if <c>x &lt;= _effectiveTolerance</c> (logarithmic singularity <c>\lim_{x \to 0^+} E_1(x) = +\infty</c>).</description></item>
    /// <item><description><c>0.0</c> if <c>x &gt; _maximumAcceptedVariableValue = -\ln(\text{tolerance})</c> (asymptotic zero cutoff).</description></item>
    /// <item><description><c>-\ln(x) - \gamma - \sum_{k=1}^\infty \frac{(-x)^k}{k \cdot k!}</c> if <c>x &lt;= 1.0</c> (Maclaurin power series).</description></item>
    /// <item><description>Continued fraction expansion if <c>x &gt; 1.0</c>.</description></item>
    /// </list>
    /// </returns>
    private double EvaluateE1(double variableValue)
    {
        if (variableValue <= _effectiveTolerance)
            return variableValue < 0.0 ? double.NaN : double.PositiveInfinity;

        if (variableValue > _maximumAcceptedVariableValue)
            return 0.0;

        return variableValue <= 1.0
            ? -Math.Log(variableValue) - MathematicConstants.EulerMascheroniConstant - EvaluateSeriesSum(variableValue)
            : EvaluateContinuedFraction(variableValue);
    }

    /// <summary>
    /// Computes the alternating power series summation:
    /// <c>S(x) = \sum_{k=1}^\infty \frac{(-x)^k}{k \cdot k!}</c>.
    /// </summary>
    /// <param name="variableValue">The argument <c>x &lt;= 1.0</c>.</param>
    /// <returns>The evaluated series sum <c>S(x)</c>.</returns>
    /// <remarks>
    /// Employs a numerically stable, iterative recurrence without direct factorial or power computation:
    /// <para>
    /// <c>f_0 = 1, \quad f_k = f_{k-1} \cdot \left(\frac{-x}{k}\right) = \frac{(-x)^k}{k!}</c>
    /// </para>
    /// <para>
    /// <c>\text{term}_k = \frac{f_k}{k} = \frac{(-x)^k}{k \cdot k!}</c>
    /// </para>
    /// <para>
    /// The iteration terminates when <c>|\text{term}_k| &lt; \text{tolerance} \cdot |S_k|</c>.
    /// </para>
    /// </remarks>
    private double EvaluateSeriesSum(double variableValue)
    {
        double seriesSum = 0.0;
        double alternatingFactor = 1.0;

        for (int termIndex = 1; termIndex <= 50; termIndex++)
        {
            alternatingFactor *= -variableValue / termIndex;
            double currentTerm = alternatingFactor / termIndex;
            seriesSum += currentTerm;

            if (Math.Abs(currentTerm) < _effectiveTolerance * Math.Abs(seriesSum))
                break;
        }

        return seriesSum;
    }

    /// <summary>
    /// Evaluates the exponential integral <c>E_1(x)</c> for <c>x &gt; 1.0</c> via the Gautschi / Cephes continued fraction:
    /// <c>E_1(x) = e^(-x) \cdot \cfrac{1}{x + \cfrac{1}{1 + \cfrac{1}{x + \cfrac{2}{1 + \cfrac{2}{x + \dots}}}}}</c>.
    /// </summary>
    /// <param name="variableValue">The argument <c>x &gt; 1.0</c>.</param>
    /// <returns>The evaluated value <c>E_1(x)</c>.</returns>
    /// <remarks>
    /// Evaluated via the three-term Euler-Wallis forward recurrence:
    /// <para>
    /// For odd step index <c>k = 3, 5, \dots</c>: <c>a_k = 1 + \frac{k - 1}{2}, \quad b_k = 1.0</c>.
    /// </para>
    /// <para>
    /// For even step index <c>k = 2, 4, \dots</c>: <c>a_k = \frac{k}{2}, \quad b_k = x</c>.
    /// </para>
    /// <para>
    /// <c>p_k = b_k \cdot p_{k-1} + a_k \cdot p_{k-2}</c>, with <c>p_0 = 1, p_1 = 1</c>.
    /// </para>
    /// <para>
    /// <c>q_k = b_k \cdot q_{k-1} + a_k \cdot q_{k-2}</c>, with <c>q_0 = x, q_1 = x + 1</c>.
    /// </para>
    /// <para>
    /// Convergent ratio: <c>r_k = \frac{p_k}{q_k}</c>. If <c>|p_k| &gt; \text{RescalingThreshold} = 2^{57}</c>,
    /// all recurrence terms are divided by <c>\text{RescalingThreshold}</c> to prevent IEEE 754 overflow.
    /// </para>
    /// <para>
    /// Iteration terminates when the relative change satisfies <c>\left|\frac{r_k - r_{k-1}}{r_k}\right| &lt; \text{tolerance}</c>,
    /// returning <c>r_k \cdot e^(-x)</c>.
    /// </para>
    /// </remarks>
    private double EvaluateContinuedFraction(double variableValue)
    {
        double numeratorKMinus2 = 1.0;
        double denominatorKMinus2 = variableValue;
        double numeratorKMinus1 = 1.0;
        double denominatorKMinus1 = variableValue + 1.0;
        double convergent = numeratorKMinus1 / denominatorKMinus1;
        double relativeDifference;
        int fractionIndex = 1;

        do
        {
            fractionIndex++;
            (double partialDenominator, double partialNumerator) = fractionIndex % 2 == 1 ? (1.0, 1.0 + (fractionIndex - 1) / 2) : (variableValue, fractionIndex / 2);

            double currentNumerator = numeratorKMinus1 * partialDenominator + numeratorKMinus2 * partialNumerator;
            double currentDenominator = denominatorKMinus1 * partialDenominator + denominatorKMinus2 * partialNumerator;

            if (currentDenominator != 0.0)
            {
                double currentConvergent = currentNumerator / currentDenominator;
                relativeDifference = Math.Abs((convergent - currentConvergent) / currentConvergent);
                convergent = currentConvergent;
            }
            else
            {
                relativeDifference = 1.0;
            }

            numeratorKMinus2 = numeratorKMinus1;
            numeratorKMinus1 = currentNumerator;
            denominatorKMinus2 = denominatorKMinus1;
            denominatorKMinus1 = currentDenominator;

            if (Math.Abs(currentNumerator) > RescalingThreshold)
            {
                numeratorKMinus2 /= RescalingThreshold;
                numeratorKMinus1 /= RescalingThreshold;
                denominatorKMinus2 /= RescalingThreshold;
                denominatorKMinus1 /= RescalingThreshold;
            }
        }
        while (relativeDifference >= _effectiveTolerance);

        return convergent * Math.Exp(-variableValue);
    }
}
