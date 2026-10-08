namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// Configuration parameters for finding equilibrium points, calibration roots, and zero-crossing states.
/// Encapsulates search interval boundaries, target convergence tolerance, and computational iteration safeguards.
/// </summary>
/// <param name="InitialPoint">Starting boundary of the search interval.</param>
/// <param name="FinalPoint">Ending boundary of the search interval.</param>
/// <param name="Tolerance">Acceptable precision threshold for considering the solution converged.</param>
/// <param name="MaxIterations">Maximum number of evaluation cycles allowed to protect computational budget.</param>
public record RootFindingInput(double InitialPoint, double FinalPoint, double Tolerance, int MaxIterations);
