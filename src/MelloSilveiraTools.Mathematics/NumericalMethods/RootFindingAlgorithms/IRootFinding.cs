using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// The root-finding algorithm is a numerical method used to find the roots or zeros of a continuous function.
/// It iteratively refines the root approximation based on the function's behavior within an interval. The
/// algorithm returns the approximate root or indicates if convergence criteria were not met.
/// </summary>
public interface IRootFinding
{
    /// <summary>
    /// Find the root of a continuous <paramref name="function"/> which is in the interval informed in <paramref name="input"/>.
    /// </summary>
    /// <param name="input">Contains the inputs for root-finding algorithm.</param>
    /// <param name="function">Represents the continuous function.</param>
    /// <returns>The root of the continuous function and its error.</returns>
    /// <exception cref="NonConvergenceException">When it was not possible to converge to a value.</exception>
    (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function);
}
