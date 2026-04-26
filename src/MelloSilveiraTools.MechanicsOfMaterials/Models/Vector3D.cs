using System;
using System.Collections.Generic;
using System.Linq;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models
{
    /// <summary>
    /// It represents a vector with 3 dimensions: x, y and z.
    /// </summary>
    public struct Vector3D
    {
        /// <summary>
        /// Creates a 3D vector from its Cartesian components.
        /// </summary>
        /// <param name="x">Component along the X axis.</param>
        /// <param name="y">Component along the Y axis.</param>
        /// <param name="z">Component along the Z axis.</param>
        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

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

        /// <summary>
        /// The length of vector.
        /// </summary>
        public double Length => Math.Sqrt(LengthSquared);

        /// <summary>
        /// The length squared of vector.
        /// </summary>
        public double LengthSquared
            => Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2);

        /// <summary>
        /// Builds the vector that goes from <paramref name="point1"/> to <paramref name="point2"/>
        /// (computed component-wise as point2 - point1).
        /// </summary>
        /// <param name="point1">The start point (tail of the vector).</param>
        /// <param name="point2">The end point (head of the vector).</param>
        /// <returns>The <see cref="Vector3D"/> connecting the two points.</returns>
        public static Vector3D Create(Point3D point1, Point3D point2)
        {
            return new Vector3D(
                point2.X - point1.X,
                point2.Y - point1.Y,
                point2.Z - point1.Z);
        }

        /// <summary>
        /// Parses a comma-separated string of three numeric values into a <see cref="Vector3D"/>.
        /// </summary>
        /// <param name="vector">String in the form "x,y,z" containing the three components.</param>
        /// <returns>The <see cref="Vector3D"/> represented by the string.</returns>
        public static Vector3D Create(string vector)
        {
            List<string> vec = vector.Split(',').ToList();

            return new Vector3D
            {
                X = double.Parse(vec[0]),
                Y = double.Parse(vec[1]),
                Z = double.Parse(vec[2])
            };
        }
    }
}
