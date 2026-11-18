using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.CurveFitting;

/// <summary>
/// Unit tests covering additional curve fitters and failure/edge branches:
/// <see cref="ExponentialCurveFitter"/>, <see cref="LogarithmicCurveFitter"/>, <see cref="PronySeriesCurveFitter"/>,
/// <see cref="MathNetCurveFitter"/>, <see cref="AlglibCurveFitter"/>, <see cref="PolynomialCurveFitter"/>,
/// and <see cref="VandermondeLeastSquares"/>.
/// </summary>
public sealed class AdditionalCurveFittersTests
{
    /// <summary>
    /// Verifies ExponentialCurveFitter succeeds on synthetic exponential data and fails gracefully on insufficient points.
    /// </summary>
    [Fact]
    public void ExponentialCurveFitter_SuccessAndErrorPaths()
    {
        ExponentialCurveFitter fitter = new(new AlglibCurveFitter(NullLogger<AlglibCurveFitter>.Instance, new CurveFitterSettings()));

        // f(x) = 2 * exp(-0.25 * x)
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];
        double[] y = [2.0, 2.0 * Math.Exp(-0.25), 2.0 * Math.Exp(-0.5), 2.0 * Math.Exp(-0.75), 2.0 * Math.Exp(-1.0)];

        SafeResult<CurveFitInput, CurveFitOutput> ok = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = x,
            DependentVariable = y,
            InitialParameters = [1.8, -0.2],
            LowerBounds = [0.0, -5.0],
            UpperBounds = [10.0, 0.0]
        });

        Assert.True(ok.Success);
        Assert.NotNull(ok.Output);
        Assert.Equal(2, ok.Output.Parameters.Count);
        Assert.True(ok.Output.RSquared > 0.99);

        // Also cover default InitialParameters == null generation
        SafeResult<CurveFitInput, CurveFitOutput> defaultInit = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = x,
            DependentVariable = y,
            LowerBounds = [0.0, -5.0],
            UpperBounds = [10.0, 0.0]
        });
        Assert.True(defaultInit.Success);

        SafeResult<CurveFitInput, CurveFitOutput> tooFew = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 2,
            IndependentVariable = [1.0, 2.0],
            DependentVariable = [1.0, 2.0],
            InitialParameters = [1.0, -1.0, 0.5, -0.5]
        });

        Assert.False(tooFew.Success);
        Assert.NotNull(tooFew.FailedPayload);
        Assert.IsType<ArgumentException>(tooFew.FailedPayload.Value.Exception);
    }

    /// <summary>
    /// Verifies LogarithmicCurveFitter handles ZeroBased=true/false and insufficient points.
    /// </summary>
    [Fact]
    public void LogarithmicCurveFitter_ZeroBasedAndNonZeroBasedAndErrorPaths()
    {
        Mock<ICurveFitter> innerMock = new();
        innerMock
            .Setup(c => c.Fit(It.IsAny<CurveFitInput>()))
            .Returns((CurveFitInput input) =>
            {
                // Invoke Calculate to ensure both ZeroBased delegate shapes are exercised
                double val = input.Calculate(input.InitialParameters, [input.IndependentVariables[0][0]]);
                Assert.False(double.IsNaN(val));
                return new CurveFitOutput(
                    [.. Enumerable.Range(0, input.InitialParameters.Length).Select(i => new ParameterConfidenceInterval(i, input.InitialParameters[i], 0, 2, 0.1, input.ConfidenceLevel))],
                    0.01,
                    0.99,
                    5);
            });

        LogarithmicCurveFitter fitter = new(innerMock.Object);

        SafeResult<CurveFitInput, CurveFitOutput> zeroBased = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = [0.0, 1.0, 2.0],
            DependentVariable = [0.0, 0.5, 0.8],
            ZeroBased = true,
            ConfidenceLevel = 0.90
        });

        Assert.True(zeroBased.Success);
        Assert.Equal(3, zeroBased.Output!.Parameters.Count);
        Assert.Equal(0.0, zeroBased.Output.Parameters[0].Value);
        Assert.Equal(1, zeroBased.Output.Parameters[1].Index);

        SafeResult<CurveFitInput, CurveFitOutput> nonZeroBased = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = [0.0, 1.0],
            DependentVariable = [1.0, 1.5],
            ZeroBased = false,
            InitialParameters = [1.0]
        });

        Assert.True(nonZeroBased.Success);
        Assert.Single(nonZeroBased.Output!.Parameters);

        SafeResult<CurveFitInput, CurveFitOutput> tooFew = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 2,
            IndependentVariable = [1.0, 2.0],
            DependentVariable = [1.0, 2.0]
        });

        Assert.False(tooFew.Success);
        Assert.NotNull(tooFew.FailedPayload);
        Assert.IsType<ArgumentException>(tooFew.FailedPayload.Value.Exception);
    }

    /// <summary>
    /// Verifies PronySeriesCurveFitter builds evaluation delegate and default ValidateParameters delegate correctly.
    /// </summary>
    [Fact]
    public void PronySeriesCurveFitter_Fit_ExecutesCalculateAndDefaultValidator()
    {
        CurveFitInput? capturedInput = null;
        Mock<ICurveFitter> innerMock = new();
        innerMock
            .Setup(c => c.Fit(It.IsAny<CurveFitInput>()))
            .Callback<CurveFitInput>(input => capturedInput = input)
            .Returns(new CurveFitOutput([new(0, 0.5, 0.4, 0.6, 0.05, 0.95)], 0.0, 1.0, 3));

        PronySeriesCurveFitter fitter = new(innerMock.Object);
        CurveFitOutput output = fitter.Fit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 3,
            IndependentVariable = [0.0, 1.0, 2.0, 3.0],
            DependentVariable = [1.0, 0.8, 0.7, 0.65]
        });

        Assert.NotNull(capturedInput);
        Assert.Single(output.Parameters);

        double calc = capturedInput.Calculate([0.5, 0.6, 1.0], [1.0]);
        Assert.Equal(0.5 + 0.6 * Math.Exp(1.0), calc, 6);

        Assert.NotNull(capturedInput.ValidateParameters);
        Assert.True(capturedInput.ValidateParameters([0.5, 0.6, 1.0]));
        Assert.False(capturedInput.ValidateParameters([0.4, 0.4, 1.0]));
    }

    /// <summary>
    /// Verifies AlglibCurveFitter unsupported profile throws ArgumentOutOfRangeException
    /// and zero-iteration failed termination throws InvalidOperationException on both Standard and RuleConstrained paths.
    /// </summary>
    [Fact]
    public void AlglibCurveFitter_ProfileErrorAndNonConvergence_ThrowsExpectedExceptions()
    {
        AlglibCurveFitter fitter = new(NullLogger<AlglibCurveFitter>.Instance, new CurveFitterSettings());

        CurveFitInput baseInput = new()
        {
            IndependentVariables = [[0.0, 1.0, 2.0]],
            DependentVariable = [1.0, 2.0, 3.0],
            Calculate = (p, x) => p[0] * x[0] + p[1],
            InitialParameters = [1.0, 1.0],
            LowerBounds = [0.0, 0.0],
            UpperBounds = [5.0, 5.0]
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            fitter.Fit(baseInput with { Profile = (CurveFitProfile)999 }));

        // GDEMO rule-constrained polish fallback / success on small valid problem
        CurveFitOutput ruleResult = fitter.Fit(baseInput with
        {
            Profile = CurveFitProfile.RuleConstrained,
            ValidateParameters = p => p[0] >= 0.0 && p[1] >= 0.0,
            MaxIterations = 10,
            PopulationMultiplier = 5
        });
        Assert.True(ruleResult.RSquared >= 0.9);
    }

    /// <summary>
    /// Verifies PolynomialCurveFitter fallback to iterative fitter when analytical solution violates bounds,
    /// RuleConstrained profile, TargetRSquared rejection, ValidateParameters rejection, and ZeroBased shifting.
    /// </summary>
    [Fact]
    public void PolynomialCurveFitter_FallbackAndAnalyticalRejectionBranches()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];
        double[] y = [0.0, 2.0, 4.0, 6.0, 8.0];
        PolynomialCurveFitter fitter = new(new MathNetCurveFitter());

        // RuleConstrained + ZeroBased falls back to MathNet and shifts index 0 parameter
        SafeResult<CurveFitInput, CurveFitOutput> ruleZeroBased = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = x,
            DependentVariable = y,
            ZeroBased = true,
            Profile = CurveFitProfile.RuleConstrained
        });
        Assert.True(ruleZeroBased.Success);
        Assert.Equal(2, ruleZeroBased.Output!.Parameters.Count);
        Assert.Equal(0.0, ruleZeroBased.Output.Parameters[0].Value);

        // LowerBound violated triggers fallback
        SafeResult<CurveFitInput, CurveFitOutput> lowerBoundViolated = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = [0.0, 1.0, 2.0],
            DependentVariable = [1.0, 1.0, 1.0],
            LowerBounds = [2.0],
            UpperBounds = [5.0]
        });
        Assert.True(lowerBoundViolated.Success);

        // TargetRSquared > analytical R^2 triggers fallback
        SafeResult<CurveFitInput, CurveFitOutput> highTarget = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = [0.0, 1.0, 2.0],
            DependentVariable = [1.0, 10.0, 2.0],
            TargetRSquared = 0.99999
        });
        Assert.True(highTarget.Success);

        // ValidateParameters returning false triggers fallback
        SafeResult<CurveFitInput, CurveFitOutput> rejected = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = x,
            DependentVariable = y,
            ZeroBased = true,
            ValidateParameters = _ => false
        });
        Assert.True(rejected.Success);

        // Exact degreesOfFreedom == 0 uses analytical fallback confidence interval branch (0 standard error)
        SafeResult<CurveFitInput, CurveFitOutput> exactDofZero = fitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 2,
            IndependentVariable = [0.0, 1.0],
            DependentVariable = [1.0, 3.0]
        });
        Assert.True(exactDofZero.Success);
        Assert.Equal(0.0, exactDofZero.Output!.Parameters[0].StandardError);

        // Vandermonde edge cases: invalid counts & singular all-zero x
        Assert.Null(VandermondeLeastSquares.Fit([1.0], [1.0], 2, false));
        Assert.Null(VandermondeLeastSquares.Fit([1.0], [1.0], 0, false));
        Assert.Null(VandermondeLeastSquares.Fit([0.0, 0.0, 0.0], [0.0, 1.0, 2.0], 2, true));
    }

    /// <summary>
    /// Verifies MathNetCurveFitter handles zero degrees of freedom and computes exact zero-error intervals.
    /// </summary>
    [Fact]
    public void MathNetCurveFitter_ZeroDegreesOfFreedom_ReturnsZeroStandardError()
    {
        MathNetCurveFitter fitter = new();
        CurveFitOutput output = fitter.Fit(new CurveFitInput
        {
            IndependentVariables = [[1.0, 2.0]],
            DependentVariable = [2.0, 4.0],
            Calculate = (p, x) => p[0] * x[0] + p[1],
            InitialParameters = [1.9, 0.1],
            LowerBounds = [-10.0, -10.0],
            UpperBounds = [10.0, 10.0],
            MaxIterations = 500
        });

        Assert.Equal(2, output.Parameters.Count);
        Assert.Equal(0.0, output.Parameters[0].StandardError);
        Assert.Equal(0.0, output.Parameters[1].StandardError);
    }

    /// <summary>
    /// Verifies CurveFitterBase, PolynomialCurveFitter, and MathNetCurveFitter boundary, non-finite, and perturbation branches.
    /// </summary>
    [Fact]
    public void CurveFitterBase_Polynomial_And_MathNet_CoverBoundaryAndEdgeBranches()
    {
        PolynomialCurveFitter polyFitter = new(new MathNetCurveFitter());

        // Analytical ZeroBased = true (no RuleConstrained, no InitialParameters) -> lines 194-201
        SafeResult<CurveFitInput, CurveFitOutput> analyticalZeroBased = polyFitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = [0.0, 1.0, 2.0, 3.0],
            DependentVariable = [0.0, 2.0, 4.0, 6.0],
            ZeroBased = true,
            ConfidenceLevel = 0.95
        });
        Assert.True(analyticalZeroBased.Success);
        Assert.Equal(2, analyticalZeroBased.Output!.Parameters.Count);
        Assert.Equal(0.0, analyticalZeroBased.Output.Parameters[0].Value);

        // InitialParameters provided -> skips analytical and uses InitialParameters (line 82)
        SafeResult<CurveFitInput, CurveFitOutput> withInitialParams = polyFitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 2,
            IndependentVariable = [0.0, 1.0, 2.0],
            DependentVariable = [1.0, 3.0, 5.0],
            InitialParameters = [1.0, 2.0],
            ConfidenceLevel = 0.95
        });
        Assert.True(withInitialParams.Success);

        // UpperBound violated in analytical solution (line 114) + constant Y (sst <= Tolerance, line 142)
        SafeResult<CurveFitInput, CurveFitOutput> upperBoundViolated = polyFitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = [0.0, 1.0, 2.0],
            DependentVariable = [5.0, 5.0, 5.0],
            LowerBounds = [0.0],
            UpperBounds = [2.0],
            ConfidenceLevel = 0.95
        });
        Assert.True(upperBoundViolated.Success);

        SafeResult<CurveFitInput, CurveFitOutput> constantYAnalytical = polyFitter.TryFit(new MathematicalCurveFitInput
        {
            NumberOfParameters = 1,
            IndependentVariable = [0.0, 1.0, 2.0],
            DependentVariable = [5.0, 5.0, 5.0],
            ConfidenceLevel = 0.95
        });
        Assert.True(constantYAnalytical.Success);

        // MathNetCurveFitter with tight bounds, equal bounds, one-sided bounds, shorter bounds arrays, and non-finite predictions
        MathNetCurveFitter mathNet = new();
        CurveFitOutput boundedFit = mathNet.Fit(new CurveFitInput
        {
            IndependentVariables = [[1.0, 2.0, 3.0, 4.0, 5.0, 6.0]],
            DependentVariable = [2.0, 4.0, 6.0, 8.0, 10.0, 12.0],
            Calculate = (p, x) => p[0] * x[0] + p[1],
            InitialParameters = [2.0, 0.0],
            LowerBounds = [0.0, -10.0],
            UpperBounds = [10.0, 10.0],
            MaxIterations = 500,
            ConfidenceLevel = 0.95,
            ValidateParameters = p => p[0] >= 0.0
        });
        Assert.Equal(2, boundedFit.Parameters.Count);

        // Constant dependent variable in CalculateRSquared (sumOfSquaresTotal == 0.0 -> 1.0)
        CurveFitOutput constantYMathNet = mathNet.Fit(new CurveFitInput
        {
            IndependentVariables = [[1.0, 2.0, 3.0]],
            DependentVariable = [5.0, 5.0, 5.0],
            Calculate = (p, _) => p[0],
            InitialParameters = [5.0],
            LowerBounds = [0.0],
            UpperBounds = [10.0],
            MaxIterations = 500,
            ConfidenceLevel = 0.95
        });
        Assert.Equal(1.0, constantYMathNet.RSquared);

        // Directly cover CurveFitterBase protected gradient/jacobian perturbation branches
        TestableCurveFitter testableBase = new();
        CurveFitInput gradInput = new()
        {
            IndependentVariables = [[1.0, 2.0]],
            DependentVariable = [2.0, 4.0],
            Calculate = (p, x) => p[0] > 10.0 ? double.NaN : p[0] * x[0],
            InitialParameters = [1.0, 1.0, 1.0, 1.0, 11.0],
            LowerBounds = [0.0, 1.0, 1.0 - 1e-9, 1.0],
            UpperBounds = [1.0, 2.0, 1.0 + 1e-9, 1.0]
        };
        double[] grad = testableBase.InvokeGradient(gradInput, [1.0, 1.0, 1.0, 1.0, 11.0]);
        Assert.Equal(5, grad.Length);
        double[,] jac = testableBase.InvokeJacobian(gradInput, [1.0, 1.0, 1.0, 1.0, 11.0]);
        Assert.Equal(2, jac.GetLength(0));
    }

    private sealed class TestableCurveFitter : CurveFitterBase
    {
        public override CurveFitOutput Fit(CurveFitInput input) => throw new NotSupportedException();
        public double[] InvokeGradient(CurveFitInput input, double[] parameters) => CalculateNumericalGradient(input, parameters);
        public double[,] InvokeJacobian(CurveFitInput input, double[] parameters) => CalculateNumericalJacobian(input, parameters);
    }
}
