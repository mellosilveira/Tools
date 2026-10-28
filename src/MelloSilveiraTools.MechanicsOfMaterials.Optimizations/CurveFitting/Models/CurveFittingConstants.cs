namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

public static class CurveFittingConstants
{
    /// <summary>
    /// Default tolerance used by the curve fitting algorithms.
    /// </summary>
    public const double Tolerance = 1e-5;

    /// <summary>
    /// Default maximum iterations / epochs used by the curve fitting algorithms.
    /// </summary>
    /// <remarks>
    /// Configured to 500 to establish an efficient computational budget for population-based global optimizers
    /// (such as ALGLIB's GDEMO differential evolution in <c>RuleConstrained</c> mode, where total evaluations equal
    /// <c>MaxIterations * PopulationSize</c>), while providing sufficient iterations for gradient-based local convergence
    /// (such as ALGLIB's L-BFGS / <c>minbleic</c> in <c>Standard</c> mode and subsequent polish phases).
    /// </remarks>
    public const int MaxIterations = 500;

    /// <summary>
    /// Default pseudorandom seed used by stochastic curve fitting optimizers (e.g. ALGLIB GDEMO differential evolution)
    /// to guarantee deterministic and mathematically identical results across executions and CPU architectures.
    /// </summary>
    public const int DefaultSeed = 42;
}
