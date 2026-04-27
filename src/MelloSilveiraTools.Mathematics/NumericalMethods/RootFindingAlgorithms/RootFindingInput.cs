namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms
{
    /// <summary>
    /// Represents the input for the root-finding algorithm.
    /// </summary>
    /// <param name="InitialPoint">Initial point of interval. Unit: depends on function which is used.</param>
    /// <param name="FinalPoint">Final point of interval. Unit: depends on function which is used.</param>
    /// <param name="Tolerance">Maximum acceptable error.</param>
    /// <param name="MaxIterations">Maximum number of iterations to be performed.</param>
    public record RootFindingInput(double InitialPoint, double FinalPoint, double Tolerance, int MaxIterations);
}
