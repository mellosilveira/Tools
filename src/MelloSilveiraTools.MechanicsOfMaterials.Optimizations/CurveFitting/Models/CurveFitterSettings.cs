namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Configuration options for curve fitting algorithms, controlling deterministic execution and numerical behavior.
/// </summary>
public record CurveFitterSettings
{
    /// <summary>
    /// Gets or sets the random seed used by stochastic optimizers (such as ALGLIB's GDEMO differential evolution)
    /// to guarantee deterministic and mathematically identical reproducibility across runs and processor architectures.
    /// Defaults to <see cref="CurveFittingConstants.DefaultSeed"/>.
    /// </summary>
    public int Seed { get; init; } = CurveFittingConstants.DefaultSeed;

    /// <summary>
    /// Gets the default curve fitter implementation type registered as the unkeyed <see cref="Algorithms.ICurveFitter"/> in the DI container.
    /// Defaults to <see cref="CurveFitterType.Alglib"/>.
    /// </summary>
    public CurveFitterType DefaultCurveFitter { get; init; } = CurveFitterType.Alglib;
}
