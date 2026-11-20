using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.Models;

namespace UnitTests.Mathematics.Extensions;

/// <summary>
/// Unit tests for <see cref="DoubleExtensions"/> and <see cref="Vector3DExtensions"/>.
/// </summary>
public sealed class MathExtensionsTests
{
    [Fact]
    public void DoubleExtensions_ComputeScalarAndToleranceOperationsCorrectly()
    {
        Assert.Equal(25.0, 5.0.Squared());
        Assert.Equal(180.0, Math.PI.ToDegrees(), 10);

        Assert.True(0.0.IsPositive());
        Assert.True(3.0.IsPositive());
        Assert.False((-1.0).IsPositive());

        Assert.True((-1.0).IsNegative());
        Assert.False(1.0.IsNegative());

        Assert.True(0.0.IsNegativeOrZero());
        Assert.True((-2.0).IsNegativeOrZero());
        Assert.False(0.1.IsNegativeOrZero());

        Assert.Equal(0.0, 0.0.RelativeDifference(5.0));
        Assert.Equal(0.5, 10.0.RelativeDifference(5.0), 10);
        Assert.Equal(-50.0, 10.0.PercentageDifference(15.0), 10);
        Assert.Equal(0.5, 10.0.AbsolutRelativeDifference(15.0), 10);

        Assert.True(1e-12.EqualsWithTolerance(2e-12));
        Assert.True(100.0.EqualsWithTolerance(100.0 + 1e-9));
        Assert.True(10.0.EqualsWithTolerance(10.05, 0.01));
        Assert.False(10.0.EqualsWithTolerance(11.0));
    }

    [Fact]
    public void NullableDoubleExtensions_HandleNullAndValuesCorrectly()
    {
        double? nullVal = null;
        double? posVal = 4.0;
        double? negVal = -4.0;

        Assert.False(nullVal.IsPositive());
        Assert.True(posVal.IsPositive());
        Assert.False(negVal.IsPositive());

        Assert.False(nullVal.IsNegative());
        Assert.False(posVal.IsNegative());
        Assert.True(negVal.IsNegative());

        Assert.Null(nullVal.PercentageDifference(2.0));
        Assert.Null(posVal.PercentageDifference(null));
        Assert.Equal(50.0, posVal.PercentageDifference(2.0)!.Value, 10);
    }

    [Fact]
    public void Vector3DExtensions_ComputeZeroNormalizeCrossAndDotProductsCorrectly()
    {
        Assert.True(Vector3D.Empty.IsZero());
        Assert.False(Vector3D.Create(1.0, 0.0, 0.0).IsZero());

        Vector3D v = Vector3D.Create(3.0, 4.0, 0.0);
        Vector3D normalized = v.Normalize();
        Assert.Equal(0.6, normalized.X, 10);
        Assert.Equal(0.8, normalized.Y, 10);
        Assert.Equal(0.0, normalized.Z, 10);

        Vector3D i = Vector3D.Create(1.0, 0.0, 0.0);
        Vector3D j = Vector3D.Create(0.0, 1.0, 0.0);
        Vector3D cross = i.CrossProduct(j);
        Assert.Equal(Vector3D.Create(0.0, 0.0, 1.0), cross);
        Assert.Equal(0.0, i.DotProduct(j));
        Assert.Equal(25.0, v.DotProduct(v));
    }
}
