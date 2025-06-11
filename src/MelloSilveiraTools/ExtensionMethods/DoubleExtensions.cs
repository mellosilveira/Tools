namespace MelloSilveiraTools.ExtensionMethods
{
    /// <summary>
    /// Contains the extension methods to double.
    /// </summary>
    public static class DoubleExtensions
    {
        /// <summary>
        /// Converts the value from radians to degrees.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ToDegrees(this double value) => 180 / Math.PI * value;

        /// <summary>
        /// Calculates the relative difference between two values.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static double RelativeDifference(this double value1, double value2)
        {
            if (value1 == value2)
                return 0;

            return (value1 - value2) / value1;
        }

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
        public static bool EqualsWithTolerance(this double value1, double value2, double tolerance) => Math.Abs((value2 - value1) / value1) < tolerance;

        /// <summary>
        /// Indicates if a value is positive and is not zero.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsPositive(this double value) => !double.IsNegative(value);

        /// <summary>
        /// Indicates if a value is positive and is not zero.
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
}