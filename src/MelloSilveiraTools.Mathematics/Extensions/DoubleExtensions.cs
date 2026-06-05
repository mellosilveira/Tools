using MelloSilveiraTools.Mathematics.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Mathematics.Extensions;

/// <summary>
/// Contains the extension methods to double.
/// </summary>
public static class DoubleExtensions
{
    extension(double value)
    {
        /// <summary>
        /// Returns the square of the value (value * value).
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Squared() => value * value;

        /// <summary>
        /// Converts the value from radians to degrees.
        /// </summary>
        /// <returns></returns>
        public double ToDegrees() => 180 / Math.PI * value;

        /// <summary>
        /// Indicates if a value is non-negative (zero is considered non-negative).
        /// </summary>
        /// <returns></returns>
        public bool IsPositive() => !double.IsNegative(value);

        /// <summary>
        /// Indicates if a value is negative and is not zero.
        /// </summary>
        /// <returns></returns>
        public bool IsNegative() => double.IsNegative(value);

        /// <summary>
        /// Indicates if a value is negative or zero.
        /// </summary>
        /// <returns></returns>
        public bool IsNegativeOrZero() => value <= 0;

        /// <summary>
        /// Calculates the relative difference between two values: (value1 - value2) / value1.
        /// Returns 0 when <paramref name="value"/> is zero to avoid division by zero.
        /// </summary>
        /// <param name="value2"></param>
        /// <returns></returns>
        public double RelativeDifference(double value2) => value == 0 ? 0 : (value - value2) / value;

        /// <summary>
        /// Calculates the relative absolut difference between two values.
        /// </summary>
        /// <param name="value2"></param>
        /// <returns></returns>
        public double RelativeAbsolutDifference(double value2) => Math.Abs(value.RelativeDifference(value2));

        /// <summary>
        /// Indicates if two values are equals considering the application tolerance. 
        /// </summary>
        /// <param name="value2"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public bool EqualsWithTolerance(double value2, double? tolerance = null)
        {
            double tol = tolerance ?? MathematicConstants.RelativeTolerance;

            // When both values are zero or very close to it, use absolute comparison to avoid division by zero.
            if (Math.Abs(value) < MathematicConstants.Tolerance && Math.Abs(value2) < MathematicConstants.Tolerance)
                return true;

            // Use the larger absolute value as denominator to avoid NaN/Infinity when one value is zero.
            double denominator = Math.Max(Math.Abs(value), Math.Abs(value2));
            return Math.Abs(value2 - value) / denominator < tol;
        }
    }

    extension(double? value)
    {
        /// <summary>
        /// Indicates if a value is non-negative (zero is considered non-negative).
        /// </summary>
        /// <returns></returns>
        public bool IsPositive() => value is not null && !double.IsNegative(value.Value);

        /// <summary>
        /// Indicates if a value is negative and is not zero.
        /// </summary>
        /// <returns></returns>
        public bool IsNegative() => value is not null && double.IsNegative(value.Value);
    }
}