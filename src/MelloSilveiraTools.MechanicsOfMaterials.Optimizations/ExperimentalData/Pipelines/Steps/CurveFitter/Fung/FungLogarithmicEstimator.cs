namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter.Fung;

using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Provides analytical pre-estimation of the continuous Fung quasi-linear viscoelastic (QLV)
/// reduced relaxation function parameters using logarithmic time-scale regression as formulated
/// by Y.C. Fung (1993, Section 7.6, p. 302).
/// Acts as the single source of truth for physical bounds and default parameter values of the Fung reduced relaxation function.
/// </summary>
/// <remarks>
/// <para>
/// <b>Theoretical Foundation (Fung, 1993, p. 302):</b><br/>
/// In Fung's QLV theory, the reduced relaxation function is governed by a continuous spectrum
/// <c>S(τ) = C / τ</c> for <c>τ₁ ≤ τ ≤ τ₂</c>:
/// <code>
/// G(t) = [1 + C · (E₁(t/τ₂) - E₁(t/τ₁))] / [1 + C · ln(τ₂/τ₁)]
/// </code>
/// Taking the derivative with respect to logarithmic time u = ln(t):
/// <code>
/// dG/d(ln t) = t · (dG/dt) = C · [e^(-t/τ₁) - e^(-t/τ₂)] / [1 + C · ln(τ₂/τ₁)]
/// </code>
/// In the intermediate time domain (τ₁ &lt;&lt; t &lt;&lt; τ₂):
/// <list type="bullet">
/// <item><description><c>e^(-t/τ₁) ≈ 0</c> (since t &gt;&gt; τ₁).</description></item>
/// <item><description><c>e^(-t/τ₂) ≈ 1</c> (since t &lt;&lt; τ₂).</description></item>
/// </list>
/// Hence, the slope in semi-logarithmic coordinates is strictly constant:
/// <code>
/// dG/d(ln t) ≈ -S = -C / [1 + C · ln(τ₂/τ₁)]  ==&gt;  S &gt; 0
/// </code>
/// </para>
/// <para>
/// <b>Asymptotic Series and Analytical Extraction of τ₁:</b><br/>
/// Utilizing the series expansion for the exponential integral <c>E₁(z) = -γ - ln(z) + O(z)</c>
/// (where <c>γ</c> is the Euler-Mascheroni constant, <see cref="MathematicConstants.EulerMascheroniConstant"/>),
/// the intermediate relaxation function satisfies:
/// <code>
/// G(t) ≈ 1 - S · γ - S · ln(t/τ₁) = (1 - S · γ + S · ln(τ₁)) - S · ln(t) = A - S · ln(t)
/// </code>
/// Equating the linear regression intercept <c>A = 1 - S · γ + S · ln(τ₁)</c> yields:
/// <code>
/// ln(τ₁) = (A - 1) / S + γ  ==&gt;  τ₁ = exp((A - 1) / S + γ)
/// </code>
/// </para>
/// <para>
/// <b>Soft Tissue Testing Reality and Asymmetry of τ₂:</b><br/>
/// Experimental tension/relaxation tests on knee ligaments and soft tissues cannot be maintained
/// indefinitely without tissue desiccation, enzymatic degradation, and clamping slippage. Standard laboratory
/// protocols terminate after <c>t_max ~ 400 - 1600 s</c>, while the specimen is still in the linear logarithmic decay
/// phase (<c>τ₁ &lt;&lt; t &lt;&lt; τ₂</c>). Therefore, the long-term knee at <c>t ~ τ₂</c> (<c>τ₂ ~ 10³ - 10⁵ s</c>)
/// is never observed experimentally.<br/>
/// Consequently, this estimator:
/// <list type="number">
/// <item><description>Extracts <c>S</c> and <c>τ₁</c> analytically from the initial transition and linear logarithmic decay.</description></item>
/// <item><description>Initializes <c>τ₂⁰ = max(1000.0, 3.0 · t_max)</c> to strictly exceed experimental duration.</description></item>
/// <item><description>Deduces the consistent relaxation stiffness <c>C⁰ = S / [1 - S · ln(τ₂⁰ / τ₁)]</c>.</description></item>
/// </list>
/// These parameters provide an adaptive, physically grounded warm-start for numerical optimizers (e.g., L-BFGS),
/// drastically accelerating convergence and eliminating sensitivity to arbitrary initial guesses.
/// </para>
/// </remarks>
public static class FungLogarithmicEstimator
{
    /// <summary>
    /// Typical dimensionless relaxation stiffness (C) for biological soft tissues, used as default fallback.
    /// In biological ligaments and tendons, C typically ranges between 0.05 and 0.20 (Fung, 1993).
    /// </summary>
    public const double DefaultC = 0.1;

