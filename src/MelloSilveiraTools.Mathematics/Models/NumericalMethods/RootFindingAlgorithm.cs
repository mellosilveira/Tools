namespace MelloSilveiraTools.Mathematics.Models.NumericalMethods
{
    /// <summary>
    /// Contains the root-finding algorithms used in this project.
    /// </summary>
    public enum RootFindingAlgorithm
    {
        /// <summary>
        /// The step-by-step method is a root-finding algorithm for a continuous function within a given interval.
        /// It works by dividing the interval into smaller steps and iteratively evaluating the function at each
        /// step until a sufficiently accurate approximation of the root is found.
        /// </summary>
        StepByStepMethod = 1,

        /// <summary>
        /// The Bisection method is a root-finding algorithm for a continuous function within a given interval.
        /// It works by repeatedly dividing the interval in half and narrowing down the search range until a
        /// sufficiently accurate approximation of the root is found.
        /// </summary>
        BisectionMethod = 2,

        /// <summary>
        /// Brent's method is a hybrid root-finding algorithm combining the bisection method, 
        /// the secant method and inverse quadratic interpolation. The algorithm tries to use 
        /// the potentially fast-converging secant method or inverse quadratic interpolation 
        /// if possible, it falls back to the more robust bisection method if necessary.
        /// </summary>
        BrentMethod = 3,
    }
}
