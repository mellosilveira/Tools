using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;

namespace UnitTests.Mathematics.Functions;

/// <summary>
/// Unit tests for all <see cref="Function"/> implementations covering evaluation, derivatives, integrals, and conversions.
/// </summary>
public sealed class MathematicalFunctionsTests
{
    [Fact]
    public void FunctionImplicitIntConversion_CreatesConstantFunction()
    {
        Function fn = 7;

        ConstantFunction constant = Assert.IsType<ConstantFunction>(fn);
        Assert.Equal(FunctionType.Constant, constant.FunctionType);
        Assert.Equal(double.NegativeInfinity, constant.InitialVariableValue);
        Assert.Equal(double.PositiveInfinity, constant.FinalVariableValue);
        Assert.Equal(7.0, constant.Calculate(123.0));
    }

    [Fact]
    public void ConstantFunction_DerivativeAndIntegral_BehaveAsExpectedForZeroAndNonZero()
    {
        ConstantFunction zero = new(0.0, 0.0, 5.0);
        Assert.Equal(0.0, zero.Derivative.Calculate(2.0));
        Assert.IsType<ConstantFunction>(zero.Integral);
        Assert.Equal(0.0, zero.Integral.Calculate(2.0));
        // Cached property returns same instance.
        Assert.Same(zero.Derivative, zero.Derivative);
        Assert.Same(zero.Integral, zero.Integral);

        ConstantFunction nonZero = new(3.0, 0.0, 5.0);
        Assert.Equal(0.0, nonZero.Derivative.Calculate(2.0));
        PolynomialFunction integral = Assert.IsType<PolynomialFunction>(nonZero.Integral);
        Assert.Equal(6.0, integral.Calculate(2.0));
    }

    [Fact]
    public void CosineAndSineFunctions_CalculateDerivativeAndIntegral_MatchImplementation()
    {
        double[] coeffs = [2.0, 3.0, 0.5];
        double x = 1.2;

        CosineFunction cos = new(coeffs, 0.0, 10.0);
        Assert.Equal(FunctionType.Cosine, cos.FunctionType);
        Assert.Equal(2.0 * Math.Cos(3.0 * (x + 0.5)), cos.Calculate(x), 10);
        Assert.IsType<SineFunction>(cos.Derivative);
        Assert.Equal(-6.0 * Math.Sin(3.0 * (x + 0.5)), cos.Derivative.Calculate(x), 10);
        Assert.IsType<SineFunction>(cos.Integral);
        Assert.Equal((2.0 / 3.0) * Math.Sin(3.0 * (x + 0.5)), cos.Integral.Calculate(x), 10);

        SineFunction sin = new(coeffs, 0.0, 10.0);
        Assert.Equal(FunctionType.Sine, sin.FunctionType);
        Assert.Equal(2.0 * Math.Sin(3.0 * (x + 0.5)), sin.Calculate(x), 10);
        Assert.IsType<CosineFunction>(sin.Derivative);
        Assert.Equal(6.0 * Math.Cos(3.0 * (x + 0.5)), sin.Derivative.Calculate(x), 10);
        Assert.IsType<CosineFunction>(sin.Integral);
        Assert.Equal(-(2.0 / 3.0) * Math.Cos(3.0 * (x + 0.5)), sin.Integral.Calculate(x), 10);
    }

    [Fact]
    public void ExponentialFunction_CalculatesDerivativeAndIntegralCorrectly()
    {
        ExponentialFunction exp = new([2.0, -0.5, 1.5, 2.0], 0.0, 5.0);
        double x = 1.0;

        Assert.Equal(FunctionType.Exponential, exp.FunctionType);
        Assert.Equal(2.0 * Math.Exp(-0.5) + 1.5 * Math.Exp(2.0), exp.Calculate(x), 10);
        Assert.Equal(-1.0 * Math.Exp(-0.5) + 3.0 * Math.Exp(2.0), exp.Derivative.Calculate(x), 10);
        Assert.Equal(-4.0 * Math.Exp(-0.5) + 0.75 * Math.Exp(2.0), exp.Integral.Calculate(x), 10);
    }

    [Fact]
    public void LogarithmicFunction_CalculatesDerivativeAndIntegralCorrectly()
    {
        LogarithmicFunction log = new([1.0, 2.0, 3.0, 4.0, 5.0], 0.1, 10.0);
        double x = 2.0;

        double expected = 1.0 + 2.0 * Math.Log(3.0 * x) + 4.0 * Math.Log(5.0 * x);
        Assert.Equal(FunctionType.Logarithmic, log.FunctionType);
        Assert.Equal(expected, log.Calculate(x), 10);

        Assert.Equal((2.0 + 4.0) / x, log.Derivative.Calculate(x), 10);

        double expectedIntegral = x * (1.0 + 2.0 * (Math.Log(3.0 * x) - 1.0) + 4.0 * (Math.Log(5.0 * x) - 1.0));
        Assert.Equal(expectedIntegral, log.Integral.Calculate(x), 10);
    }

