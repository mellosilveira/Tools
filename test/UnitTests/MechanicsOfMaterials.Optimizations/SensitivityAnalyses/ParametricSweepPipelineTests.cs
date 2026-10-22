using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Managers.File.Providers.Local;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
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
    private readonly IFileStorageService _fileStorageService;

    public ParametricSweepPipelineTests()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "ParametricSweepPipelineTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputDirectory);
        _fileStorageService = new LocalFileStorageService(new LocalFileStorageSettings { BaseDirectory = _outputDirectory }, TimeProvider.System);
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

        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StoredFileEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        ServiceCollection services = new();
        services.AddKeyedSingleton<IParametricSweepGenerator, FungParametricSweepGenerator>(nameof(MechanicalModel.Fung));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(serviceProvider);
        ServiceLocator serviceLocator = new(dynamicProvider, serviceProvider);

        ParametricSweepPipeline pipeline = new(
            NullLogger<ParametricSweepPipeline>.Instance,
            NullPipelineProgressNotifier.Instance,
            factoryMock.Object,
            Mock.Of<IDifferentiation>(),
            _fileStorageService,
            repositoryMock.Object,
            serviceLocator,
            TimeProvider.System);

        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 1000.0,
            elasticPowerConstant: 1.0,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.1, 0.01, 10.0));

        string relaxationStiffnessName = nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness);
        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [relaxationStiffnessName] = new RangeParameters { InitialPoint = 0.05, FinalPoint = 0.15, Step = 0.05 }
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
            TimeStep = 0.1
        };

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());
        Assert.False(string.IsNullOrWhiteSpace(result.Data));
        string identifier = result.Data!;

        string strainCsv = (await _fileStorageService.GetMetadataAsync("SensitivityAnalysis", identifier, "Strain_SensitivityAnalysis.csv")).FullPath;
        string cCsv = (await _fileStorageService.GetMetadataAsync("SensitivityAnalysis", identifier, $"{relaxationStiffnessName}_SensitivityAnalysis.csv")).FullPath;

        Assert.True(File.Exists(strainCsv), $"Expected file {strainCsv} to exist.");
        Assert.True(File.Exists(cCsv), $"Expected file {cCsv} to exist.");

        string[] strainLines = await File.ReadAllLinesAsync(strainCsv);
        Assert.Equal(3, strainLines.Length); // Header + 2 data rows (0.02 and 0.04)
        Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", strainLines[0]);

        string[] cLines = await File.ReadAllLinesAsync(cCsv);
        Assert.Equal(4, cLines.Length); // Header + 3 data rows (0.05, 0.10, 0.15)
        Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", cLines[0]);

        repositoryMock.Verify(
            r => r.TryInsertAsync(
                It.Is<ParametricSweepEntity>(e => !string.IsNullOrWhiteSpace(e.Identifier) && e.ExternalIdentifier == identifier && e.MechanicalModelName == nameof(MechanicalModel.Fung)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repositoryMock.Verify(
            r => r.TryInsertAsync(
                It.Is<StoredFileEntity>(f => f.ExternalIdentifier == identifier && f.Context == "SensitivityAnalysis" && f.SizeBytes > 0),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
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

        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StoredFileEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        ServiceCollection services = new();
        services.AddKeyedSingleton<IParametricSweepGenerator, FungParametricSweepGenerator>(nameof(MechanicalModel.Fung));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(serviceProvider);
        ServiceLocator serviceLocator = new(dynamicProvider, serviceProvider);

        Differentiation differentiation = new();
        ParametricSweepPipeline pipeline = new(
            NullLogger<ParametricSweepPipeline>.Instance,
            NullPipelineProgressNotifier.Instance,
            factoryMock.Object,
            differentiation,
            _fileStorageService,
            repositoryMock.Object,
            serviceLocator,
            TimeProvider.System);

        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 1000.0,
            elasticPowerConstant: 1.0,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.1, 0.01, 10.0));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness)] = new RangeParameters { InitialPoint = 0.05, FinalPoint = 0.05, Step = 0.05 }
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
            AsymptoteTolerance = 1e-4
        };

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());
        Assert.False(string.IsNullOrWhiteSpace(result.Data));
    }
}
