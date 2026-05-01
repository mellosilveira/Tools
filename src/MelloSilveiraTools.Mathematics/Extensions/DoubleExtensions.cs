using MelloSilveiraTools.Mathematics.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Mathematics.Extensions;

/// <summary>
/// Contains the extension methods to double.
/// </summary>
public static class DoubleExtensions
{
    /// <summary>
    /// Returns the square of the value (value * value).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Squared(this double value) => value * value;

    /// <summary>
    /// Converts the value from radians to degrees.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static double ToDegrees(this double value) => 180 / Math.PI * value;

    /// <summary>
    /// Calculates the relative difference between two values: (value1 - value2) / value1.
    /// Returns 0 when <paramref name="value1"/> is zero to avoid division by zero.
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static double RelativeDifference(this double value1, double value2)
        => value1 == 0 ? 0 : (value1 - value2) / value1;

    /// <summary>
    /// Calculates the relative absolut difference between two values.
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static double RelativeAbsolutDifference(this double value1, double value2) => Math.Abs(value1.RelativeDifference(value2));

    /// <summary>
    /// Indicates if two values are equals considering the application tolerance. 
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static bool EqualsWithTolerance(this double value1, double value2, double? tolerance = null)
    {
        double tol = tolerance ?? MathematicConstants.RelativeTolerance;

        // When both values are zero or very close to it, use absolute comparison to avoid division by zero.
        if (Math.Abs(value1) < MathematicConstants.Tolerance && Math.Abs(value2) < MathematicConstants.Tolerance)
            return true;

        // Use the larger absolute value as denominator to avoid NaN/Infinity when one value is zero.
        double denominator = Math.Max(Math.Abs(value1), Math.Abs(value2));
        return Math.Abs(value2 - value1) / denominator < tol;
    }

    /// <summary>
    /// Indicates if a value is non-negative (zero is considered non-negative).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsPositive(this double value) => !double.IsNegative(value);

    /// <summary>
    /// Indicates if a value is non-negative (zero is considered non-negative).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsPositive(this double? value) => !double.IsNegative(value.GetValueOrDefault());

    /// <summary>
    /// Indicates if a value is negative and is not zero.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNegative(this double value) => double.IsNegative(value);

    /// <summary>
    /// Indicates if a value is negative or zero.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNegativeOrZero(this double value) => double.IsNegative(value) || value == 0;

    /// <summary>
    /// Indicates if a value is negative and is not zero.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNegative(this double? value) => double.IsNegative(value.GetValueOrDefault());
}