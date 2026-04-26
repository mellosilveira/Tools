using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models
{
    /// <summary>
    /// It represents a point with 3 dimensions: x, y and z.
    /// </summary>
    public struct Point3D
    {
        /// <summary>
        /// The value at axis X.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// The value at axis Y.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// The value at axis Z.
        /// </summary>
        public double Z { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"({X}, {Y}, {Z})";

        /// <summary>
        /// Parses a comma-separated string of three numeric values (in millimeters) into a
        /// <see cref="Point3D"/>. Each component is divided by 1000 so the resulting point is
        /// expressed in meters.
        /// </summary>
        /// <param name="point">String in the form "x,y,z" with coordinates in millimeters.</param>
        /// <returns>The <see cref="Point3D"/> in meters represented by the string.</returns>
        public static Point3D Create(string point)
        {
            List<string> points = point.Split(',').ToList();

            return new Point3D
            {
                X = double.Parse(points[0], CultureInfo.InvariantCulture) / 1000,
                Y = double.Parse(points[1], CultureInfo.InvariantCulture) / 1000,
                Z = double.Parse(points[2], CultureInfo.InvariantCulture) / 1000,
            };
        }
    }
}
