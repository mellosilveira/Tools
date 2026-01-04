using System.Globalization;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.NumericalMethods
{
    /// <summary>
    /// It contains the finite element analysis results to a specific time.
    /// </summary>
    public class NumericalMethodResult
    {
        /// <summary>
        /// Basic constructor.
        /// </summary>
        public NumericalMethodResult() { }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="numberOfBoundaryConditions"></param>
        public NumericalMethodResult(uint numberOfBoundaryConditions)
        {
            Displacement = new double[numberOfBoundaryConditions];
            Velocity = new double[numberOfBoundaryConditions];
            Acceleration = new double[numberOfBoundaryConditions];
            EquivalentForce = new double[numberOfBoundaryConditions];
        }

        /// <summary>
        /// Unit: s (second).
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// Unit: m (meter).
        /// </summary>
        public double[] Displacement { get; set; }

        /// <summary>
        /// Unit: m/s (meter per second).
        /// </summary>
        public double[] Velocity { get; set; }

        /// <summary>
        /// Unit: m/s² (meter per squared second).
        /// </summary>
        public double[] Acceleration { get; set; }

        /// <summary>
        /// Unit: N (Newton).
        /// </summary>
        public double[] EquivalentForce { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            return $"{Time}," +
                $"{string.Join(",", Displacement)}," +
                $"{string.Join(",", Velocity)}," +
                $"{string.Join(",", Acceleration)}," +
                $"{string.Join(",", EquivalentForce)}";
        }
    }
}
