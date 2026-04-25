namespace MelloSilveiraTools.Mathematics.Domain.NumericalMethods
{
    /// <summary>
    /// It contains the input data for a numerical method.
    /// </summary>
    public class NumericalMethodInput
    {
        /// <summary>
        /// Unit: s (second).
        /// </summary>
        public required double TimeStep { get; set; }

        /// <summary>
        /// Unit: kg (kilogram).
        /// </summary>
        public required double[,] Mass { get; set; }

        /// <summary>
        /// Unit: N/m (Newton per meter).
        /// </summary>
        public required double[,] Stiffness { get; set; }

        /// <summary>
        /// Unit: N.s/m (Newston-second per meter).
        /// </summary>
        public required double[,] Damping { get; set; }

        /// <summary>
        /// Unit: N (Newton).
        /// </summary>
        public required double[] EquivalentForce { get; set; }

        /// <summary>
        /// Dimensionless.
        /// </summary>
        public required uint NumberOfBoundaryConditions { get; set; }
    }
}
