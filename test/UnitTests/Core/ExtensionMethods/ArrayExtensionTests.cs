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
}
