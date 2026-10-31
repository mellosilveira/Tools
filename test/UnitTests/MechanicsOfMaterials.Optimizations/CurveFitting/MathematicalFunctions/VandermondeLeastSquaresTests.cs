using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

namespace UnitTests.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public class VandermondeLeastSquaresTests
{
    [Fact]
    public void Fit_LinearPolynomial_ShouldRecoverExactCoefficients()
    {
        // y = 3 + 2x
        double[] x = [1.0, 2.0, 3.0, 4.0, 5.0];
        double[] y = [5.0, 7.0, 9.0, 11.0, 13.0];

        double[]? coefficients = VandermondeLeastSquares.Fit(x, y, 2, zeroBased: false);

        Assert.NotNull(coefficients);
        Assert.Equal(2, coefficients.Length);
        Assert.Equal(3.0, coefficients[0], precision: 6);
        Assert.Equal(2.0, coefficients[1], precision: 6);
    }

    [Fact]
    public void Fit_QuadraticPolynomial_ShouldRecoverExactCoefficients()
    {
        // y = 1 - 2x + 4x^2
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0];
        double[] y = [1.0, 3.0, 13.0, 31.0, 57.0, 91.0];

        double[]? coefficients = VandermondeLeastSquares.Fit(x, y, 3, zeroBased: false);

        Assert.NotNull(coefficients);
        Assert.Equal(3, coefficients.Length);
        Assert.Equal(1.0, coefficients[0], precision: 6);
        Assert.Equal(-2.0, coefficients[1], precision: 6);
        Assert.Equal(4.0, coefficients[2], precision: 6);
    }

    [Fact]
    public void Fit_ZeroBasedPolynomial_ShouldOmitConstantTerm()
    {
        // y = 5x + 3x^2 (c0 = 0)
        double[] x = [1.0, 2.0, 3.0, 4.0, 5.0];
        double[] y = [8.0, 22.0, 42.0, 68.0, 100.0];

        double[]? coefficients = VandermondeLeastSquares.Fit(x, y, 2, zeroBased: true);

        Assert.NotNull(coefficients);
        Assert.Equal(2, coefficients.Length);
        Assert.Equal(5.0, coefficients[0], precision: 6);
        Assert.Equal(3.0, coefficients[1], precision: 6);
    }

    [Fact]
    public void Fit_InsufficientPoints_ShouldReturnNull()
    {
        double[] x = [1.0, 2.0];
        double[] y = [3.0, 4.0];

        double[]? coefficients = VandermondeLeastSquares.Fit(x, y, 3, zeroBased: false);

        Assert.Null(coefficients);
    }

    [Fact]
    public void Fit_ColinearOrIdenticalPoints_ShouldReturnNull()
    {
        double[] x = [2.0, 2.0, 2.0];
        double[] y = [1.0, 2.0, 3.0];

        double[]? coefficients = VandermondeLeastSquares.Fit(x, y, 2, zeroBased: false);

        Assert.Null(coefficients);
    }
}
