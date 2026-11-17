using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.Models.Statistics;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

namespace UnitTests.Mathematics.Models;

/// <summary>
/// Unit tests for mathematical domain models and static helpers.
/// </summary>
public sealed class MathModelsTests
{
    [Fact]
    public void CustomMath_ComputesCosSumAverageAndAverageTimeStep()
    {
        Point3D cos = CustomMath.Cos(Vector3D.Create(0.0, Math.PI, Math.PI / 2.0));
        Assert.Equal(1.0, cos.X, 10);
        Assert.Equal(-1.0, cos.Y, 10);
        Assert.Equal(0.0, cos.Z, 10);

        Assert.Equal(15.0, CustomMath.Sum(4.0, 5.0, 6.0), 10);
        Assert.Equal(5.0, CustomMath.Average(4.0, 5.0, 6.0), 10);

        double[][] series =
        [
            [0.0, 0.5, 1.0],
            [0.0, 1.5]
        ];
        // Intervals: 0.5, 0.5, 1.5 -> mean = 2.5 / 3
        Assert.Equal(2.5 / 3.0, CustomMath.CalculateAverageTimeStep(series), 10);
        Assert.Equal(0.0, CustomMath.CalculateAverageTimeStep([[1.0], []]));
    }

    [Fact]
    public void Point3D_CreateParseEqualityHashAndString_WorkCorrectly()
    {
        Point3D parsed = Point3D.Parse("1000,2000,3000");
        Point3D created = Point3D.Create(1.0, 2.0, 3.0);
        Point3D different = Point3D.Create(1.0, 2.0, 4.0);

        Assert.Equal(1.0, parsed.X, 10);
        Assert.Equal(2.0, parsed.Y, 10);
        Assert.Equal(3.0, parsed.Z, 10);

        Assert.True(parsed.Equals(created));
        Assert.True(parsed == created);
        Assert.False(parsed != created);
        Assert.False(parsed.Equals("not-a-point"));
        Assert.True(parsed != different);
        Assert.Equal(created.GetHashCode(), Point3D.Create(1.0, 2.0, 3.0).GetHashCode());
        Assert.Contains("1", created.ToString());
    }

    [Fact]
    public void BoundaryModelsAndInputOutputRecords_PreserveValues()
    {
        BoundaryValues bv = new(10.0, 15.0);
        Assert.Equal(10.0, bv.InitialValue);
        Assert.Equal(15.0, bv.FinalValue);
        Assert.Equal(0.5, bv.Variation, 10);

        StatisticalData stats = new(1, 2, 3, 0, 5, -1, 7, 1.2, [10.0], [1, 2, 3]);
        BoundaryStatisticalSummary summary = new(stats, stats, stats);
        VariableBoundaryValues variableBoundaries = new() { ["stress"] = bv };
        IntegralInput twoArgIntegral = new(10.0, 0.1);
        NonConvergenceException ex1 = new("Bisection", 1.5, 0.02);
        NonConvergenceException ex2 = new("Bisection", 1.5, 0.02, new InvalidOperationException("inner"));
        RootFindingInput rfInput = new(0.0, 1.0, 1e-6, 100);

        Assert.Same(stats, summary.InitialValues);
        Assert.Same(bv, variableBoundaries["stress"]);
        Assert.Equal(MathematicConstants.InitialTime, twoArgIntegral.InitialPoint);
        Assert.Equal(10.0, twoArgIntegral.FinalPoint);
        Assert.Equal(0.1, twoArgIntegral.Step);
        Assert.Contains("Bisection", ex1.Message);
        Assert.NotNull(ex2.InnerException);
        Assert.Equal(100, rfInput.MaxIterations);
    }
}
