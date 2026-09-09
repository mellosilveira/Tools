namespace MelloSilveiraTools.Core.Models;

/// <summary>
/// It contains the constants used in the project.
/// </summary>
public class Constants
{
    /// <summary>
    /// The invalid values for double parameters.
    /// </summary>
    public static List<double> InvalidValues =>
    [
        double.NaN,
        double.PositiveInfinity,
        double.NegativeInfinity,
        double.MaxValue,
        double.MinValue
    ];
}
