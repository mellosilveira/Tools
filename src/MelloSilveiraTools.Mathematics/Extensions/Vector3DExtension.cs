using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Extensions
{
    /// <summary>
    /// It contains the extension methods to Vector3D.
    /// </summary>
    public static class Vector3DExtension
    {
        /// <summary>
        /// Determines whether every component of the supplied vector is exactly zero.
        /// </summary>
        /// <param name="vector">The vector to test.</param>
        /// <returns><c>true</c> when X, Y and Z are all zero; otherwise <c>false</c>.</returns>
        public static bool IsZero(this Vector3D vector) => vector.X == 0 && vector.Y == 0 && vector.Z == 0;

        /// <summary>
        /// This method normalizes the vector.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3D Normalize(this Vector3D vector)
            => Vector3D.Create
            (
                vector.X / vector.Length,
                vector.Y / vector.Length,
                vector.Z / vector.Length
            );

        /// <summary>
        /// This method calculates the cross product between two vectors.
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static Vector3D CrossProduct(this Vector3D vector1, Vector3D vector2)
            => Vector3D.Create
            (
                vector1.Y * vector2.Z - vector1.Z * vector2.Y,
                vector1.Z * vector2.X - vector1.X * vector2.Z,
                vector1.X * vector2.Y - vector1.Y * vector2.X
            );

        /// <summary>
        /// This method sums two vectors.
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static Vector3D Sum(this Vector3D vector1, Vector3D vector2)
            => Vector3D.Create
            (
                vector1.X + vector2.X,
                vector1.Y + vector2.Y,
                vector1.Z + vector2.Z
            );

        /// <summary>
        /// This method subtracts two vectors.
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static Vector3D Subtract(this Vector3D vector1, Vector3D vector2)
            => Vector3D.Create
            (
                vector1.X - vector2.X,
                vector1.Y - vector2.Y,
                vector1.Z - vector2.Z
            );

        /// <summary>
        /// This method calculates the dot product between two vectors.
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static double DotProduct(this Vector3D vector1, Vector3D vector2)
            => vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;

        /// <summary>
        /// This method returns true if the vector is empty and false, otherwise.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static bool IsEmpty(this Vector3D vector) => vector.X == 0 && vector.Y == 0 && vector.Z == 0;
    }
}
