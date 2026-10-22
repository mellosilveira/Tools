using MelloSilveiraTools.Core.ExtensionMethods;

namespace UnitTests.Core.ExtensionMethods;

public class ArrayExtensionTests
{
    [Fact]
    public void CalculateNorm_ShouldReturnEuclideanLength()
    {
        double[] vector = [3.0, 4.0];

        double norm = vector.CalculateNorm();

        Assert.Equal(5.0, norm, precision: 5);
    }

    [Fact]
    public void NormalizeVector_ShouldReturnUnitLengthVector()
    {
        double[] vector = [3.0, 4.0];

        double[] normalized = vector.NormalizeVector();

        Assert.Equal(0.6, normalized[0], precision: 5);
        Assert.Equal(0.8, normalized[1], precision: 5);
        Assert.Equal(1.0, normalized.CalculateNorm(), precision: 5);
    }

    [Fact]
    public void InverseMatrix_2x2_ShouldReturnCorrectInverse()
    {
        double[,] matrix = new double[,]
        {
            { 4.0, 7.0 },
            { 2.0, 6.0 }
        };

        double[,] inverse = matrix.InverseMatrix();

        Assert.Equal(0.6, inverse[0, 0], precision: 4);
        Assert.Equal(-0.7, inverse[0, 1], precision: 4);
        Assert.Equal(-0.2, inverse[1, 0], precision: 4);
        Assert.Equal(0.4, inverse[1, 1], precision: 4);
    }

    [Fact]
    public void Multiply_MatrixAndVector_ComputesMatrixVectorProduct()
    {
        double[,] matrix = new double[,]
        {
            { 1.0, 2.0 },
            { 3.0, 4.0 }
        };
        double[] vector = [5.0, 6.0];

        double[] result = matrix.Multiply(vector);

        // [1*5 + 2*6, 3*5 + 4*6] = [17, 39]
        Assert.Equal(2, result.Length);
        Assert.Equal(17.0, result[0], precision: 5);
        Assert.Equal(39.0, result[1], precision: 5);
    }

    [Fact]
    public void Multiply_MatrixAndVector_DimensionMismatch_ThrowsArgumentOutOfRangeException()
    {
        double[,] matrix = new double[2, 3];
        double[] vector = [1.0, 2.0];

        Assert.Throws<ArgumentOutOfRangeException>(() => matrix.Multiply(vector));
    }

    [Fact]
    public void Multiply_VectorAndScalar_ScalesEachElement()
    {
        double[] vector = [1.5, -2.0, 3.0];

        double[] result = vector.Multiply(2.0);

        Assert.Equal([3.0, -4.0, 6.0], result);
    }

    [Fact]
    public void Sum_TwoVectors_AddsElementwise()
    {
        double[] v1 = [1.0, 2.0, 3.0];
        double[] v2 = [4.0, 5.0, 6.0];

        double[] result = v1.Sum(v2);

        Assert.Equal([5.0, 7.0, 9.0], result);
    }

    [Fact]
    public void Subtract_TwoVectors_SubtractsElementwise()
    {
        double[] v1 = [10.0, 20.0, 30.0];
        double[] v2 = [4.0, 5.0, 6.0];

        double[] result = v1.Subtract(v2);

        Assert.Equal([6.0, 15.0, 24.0], result);
    }

    [Fact]
    public void Sum_MultipleVectors_AddsAllElementwise()
    {
        double[] baseVec = [1.0, 2.0];
        double[] v1 = [10.0, 20.0];
        double[] v2 = [100.0, 200.0];

        double[] result = baseVec.Sum(v1, v2);

        Assert.Equal([111.0, 222.0], result);
    }

    [Fact]
    public void Subtract_ThreeVectors_SubtractsInSequence()
    {
        double[] v1 = [100.0, 200.0];
        double[] v2 = [10.0, 20.0];
        double[] v3 = [1.0, 2.0];

        double[] result = v1.Subtract(v2, v3);

        Assert.Equal([89.0, 178.0], result);
    }

    [Fact]
    public void MathOperation_DifferentLengths_ThrowsArgumentOutOfRangeException()
    {
        double[] v1 = [1.0, 2.0];
        double[] v2 = [1.0, 2.0, 3.0];

        Assert.Throws<ArgumentOutOfRangeException>(() => v1.MathOperation((a, b) => a + b, v2));
    }
}
