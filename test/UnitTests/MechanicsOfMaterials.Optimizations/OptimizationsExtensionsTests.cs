using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;

namespace UnitTests.MechanicsOfMaterials.Optimizations;

/// <summary>
/// Unit tests for Optimizations extension methods:
/// <see cref="ArrayExtensions"/>, <see cref="CurveSegmentExtensions"/>, and <see cref="RangeParametersExtensions"/>.
/// </summary>
public sealed class OptimizationsExtensionsTests
{
    /// <summary>
    /// Verifies Normalize and TranslateToOrigin with both default and explicit reference values.
    /// </summary>
    [Fact]
    public void ArrayExtensions_NormalizeAndTranslateToOrigin_WorkAsExpected()
    {
        double[] values = [2.0, 4.0, 8.0];

        double[] normDefault = values.Normalize();
        Assert.Equal([0.25, 0.5, 1.0], normDefault);

        double[] normExplicit = values.Normalize(4.0);
        Assert.Equal([0.5, 1.0, 2.0], normExplicit);

        double[] transDefault = values.TranslateToOrigin();
        Assert.Equal([0.0, 2.0, 6.0], transDefault);

        double[] transExplicit = values.TranslateToOrigin(1.0);
        Assert.Equal([1.0, 3.0, 7.0], transExplicit);
    }

    /// <summary>
    /// Verifies GetTimePoints and GetFirstRampTime across populated, empty-ramp, and missing-ramp collections.
    /// </summary>
    [Fact]
    public void CurveSegmentExtensions_ExtractTimePointsAndFirstRampTime()
    {
        CurveSegment ramp1 = new()
        {
            Type = SegmentType.Ramp,
            TimePoints = [1.0, 2.0, 3.5],
            ExperimentalStrain = [0.0, 0.01, 0.02],
            ExperimentalStress = [0.0, 0.5, 1.0]
        };

        CurveSegment relax1 = new()
        {
            Type = SegmentType.Relaxation,
            TimePoints = [3.5, 10.0],
            ExperimentalStrain = [0.02, 0.02],
            ExperimentalStress = [1.0, 0.8]
        };

        double[][] times = ((IReadOnlyList<CurveSegment>)[ramp1, relax1]).GetTimePoints();
        Assert.Equal(2, times.Length);
        Assert.Equal([1.0, 2.0, 3.5], times[0]);
        Assert.Equal([3.5, 10.0], times[1]);

        Assert.Equal(2.5, new CurveSegment[] { relax1, ramp1 }.GetFirstRampTime());
        Assert.Null(new CurveSegment[] { relax1 }.GetFirstRampTime());

        CurveSegment emptyRamp = new()
        {
            Type = SegmentType.Ramp,
            TimePoints = [],
            ExperimentalStrain = [],
            ExperimentalStress = []
        };
        Assert.Null(new CurveSegment[] { emptyRamp }.GetFirstRampTime());
    }

    /// <summary>
    /// Verifies GenerateValues handles null, single-point, additive step, and multiplicative factor ranges.
    /// </summary>
    [Fact]
    public void RangeParametersExtensions_GenerateValues_CoversAllProgressionModes()
    {
        RangeParameters? nullRange = null;
        Assert.Empty(nullRange.GenerateValues());

        RangeParameters singleNoFinal = new() { InitialPoint = 5.0, Step = 1.0 };
        Assert.Equal([5.0], singleNoFinal.GenerateValues());

        RangeParameters singleNoIncrement = new() { InitialPoint = 5.0, FinalPoint = 10.0 };
        Assert.Equal([5.0], singleNoIncrement.GenerateValues());

        RangeParameters additive = new() { InitialPoint = 1.0, Step = 0.5, FinalPoint = 2.0 };
        Assert.Equal([1.0, 1.5, 2.0], additive.GenerateValues());

        RangeParameters multiplicative = new() { InitialPoint = 1.0, MultiplicativeFactor = 2.0, FinalPoint = 8.0 };
        Assert.Equal([1.0, 2.0, 4.0, 8.0], multiplicative.GenerateValues());
    }

    /// <summary>
    /// Verifies edge cases on ArrayExtensions, CurveSegmentExtensions, and RangeParametersExtensions.
    /// </summary>
    [Fact]
    public void ArrayCurveSegmentAndRangeParametersExtensions_EdgeInputs_BehaveAsExpected()
    {
        double[] empty = [];
        Assert.Empty(empty.Normalize(1.0));
        Assert.Empty(empty.TranslateToOrigin(0.0));

        RangeParameters noStepOrFactorWithFinal = new() { InitialPoint = 5.0, FinalPoint = 10.0 };
        Assert.Equal([5.0], noStepOrFactorWithFinal.GenerateValues());

        CurveSegment[] emptySegments = [];
        Assert.Null(emptySegments.GetFirstRampTime());
        Assert.Equal(0.0, emptySegments.CalculateAverageTimeStep());

        CurveSegment singlePointSegment = new()
        {
            Type = SegmentType.Relaxation,
            TimePoints = [1.0],
            ExperimentalStrain = [0.01],
            ExperimentalStress = [1.0]
        };
        Assert.Equal(0.0, new[] { singlePointSegment }.CalculateAverageTimeStep());
    }
}
