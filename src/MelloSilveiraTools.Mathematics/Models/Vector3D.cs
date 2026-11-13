using MelloSilveiraTools.Mathematics.Extensions;
using System.Globalization;

namespace MelloSilveiraTools.Mathematics.Models;

/// <summary>
/// Represents a vector with 3 dimensions: x, y and z.
/// </summary>
public readonly struct Vector3D
{
    /// <summary>
    /// Class constructor.
    /// </summary>
    /// <param name="x">The value at axis X.</param>
    /// <param name="y">The value at axis Y.</param>
    /// <param name="z">The value at axis Z.</param>
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
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{X},{Y},{Z}");

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is Vector3D vector3D && X.EqualsWithTolerance(vector3D.X) && Y.EqualsWithTolerance(vector3D.Y) && Z.EqualsWithTolerance(vector3D.Z);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>
    /// An empty (zero) vector.
    /// </summary>
    public static readonly Vector3D Empty = new(0.0, 0.0, 0.0);

    /// <summary>
    /// Creates a <see cref="Vector3D"/> based on two <see cref="Point3D"/> instances.
    /// </summary>
    /// <param name="point1">Starting point.</param>
    /// <param name="point2">Ending point.</param>
    /// <returns>A new <see cref="Vector3D"/> representing the difference vector from <paramref name="point1"/> to <paramref name="point2"/>.</returns>
    public static Vector3D Create(Point3D point1, Point3D point2) => new(point2.X - point1.X, point2.Y - point1.Y, point2.Z - point1.Z);

    /// <summary>
    /// Creates a <see cref="Vector3D"/> based on two point coordinate strings in millimeters.
    /// </summary>
    /// <param name="point1">The starting point as a comma-separated string in millimeters.</param>
    /// <param name="point2">The ending point as a comma-separated string in millimeters.</param>
    /// <returns>A new <see cref="Vector3D"/> representing the difference vector.</returns>
    public static Vector3D Create(string point1, string point2) => Create(Point3D.Parse(point1), Point3D.Parse(point2));

    /// <summary>
    /// Creates a <see cref="Vector3D"/> based on the axis x, y and z components.
    /// </summary>
    /// <param name="x">The value at axis X.</param>
    /// <param name="y">The value at axis Y.</param>
    /// <param name="z">The value at axis Z.</param>
    /// <returns>A new <see cref="Vector3D"/> instance.</returns>
    public static Vector3D Create(double x, double y, double z) => new(x, y, z);

    /// <summary>
    /// Creates a <see cref="Vector3D"/> based on a comma-separated string representation.
    /// </summary>
    /// <param name="vectorAsString">Comma-separated string representing the X, Y, and Z coordinates.</param>
    /// <returns>A new <see cref="Vector3D"/> instance initialized with the parsed coordinates.</returns>
    public static Vector3D Parse(string vectorAsString)
    {
        string[] vec = vectorAsString.Split(',');
        return new Vector3D
        (
            x: double.Parse(vec[0], CultureInfo.InvariantCulture),
            y: double.Parse(vec[1], CultureInfo.InvariantCulture),
            z: double.Parse(vec[2], CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// Safely creates a <see cref="Vector3D"/> based on a comma-separated string, returning <see cref="Empty"/> for null or whitespace input.
    /// </summary>
    /// <param name="vectorAsString">Comma-separated string representing the X, Y, and Z coordinates.</param>
    /// <returns>A new <see cref="Vector3D"/> instance initialized with the parsed coordinates, or <see cref="Empty"/> if the input is null or whitespace.</returns>
    public static Vector3D SafeParse(string vectorAsString) => string.IsNullOrWhiteSpace(vectorAsString) ? Empty : Parse(vectorAsString);

    /// <summary>
    /// Attempts to parse a comma-separated string representation into a <see cref="Vector3D"/>.
    /// </summary>
    /// <param name="vectorAsString">Comma-separated string representing the X, Y, and Z coordinates.</param>
    /// <param name="vector3D">When this method returns, contains the parsed <see cref="Vector3D"/> if parsing succeeded, or <c>null</c> if parsing failed.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string vectorAsString, out Vector3D? vector3D)
    {
        try
        {
            string[] vec = vectorAsString.Split(',');
            vector3D = new Vector3D
            (
                x: double.Parse(vec[0], CultureInfo.InvariantCulture),
                y: double.Parse(vec[1], CultureInfo.InvariantCulture),
                z: double.Parse(vec[2], CultureInfo.InvariantCulture)
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

    /// <summary>
    /// Scales a <see cref="Vector3D"/> by a scalar.
    /// </summary>
    /// <param name="vector">The vector to scale.</param>
    /// <param name="value">The scalar multiplier.</param>
    /// <returns>A new <see cref="Vector3D"/> with each component multiplied by <paramref name="value"/>.</returns>
    public static Vector3D operator *(Vector3D vector, double value) => new(vector.X * value, vector.Y * value, vector.Z * value);

    /// <summary>
    /// Divides a <see cref="Vector3D"/> by a scalar.
    /// </summary>
    /// <param name="vector">The vector to divide.</param>
    /// <param name="value">The scalar divisor.</param>
    /// <returns>A new <see cref="Vector3D"/> with each component divided by <paramref name="value"/>.</returns>
    public static Vector3D operator /(Vector3D vector, double value) => new(vector.X / value, vector.Y / value, vector.Z / value);
}
