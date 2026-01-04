namespace MelloSilveiraTools.Domain.Models
{
    /// <summary>
    /// It contains the available numerical methods for differential equations.
    /// </summary>
    public enum DifferentialEquationMethodType
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
}
