using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class Test
{
    private readonly ExperimentalDataService _processor = new(Mock.Of<ILogger<ExperimentalDataService>>(), new Differentiation());
    private readonly ExperimentalDataProcessingOptions _options = new(0.0, BufferSize: 10, RelativeTolerance: 1e-6, Tolerance: 1e-6, AccelerationTolerance: 1e-6, SkipTimeStep: 0.0);

    [Theory]
    [MemberData(nameof(GetTestData))]
    public void TestMethod(SegmentType currentType, ExperimentalDataPoint[] buffer, List<(SegmentType SegmentType, ArraySegment<ExperimentalDataPoint> Points)> expected)
    {
        // Act
        List<(SegmentType SegmentType, ArraySegment<ExperimentalDataPoint> Points)> result = _processor.ExtractSegments(currentType, buffer, count: buffer.Length, _options);

        // Assert
        Assert.Equal(expected.Count, result.Count);

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].SegmentType, result[i].SegmentType);
            for (int j = 0; j < expected[i].Points.Count; j++)
            {
                // O número '5' indica que a tolerância é de 5 casas decimais (1e-5) maior do que foi definido em _options.
                Assert.Equal(expected[i].Points[j].Time, result[i].Points[j].Time, 5);
                Assert.Equal(expected[i].Points[j].Strain, result[i].Points[j].Strain, 5);
                Assert.Equal(expected[i].Points[j].Stress, result[i].Points[j].Stress, 5);
            }
        }
    }

    [Fact]
    public async Task SegmentPointsAsync_WithUtf8Stream_SegmentsPointsCorrectly()
    {
        // Arrange
        string strainContent = "1.0, 1.0\r\n2.0, 4.0\r\n3.0, 9.0\r\n4.0, 16.0\r\n";
        string stressContent = "1.0, 2.0\r\n2.0, 8.0\r\n3.0, 18.0\r\n4.0, 32.0\r\n";

        using MemoryStream strainStream = new(System.Text.Encoding.UTF8.GetBytes(strainContent));
        using MemoryStream stressStream = new(System.Text.Encoding.UTF8.GetBytes(stressContent));

        ExperimentalDataProcessingOptions options = new(0.0, BufferSize: 4, RelativeTolerance: 1e-6, Tolerance: 1e-6, AccelerationTolerance: 1e-6, SkipTimeStep: 0.0);

        // Act
        List<SegmentedDataPoint> points = [];
        await foreach (SegmentedDataPoint point in _processor.SegmentPointsAsync(strainStream, stressStream, options))
        {
            points.Add(point);
        }

        // Assert
        Assert.NotEmpty(points);
        Assert.Equal(4, points.Count);
        Assert.Equal(0.0, points[0].ProcessedDataPoint.Time);
        Assert.Equal(1.0, points[0].ProcessedDataPoint.Strain);
        Assert.Equal(2.0, points[0].ProcessedDataPoint.Stress);
        Assert.Equal(SegmentType.Ramp, points[0].SegmentType);
    }

    [Fact]
    public async Task ProcessAsync_WithUtf8Stream_ProcessesAndAssemblesSegmentsThroughPipeline()
    {
        // Arrange
        string strainContent = "1.0, 1.0\r\n2.0, 4.0\r\n3.0, 9.0\r\n4.0, 16.0\r\n";
        string stressContent = "1.0, 2.0\r\n2.0, 8.0\r\n3.0, 18.0\r\n4.0, 32.0\r\n";

        using MemoryStream strainStream = new(System.Text.Encoding.UTF8.GetBytes(strainContent));
        using MemoryStream stressStream = new(System.Text.Encoding.UTF8.GetBytes(stressContent));

        ExperimentalDataProcessingOptions options = new(0.0, BufferSize: 4, RelativeTolerance: 1e-6, Tolerance: 1e-6, AccelerationTolerance: 1e-6, SkipTimeStep: 0.0);

        // Act
        MelloSilveiraTools.Core.Models.Result<CurveSegment[]> result = await _processor.ProcessAsync(strainStream, stressStream, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal(SegmentType.Ramp, result.Data[0].Type);
        Assert.Equal(4, result.Data[0].TimePoints.Length);
        Assert.Equal(0.0, result.Data[0].TimePoints[0]);
        Assert.Equal(1.0, result.Data[0].ExperimentalStrain[0]);
        Assert.Equal(2.0, result.Data[0].ExperimentalStress[0]);
    }

    public static TheoryData<SegmentType, ExperimentalDataPoint[], List<(SegmentType SegmentType, ArraySegment<ExperimentalDataPoint> Points)>> GetTestData() => new()
    {
        // --------------------------------------------------------------------------------
        // CASO 1: Rampa Pura 
        // Strain e Stress aumentam de forma quadrática garantindo Aceleração != 0
        // --------------------------------------------------------------------------------
        {
            SegmentType.Recovery,
            [
                new ExperimentalDataPoint(Time: 1.0, Stress: 2.0, Strain: 1.0),
                new ExperimentalDataPoint(Time: 2.0, Stress: 8.0, Strain: 4.0),
                new ExperimentalDataPoint(Time: 3.0, Stress: 18.0, Strain: 9.0),
                new ExperimentalDataPoint(Time: 4.0, Stress: 32.0, Strain: 16.0)
            ],
            [
                (SegmentType.Ramp, new ArraySegment<ExperimentalDataPoint>([
                    new(Time: 1.0, Strain: 1.0, Stress: 2.0),
                    new(Time: 2.0, Strain: 4.0, Stress: 8.0),
                    new(Time: 3.0, Strain: 9.0, Stress: 18.0),
                    new(Time: 4.0, Strain: 16.0, Stress: 32.0)
                ]))
            ]
        },

        // --------------------------------------------------------------------------------
        // CASO 2: Transição Rampa -> Relaxação 
        // A deformação trava no platô (com micro-ruído < Tolerance zerado pela regra), a tensão começa a decair
        // --------------------------------------------------------------------------------
        {
            SegmentType.Ramp,
            [
                new ExperimentalDataPoint(Time: 1.0, Stress: 2.0, Strain: 1.0),
                new ExperimentalDataPoint(Time: 2.0, Stress: 8.0, Strain: 4.0),
                new ExperimentalDataPoint(Time: 3.0, Stress: 6.0, Strain: 4.0000001),
                new ExperimentalDataPoint(Time: 4.0, Stress: 5.0, Strain: 4.0000002)
            ],
            [
                (SegmentType.Ramp, new ArraySegment<ExperimentalDataPoint>([new(Time: 1.0, Strain: 1.0, Stress: 2.0), new(Time: 2.0, Strain: 4.0, Stress: 8.0)])),
                (SegmentType.Relaxation, new ArraySegment<ExperimentalDataPoint>([new(Time: 3.0, Strain: 4.0000001, Stress: 6.0), new(Time: 4.0, Strain: 4.0000002, Stress: 5.0)]))
            ]
        },

        // --------------------------------------------------------------------------------
        // CASO 3: Relaxação Pura 
        // Deformação constante no teto (flutuações < 1e-6 zeradas), Tensão decai exponencialmente
        // --------------------------------------------------------------------------------
        {
            SegmentType.Relaxation,
            [
                new ExperimentalDataPoint(Time: 1.0, Stress: 20.0, Strain: 10.0),
                new ExperimentalDataPoint(Time: 2.0, Stress: 15.0, Strain: 10.0000001),
                new ExperimentalDataPoint(Time: 3.0, Stress: 12.0, Strain: 10.0000003),
                new ExperimentalDataPoint(Time: 4.0, Stress: 10.0, Strain: 10.0000002)
            ],
            [
                (SegmentType.Relaxation, new ArraySegment<ExperimentalDataPoint>([
                    new(Time: 1.0, Strain: 10.0, Stress: 20.0),
                    new(Time: 2.0, Strain: 10.0000001, Stress: 15.0),
                    new(Time: 3.0, Strain: 10.0000003, Stress: 12.0),
                    new(Time: 4.0, Strain: 10.0000002, Stress: 10.0)
                ]))
            ]
        },

        // --------------------------------------------------------------------------------
        // CASO 4: Transição Relaxação -> Descida 
        // Corpo de prova volta a ser descomprimido (Strain cai acentuadamente)
        // --------------------------------------------------------------------------------
        {
            SegmentType.Relaxation,
            [
                new ExperimentalDataPoint(Time: 1.0, Stress: 10.0, Strain: 10.0),
                new ExperimentalDataPoint(Time: 2.0, Stress: 9.0, Strain: 10.0000001),
                new ExperimentalDataPoint(Time: 3.0, Stress: 5.0, Strain: 9.0),
                new ExperimentalDataPoint(Time: 4.0, Stress: 2.0, Strain: 4.0)
            ],
            [
                (SegmentType.Relaxation, new ArraySegment<ExperimentalDataPoint>([new(Time: 1.0, Strain: 10.0, Stress: 10.0)])),
                (SegmentType.Descent, new ArraySegment<ExperimentalDataPoint>([
                    new(Time: 2.0, Strain: 10.0000001, Stress: 9.0),
                    new(Time: 3.0, Strain: 9.0, Stress: 5.0),
                    new(Time: 4.0, Strain: 4.0, Stress: 2.0)
                ]))
            ]
        },

        // --------------------------------------------------------------------------------
        // CASO 5: Descida Pura 
        // Strain e Stress diminuem de forma quadrática invertida
        // --------------------------------------------------------------------------------
        {
            SegmentType.Descent,
            [
                new ExperimentalDataPoint(Time: 1.0, Stress: 32.0, Strain: 16.0),
                new ExperimentalDataPoint(Time: 2.0, Stress: 18.0, Strain: 9.0),
                new ExperimentalDataPoint(Time: 3.0, Stress: 8.0, Strain: 4.0),
                new ExperimentalDataPoint(Time: 4.0, Stress: 2.0, Strain: 1.0)
            ],
            [
                (SegmentType.Descent, new ArraySegment<ExperimentalDataPoint>([
                    new(Time: 1.0, Strain: 16.0, Stress: 32.0),
                    new(Time: 2.0, Strain: 9.0, Stress: 18.0),
                    new(Time: 3.0, Strain: 4.0, Stress: 8.0),
                    new(Time: 4.0, Strain: 1.0, Stress: 2.0)
                ]))
            ]
        },

        // --------------------------------------------------------------------------------
        // CASO 6: Transição Descida -> Recuperação 
        // A descida finaliza no piso e entra no estágio de recuperação
        // --------------------------------------------------------------------------------
        {
            SegmentType.Descent,
            [
                new ExperimentalDataPoint(Time: 1.0, Stress: 8.0, Strain: 4.0),
                new ExperimentalDataPoint(Time: 2.0, Stress: 2.0, Strain: 1.0),
                new ExperimentalDataPoint(Time: 3.0, Stress: 1.5, Strain: 1.0000001),
                new ExperimentalDataPoint(Time: 4.0, Stress: 1.2, Strain: 1.0)
            ],
            [
                (SegmentType.Descent, new ArraySegment<ExperimentalDataPoint>([new(Time: 1.0, Strain: 4.0, Stress: 8.0), new(Time: 2.0, Strain: 1.0, Stress: 2.0)])),
                (SegmentType.Recovery, new ArraySegment<ExperimentalDataPoint>([new(Time: 3.0, Strain: 1.0000001, Stress: 1.5), new(Time: 4.0, Strain: 1.0, Stress: 1.2)]))
            ]
        },

        // --------------------------------------------------------------------------------
        // CASO 7: Transição Recuperação -> Rampa 
        // O corpo de prova estava em repouso (Recuperação) e volta a ser tracionado (Rampa)
        // --------------------------------------------------------------------------------
        {
            SegmentType.Recovery,
            [
                new ExperimentalDataPoint(Time: 1.0, Stress: 1.1, Strain: 1.0000001),
                new ExperimentalDataPoint(Time: 2.0, Stress: 1.0, Strain: 1.0),
                new ExperimentalDataPoint(Time: 3.0, Stress: 8.0, Strain: 4.0),
                new ExperimentalDataPoint(Time: 4.0, Stress: 18.0, Strain: 9.0)
            ],
            [
                (SegmentType.Recovery, new ArraySegment<ExperimentalDataPoint>([new(Time: 1.0, Strain: 1.0000001, Stress: 1.1)])),
                (SegmentType.Ramp, new ArraySegment<ExperimentalDataPoint>([
                    new(Time: 2.0, Strain: 1.0, Stress: 1.0),
                    new(Time: 3.0, Strain: 4.0, Stress: 8.0),
                    new(Time: 4.0, Strain: 9.0, Stress: 18.0)
                ]))
            ]
        },

        // --------------------------------------------------------------------------------
        // CASO 8: Recuperação Pura 
        // Deformação constante no piso (com micro-ruído), Tensão em relaxação/recuperação assintótica
        // --------------------------------------------------------------------------------
        {
            SegmentType.Recovery,
            [
                new ExperimentalDataPoint(Time: 1.0, Stress: 2.0, Strain: 1.0),
                new ExperimentalDataPoint(Time: 2.0, Stress: 1.5, Strain: 1.0000001),
                new ExperimentalDataPoint(Time: 3.0, Stress: 1.2, Strain: 1.0000003),
                new ExperimentalDataPoint(Time: 4.0, Stress: 1.0, Strain: 1.0000002)
            ],
            [
                (SegmentType.Recovery, new ArraySegment<ExperimentalDataPoint>([
                    new(Time: 1.0, Strain: 1.0, Stress: 2.0),
                    new(Time: 2.0, Strain: 1.0000001, Stress: 1.5),
                    new(Time: 3.0, Strain: 1.0000003, Stress: 1.2),
                    new(Time: 4.0, Strain: 1.0000002, Stress: 1.0)
                ]))
            ]
        }
    };
}