namespace MelloSilveiraTools.MechanicsOfMaterials.Models
{
    /// <summary>
    /// It is responsible to convert units.
    /// </summary>
    public static class UnitConverter
    {
        /// <summary>
        /// Converts a linear velocity from kilometers per hour to meters per second.
        /// </summary>
        /// <param name="valueInKmh">The velocity expressed in km/h.</param>
        /// <returns>The equivalent velocity in m/s.</returns>
        public static double FromKmhToMs(double valueInKmh) => valueInKmh / 3.6;

        /// <summary>
        /// Converts an angular frequency from revolutions per minute to radians per second.
        /// </summary>
        /// <param name="valueInRpm">The angular frequency expressed in RPM.</param>
        /// <returns>The equivalent angular frequency in rad/s.</returns>
        public static double FromRpmToRads(double valueInRpm) => valueInRpm * Math.PI / 30;
    }
}
