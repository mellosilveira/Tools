using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <inheritdoc cref="IRootFinding"/>
public abstract class RootFinding : IRootFinding
{
    /// <inheritdoc/>
    public abstract (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function);

    /// <summary>
    /// Throws exception indicating a non convergence of root-finding algorithm.
    /// </summary>
    /// <returns></returns>
    protected Exception GetNonConvergenceException(double point, double value) => new NonConvergenceException(GetType().Name, point, value);
}