    [Fact]
    public void PolynomialFunction_HandlesDegreeZeroOneAndHigherForDerivativeAndIntegral()
    {
        PolynomialFunction zeroPoly = new([0.0]);
        Assert.Equal(0.0, zeroPoly.Calculate(5.0));
        Assert.IsType<ConstantFunction>(zeroPoly.Derivative);
        Assert.Equal(0.0, zeroPoly.Derivative.Calculate(5.0));
        Assert.Single(zeroPoly.Integral.Coefficients);
        Assert.Equal(0.0, zeroPoly.Integral.Calculate(5.0));

        PolynomialFunction linear = new([2.0, 5.0]);
        ConstantFunction linearDeriv = Assert.IsType<ConstantFunction>(linear.Derivative);
        Assert.Equal(5.0, linearDeriv.Calculate(10.0));

        PolynomialFunction cubic = new([1.0, 2.0, 3.0, 4.0], 0.0, 5.0);
        // 1 + 2*(2) + 3*(4) + 4*(8) = 49
        Assert.Equal(49.0, cubic.Calculate(2.0), 10);
        // Derivative: 2 + 6x + 12x^2 -> at x=2: 2 + 12 + 48 = 62
        PolynomialFunction cubicDeriv = Assert.IsType<PolynomialFunction>(cubic.Derivative);
        Assert.Equal(62.0, cubicDeriv.Calculate(2.0), 10);
        // Integral: x + x^2 + x^3 + x^4 -> at x=2: 2 + 4 + 8 + 16 = 30
        PolynomialFunction cubicInteg = Assert.IsType<PolynomialFunction>(cubic.Integral);
        Assert.Equal(30.0, cubicInteg.Calculate(2.0), 10);
    }

    [Fact]
    public void PowerLaw_ValidatesCoefficientLengthAndExecutesDerivativeAndIntegral()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PowerLaw([1.0]));

        PowerLaw p1 = new([2.0, 3.0], 1.0, 5.0);
        Assert.Equal(2.0 * Math.Pow(2.0, -3.0), p1.Calculate(2.0), 10);

        // Derivative of 2 * x^(-3) is -6 * x^(-4); integral is -1 * x^(-2).
        Function deriv = p1.Derivative;
        Assert.Equal([-6.0, 4.0], deriv.Coefficients);
        Assert.Equal(-6.0 * Math.Pow(2.0, -4.0), deriv.Calculate(2.0), 10);

        Function integ = p1.Integral;
        Assert.Equal([-1.0, 2.0], integ.Coefficients);
        Assert.Equal(-1.0 * Math.Pow(2.0, -2.0), integ.Calculate(2.0), 10);

        // Special case a_1 = 1: integral of 3 * x^(-1) is 3 * ln(x).
        PowerLaw reciprocal = new([3.0, 1.0], 1.0, 5.0);
        Function reciprocalIntegral = reciprocal.Integral;
        Assert.IsType<LogarithmicFunction>(reciprocalIntegral);
        Assert.Equal(3.0 * Math.Log(2.0), reciprocalIntegral.Calculate(2.0), 10);

        // Special case a_1 = 0: derivative is PowerLaw([0.0, 1.0]) which evaluates to 0.
        PowerLaw constantLike = new([3.0, 0.0], 0.0, 5.0);
        Function zeroDerivative = constantLike.Derivative;
        Assert.IsType<PowerLaw>(zeroDerivative);
        Assert.Equal([0.0, 1.0], zeroDerivative.Coefficients);
        Assert.Equal(0.0, zeroDerivative.Calculate(2.0));
    }

    [Fact]
    public void GenericFunction_EvaluatesDelegatesAndThrowsWhenDerivativeOrIntegralMissing()
    {
        GenericFunction full = new(0.0, 5.0, x => x * x, x => 2.0 * x, x => x * x * x / 3.0);
        Assert.Equal(9.0, full.Calculate(3.0));
        Assert.Equal(6.0, full.Derivative.Calculate(3.0));
        Assert.Equal(9.0, full.Integral.Calculate(3.0));

        GenericFunction bare = new(0.0, 5.0, x => x);
        Assert.Throws<ArgumentNullException>(() => bare.Derivative);
        Assert.Throws<ArgumentNullException>(() => bare.Integral);
    }
}
