using MelloSilveiraTools.Mathematics.Extensions;

namespace MelloSilveiraTools.Mathematics.Models
{
    /// <summary>
    /// Represents a vector with 3 dimensions: x, y and z.
    /// </summary>
    public readonly struct Vector3D
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        private Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// The value at axis X.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// The value at axis Y.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// The value at axis Z.
        /// </summary>
        public double Z { get; }

        /// <summary>
        /// The length of vector.
        /// </summary>
        public double Length => Math.Sqrt(LengthSquared);

        /// <summary>
        /// The length squared of vector.
        /// </summary>
        public double LengthSquared => X.Squared() + Y.Squared() + Z.Squared();

        /// <inheritdoc/>
        public override string ToString() => $"{X},{Y},{Z}";

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is Vector3D vector3D && X.EqualsWithTolerance(vector3D.X) && Y.EqualsWithTolerance(vector3D.Y) && Z.EqualsWithTolerance(vector3D.Z);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        /// <summary>
        /// An empty vector.
        /// </summary>
        public static Vector3D Empty => new(0.0, 0.0, 0.0);

        /// <summary>
        /// Creates a <see cref="Vector3D"/> based on two <see cref="Point3D"/>.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static Vector3D Create(Point3D point1, Point3D point2) => new(point2.X - point1.X, point2.Y - point1.Y, point2.Z - point1.Z);

        /// <summary>
        /// Creates a <see cref="Vector3D"/> based on two <see cref="Point3D"/>.
        /// </summary>
        /// <param name="point1">The points as string at millimeters.</param>
        /// <param name="point2">The points as string at millimeters.</param>
        /// <returns></returns>
        public static Vector3D Create(string point1, string point2) => Create(Point3D.Parse(point1), Point3D.Parse(point2));

        /// <summary>
        /// Creates a <see cref="Vector3D"/> based on the axis x, y and z.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3D Create(double x, double y, double z) => new(x, y, z);

        /// <summary>
        /// Creates a <see cref="Vector3D"/> based on a string.
        /// </summary>
        /// <param name="vectorAsString"></param>
        /// <returns></returns>
        public static Vector3D Parse(string vectorAsString)
        {
            var vec = vectorAsString.Split(',');

            return new Vector3D
            (
                x: double.Parse(vec[0]),
                y: double.Parse(vec[1]),
                z: double.Parse(vec[2])
            );
        }

        /// <summary>
        /// Safaselly creates a <see cref="Vector3D"/> based on a string.
        /// </summary>
        /// <param name="vectorAsString"></param>
        /// <returns></returns>
        public static Vector3D SafeParse(string vectorAsString) => string.IsNullOrWhiteSpace(vectorAsString) ? Empty : Parse(vectorAsString);

        /// <summary>
        /// Creates a <see cref="Vector3D"/> based on a string.
        /// </summary>
        /// <param name="vectorAsString"></param>
        /// <param name="vector3D"></param>
        /// <returns></returns>
        public static bool TryParse(string vectorAsString, out Vector3D? vector3D)
        {
            try
            {
                string[] vec = vectorAsString.Split(',');
                vector3D = new Vector3D
                (
                    x: double.Parse(vec[0]),
                    y: double.Parse(vec[1]),
                    z: double.Parse(vec[2])
                );

                return true;
            }
            catch
            {
                vector3D = null;
                return false;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether two specified <see cref="Vector3D"/> values are equal.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>True, if left and right are equal. False, otherwise.</returns>
        public static bool operator ==(Vector3D left, Vector3D right) => left.Equals(right);

        /// <summary>
        /// Returns a value that indicates whether two specified <see cref="Vector3D"/> values are not equal.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>True, if left and right are not equal. False, otherwise.</returns>
        public static bool operator !=(Vector3D left, Vector3D right) => !(left == right);

        /// <summary>
        /// Sums two <see cref="Vector3D"/>.
        /// </summary>
        /// <param name="left">The first value to sum.</param>
        /// <param name="right">The second value to sum.</param>
        /// <returns>An instance of <see cref="Vector3D"/> with the result of summation.</returns>
        public static Vector3D operator +(Vector3D left, Vector3D right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        /// <summary>
        /// Substracts two <see cref="Vector3D"/>.
        /// </summary>
        /// <param name="left">The first value to subtract.</param>
        /// <param name="right">The second value to subtract.</param>
        /// <returns>An instance of <see cref="Vector3D"/> with the result of subtraction.</returns>
        public static Vector3D operator -(Vector3D left, Vector3D right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        public static Vector3D operator *(Vector3D vector, double value) => new(vector.X * value, vector.Y * value, vector.Z * value);
    }
}