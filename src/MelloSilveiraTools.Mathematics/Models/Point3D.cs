using MelloSilveiraTools.Mathematics.Converters;
using SoftTissue.SharedModules.ExtensionMethods;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace MelloSilveiraTools.Mathematics.Models;

/// <summary>
/// Represents a point with 3 dimensions: x, y and z.
/// </summary>
public readonly struct Point3D
{
    private Point3D(double x, double y, double z)
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

    /// <inheritdoc/>
    public override string ToString() => $"({X},{Y},{Z})";

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object obj) =>
        obj is Point3D point3D
        && (X.EqualsWithTolerance(point3D.X)
            && Y.EqualsWithTolerance(point3D.Y)
            && Z.EqualsWithTolerance(point3D.Z));

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>
    /// Creates a <see cref="Point3D"/> based on a string.
    /// </summary>
    /// <param name="point">The points as string at milimeters.</param>
    /// <returns>A new instance of <see cref="Point3D"/>.</returns>
    public static Point3D Parse(string point)
    {
        string[] points = point.Split(',');
        return new Point3D
        (
            UnitConverter.ConvertMmToM(double.Parse(points[0], CultureInfo.InvariantCulture)),
            UnitConverter.ConvertMmToM(double.Parse(points[1], CultureInfo.InvariantCulture)),
            UnitConverter.ConvertMmToM(double.Parse(points[2], CultureInfo.InvariantCulture))
        );
    }

    /// <summary>
    /// Creates a <see cref="Point3D"/> based on each axis.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>A new instance of <see cref="Point3D"/>.</returns>
    public static Point3D Create(double x, double y, double z) => new(x, y, z);

    /// <summary>
    /// Returns a value that indicates whether two specified <see cref="Point3D"/> values are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True, if left and right are equal. False, otherwise.</returns>
    public static bool operator ==(Point3D left, Point3D right) => left.Equals(right);

    /// <summary>
    /// Returns a value that indicates whether two specified <see cref="Point3D"/> values are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True, if left and right are not equal. False, otherwise.</returns>
    public static bool operator !=(Point3D left, Point3D right) => !(left == right);
}