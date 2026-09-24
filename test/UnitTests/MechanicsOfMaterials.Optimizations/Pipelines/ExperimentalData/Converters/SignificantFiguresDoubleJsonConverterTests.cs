using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using System.Text.Json;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;

public class SignificantFiguresDoubleJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public SignificantFiguresDoubleJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new SignificantFiguresDoubleJsonConverter(7));
    }

    [Theory]
    // Ruído de ponto flutuante (perto de 2)
    [InlineData(1.9999999999, 2.0)]
    [InlineData(2.0000000001, 2.0)]
    
    // Valores pequenos em notação científica (exemplo do usuário)
    [InlineData(3.50000000001E-07, 3.5E-07)]
    [InlineData(1.23456789E-12, 1.234568E-12)]
    
    // Valores muito grandes
    [InlineData(1.23456789E+20, 1.234568E+20)]
    
    // Valores exatos sem ruído
    [InlineData(2.5, 2.5)]
    [InlineData(-150.0, -150.0)]
    [InlineData(0.0, 0.0)]
    
    // Teste de truncamento com exatamente 7 algarismos significativos
    [InlineData(1234.56789, 1234.568)] // arredonda no 7º algarismo
    [InlineData(-0.000123456789, -0.0001234568)]
    public void Should_Cleanse_FloatingPoint_Noise_When_Serializing(double input, double expectedOutput)
    {
        // Act
        string json = JsonSerializer.Serialize(input, _options);
        
        // Assert
        string expectedJson = JsonSerializer.Serialize(expectedOutput);
        Assert.Equal(expectedJson, json);

        double deserialized = JsonSerializer.Deserialize<double>(json, _options);
        Assert.Equal(expectedOutput, deserialized);
    }

    [Fact]
    public void Should_Handle_NaN_And_Infinity_Correctly()
    {
        // Act
        string jsonNaN = JsonSerializer.Serialize(double.NaN, _options);
        string jsonPosInf = JsonSerializer.Serialize(double.PositiveInfinity, _options);
        string jsonNegInf = JsonSerializer.Serialize(double.NegativeInfinity, _options);

        // Assert
        Assert.Equal("\"NaN\"", jsonNaN);
        Assert.Equal("\"Infinity\"", jsonPosInf);
        Assert.Equal("\"-Infinity\"", jsonNegInf);
    }

    [Fact]
    public void Should_Serialize_Inside_An_Object_Correctly()
    {
        // Arrange
        TestObject obj = new()
        {
            SmallValue = 3.500000001E-07,
            NoisyValue = 1.9999999999
        };

        // Act
        string json = JsonSerializer.Serialize(obj, _options);

        // Assert
        Assert.Contains("3.5E-07", json);
        Assert.Contains("2", json);

        TestObject deserialized = JsonSerializer.Deserialize<TestObject>(json, _options)!;
        Assert.Equal(3.5E-07, deserialized.SmallValue);
        Assert.Equal(2.0, deserialized.NoisyValue);
    }

    private class TestObject
    {
        public double SmallValue { get; set; }
        public double NoisyValue { get; set; }
    }
}
