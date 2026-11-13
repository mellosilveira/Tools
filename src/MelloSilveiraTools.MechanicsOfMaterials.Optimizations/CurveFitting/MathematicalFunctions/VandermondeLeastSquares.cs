using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

/// <summary>
/// Provides analytical least-squares fitting for polynomial functions using the Vandermonde matrix
/// and QR decomposition via Modified Gram-Schmidt orthogonalization.
/// </summary>
public static class VandermondeLeastSquares
{
    /// <summary>
    /// Constructs a rectangular Vandermonde matrix of size <c>N x M</c>.
    /// </summary>
    /// <param name="x">The independent variable data points of length <c>N</c>.</param>
    /// <param name="numberOfParameters">The number of polynomial parameters <c>M</c>.</param>
    /// <param name="zeroBased">
    /// When <see langword="true"/>, the constant term <c>c0 = 0</c> is omitted, generating powers <c>x^1, x^2, ..., x^M</c>;
    /// otherwise, generates standard powers <c>x^0, x^1, ..., x^{M-1}</c>.
    /// </param>
    /// <returns>A 2D array representing the Vandermonde matrix <c>V[N, M]</c>.</returns>
    private static double[,] BuildVandermondeMatrix(ReadOnlySpan<double> x, int numberOfParameters, bool zeroBased)
    {
        int n = x.Length;
        int m = numberOfParameters;
        double[,] v = new double[n, m];

        for (int i = 0; i < n; i++)
        {
            double xi = x[i];
            double currentPower = zeroBased ? xi : 1.0;
            v[i, 0] = currentPower;

            for (int j = 1; j < m; j++)
            {
                currentPower *= xi;
                v[i, j] = currentPower;
            }
        }

        return v;
    }

    /// <summary>
    /// Computes the analytical least-squares solution <c>c</c> minimizing <c>||V * c - y||^2</c>
    /// along with the diagonal elements of <c>(V^T * V)^-1</c> for parameter confidence interval estimation
    /// using QR decomposition with Modified Gram-Schmidt orthogonalization and triangular back-substitution.
    /// </summary>
    /// <param name="x">The independent variable values.</param>
    /// <param name="y">The dependent variable values.</param>
    /// <param name="numberOfParameters">The number of polynomial parameters.</param>
    /// <param name="zeroBased">Whether the polynomial has a forced zero intercept (<c>c0 = 0</c>).</param>
    /// <param name="tolerance">The tolerance for numerical stability.</param>
    /// <returns>
    /// A tuple containing the optimal parameter vector and the diagonal elements of <c>(V^T * V)^-1</c>,
    /// or <see langword="null"/> if the matrix is rank-deficient or singular.
    /// </returns>
    public static (double[] Parameters, double[] DiagonalInvJtJ)? Fit(
        ReadOnlySpan<double> x,
        ReadOnlySpan<double> y,
        int numberOfParameters,
        bool zeroBased,
        double tolerance = MathematicConstants.Tolerance)
    {
        int n = x.Length;
        int m = numberOfParameters;

        if (n < m || m <= 0 || y.Length != n)
        {
            return null;
        }

        double[,] v = BuildVandermondeMatrix(x, m, zeroBased);
        double[,] q = (double[,])v.Clone();
        double[,] r = new double[m, m];

        // Modified Gram-Schmidt QR decomposition
        for (int j = 0; j < m; j++)
        {
            for (int k = 0; k < j; k++)
            {
                double dot = 0.0;
                for (int i = 0; i < n; i++)
                {
                    dot += q[i, k] * q[i, j];
                }

                r[k, j] = dot;
                for (int i = 0; i < n; i++)
                {
                    q[i, j] -= dot * q[i, k];
                }
            }

            double normSq = 0.0;
            for (int i = 0; i < n; i++)
            {
                normSq += q[i, j] * q[i, j];
            }

            double norm = Math.Sqrt(normSq);
            if (norm < tolerance || double.IsNaN(norm))
            {
                return null;
            }

            r[j, j] = norm;
            double invNorm = 1.0 / norm;
            for (int i = 0; i < n; i++)
            {
                q[i, j] *= invNorm;
            }
        }

        // Compute d = Q^T * y
        double[] d = new double[m];
        for (int j = 0; j < m; j++)
        {
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                sum += q[i, j] * y[i];
            }
            d[j] = sum;
        }

        // Back-substitution: R * c = d
        double[] c = new double[m];
        for (int j = m - 1; j >= 0; j--)
        {
            double sum = d[j];
            for (int k = j + 1; k < m; k++)
            {
                sum -= r[j, k] * c[k];
            }

            if (Math.Abs(r[j, j]) < 1e-15)
            {
                return null;
            }

            c[j] = sum / r[j, j];
        }

        for (int j = 0; j < m; j++)
        {
            if (double.IsNaN(c[j]) || double.IsInfinity(c[j]))
            {
                return null;
            }
        }

        // Invert upper triangular matrix R to obtain (V^T * V)^-1 = R^-1 * (R^-1)^T
        double[,] invR = new double[m, m];
        for (int col = 0; col < m; col++)
        {
            for (int row = col; row >= 0; row--)
            {
                double sum = (row == col) ? 1.0 : 0.0;
                for (int k = row + 1; k <= col; k++)
                {
                    sum -= r[row, k] * invR[k, col];
                }

                invR[row, col] = sum / r[row, row];
            }
        }

        double[] diagonalInvJtJ = new double[m];
        for (int j = 0; j < m; j++)
        {
            double sum = 0.0;
            for (int k = j; k < m; k++)
            {
                sum += invR[j, k] * invR[j, k];
            }

            diagonalInvJtJ[j] = sum;
        }

        return (c, diagonalInvJtJ);
    }
}
