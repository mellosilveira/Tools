using System.Collections.Concurrent;
using System.Threading.Channels;
using AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;
using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Polly;
using Moq;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.Pipelines;

/// <summary>
/// Acceptance tests verifying end-to-end execution of <see cref="ExecuteBatchExperimentalDataPipeline"/>
/// in both sequential and parallel modes, as well as the experimental-data-to-parametric-sweep bridge
/// via <see cref="ExecuteParametricSweepFromExperimentalDataCommand"/> across multiple constitutive models.
/// </summary>
public sealed class BatchAndParametricSweepStepByStepAcceptanceTests : IAsyncLifetime
{
    private const string ExpectedSweepCsvHeader = "VariableValue,InitialStress,FinalStress,AbsoluteDeltaStress,PercentageDeltaStress,AsymptoteTime";

    private readonly List<string> _createdIdentifiers = [];
    private readonly ServiceProvider _serviceProvider;
    private readonly ConcurrentBag<StorageEntryMetadataEntity> _persistedFiles = [];
    private readonly ConcurrentBag<MechanicalModelCurveFitOutputEntity> _persistedCurveFits = [];
    private readonly ConcurrentBag<MechanicalModelSimulationEntity> _persistedSimulations = [];
    private readonly ConcurrentBag<ParametricSweepEntity> _persistedSweeps = [];

