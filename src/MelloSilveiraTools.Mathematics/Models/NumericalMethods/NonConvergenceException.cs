namespace MelloSilveiraTools.Mathematics.Models.NumericalMethods
{
    public class NonConvergenceException : Exception
    {
        public NonConvergenceException(string numericalMethodName, double point, double value, Exception? innerException)
            : base($"It was not possible to converge to a value using '{numericalMethodName}'. Point: {point}. Value: {value}.", innerException) { }

        public NonConvergenceException(string numericalMethodName, double point, double value)
            : this(numericalMethodName, point, value, null) { }
    }
}