namespace MelloSilveiraTools.Mathematics.Models;

/// <summary>
/// Contain the constats used in application.
/// </summary>
public static class MathematicConstants
{
    /// <summary>
    /// The constant of Euler-Mascheroni.
    /// </summary>
    public const double EulerMascheroniConstant = 0.5772156649015328606065120900824024310421;

    /// <summary>
    /// The tolerance for time comparisons.
    /// Used to distinguish "effectively zero" from any meaningful positive value.
    /// Chosen as 1e-10 s (0.1 nanosecond), well below any physically relevant time in the simulation.
    /// </summary>
    public const double Tolerance = 1e-10;

    /// <summary>
    /// The relative double-point precision accepted for value equality comparisons.
    /// Two values whose relative difference is less than this threshold are considered equal.
    /// </summary>
    public const double RelativeTolerance = 1e-8;
}