    /// <summary>
    /// Initializes a self-contained DI container wired for batch experimental processing and
    /// experimental-to-parametric-sweep chaining with an in-memory <see cref="IRepository"/> mock.
    /// </summary>
    public BatchAndParametricSweepStepByStepAcceptanceTests()
    {
        long nextEntityId = 0;

        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref nextEntityId);
                _persistedFiles.Add(entity with { Id = id });
                return Result.CreateSuccessCreated(id);
            });

        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<MechanicalModelCurveFitOutputEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MechanicalModelCurveFitOutputEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref nextEntityId);
                _persistedCurveFits.Add(entity with { Id = id });
                return Result.CreateSuccessCreated(id);
            });

        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<MechanicalModelSimulationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MechanicalModelSimulationEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref nextEntityId);
                _persistedSimulations.Add(entity with { Id = id });
                return Result.CreateSuccessCreated(id);
            });

        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParametricSweepEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref nextEntityId);
                _persistedSweeps.Add(entity with { Id = id });
                return Result.CreateSuccessCreated(id);
            });

        repositoryMock
            .Setup(r => r.GetAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(
                It.IsAny<MechanicalModelCurveFitOutputFilter>(),
                It.IsAny<Pagination?>(),
                It.IsAny<CancellationToken>()))
            .Returns((MechanicalModelCurveFitOutputFilter filter, Pagination? _, CancellationToken cancellationToken) =>
                StreamMatchingCurveFitsAsync(filter, cancellationToken));

        ServiceCollection services = new();
        services
            .AddCoreServices(
                new ResiliencePipelineSettings
                {
                    BackoffType = DelayBackoffType.Constant,
                    DelayInMilliseconds = 10,
                    MaxRetryAttempts = 1,
                    UseJitter = false
                },
                pipelineProgressNotifierSettings: new PipelineProgressNotifierSettings
                {
                    ChannelCapacity = 32768,
                    FullMode = BoundedChannelFullMode.Wait,
                    MaxChannels = 64
                })
            .AddMathematicsServices()
            .AddMechanicsOfMaterialsServices(addMechanicalModels: true)
            .AddOptimizationsServices(
                new CurveFitterSettings(),
                new ExperimentalDataSettings
                {
                    SegmenterOptions = new PipelineStepOptions(1, 256, true),
                    FileWriterOptions = new PipelineStepOptions(1, 256, true),
                    PersistanceOptions = new PipelineStepOptions(1, 64, true),
                    SegmentBuilderOptions = new PipelineStepOptions(1, 256, true),
                    CurveFitterOptions = new PipelineStepOptions(1, 64, true),
                    SimulationInputBuilderOptions = new PipelineStepOptions(1, 64, true),
                    IngestionBufferCapacity = 32,
        MaxCsvSizeBytes = 50L * 1024 * 1024
                },
                new ParametricSweepSettings
                {
                    IngestionBufferCapacity = 32
                })
            .AddSingleton<IRepository>(repositoryMock.Object);

        services.TryAddTransient<MelloSilveiraTools.Core.Validators.IValidator<ExecuteBatchExperimentalDataPipelineRequest>, ExecuteBatchExperimentalDataPipelineValidator>();
        services.TryAddTransient<ExecuteBatchExperimentalDataPipeline>();
        services.TryAddTransient<MelloSilveiraTools.Core.Validators.IValidator<ExecuteParametricSweepFromExperimentalDataRequest>, ExecuteParametricSweepFromExperimentalDataValidator>();
        services.TryAddTransient<ExecuteParametricSweepFromExperimentalDataCommand>();

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that <see cref="ExecuteBatchExperimentalDataPipeline"/> yields identifiers for each
    /// experimental data group, processes all groups completely in both sequential and parallel modes,
    /// and persists entities and CSV artifacts for every group.
    /// </summary>
    /// <param name="executionMode">Batch execution strategy (Sequential or Parallel).</param>
    [Theory]
    [InlineData(PipelineExecutionMode.Sequential)]
    [InlineData(PipelineExecutionMode.Parallel)]
    public async Task ExecuteBatchExperimentalDataPipeline_WithTwoRealGroups_ShouldYieldIdentifiersAndCompleteBothRunsAsync(PipelineExecutionMode executionMode)
    {
        // Arrange
        string group1Identifier = $"accept-batch-g1-{executionMode.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";
        string group2Identifier = $"accept-batch-g2-{executionMode.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";
        _createdIdentifiers.Add(group1Identifier);
        _createdIdentifiers.Add(group2Identifier);

        (string strainPath, string stressPath) = GetSoftTissueRawPaths();
        ExecuteBatchExperimentalDataPipeline command = _serviceProvider.GetRequiredService<ExecuteBatchExperimentalDataPipeline>();
        ChannelPipelineProgressNotifier progressNotifier = Assert.IsType<ChannelPipelineProgressNotifier>(_serviceProvider.GetRequiredService<IPipelineProgressNotifier>());

        ExecuteBatchExperimentalDataPipelineRequest request = new()
        {
            MechanicalModelName = "SimplifiedFung",
            TargetSegments = [SegmentType.Relaxation],
            CurveFitterType = CurveFitterType.Alglib,
            TargetRSquared = 0.85,
            ConfidenceLevel = 0.95,
            AsymptoteConsecutivePointsThreshold = 5,
            ExecutionMode = executionMode,
            MaxDegreeOfParallelism = 2,
            Options = new ExperimentalDataProcessingOptions(BufferSize: 10, AccelerationTolerance: 2.0),
            Groups =
            [
                new ExperimentalDataGroupRequest
                {
                    Identifier = group1Identifier,
                    StrainFileLocation = strainPath,
                    StressFileLocation = stressPath,
                    StartExperimentalTimeThreshold = 1202.0
                },
                new ExperimentalDataGroupRequest
                {
                    Identifier = group2Identifier,
                    StrainFileLocation = strainPath,
                    StressFileLocation = stressPath,
                    StartExperimentalTimeThreshold = 1202.0
                }
            ]
        };

        // Act
        Result<IAsyncEnumerable<string>> batchResponse = await command.ExecuteAsync(request);
        Assert.True(batchResponse.Success, batchResponse.JoinedMessages);
        Assert.NotNull(batchResponse.Data);

        List<string> yieldedIdentifiers = [];
        await foreach (string id in batchResponse.Data)
        {
            yieldedIdentifiers.Add(id);
        }

        // Assert: Identifiers match requested groups and both pipelines drain cleanly
        Assert.Equal(2, yieldedIdentifiers.Count);
        Assert.Contains(group1Identifier, yieldedIdentifiers);
        Assert.Contains(group2Identifier, yieldedIdentifiers);

        List<PipelineProgressEvent> eventsGroup1 = await DrainProgressEventsWithTimeoutAsync(progressNotifier, group1Identifier, TimeSpan.FromSeconds(60));
        List<PipelineProgressEvent> eventsGroup2 = await DrainProgressEventsWithTimeoutAsync(progressNotifier, group2Identifier, TimeSpan.FromSeconds(60));

        Assert.NotEmpty(eventsGroup1);
        Assert.NotEmpty(eventsGroup2);

        foreach (string groupId in yieldedIdentifiers)
        {
            Assert.Contains(_persistedCurveFits, c => c.ExtenalIdentifier == groupId && c.MechanicalModelName == "SimplifiedFung");
            Assert.Contains(_persistedSimulations, s => s.ExternalIdentifier == groupId && s.MechanicalModelName == "SimplifiedFung");
            Assert.True(_persistedFiles.Count(f => f.ExternalIdentifier == groupId) >= 2, $"Expected persisted metadata for group '{groupId}'.");

            string groupStorageDir = Path.Combine(Path.GetTempPath(), "storage", groupId);
            Assert.True(Directory.Exists(groupStorageDir), $"Expected generated storage artifacts directory '{groupStorageDir}'.");
            Assert.NotEmpty(Directory.GetFiles(groupStorageDir, "*.csv", SearchOption.AllDirectories));
        }
    }

    /// <summary>
    /// Verifies the full bridge from experimental data processing into parametric sensitivity sweeps:
    /// runs <see cref="IPipeline{ExperimentalDataProcessingInput, String}"/> to fit real ligament data for "Fung",
    /// stores the fitted constitutive parameters in <see cref="IRepository"/>, then invokes
    /// <see cref="ExecuteParametricSweepFromExperimentalDataCommand"/> and verifies persisted sweep entities
    /// and sensitivity CSV files.
    /// </summary>
    [Fact]
    public async Task ExecuteExperimentalDataThenParametricSweepFromExperimentalData_EndToEnd_ShouldBridgeCurveFitsIntoSweepCsvsAndEntitiesAsync()
    {
        const string modelName = "Fung";

        // Arrange Step 1: Fit experimental raw data and capture entities in IRepository
        string experimentalIdentifier = $"accept-bridge-exp-fung-{Guid.NewGuid():N}";
        _createdIdentifiers.Add(experimentalIdentifier);

        (string strainPath, string stressPath) = GetSoftTissueRawPaths();
        IPipeline<ExperimentalDataProcessingInput, string> expPipeline = _serviceProvider.GetRequiredService<IPipeline<ExperimentalDataProcessingInput, string>>();

        Result<string> expResult = await expPipeline.ProcessAsync(new ExperimentalDataProcessingInput
        {
            Identifier = experimentalIdentifier,
            StrainFileLocation = strainPath,
            StressFileLocation = stressPath,
            StartExperimentalTimeThreshold = 1202.0,
            MechanicalModelName = modelName,
            TargetSegments = [SegmentType.Relaxation],
            CurveFitterType = CurveFitterType.Alglib,
            TargetRSquared = 0.85,
            ConfidenceLevel = 0.95,
            AsymptoteConsecutivePointsThreshold = 5,
            Options = new ExperimentalDataProcessingOptions(BufferSize: 10, AccelerationTolerance: 2.0)
        });

        Assert.True(expResult.Success, $"Experimental pipeline for '{modelName}' failed: {expResult.JoinedMessages}");
        Assert.Contains(_persistedCurveFits, c => c.ExtenalIdentifier == experimentalIdentifier && c.MechanicalModelName == modelName);

        // Arrange Step 2: Execute parametric sweep from stored curve fits
        Dictionary<string, RangeParameters> constantVariables = new()
        {
            ["ElasticStressConstant"] = new RangeParameters { InitialPoint = 8.0, FinalPoint = 12.0, Step = 2.0 }
        };

        ExecuteParametricSweepFromExperimentalDataRequest sweepRequest = new()
        {
            ExperimentalDataIdentifier = experimentalIdentifier,
            MechanicalModelName = modelName,
            ExecutionMode = PipelineExecutionMode.Sequential,
            BaseStrain = 0.04,
            StrainRange = new RangeParameters { InitialPoint = 0.03, FinalPoint = 0.05, Step = 0.01 },
            ConstantVariables = constantVariables,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 1.0,
            TimeStep = 10.0,
            FinalTime = 51.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(1.0, 51.0, 1e-2, 50)
        };

        ExecuteParametricSweepFromExperimentalDataCommand sweepCommand = _serviceProvider.GetRequiredService<ExecuteParametricSweepFromExperimentalDataCommand>();
        ChannelPipelineProgressNotifier progressNotifier = Assert.IsType<ChannelPipelineProgressNotifier>(_serviceProvider.GetRequiredService<IPipelineProgressNotifier>());

        // Act
        Result<IAsyncEnumerable<string>> commandResult = await sweepCommand.ExecuteAsync(sweepRequest);
        Assert.True(commandResult.Success, commandResult.JoinedMessages);
        Assert.NotNull(commandResult.Data);

        List<string> sweepIdentifiers = [];
        await foreach (string sweepId in commandResult.Data)
        {
            Assert.False(string.IsNullOrWhiteSpace(sweepId));
            _createdIdentifiers.Add(sweepId);
            sweepIdentifiers.Add(sweepId);
        }

        // Assert
        Assert.NotEmpty(sweepIdentifiers);

        foreach (string sweepId in sweepIdentifiers)
        {
            List<PipelineProgressEvent> sweepEvents = await DrainProgressEventsWithTimeoutAsync(progressNotifier, sweepId, TimeSpan.FromSeconds(60));
            Assert.NotEmpty(sweepEvents);
        }

        List<ParametricSweepEntity> persistedForRun = [.. _persistedSweeps.Where(s => sweepIdentifiers.Contains(s.ExternalIdentifier!))];
        Assert.Equal(sweepIdentifiers.Count, persistedForRun.Count);

        foreach (string sweepIdentifier in sweepIdentifiers)
        {
            string sweepDir = Path.Combine(Path.GetTempPath(), "storage", sweepIdentifier);
            Assert.True(Directory.Exists(sweepDir), $"Expected parametric sweep CSV directory '{sweepDir}'.");

            string[] csvFiles = Directory.GetFiles(sweepDir, "*.csv", SearchOption.AllDirectories);
            Assert.True(csvFiles.Length >= 2, $"Expected CSV files for swept variables + strain in '{sweepDir}'.");

            foreach (string csvFile in csvFiles)
            {
                string[] lines = await File.ReadAllLinesAsync(csvFile);
                Assert.True(lines.Length >= 2, $"CSV '{csvFile}' must contain header and sweep rows.");
                Assert.Equal(ExpectedSweepCsvHeader, lines[0]);
            }
        }
    }

    /// <summary>
    /// Fits real multi-strain <c>LCA_02_07_21_L38_D11</c> relaxation segments via <see cref="IMechanicalModelCurveFitterStep"/>,
    /// persists the curve fit output, and runs <see cref="IPipeline{ParametricSweepPipelineInput, String}"/> directly with
    /// the fitted constitutive parameters baseline across "Fung", "SimplifiedFung", and "Schapery", verifying
    /// step-by-step telemetry, generated sensitivity CSVs, and persisted <see cref="ParametricSweepEntity"/> records.
    /// </summary>
    /// <param name="modelName">Constitutive model name to evaluate.</param>
    [Theory]
    [InlineData("Fung")]
    [InlineData("SimplifiedFung")]
    [InlineData("Schapery")]
    public async Task ExecuteCurveFit_ThenParametricSweepPipeline_AcrossAllMechanicalModels_ShouldGenerateSweepCsvsAndPersistSweepEntityAsync(string modelName)
    {
        // Arrange Step 1: Fit real LCA_02_07_21_L38_D11 segments including terminal Unknown marker
        string experimentalIdentifier = $"accept-model-bridge-{modelName.ToLowerInvariant()}-{Guid.NewGuid():N}";
        string sweepIdentifier = $"accept-model-sweep-{modelName.ToLowerInvariant()}-{Guid.NewGuid():N}";
        _createdIdentifiers.Add(experimentalIdentifier);
        _createdIdentifiers.Add(sweepIdentifier);

        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync($"{modelName}RelaxationOnly", useSharedFiles: true);
        (_, CurveSegment[] segments) = groups.First(g => g.Segments.Count(s => s.Type == SegmentType.Relaxation) >= 4);

        IMechanicalModelStepFactory stepFactory = _serviceProvider.GetRequiredService<IMechanicalModelStepFactory>();
        IRepository repository = _serviceProvider.GetRequiredService<IRepository>();
        ILoggerFactory loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

        await using IMechanicalModelCurveFitterStep fitterStep = stepFactory.CreateCurveFitterStep(
            modelName,
            [SegmentType.Relaxation],
            CurveFitterType.Alglib,
            targetRSquared: 0.85,
            confidenceLevel: 0.95);
        using CurveFitOutputIdentifierBuilderStep idBuilder = new();
        await using CurveFitOutputPersistenceStep fitPersistence = new(
            loggerFactory.CreateLogger<CurveFitOutputPersistenceStep>(),
            repository,
            experimentalIdentifier,
            modelName);

        List<MechanicalModelCurveFitOutput> outputs = [];
        foreach (CurveSegment segment in segments)
        {
            await foreach (MechanicalModelCurveFitOutput fitOutput in fitterStep.ExecuteAsync(segment, CancellationToken.None))
            {
                outputs.Add(fitOutput);
            }
        }

        await foreach (MechanicalModelCurveFitOutput fitOutput in fitterStep.ExecuteAsync(new CurveSegment
        {
            Type = SegmentType.Unknown,
            TimePoints = [],
            ExperimentalStrain = [],
            ExperimentalStress = []
        }, CancellationToken.None))
        {
            outputs.Add(fitOutput);
        }

        Assert.NotEmpty(outputs);
        MechanicalModelCurveFitOutput selectedFit = outputs[^1];
        string fitIdentifier = idBuilder.Execute(selectedFit);
        await fitPersistence.ExecuteAsync((fitIdentifier, selectedFit));
        Assert.Contains(_persistedCurveFits, c => c.ExtenalIdentifier == experimentalIdentifier && c.MechanicalModelName == modelName);

        // Arrange Step 2: Run parametric sweep directly using the fitted constitutive baseline
        IPipeline<ParametricSweepPipelineInput, string> sweepPipeline = _serviceProvider.GetRequiredService<IPipeline<ParametricSweepPipelineInput, string>>();
        ChannelPipelineProgressNotifier progressNotifier = Assert.IsType<ChannelPipelineProgressNotifier>(_serviceProvider.GetRequiredService<IPipelineProgressNotifier>());

        Dictionary<string, RangeParameters> constantVariables = string.Equals(modelName, "Schapery", StringComparison.Ordinal)
            ? new Dictionary<string, RangeParameters>
            {
                ["Ge"] = new RangeParameters { InitialPoint = 5.0, FinalPoint = 15.0, Step = 5.0 }
            }
            : new Dictionary<string, RangeParameters>
            {
                ["ElasticStressConstant"] = new RangeParameters { InitialPoint = 8.0, FinalPoint = 12.0, Step = 2.0 }
            };

        ParametricSweepPipelineInput sweepInput = new()
        {
            Identifier = sweepIdentifier,
            MechanicalModelName = modelName,
            Baseline = selectedFit.ConstitutiveParameters,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            StrainRange = new RangeParameters { InitialPoint = 0.03, FinalPoint = 0.05, Step = 0.01 },
            BaseStrain = 0.04,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 1.0,
            TimeStep = 10.0,
            FinalTime = 51.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(1.0, 51.0, 1e-2, 50)
        };

        // Act
        Task<Result<string>> sweepTask = sweepPipeline.ProcessAsync(sweepInput);
        List<PipelineProgressEvent> sweepEvents = await DrainProgressEventsWithTimeoutAsync(progressNotifier, sweepIdentifier, TimeSpan.FromSeconds(60));
        Result<string> sweepResult = await sweepTask;

        // Assert
        Assert.True(sweepResult.Success, $"Sweep failed for '{modelName}': {sweepResult.JoinedMessages}");
        Assert.Equal(sweepIdentifier, sweepResult.Data);
        Assert.Contains(sweepEvents, e => e.StepName == $"{modelName}ParametricSweepGeneratorStep" && e.State == PipelineStepState.Completed);
        Assert.Contains(sweepEvents, e => e.StepName == "ParametricSweepSimulationStep");
        Assert.Contains(sweepEvents, e => e.StepName == "ParametricSweepCsvWriterStep");
        Assert.Contains(sweepEvents, e => e.StepName == "ParametricSweepPersistenceStep");

        Assert.Contains(_persistedSweeps, s => s.ExternalIdentifier == sweepIdentifier && s.MechanicalModelName == modelName);

        string sweepDir = Path.Combine(Path.GetTempPath(), "storage", sweepIdentifier);
        Assert.True(Directory.Exists(sweepDir));
        string[] csvFiles = Directory.GetFiles(sweepDir, "*.csv", SearchOption.AllDirectories);
        Assert.True(csvFiles.Length >= 2);

        foreach (string csvFile in csvFiles)
        {
            string[] lines = await File.ReadAllLinesAsync(csvFile);
            Assert.True(lines.Length >= 2);
            Assert.Equal(ExpectedSweepCsvHeader, lines[0]);
        }
    }

    /// <summary>
    /// No-op async initialization required by <see cref="IAsyncLifetime"/>.
    /// </summary>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Disposes the async service provider and cleans up temporary storage folders written under
    /// <c>%TEMP%\storage\&lt;identifier&gt;</c>.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();

        foreach (string identifier in _createdIdentifiers.Distinct(StringComparer.Ordinal))
        {
            DeleteIfExists(Path.Combine(Path.GetTempPath(), "storage", identifier));
            DeleteIfExists(Path.Combine(Path.GetTempPath(), "storage", "ExperimentalData", identifier));
        }
    }

    private static void DeleteIfExists(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup of temp artifacts
        }
    }

    private async IAsyncEnumerable<MechanicalModelCurveFitOutputEntity> StreamMatchingCurveFitsAsync(
        MechanicalModelCurveFitOutputFilter filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        foreach (MechanicalModelCurveFitOutputEntity entity in _persistedCurveFits)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(filter.ExtenalIdentifier) && !string.Equals(entity.ExtenalIdentifier, filter.ExtenalIdentifier, StringComparison.Ordinal))
                continue;

            if (!string.IsNullOrWhiteSpace(filter.MechanicalModelName) && !string.Equals(entity.MechanicalModelName, filter.MechanicalModelName, StringComparison.Ordinal))
                continue;

            yield return entity;
        }
    }

    private static (string StrainPath, string StressPath) GetSoftTissueRawPaths()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "MechanicalOfMaterials.Optimizations", "CurveFitterSteps", "TestData", "SoftTissueRaw");
        string strainPath = Path.Combine(directory, "LCA_02_07_21_L38_D11_strain.csv");
        string stressPath = Path.Combine(directory, "LCA_02_07_21_L38_D11_stress.csv");

        Assert.True(File.Exists(strainPath), $"Missing test dataset file: {strainPath}");
        Assert.True(File.Exists(stressPath), $"Missing test dataset file: {stressPath}");

        return (strainPath, stressPath);
    }

    private static async Task<List<PipelineProgressEvent>> DrainProgressEventsWithTimeoutAsync(
        ChannelPipelineProgressNotifier notifier,
        string identifier,
        TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        List<PipelineProgressEvent> events = [];

        while (!cts.IsCancellationRequested)
        {
            try
            {
                await foreach (PipelineProgressEvent progressEvent in notifier.ReadAllAsync(identifier, cts.Token))
                {
                    events.Add(progressEvent);
                }

                return events;
            }
            catch (KeyNotFoundException)
            {
                await Task.Delay(15, cts.Token);
            }
        }

        throw new TimeoutException($"Timed out waiting for progress channel '{identifier}'.");
    }
}
