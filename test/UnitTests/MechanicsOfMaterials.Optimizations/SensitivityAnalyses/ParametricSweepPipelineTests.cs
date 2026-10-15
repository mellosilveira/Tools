using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;

public sealed class ParametricSweepPipelineTests : IDisposable
{
    private readonly string _outputDirectory;

    public ParametricSweepPipelineTests()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "ParametricSweepPipelineTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
        {
            try
            {
                Directory.Delete(_outputDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors on temp files
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_Should_ExecutePipelineAndProduceSegregatedCsvFiles()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> facadeMock = new();
        facadeMock
            .Setup(f => f.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), It.IsAny<double>(), It.IsAny<double?>()))
            .Returns<GenericMechanicalModelInput, double, double?>((_, time, _) =>
            {
                // Decaying stress: 10 at t=0, approaching 2 at large t
                return 2.0 + 8.0 * Math.Exp(-time / 5.0);
            });

        Mock<IMechanicalModelCalculatorFactory> factoryMock = new();
        factoryMock
            .Setup(f => f.CreateCalculatorFacade(It.IsAny<string>()))
            .Returns(facadeMock.Object);

        ServiceCollection services = new();
        services.AddKeyedSingleton<IParametricSweepGenerator, FungParametricSweepGenerator>(nameof(MechanicalModel.Fung));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(serviceProvider);
        ServiceLocator serviceLocator = new(dynamicProvider, serviceProvider);

        ParametricSweepPipeline pipeline = new(
            NullLogger<ParametricSweepPipeline>.Instance,
            factoryMock.Object,
            Mock.Of<IDifferentiation>(),
            serviceLocator);

        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 1000.0,
            elasticPowerConstant: 1.0,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.1, 0.01, 10.0));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            ["c"] = new RangeParameters { InitialPoint = 0.05, FinalPoint = 0.15, Step = 0.05 }
        };

        ParametricSweepInput input = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = new RangeParameters { InitialPoint = 0.02, FinalPoint = 0.04, Step = 0.02 },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1,
            OutputDirectory = _outputDirectory
        };

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());
        Assert.False(string.IsNullOrWhiteSpace(result.Data));

        string strainCsv = Path.Combine(_outputDirectory, "Strain_SensitivityAnalysis.csv");
        string cCsv = Path.Combine(_outputDirectory, "c_SensitivityAnalysis.csv");

        Assert.True(File.Exists(strainCsv), $"Expected file {strainCsv} to exist.");
        Assert.True(File.Exists(cCsv), $"Expected file {cCsv} to exist.");

        string[] strainLines = await File.ReadAllLinesAsync(strainCsv);
        Assert.Equal(3, strainLines.Length); // Header + 2 data rows (0.02 and 0.04)
        Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", strainLines[0]);

        string[] cLines = await File.ReadAllLinesAsync(cCsv);
        Assert.Equal(4, cLines.Length); // Header + 3 data rows (0.05, 0.10, 0.15)
        Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", cLines[0]);
    }

    [Fact]
    public async Task ProcessAsync_Should_RespectExplicitDifferentiationAndTolerance()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> facadeMock = new();
        facadeMock
            .Setup(f => f.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), It.IsAny<double>(), It.IsAny<double?>()))
            .Returns<GenericMechanicalModelInput, double, double?>((_, time, _) =>
            {
                return 2.0 + 8.0 * Math.Exp(-time / 5.0);
            });

        Mock<IMechanicalModelCalculatorFactory> factoryMock = new();
        factoryMock
            .Setup(f => f.CreateCalculatorFacade(It.IsAny<string>()))
            .Returns(facadeMock.Object);

        ServiceCollection services = new();
        services.AddKeyedSingleton<IParametricSweepGenerator, FungParametricSweepGenerator>(nameof(MechanicalModel.Fung));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(serviceProvider);
        ServiceLocator serviceLocator = new(dynamicProvider, serviceProvider);

        MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations.Differentiation differentiation = new();
        ParametricSweepPipeline pipeline = new(
            NullLogger<ParametricSweepPipeline>.Instance,
            factoryMock.Object,
            differentiation,
            serviceLocator);

        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 1000.0,
            elasticPowerConstant: 1.0,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.1, 0.01, 10.0));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            ["c"] = new RangeParameters { InitialPoint = 0.05, FinalPoint = 0.05, Step = 0.05 }
        };

        ParametricSweepInput input = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = null,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1,
            AsymptoteTolerance = 1e-4,
            OutputDirectory = _outputDirectory
        };

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());
        Assert.False(string.IsNullOrWhiteSpace(result.Data));
    }
}