    /// <summary>
    /// Typical fast relaxation time (τ₁), in seconds, used as default fallback (10 milliseconds).
    /// Represents the rapid initial viscous damping mechanism in collagenous tissue.
    /// </summary>
    public const double DefaultFastRelaxationTime = 0.01;

    /// <summary>
    /// Typical slow relaxation time (τ₂), in seconds, used as default fallback (1000 seconds ≈ 16.7 minutes).
    /// Represents the long-term equilibrium transition in biological soft tissue.
    /// </summary>
    public const double DefaultSlowRelaxationTime = 1000.0;

    /// <summary>
    /// Minimum coefficient of determination (R²) required for the logarithmic regression
    /// to be considered sufficiently linear and physically reliable for warm-starting the non-linear optimizer.
    /// When R² is below this threshold, the signal is considered degraded, noisy, or non-linear,
    /// warranting fallback to safe standard initial parameters.
    /// </summary>
    public const double MinimumLogarithmicRSquared = 0.85;

    /// <summary>
    /// Theoretical normalized stress at origin (G(0) = σ(0) / σ(0) = 1.0).
    /// The reduced relaxation function is by definition normalized to unity at the beginning of relaxation.
    /// </summary>
    public const double NormalizedPeakStress = 1.0;

    /// <summary>
    /// Minimum physical bound for fast relaxation time (τ₁), in seconds (0.1 millisecond).
    /// Corresponds to the shortest observable viscoelastic relaxation timescale in collagenous matrices.
    /// </summary>
    public const double MinFastRelaxationTime = 1e-4;

    /// <summary>
    /// Maximum physical bound for fast relaxation time (τ₁), in seconds (1.0 second).
    /// Fast relaxation governs immediate dissipation; values above 1.0 s enter the intermediate logarithmic regime.
    /// </summary>
    public const double MaxFastRelaxationTime = 1.0;

    /// <summary>
    /// Minimum physical bound for slow relaxation time (τ₂), in seconds (10.0 seconds).
    /// </summary>
    public const double MinSlowRelaxationTime = 10.0;

    /// <summary>
    /// Maximum physical bound for slow relaxation time (τ₂), in seconds (1,000,000 seconds ≈ 11.5 days).
    /// Enforces an upper ceiling for asymptotic equilibrium search.
    /// </summary>
    public const double MaxSlowRelaxationTime = 1e6;

    /// <summary>
    /// Minimum physical bound for continuous relaxation spectrum magnitude (C).
    /// </summary>
    public const double MinC = 1e-4;

    /// <summary>
    /// Maximum physical bound for continuous relaxation spectrum magnitude (C).
    /// In physical soft tissues, C rarely exceeds 1.0; 2.0 provides a safe numerical upper ceiling.
    /// </summary>
    public const double MaxC = 2.0;

    /// <summary>
    /// Minimum required separation ratio (τ₂ / τ₁) between slow and fast relaxation times, set to 10.0.
    /// According to Fung (1993, Section 7.6), the continuous spectrum S(τ) = C / τ requires τ₁ &lt; τ₂
    /// spanning at least one full order of magnitude (decade in logarithmic scale).
    /// This guarantees that ln(τ₂ / τ₁) ≥ ln(10) ≈ 2.3026 &gt; 0, preventing division by zero,
    /// domain singularities, or negative denominators in the spectrum normalization term 1 + C · ln(τ₂ / τ₁).
    /// </summary>
    public const double MinimumSpectrumDecadesRatio = 10.0;

