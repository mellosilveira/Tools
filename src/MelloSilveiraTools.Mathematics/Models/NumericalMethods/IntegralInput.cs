namespace MelloSilveiraTools.Mathematics.Models.NumericalMethods
{
    /// <summary>
    /// Contains the input data for integrations.
    /// </summary>
    public class IntegralInput
    {
        /// <summary>
        /// The initial point.
        /// </summary>
        public double InitialPoint { get; set; }

        /// <summary>
        /// The final point.
        /// </summary>
        public double FinalPoint { get; set; }

        /// <summary>
        /// The step to be used while iterating.
        /// </summary>
        public double Step { get; set; }
    }
}
