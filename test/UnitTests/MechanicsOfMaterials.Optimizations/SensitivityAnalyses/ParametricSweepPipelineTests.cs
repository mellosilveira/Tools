using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Managers.File.Providers.Local;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace UnitTests.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;

public sealed class ParametricSweepPipelineTests : IDisposable
{
    private readonly string _outputDirectory;
    private readonly IFileStorageService _fileStorageService;

    public ParametricSweepPipelineTests()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "ParametricSweepPipelineTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputDirectory);
        _fileStorageService = new LocalFileStorageService(TimeProvider.System, new LocalFileStorageSettings { BaseDirectory = _outputDirectory });
    }

    public void Dispose()
    {
        _fileStorageService.Dispose();
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
    public async Task ProcessAsync_Should_ExecutePipelineProducePartitionedCsvsAndPersistSweptVariablesDictionary()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> facadeMock = new();
        facadeMock
            .Setup(f => f.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), It.IsAny<double>(), It.IsAny<double?>()))
            .Returns<GenericMechanicalModelInput, double, double?>((_, time, _) => 2.0 + 8.0 * Math.Exp(-time / 5.0));

        Mock<IMechanicalModelCalculatorFactory> factoryMock = new();
        factoryMock
            .Setup(f => f.CreateCalculatorFacade(It.IsAny<string>()))
            .Returns(facadeMock.Object);

        long nextFileId = 500L;
        List<StorageEntryMetadataEntity> persistedFiles = [];
        ParametricSweepEntity? persistedSweepEntity = null;

        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .Callback<StorageEntryMetadataEntity, CancellationToken>((fileEntity, _) => persistedFiles.Add(fileEntity))
            .ReturnsAsync(() => Result.CreateSuccessOk(++nextFileId));

        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ParametricSweepEntity, CancellationToken>((sweepEntity, _) => persistedSweepEntity = sweepEntity)
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        ServiceCollection services = new();
        services.AddKeyedSingleton<IParametricSweepGenerator, FungParametricSweepGenerator>(nameof(MechanicalModel.Fung));
        services.AddKeyedSingleton<IRootFinding, StepByStepMethod>(RootFindingAlgorithm.StepByStepMethod);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(serviceProvider);
        ServiceLocator serviceLocator = new(dynamicProvider, serviceProvider);

        PipelineFactory pipelineFactory = new(NullPipelineProgressNotifier.Instance, TimeProvider.System);
        ParametricSweepSettings settings = new() { IngestionBufferCapacity = 64 };
        Differentiation differentiation = new();

        ParametricSweepPipeline pipeline = new(
            NullLoggerFactory.Instance,
            factoryMock.Object,
            differentiation,
            _fileStorageService,
            repositoryMock.Object,
            pipelineFactory,
            serviceLocator,
            settings);

        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 1000.0,
            elasticPowerConstant: 1.0,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.1, 0.01, 10.0));

        RangeParameters strainRange = new() { InitialPoint = 0.02, FinalPoint = 0.04, Step = 0.02 };
        RangeParameters cRange = new() { InitialPoint = 0.05, FinalPoint = 0.15, Step = 0.05 };

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness)] = cRange
        };

        ParametricSweepPipelineInput input = new()
        {
            Identifier = "sweep_e2e_01",
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = strainRange,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 200.0, 1e-2, 200)
        };

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());
        Assert.Equal("sweep_e2e_01", result.Data);

        Assert.Equal(2, persistedFiles.Count);
        Assert.All(persistedFiles, f =>
        {
            Assert.Equal("sweep_e2e_01", f.ExternalIdentifier);
            Assert.True(f.SizeBytes > 0);
            Assert.True(File.Exists(f.Location));
        });

        StorageEntryMetadataEntity strainFileEntity = persistedFiles[0];
        string[] strainLines = await File.ReadAllLinesAsync(strainFileEntity.Location);
        Assert.Equal(3, strainLines.Length); // Header + 2 rows (0.02, 0.04)

        StorageEntryMetadataEntity cFileEntity = persistedFiles[1];
        string[] cLines = await File.ReadAllLinesAsync(cFileEntity.Location);
        Assert.Equal(4, cLines.Length); // Header + 3 rows (0.05, 0.10, 0.15)

        Assert.NotNull(persistedSweepEntity);
        Assert.Equal("sweep_e2e_01", persistedSweepEntity.ExternalIdentifier);
        Assert.Equal(nameof(MechanicalModel.Fung), persistedSweepEntity.MechanicalModelName);
        Assert.Equal(RootFindingAlgorithm.StepByStepMethod, persistedSweepEntity.RootFindingAlgorithm);

        Dictionary<string, SweptVariableFileMetadata>? sweptVariables =
            JsonSerializer.Deserialize<Dictionary<string, SweptVariableFileMetadata>>(persistedSweepEntity.SweptVariablesJson);
        Assert.NotNull(sweptVariables);
        Assert.Equal(2, sweptVariables.Count);

        Assert.Equal(501L, sweptVariables["Strain"].OutputFileMetadataId);
        Assert.Equal(strainRange, sweptVariables["Strain"].Range);

        Assert.Equal(502L, sweptVariables[nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness)].OutputFileMetadataId);
        Assert.Equal(cRange, sweptVariables[nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness)].Range);
    }
}
