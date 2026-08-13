using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class Test
{
    private readonly ExperimentalDataProcessor _processor = new(Mock.Of<ILogger<ExperimentalDataProcessor>>(), new Differentiation());
    private readonly ExperimentalDataProcessingOptions _options = new(0.0, BufferSize: 10, RelativeTolerance: 1e-6, Tolerance: 1e-6, DerivativeTolerance: 1e-6, SkipTimeStep: 0.0);

    [Theory]
    [MemberData(nameof(GetTestData))]
    public void TestMethod(SegmentType currentType, ExperimentalDataPoint[] buffer, Dictionary<SegmentType, ExperimentalDataPoint[]> expected)
    {
        // Act
        var result = _processor.DetermineSegmentType(currentType, buffer, _options);

        // Assert
        Assert.Equal(expected.Keys.Count, result.Keys.Count);

        foreach (var key in expected.Keys)
        {
            Assert.True(result.ContainsKey(key), $"O resultado não contém o segmento {key}.");

            var expectedArr = expected[key];
            var resultArr = result[key];

            Assert.Equal(expectedArr.Length, resultArr.Length);

            for (int i = 0; i < expectedArr.Length; i++)
            {
                // O número '5' indica que a tolerância é de 5 casas decimais (1e-5) maior do que foi definido em _options.
                Assert.Equal(expectedArr[i].Time, resultArr[i].Time, 5);
                Assert.Equal(expectedArr[i].Strain, resultArr[i].Strain, 5);
            }
        }
    }

    public static TheoryData<SegmentType, ExperimentalDataPoint[], Dictionary<SegmentType, ExperimentalDataPoint[]>> GetTestData() => new()
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
            new Dictionary<SegmentType, ExperimentalDataPoint[]>
            {
                {
                    SegmentType.Ramp,
                    [
                        new ExperimentalDataPoint(Time: 1.0, Strain: 1.0, Stress: 2.0),
                        new ExperimentalDataPoint(Time: 2.0, Strain: 4.0, Stress: 8.0),
                        new ExperimentalDataPoint(Time: 3.0, Strain: 9.0, Stress: 18.0),
                        new ExperimentalDataPoint(Time: 4.0, Strain: 16.0, Stress: 32.0)
                    ]
                }
            }
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
            new Dictionary<SegmentType, ExperimentalDataPoint[]>
            {
                {
                    SegmentType.Ramp,
                    new[]
                    {
                        new ExperimentalDataPoint(Time: 1.0, Strain: 1.0, Stress: 2.0),
                        new ExperimentalDataPoint(Time: 2.0, Strain: 4.0, Stress: 8.0)
                    }
                },
                {
                    SegmentType.Relaxation,
                    [
                        // StrainRate = 1e-7 é mascarado pela Tolerance -> 0.0
                        new ExperimentalDataPoint(Time: 3.0, Strain: 4.0000001, Stress: 6.0),
                        new ExperimentalDataPoint(Time: 4.0, Strain: 4.0000002, Stress: 5.0)
                    ]
                }
            }
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
            new Dictionary<SegmentType, ExperimentalDataPoint[]>
            {
                {
                    SegmentType.Relaxation,
                    [
                        new ExperimentalDataPoint(Time: 1.0, Strain: 10.0, Stress: 20.0),
                        new ExperimentalDataPoint(Time: 2.0, Strain: 10.0000001, Stress: 15.0),
                        new ExperimentalDataPoint(Time: 3.0, Strain: 10.0000003, Stress: 12.0),
                        new ExperimentalDataPoint(Time: 4.0, Strain: 10.0000002, Stress: 10.0)
                    ]
                }
            }
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
            new Dictionary<SegmentType, ExperimentalDataPoint[]>
            {
                { SegmentType.Relaxation, [new ExperimentalDataPoint(Time: 1.0, Strain: 10.0, Stress: 10.0)] },
                {
                    SegmentType.Descent,
                    [
                        new ExperimentalDataPoint(Time: 2.0, Strain: 10.0000001, Stress: 9.0),
                        new ExperimentalDataPoint(Time: 3.0, Strain: 9.0, Stress: 5.0),
                        new ExperimentalDataPoint(Time: 4.0, Strain: 4.0, Stress: 2.0)
                    ]
                }
            }
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
            new Dictionary<SegmentType, ExperimentalDataPoint[]>
            {
                {
                    SegmentType.Descent,
                    [
                        new ExperimentalDataPoint(Time: 1.0, Strain: 16.0, Stress: 32.0),
                        new ExperimentalDataPoint(Time: 2.0, Strain: 9.0, Stress: 18.0),
                        new ExperimentalDataPoint(Time: 3.0, Strain: 4.0, Stress: 8.0),
                        new ExperimentalDataPoint(Time: 4.0, Strain: 1.0, Stress: 2.0)
                    ]
                }
            }
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
            new Dictionary<SegmentType, ExperimentalDataPoint[]>
            {
                {
                    SegmentType.Descent,
                    [
                        new ExperimentalDataPoint(Time: 1.0, Strain: 4.0, Stress: 8.0),
                        new ExperimentalDataPoint(Time: 2.0, Strain: 1.0, Stress: 2.0)
                    ]
                },
                {
                    SegmentType.Recovery,
                    [
                        new ExperimentalDataPoint(Time: 3.0, Strain: 1.0000001, Stress: 1.5),
                        new ExperimentalDataPoint(Time: 4.0, Strain: 1.0, Stress: 1.2)
                    ]
                }
            }
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
            new Dictionary<SegmentType, ExperimentalDataPoint[]>
            {
                { SegmentType.Recovery, [new ExperimentalDataPoint(Time: 1.0, Strain: 1.0000001, Stress: 1.1)] },
                {
                    SegmentType.Ramp,
                    [
                        new ExperimentalDataPoint(Time: 2.0, Strain: 1.0, Stress: 1.0),
                        new ExperimentalDataPoint(Time: 3.0, Strain: 4.0, Stress: 8.0),
                        new ExperimentalDataPoint(Time: 4.0, Strain: 9.0, Stress: 18.0)
                    ]
                }
            }
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
            new Dictionary<SegmentType, ExperimentalDataPoint[]>
            {
                {
                    SegmentType.Recovery,
                    [
                        new ExperimentalDataPoint(Time: 1.0, Strain: 1.0, Stress: 2.0),
                        new ExperimentalDataPoint(Time: 2.0, Strain: 1.0000001, Stress: 1.5),
                        new ExperimentalDataPoint(Time: 3.0, Strain: 1.0000003, Stress: 1.2),
                        new ExperimentalDataPoint(Time: 4.0, Strain: 1.0000002, Stress: 1.0)
                    ]
                }
            }
        }
    };
}