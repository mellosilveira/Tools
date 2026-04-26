using MelloSilveiraTools.MechanicsOfMaterials.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Extensions
{
    /// <summary>
    /// It contains the extension methods to Vector3D.
    /// </summary>
    public static class Vector3DExtension
    {
        /// <summary>
        /// Returns the unit vector that shares the direction of <paramref name="vector"/> (each
        /// component is divided by the vector's length).
        /// </summary>
        /// <param name="vector">The vector to be normalized.</param>
        /// <returns>A <see cref="Vector3D"/> with unit length pointing in the same direction.</returns>
        public static Vector3D Normalize(this Vector3D vector)
            => new()
            {
                X = vector.X / vector.Length,
                Y = vector.Y / vector.Length,
                Z = vector.Z / vector.Length
            };

        /// <summary>
        /// Computes the cross product vector1 x vector2, producing a vector perpendicular to both
        /// operands whose magnitude equals the area of the parallelogram they span.
        /// </summary>
        /// <param name="vector1">The left-hand vector of the cross product.</param>
        /// <param name="vector2">The right-hand vector of the cross product.</param>
        /// <returns>The cross-product <see cref="Vector3D"/>.</returns>
        public static Vector3D CrossProduct(this Vector3D vector1, Vector3D vector2)
            => new()
            {
                X = vector1.Y * vector2.Z - vector1.Z * vector2.Y,
                Y = vector1.Z * vector2.X - vector1.X * vector2.Z,
                Z = vector1.X * vector2.Y - vector1.Y * vector2.X
            };

        /// <summary>
        /// Computes the scalar (dot) product between two vectors: vector1 · vector2.
        /// </summary>
        /// <param name="vector1">The first operand.</param>
        /// <param name="vector2">The second operand.</param>
        /// <returns>The scalar value of the dot product.</returns>
        public static double DotProduct(this Vector3D vector1, Vector3D vector2)
            => vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;

        /// <summary>
        /// Determines whether every component of the supplied vector is exactly zero.
        /// </summary>
        /// <param name="vector">The vector to test.</param>
        /// <returns><c>true</c> when X, Y and Z are all zero; otherwise <c>false</c>.</returns>
        public static bool IsZero(this Vector3D vector) => vector.X == 0 && vector.Y == 0 && vector.Z == 0;
    }
}