    /// <summary>
    /// Multiplier applied to the maximum observed experimental time (t_max) to initialize the slow
    /// relaxation time (τ₂). Set to 3.0 because biological relaxation tests of soft tissues terminate
    /// prematurely (t_max ~ 400 - 1600 s) to prevent autolysis and tissue desiccation, whereas the true
    /// asymptotic equilibrium (τ₂) lies beyond the test duration. Initializing τ₂ at least 3 times t_max
    /// (and at least <see cref="DefaultSlowRelaxationTime"/>) ensures that the numerical optimizer begins
    /// in the unobserved long-term equilibrium regime without colliding with the experimental time window.
    /// </summary>
    public const double UnobservedEquilibriumTimeMultiplier = 3.0;

    /// <summary>
    /// Minimum number of raw data points required to attempt logarithmic regression.
    /// A linear fit requires at least 2 points for a line and at least 3 points to evaluate degrees of freedom (ν = N - 2 ≥ 1).
    /// </summary>
    public const int MinimumInputLength = 3;

    /// <summary>
    /// Minimum number of valid positive time-stress coordinate pairs required to compute linear regression.
    /// </summary>
    public const int MinimumValidPoints = 2;

    /// <summary>
    /// Physical lower bounds for continuous Fung reduced relaxation parameters:
    /// [0] = <see cref="MinC"/> (1e-4),
    /// [1] = <see cref="MinFastRelaxationTime"/> (1e-4 s),
    /// [2] = <see cref="MinSlowRelaxationTime"/> (10.0 s).
    /// </summary>
    public static readonly double[] RelaxationLowerBounds = [MinC, MinFastRelaxationTime, MinSlowRelaxationTime];

    /// <summary>
    /// Physical upper bounds for continuous Fung reduced relaxation parameters:
    /// [0] = <see cref="MaxC"/> (2.0),
    /// [1] = <see cref="MaxFastRelaxationTime"/> (1.0 s),
    /// [2] = <see cref="MaxSlowRelaxationTime"/> (1e6 s).
    /// </summary>
    public static readonly double[] RelaxationUpperBounds = [MaxC, MaxFastRelaxationTime, MaxSlowRelaxationTime];

    /// <summary>
    /// Default fallback initial parameters for continuous Fung reduced relaxation parameters:
    /// [0] = <see cref="DefaultC"/> (0.1),
    /// [1] = <see cref="DefaultFastRelaxationTime"/> (0.01 s),
    /// [2] = <see cref="DefaultSlowRelaxationTime"/> (1000.0 s).
    /// </summary>
    public static readonly double[] DefaultInitialParameters = [DefaultC, DefaultFastRelaxationTime, DefaultSlowRelaxationTime];

