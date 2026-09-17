namespace MelloSilveiraTools.Mathematics.Models.NumericalMethods;

/// <summary>
/// It contains the available numerical methods for differential equations.
/// </summary>
public enum DifferentialEquationMethodType : int
{
    /// <summary>
    /// Newmark numerical method.
    /// </summary>
    Newmark = 1,

    /// <summary>
    /// Newmark-Beta numerical method.
    /// </summary>
    NewmarkBeta = 2
}
