using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

/// <summary>
/// Represents an optimized constitutive parameter group produced by a curve fitting operation.
/// </summary>
public record ParameterGroupResultData
{
    /// <summary>
    /// Optimized constitutive parameters.
    /// </summary>
    public required ConstitutiveParameters OptimizedParameters { get; init; }

    /// <summary>
    /// The strain interval where these mathematical parameters remain physically valid.
    /// </summary>
    public required AcceptedRange AcceptedStrainRange { get; init; }

    /// <summary>
    /// The coefficient of determination (R²) achieved by the fit.
    /// </summary>
    public double RSquared { get; init; }

    /// <summary>
    /// The sum of squared residuals (SSR) achieved by the fit.
    /// </summary>
    public double FinalError { get; init; }

    /// <summary>
    /// The total number of iterations executed by the optimizer.
    /// </summary>
    public int Iterations { get; init; }

    public static ParameterGroupResultData FromOutput(Pipelines.ExperimentalData.Models.MechanicalModelCurveFitOutput output) => new()
    {
        OptimizedParameters = output.ConstitutiveParameters,
        AcceptedStrainRange = output.AcceptedRange,
        RSquared = output.RSquared,
        FinalError = output.FinalError,
        Iterations = output.Iterations
    };
}