using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter.Fung;

namespace UnitTests.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

public class FungLogarithmicEstimatorTests
{
    [Fact]
    public void Estimate_NullTimePoints_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FungLogarithmicEstimator.Estimate(null!, [1.0, 0.9]));
    }

    [Fact]
    public void Estimate_NullStress_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FungLogarithmicEstimator.Estimate([0.0, 1.0], null!));
    }

    [Fact]
    public void Estimate_InsufficientPoints_ReturnsDefaultResult()
    {
        double[] timePoints = [0.0, 1.0];
        double[] normalizedStress = [1.0, 0.9];

        FungLogarithmicEstimationResult result = FungLogarithmicEstimator.Estimate(timePoints, normalizedStress);

        Assert.Equal(FungLogarithmicEstimator.DefaultC, result.InitialC);
        Assert.Equal(FungLogarithmicEstimator.DefaultFastRelaxationTime, result.FastRelaxationTime);
        Assert.Equal(FungLogarithmicEstimator.DefaultSlowRelaxationTime, result.SlowRelaxationTime);
        Assert.Equal(0.0, result.RSquared);
    }

    [Fact]
    public void Estimate_SyntheticFungLogData_RecoversAccurateParameters()
    {
        // Ground truth parameters
        const double trueC = 0.1;
        const double trueTau1 = 0.01;
        const double trueTau2 = 1000.0;

        double slope = trueC / (1.0 + trueC * Math.Log(trueTau2 / trueTau1));
        double intercept = FungLogarithmicEstimator.NormalizedPeakStress - (slope * MathematicConstants.EulerMascheroniConstant) + (slope * Math.Log(trueTau1));

        // Generate synthetic points in the intermediate logarithmic region (t >> tau1, t << tau2)
        double[] timePoints = [0.0, 1.0, 2.0, 5.0, 10.0, 20.0, 50.0, 100.0, 200.0, 300.0];
        double[] normalizedStress = new double[timePoints.Length];
        normalizedStress[0] = FungLogarithmicEstimator.NormalizedPeakStress;

        for (int i = 1; i < timePoints.Length; i++)
        {
            normalizedStress[i] = intercept - slope * Math.Log(timePoints[i]);
        }

        FungLogarithmicEstimationResult result = FungLogarithmicEstimator.Estimate(timePoints, normalizedStress);

        Assert.True(result.RSquared > 0.999, $"Expected R² near 1.0, got {result.RSquared}");
        Assert.True(result.RSquared >= FungLogarithmicEstimator.MinimumLogarithmicRSquared, "R² must exceed minimum linearity quality gate.");
        Assert.Equal(trueTau1, result.FastRelaxationTime, 2);
        Assert.True(result.SlowRelaxationTime >= FungLogarithmicEstimator.DefaultSlowRelaxationTime, "Slow relaxation time should exceed default fallback.");
        Assert.True(result.InitialC > 0.0, "Initial C must be positive.");
    }

    [Fact]
    public void Estimate_NonDecayingStress_ReturnsDefaultResult()
    {
        double[] timePoints = [0.0, 1.0, 2.0, 3.0, 4.0];
        double[] flatStress = [1.0, 1.0, 1.0, 1.0, 1.0];

        FungLogarithmicEstimationResult result = FungLogarithmicEstimator.Estimate(timePoints, flatStress);

        Assert.Equal(FungLogarithmicEstimator.DefaultC, result.InitialC);
        Assert.Equal(FungLogarithmicEstimator.DefaultFastRelaxationTime, result.FastRelaxationTime);
        Assert.Equal(FungLogarithmicEstimator.DefaultSlowRelaxationTime, result.SlowRelaxationTime);
    }

    [Fact]
    public void Estimate_GuaranteesPhysicalSeparation_Tau1LessThanTau2()
    {
        // Rapidly decaying mock data
        double[] timePoints = [0.0, 0.5, 1.0, 5.0, 10.0, 50.0, 100.0];
        double[] decayingStress = [1.0, 0.85, 0.78, 0.65, 0.58, 0.45, 0.40];

        FungLogarithmicEstimationResult result = FungLogarithmicEstimator.Estimate(timePoints, decayingStress);

        Assert.True(result.FastRelaxationTime > 0.0, "tau1 must be positive.");
        Assert.True(result.FastRelaxationTime <= FungLogarithmicEstimator.MaxFastRelaxationTime, "tau1 upper bound is respected.");
        Assert.True(result.SlowRelaxationTime >= FungLogarithmicEstimator.MinSlowRelaxationTime, "tau2 lower bound is respected.");
        Assert.True(result.FastRelaxationTime < result.SlowRelaxationTime, "tau1 must strictly be less than tau2.");
        Assert.True(result.InitialC > 0.0, "Stiffness C must be positive.");
        Assert.True(result.RSquared > 0.0, "R² should be positive for decaying stress.");
    }

    [Fact]
    public void Estimate_WithCustomTolerance_RespectsThreshold()
    {
        // Points where time or stress differences are extremely small
        double[] timePoints = [0.0, 1.0, 1.000000001, 1.000000002];
        double[] stress = [1.0, 0.9, 0.899999999, 0.899999998];

        // With a larger custom tolerance of 1e-4, the near-identical points are treated as zero variance
        FungLogarithmicEstimationResult resultWithHighTolerance = FungLogarithmicEstimator.Estimate(timePoints, stress, tolerance: 1e-4);

        // Fallback default is triggered because valid variance is within tolerance
        Assert.Equal(FungLogarithmicEstimator.DefaultC, resultWithHighTolerance.InitialC);
        Assert.Equal(FungLogarithmicEstimator.DefaultFastRelaxationTime, resultWithHighTolerance.FastRelaxationTime);
        Assert.Equal(FungLogarithmicEstimator.DefaultSlowRelaxationTime, resultWithHighTolerance.SlowRelaxationTime);
    }

    [Fact]
    public void RelaxationBounds_MatchPhysicalConstants()
    {
        Assert.Equal([FungLogarithmicEstimator.MinC, FungLogarithmicEstimator.MinFastRelaxationTime, FungLogarithmicEstimator.MinSlowRelaxationTime], FungLogarithmicEstimator.RelaxationLowerBounds);
        Assert.Equal([FungLogarithmicEstimator.MaxC, FungLogarithmicEstimator.MaxFastRelaxationTime, FungLogarithmicEstimator.MaxSlowRelaxationTime], FungLogarithmicEstimator.RelaxationUpperBounds);
        Assert.Equal([FungLogarithmicEstimator.DefaultC, FungLogarithmicEstimator.DefaultFastRelaxationTime, FungLogarithmicEstimator.DefaultSlowRelaxationTime], FungLogarithmicEstimator.DefaultInitialParameters);
    }
}
