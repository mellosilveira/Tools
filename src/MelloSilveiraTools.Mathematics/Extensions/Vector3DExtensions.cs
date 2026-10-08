using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Extensions;

/// <summary>
/// It contains the extension methods to Vector3D.
/// </summary>
public static class Vector3DExtensions
{
    extension(Vector3D vector)
    {
        /// <summary>
        /// Determines whether every component of the supplied vector is exactly zero.
        /// </summary>
        /// <returns><c>true</c> when X, Y and Z are all zero; otherwise <c>false</c>.</returns>
        public bool IsZero() => vector.X == 0 && vector.Y == 0 && vector.Z == 0;

        /// <summary>
        /// Normalizes the vector.
        /// </summary>
        /// <returns></returns>
        public Vector3D Normalize() => vector / vector.Length;

        /// <summary>
        /// Calculates the cross product between two vectors.
        /// </summary>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public Vector3D CrossProduct(Vector3D vector2) => Vector3D.Create
        (
            vector.Y * vector2.Z - vector.Z * vector2.Y,
            vector.Z * vector2.X - vector.X * vector2.Z,
            vector.X * vector2.Y - vector.Y * vector2.X
        );

        /// <summary>
        /// Calculates the dot product between two vectors.
        /// </summary>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public double DotProduct(Vector3D vector2) => vector.X * vector2.X + vector.Y * vector2.Y + vector.Z * vector2.Z;
    }
}
