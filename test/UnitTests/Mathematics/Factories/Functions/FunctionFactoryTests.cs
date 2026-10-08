using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;

namespace UnitTests.Mathematics.Factories.Functions;

public class FunctionFactoryTests
{
    [Theory]
    [InlineData(FunctionType.Constant, typeof(ConstantFunction))]
    [InlineData(FunctionType.Polynomial, typeof(PolynomialFunction))]
    [InlineData(FunctionType.Exponential, typeof(ExponentialFunction))]
    [InlineData(FunctionType.Sine, typeof(SineFunction))]
    [InlineData(FunctionType.Cosine, typeof(CosineFunction))]
    [InlineData(FunctionType.PowerLaw, typeof(PowerLaw))]
    [InlineData(FunctionType.Logarithmic, typeof(LogarithmicFunction))]
    public void Create_ShouldReturnCorrectFunctionType(FunctionType functionType, Type expectedType)
    {
        FunctionFactory factory = new();
        double[] coefficients = [1.0, 2.0];

        Function function = factory.Create(functionType, 0, 10, coefficients);

        Assert.NotNull(function);
        Assert.IsType(expectedType, function);
    }

    [Fact]
    public void Create_WithInvalidFunctionType_ShouldThrowArgumentOutOfRangeException()
    {
        FunctionFactory factory = new();
        double[] coefficients = [1.0, 2.0];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            factory.Create((FunctionType)9999, 0, 1, coefficients);
        });
    }
}
