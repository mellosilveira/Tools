using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Converters
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

        /// <summary>
        /// Converts a value from decimal to percentage.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertDecimalToPercentage(double value) => value * 100;

        /// <summary>
        /// Converts a value from square milimeters to square meter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertMm2ToM2(double value) => value / 1e6;

        /// <summary>
        /// Converts a value from milimeters to meter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertMmToM(double value) => value / 1e3;

        /// <summary>
        /// Converts a value from milimeters to meter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertMToMm(double value) => value * 1e3;

        /// <summary>
        /// Converts a value from Pascal to Mega-Pascal.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertPaToMPa(double value) => value / 1e6;

        /// <summary>
        /// Converts a value from Mega-Pascal to Pascal.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertMPaToPa(double value) => value * 1e6;

        /// <summary>
        /// Converts a value from radian to degree.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertRadToDegree(double value) => value * 180 / Math.PI;

        /// <summary>
        /// Converts a value from radian to degree.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Vector3D ConvertRadToDegree(Vector3D value) => Vector3D.Create
        (
            ConvertRadToDegree(value.X),
            ConvertRadToDegree(value.Y),
            ConvertRadToDegree(value.Z)
        );
    }
}