    /// <summary>
    /// Estimates initial continuous Fung parameters (<c>C</c>, <c>τ₁</c>, <c>τ₂</c>) from normalized
    /// stress relaxation time-series data using logarithmic linear regression.
    /// </summary>
    /// <param name="timePoints">
    /// Array of relaxation times translated to origin (where <c>timePoints[0] = 0.0</c>), in seconds.
    /// </param>
    /// <param name="normalizedStress">
    /// Array of normalized stress values <c>G(t) = σ(t) / σ(0)</c> (where <c>normalizedStress[0] = 1.0</c>).
    /// </param>
    /// <param name="tolerance">
    /// The numerical tolerance threshold for detecting zero-variance or near-zero values.
    /// Defaults to <see cref="MathematicConstants.Tolerance"/>.
    /// </param>
    /// <returns>
    /// A <see cref="FungLogarithmicEstimationResult"/> containing the estimated initial parameters and regression metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="timePoints"/> or <paramref name="normalizedStress"/> is <c>null</c>.
    /// </exception>
    public static FungLogarithmicEstimationResult Estimate(double[] timePoints, double[] normalizedStress, double tolerance = MathematicConstants.Tolerance)
    {
        ArgumentNullException.ThrowIfNull(timePoints);
        ArgumentNullException.ThrowIfNull(normalizedStress);

        if (timePoints.Length != normalizedStress.Length || timePoints.Length < MinimumInputLength)
        {
            return CreateDefaultResult();
        }

        // Collect valid points in the logarithmic time domain (t > tolerance and G > tolerance).
        int validCount = 0;
        for (int i = 0; i < timePoints.Length; i++)
        {
            if (timePoints[i] > tolerance && normalizedStress[i] > tolerance)
            {
                validCount++;
            }
        }

        if (validCount < MinimumValidPoints)
        {
            return CreateDefaultResult();
        }

        double[] xLogTime = new double[validCount];
        double[] yStress = new double[validCount];
        int writeIndex = 0;
        double maxTime = 0.0;

        for (int i = 0; i < timePoints.Length; i++)
        {
            double t = timePoints[i];
            double g = normalizedStress[i];
            if (t > tolerance && g > tolerance)
            {
                xLogTime[writeIndex] = Math.Log(t);
                yStress[writeIndex] = g;
                if (t > maxTime)
                {
                    maxTime = t;
                }
                writeIndex++;
            }
        }

        // Compute Ordinary Least Squares (OLS) linear regression: y = A - S * x
        double sumX = 0.0;
        double sumY = 0.0;
        for (int i = 0; i < validCount; i++)
        {
            sumX += xLogTime[i];
            sumY += yStress[i];
        }

        double meanX = sumX / validCount;
        double meanY = sumY / validCount;

        double sumCrossDiff = 0.0;
        double sumSquareDiffX = 0.0;
        double sumSquareDiffY = 0.0;

        for (int i = 0; i < validCount; i++)
        {
            double diffX = xLogTime[i] - meanX;
            double diffY = yStress[i] - meanY;
            sumCrossDiff += diffX * diffY;
            sumSquareDiffX += diffX * diffX;
            sumSquareDiffY += diffY * diffY;
        }

        if (sumSquareDiffX <= tolerance)
        {
            return CreateDefaultResult();
        }

        // Model: y = A - S * x (where S = -dG/d(ln t) > 0 for decaying stress)
        double slope = -sumCrossDiff / sumSquareDiffX;
        double intercept = meanY + (slope * meanX);

        double rSquared = 0.0;
        if (sumSquareDiffY > tolerance)
        {
            rSquared = Math.Clamp((sumCrossDiff * sumCrossDiff) / (sumSquareDiffX * sumSquareDiffY), 0.0, 1.0);
        }

        // If the slope is not positive (stress not decaying), fall back to defaults.
        if (slope <= tolerance)
        {
            return CreateDefaultResult();
        }

        // Fast relaxation time τ₁ recovery: ln(τ₁) = (A - 1) / S + γ
        double lnTau1 = ((intercept - NormalizedPeakStress) / slope) + MathematicConstants.EulerMascheroniConstant;
        double estimatedTau1 = Math.Exp(lnTau1);
        double fastRelaxationTime = Math.Clamp(estimatedTau1, MinFastRelaxationTime, MaxFastRelaxationTime);

        // Slow relaxation time τ₂ estimation: must exceed test duration t_max
        double estimatedTau2 = Math.Max(DefaultSlowRelaxationTime, UnobservedEquilibriumTimeMultiplier * maxTime);
        double slowRelaxationTime = Math.Clamp(estimatedTau2, MinSlowRelaxationTime, MaxSlowRelaxationTime);

        // Ensure τ₁ < τ₂ separation spanning at least one decade (MinimumSpectrumDecadesRatio = 10.0)
        // to prevent logarithmic domain collapse ln(τ₂ / τ₁) <= 0.
        if (fastRelaxationTime >= slowRelaxationTime)
        {
            fastRelaxationTime = Math.Min(MaxFastRelaxationTime, slowRelaxationTime / MinimumSpectrumDecadesRatio);
        }

        // Stiffness C estimation: S = C / [1 + C * ln(τ₂ / τ₁)] ==> C = S / [1 - S * ln(τ₂ / τ₁)]
        double denominator = 1.0 - (slope * Math.Log(slowRelaxationTime / fastRelaxationTime));
        double initialC = denominator > tolerance ? Math.Clamp(slope / denominator, MinC, MaxC) : Math.Clamp(DefaultC, MinC, MaxC);
        return new FungLogarithmicEstimationResult(
            initialC,
            fastRelaxationTime,
            slowRelaxationTime,
            rSquared);
    }

    private static FungLogarithmicEstimationResult CreateDefaultResult() => new(
        DefaultC,
        DefaultFastRelaxationTime,
        DefaultSlowRelaxationTime,
        0.0);
}
