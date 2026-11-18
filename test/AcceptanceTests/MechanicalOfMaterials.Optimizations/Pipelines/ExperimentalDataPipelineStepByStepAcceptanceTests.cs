using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Channels;
using AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;
using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.Pipelines;

/// <summary>
/// Acceptance tests for <see cref="IPipeline{TInput, TOutput}"/> executing the experimental data
/// processing DAG step-by-step across constitutive mechanical models ("Fung", "SimplifiedFung", "Schapery"),
/// validating real-time progress notifications, generated physical CSV artifacts, and persisted entities.
/// </summary>
public sealed class ExperimentalDataPipelineStepByStepAcceptanceTests : IAsyncLifetime
{
    private readonly List<string> _createdIdentifiers = [];
    private readonly ServiceProvider _serviceProvider;
    private readonly ConcurrentBag<StorageEntryMetadataEntity> _persistedFiles = [];
    private readonly ConcurrentBag<MechanicalModelCurveFitOutputEntity> _persistedCurveFits = [];
    private readonly ConcurrentBag<MechanicalModelSimulationEntity> _persistedSimulations = [];

    /// <summary>
    /// Initializes a self-contained dependency injection container mirroring production wiring
    /// for <see cref="ExecuteExperimentalDataPipeline"/> with an in-memory <see cref="IRepository"/> double.
    /// </summary>
    public ExperimentalDataPipelineStepByStepAcceptanceTests()
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
                    IngestionBufferCapacity = 32
                },
                new ParametricSweepSettings
                {
                    IngestionBufferCapacity = 32
                })
            .AddSingleton<IRepository>(repositoryMock.Object);

        services.TryAddTransient<ExecuteExperimentalDataPipeline>();

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that <see cref="IPipeline{TInput, TOutput}"/> executes every DAG step in order for
    /// "Fung" and "SimplifiedFung" on the real <c>LCA_02_07_21_L38_D11</c> experimental raw CSV pair,
    /// emits expected step telemetry states, writes valid physical CSV outputs, and persists database entities.
    /// </summary>
    /// <param name="modelName">The constitutive mechanical model name to fit and simulate.</param>
    [Theory]
    [InlineData("Fung")]
    [InlineData("SimplifiedFung")]
    public async Task ExecuteExperimentalDataPipeline_AcrossMechanicalModels_ShouldExecuteEveryStepAndPersistValidOutputsAsync(string modelName)
    {
        // Arrange
        string identifier = $"accept-exp-{modelName.ToLowerInvariant()}-{Guid.NewGuid():N}";
        _createdIdentifiers.Add(identifier);

        (string strainPath, string stressPath) = GetSoftTissueRawPaths();
        IPipeline<ExperimentalDataProcessingInput, string> pipeline = _serviceProvider.GetRequiredService<IPipeline<ExperimentalDataProcessingInput, string>>();
        ChannelPipelineProgressNotifier progressNotifier = Assert.IsType<ChannelPipelineProgressNotifier>(_serviceProvider.GetRequiredService<IPipelineProgressNotifier>());

        ExperimentalDataProcessingInput input = new()
        {
            Identifier = identifier,
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
        };

        // Act
        Task<Result<string>> pipelineTask = pipeline.ProcessAsync(input);
        List<PipelineProgressEvent> events = await DrainProgressEventsAsync(progressNotifier, identifier, pipelineTask);
        Result<string> result = await pipelineTask;

        // Assert: Pipeline Result
        Assert.True(result.Success, $"Pipeline for '{modelName}' failed: {result.JoinedMessages}");
        Assert.Equal(identifier, result.Data);

        // Assert: Step-by-Step Telemetry Coverage
        string curveFitterStepName = $"{modelName}RelaxationOnlyCurveFitterStep";

        AssertStepStates(events, "ExperimentalDataSegmenter", PipelineStepState.Started, PipelineStepState.Running, PipelineStepState.Completed);
        AssertStepStates(events, nameof(ExperimentalDataFileWriterStep), PipelineStepState.Started, PipelineStepState.Running);
        AssertStepStates(events, nameof(ExperimentalDataDownsamplerStep), PipelineStepState.Started, PipelineStepState.Running, PipelineStepState.Completed);
        AssertStepStates(events, nameof(CurveSegmentBuilderStep), PipelineStepState.Started, PipelineStepState.Running, PipelineStepState.Completed);
        AssertStepStates(events, curveFitterStepName, PipelineStepState.Started, PipelineStepState.Running, PipelineStepState.Completed);
        AssertStepStates(events, nameof(CurveFitOutputIdentifierBuilderStep), PipelineStepState.Started, PipelineStepState.Running);
        AssertStepStates(events, nameof(MechanicalModelSimulationStep), PipelineStepState.Started, PipelineStepState.Running);
        AssertStepStates(events, nameof(CurveFitOutputPersistenceStep), PipelineStepState.Started, PipelineStepState.Running);
        AssertStepStates(events, nameof(MechanicalModelOutputPersistenceStep), PipelineStepState.Started, PipelineStepState.Running);

        // Assert: Persisted Curve Fit Entities
        List<MechanicalModelCurveFitOutputEntity> curveFits = [.. _persistedCurveFits.Where(c => c.ExtenalIdentifier == identifier)];
        Assert.NotEmpty(curveFits);
        Assert.All(curveFits, curveFit =>
        {
            Assert.Equal(modelName, curveFit.MechanicalModelName);
            Assert.False(string.IsNullOrWhiteSpace(curveFit.Identifier));
            Assert.False(string.IsNullOrWhiteSpace(curveFit.ConstitutiveParameters));
            Assert.True(curveFit.InitialAcceptedRange > 0.0);
            Assert.True(curveFit.FinalAcceptedRange >= curveFit.InitialAcceptedRange);
        });

        // Assert: Persisted Simulation Entities
        List<MechanicalModelSimulationEntity> simulations = [.. _persistedSimulations.Where(s => s.ExternalIdentifier == identifier)];
        Assert.NotEmpty(simulations);
        Assert.All(simulations, sim =>
        {
            Assert.Equal(modelName, sim.MechanicalModelName);
            Assert.True(sim.ExperimentalDataFileMetadataId > 0);
            Assert.True(sim.SimulationOutputFileMetadataId > 0);
            Assert.False(string.IsNullOrWhiteSpace(sim.InitialOutputJson));
            Assert.False(string.IsNullOrWhiteSpace(sim.FinalOutputJson));
            Assert.False(string.IsNullOrWhiteSpace(sim.AbsoluteDeltaOutputJson));
            Assert.False(string.IsNullOrWhiteSpace(sim.PercentageDeltaOutputJson));
        });

        // Assert: Physical Disk CSV Artifacts via persisted metadata entries
        List<StorageEntryMetadataEntity> storedMetadata = [.. _persistedFiles.Where(f => f.ExternalIdentifier == identifier)];
        Assert.True(storedMetadata.Count >= 2, "At least experimental segmented CSV and simulation output CSV metadata must be persisted.");

        StorageEntryMetadataEntity experimentalMetadata = storedMetadata.First(f =>
            f.Directory.Contains("ExperimentalData", StringComparison.OrdinalIgnoreCase)
            || f.Location.Contains("ExperimentalData", StringComparison.OrdinalIgnoreCase));
        Assert.True(File.Exists(experimentalMetadata.Location), $"Expected segmented CSV file at '{experimentalMetadata.Location}'.");

        string[] segmentedLines = await File.ReadAllLinesAsync(experimentalMetadata.Location);
        Assert.True(segmentedLines.Length > 10, "Segmented CSV should contain header and processed experimental data rows.");
        Assert.Equal("Time,Strain,Stress,SegmentType,StrainRate,StrainAcceleration,StressRate,StressAcceleration", segmentedLines[0]);

        List<StorageEntryMetadataEntity> simulationMetadata = [.. storedMetadata.Where(f => !string.Equals(f.Location, experimentalMetadata.Location, StringComparison.OrdinalIgnoreCase))];
        Assert.NotEmpty(simulationMetadata);

        foreach (StorageEntryMetadataEntity simFile in simulationMetadata)
        {
            Assert.True(File.Exists(simFile.Location), $"Expected simulation CSV file at '{simFile.Location}'.");
            string[] simLines = await File.ReadAllLinesAsync(simFile.Location);
            Assert.True(simLines.Length > 2, $"Simulation CSV '{simFile.Location}' must contain header and simulation rows.");
            string[] sampleColumns = simLines[1].Split(',');
            Assert.True(sampleColumns.Length >= 2);
            Assert.True(double.TryParse(sampleColumns[0], CultureInfo.InvariantCulture, out double parsedTime) && double.IsFinite(parsedTime));
        }
    }

    /// <summary>
    /// Verifies that <see cref="SchaperyRelaxationOnlyCurveFitterStep"/> followed by
    /// <see cref="CurveFitOutputIdentifierBuilderStep"/>, <see cref="MechanicalModelSimulationStep"/>,
    /// <see cref="CurveFitOutputPersistenceStep"/>, and <see cref="MechanicalModelOutputPersistenceStep"/>
    /// processes real multi-strain <c>LCA_02_07_21_L38_D11</c> relaxation segments and persists all artifacts.
    /// </summary>
    [Fact]
    public async Task ExecuteSchaperySteps_OnRealMultiStrainLigamentSegments_ShouldFitSimulateAndPersistAllArtifactsAsync()
    {
        // Arrange
        string identifier = $"accept-exp-schapery-{Guid.NewGuid():N}";
        _createdIdentifiers.Add(identifier);

        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync("SchaperyRelaxationOnly", useSharedFiles: true);
        (_, CurveSegment[] allSegments) = groups.First(g => g.Segments.Count(s => s.Type == SegmentType.Relaxation) >= 4);

        IMechanicalModelStepFactory stepFactory = _serviceProvider.GetRequiredService<IMechanicalModelStepFactory>();
        IRepository repository = _serviceProvider.GetRequiredService<IRepository>();
        ILoggerFactory loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        IFileStorageService fileStorageService = _serviceProvider.GetRequiredService<IFileStorageService>();
        IMechanicalModelTypeResolver typeResolver = _serviceProvider.GetRequiredKeyedService<IMechanicalModelTypeResolver>("Schapery");
        IMechanicalModelCalculatorFactory calculatorFactory = _serviceProvider.GetRequiredService<IMechanicalModelCalculatorFactory>();

        // Write segmented experimental points CSV to disk first so MechanicalModelOutputPersistenceStep can fetch its metadata
        (string expLocation, ExperimentalDataFileWriterStep expWriter) = await ExperimentalDataFileWriterStep.CreateAsync(fileStorageService, identifier);
        foreach (CurveSegment segment in allSegments)
        {
            for (int i = 0; i < segment.TimePoints.Length; i++)
            {
                await expWriter.ExecuteAsync(new SegmentedDataPoint(
                    segment.Type,
                    new ProcessedDataPoint(segment.TimePoints[i], segment.ExperimentalStrain[i], 0.0, 0.0, segment.ExperimentalStress[i], 0.0, 0.0)));
            }
        }

        await expWriter.DisposeAsync();

        await using IMechanicalModelCurveFitterStep fitterStep = stepFactory.CreateCurveFitterStep(
            "Schapery",
            [SegmentType.Relaxation],
            CurveFitterType.Alglib,
            targetRSquared: 0.85,
            confidenceLevel: 0.95);
        using CurveFitOutputIdentifierBuilderStep idBuilder = new();
        await using MechanicalModelSimulationStep simulationStep = new(
            fileStorageService,
            typeResolver,
            calculatorFactory,
            identifier,
            simulationTimeStep: null,
            finalSimulationTime: null,
            asymptoteConsecutivePointsThreshold: 5);
        await using CurveFitOutputPersistenceStep curveFitPersistence = new(
            loggerFactory.CreateLogger<CurveFitOutputPersistenceStep>(),
            repository,
            identifier,
            "Schapery");
        await using MechanicalModelOutputPersistenceStep simulationPersistence = new(
            loggerFactory.CreateLogger<MechanicalModelOutputPersistenceStep>(),
            fileStorageService,
            repository,
            identifier,
            "Schapery");

        List<MechanicalModelCurveFitOutput> fitOutputs = [];

        // Act
        foreach (CurveSegment segment in allSegments)
        {
            await foreach (MechanicalModelCurveFitOutput fitOutput in fitterStep.ExecuteAsync(segment, CancellationToken.None))
            {
                fitOutputs.Add(fitOutput);
            }
        }

        CurveSegment terminalSegment = new()
        {
            Type = SegmentType.Unknown,
            TimePoints = [],
            ExperimentalStrain = [],
            ExperimentalStress = []
        };

        await foreach (MechanicalModelCurveFitOutput fitOutput in fitterStep.ExecuteAsync(terminalSegment, CancellationToken.None))
        {
            fitOutputs.Add(fitOutput);
        }

        foreach (MechanicalModelCurveFitOutput fitOutput in fitOutputs)
        {
            Assert.IsType<SchaperyConstitutiveParameters>(fitOutput.ConstitutiveParameters);
            string fitId = idBuilder.Execute(fitOutput);
            (double? asymptoteTime, SimulationDeltaOutput deltaOutput, StorageEntryMetadata simMetadata) = await simulationStep.ExecuteAsync(fitOutput);
            await curveFitPersistence.ExecuteAsync((fitId, fitOutput));
            await simulationPersistence.ExecuteAsync(new MechanicalModelOutputPersistenceInput(expLocation, fitId, asymptoteTime, deltaOutput, simMetadata));
        }

        // Assert
        Assert.True(fitOutputs.Count >= 2, $"Expected anchor, segment, and consolidated Schapery fits, got {fitOutputs.Count}.");

        List<MechanicalModelCurveFitOutputEntity> curveFits = [.. _persistedCurveFits.Where(c => c.ExtenalIdentifier == identifier && c.MechanicalModelName == "Schapery")];
        Assert.Equal(fitOutputs.Count, curveFits.Count);

        List<MechanicalModelSimulationEntity> simulations = [.. _persistedSimulations.Where(s => s.ExternalIdentifier == identifier && s.MechanicalModelName == "Schapery")];
        Assert.Equal(fitOutputs.Count, simulations.Count);

        List<StorageEntryMetadataEntity> storedMetadata = [.. _persistedFiles.Where(f => f.ExternalIdentifier == identifier)];
        Assert.True(storedMetadata.Count >= 2);
        Assert.All(storedMetadata, f => Assert.True(File.Exists(f.Location), $"Missing Schapery output file '{f.Location}'."));
    }

    /// <summary>
    /// Verifies that executing <see cref="ExecuteExperimentalDataPipeline"/> command with a non-zero
    /// <see cref="ExecuteExperimentalDataPipelineRequestBase.DownsamplingSkipTimeStep"/> returns
    /// 202 Accepted immediately, downsamples the raw data stream cleanly, and persists curve fit and simulation entities.
    /// </summary>
    [Fact]
    public async Task ExecuteExperimentalDataPipeline_WithDownsamplingSkipTimeStep_ShouldReduceSegmentedPointsAndCompleteAllStepsAsync()
    {
        // Arrange
        string baselineIdentifier = $"accept-exp-full-{Guid.NewGuid():N}";
        string downsampledIdentifier = $"accept-exp-downsampled-{Guid.NewGuid():N}";
        _createdIdentifiers.Add(baselineIdentifier);
        _createdIdentifiers.Add(downsampledIdentifier);

        (string strainPath, string stressPath) = GetSoftTissueRawPaths();
        ExecuteExperimentalDataPipeline command = _serviceProvider.GetRequiredService<ExecuteExperimentalDataPipeline>();
        ChannelPipelineProgressNotifier progressNotifier = Assert.IsType<ChannelPipelineProgressNotifier>(_serviceProvider.GetRequiredService<IPipelineProgressNotifier>());

        ExecuteExperimentalDataPipelineRequest baselineRequest = new()
        {
            MechanicalModelName = "SimplifiedFung",
            TargetSegments = [SegmentType.Relaxation],
            CurveFitterType = CurveFitterType.Alglib,
            TargetRSquared = 0.85,
            ConfidenceLevel = 0.95,
            AsymptoteConsecutivePointsThreshold = 5,
            Options = new ExperimentalDataProcessingOptions(BufferSize: 10, AccelerationTolerance: 2.0),
            ExperimentalDataGroupRequest = new ExperimentalDataGroupRequest
            {
                Identifier = baselineIdentifier,
                StrainFileLocation = strainPath,
                StressFileLocation = stressPath,
                StartExperimentalTimeThreshold = 1202.0
            }
        };

        ExecuteExperimentalDataPipelineRequest downsampledRequest = baselineRequest with
        {
            DownsamplingSkipTimeStep = 0.5,
            ExperimentalDataGroupRequest = baselineRequest.ExperimentalDataGroupRequest with
            {
                Identifier = downsampledIdentifier
            }
        };

        // Act: Run baseline and downsampled commands
        Result<string> baselineResponse = await command.ExecuteAsync(baselineRequest);
        Assert.True(baselineResponse.Success, baselineResponse.JoinedMessages);
        Assert.Equal(StatusCode.Accepted, baselineResponse.StatusCode);
        List<PipelineProgressEvent> baselineEvents = await DrainProgressEventsWithTimeoutAsync(progressNotifier, baselineIdentifier, TimeSpan.FromSeconds(60));

        Result<string> downsampledResponse = await command.ExecuteAsync(downsampledRequest);
        Assert.True(downsampledResponse.Success, downsampledResponse.JoinedMessages);
        Assert.Equal(StatusCode.Accepted, downsampledResponse.StatusCode);
        List<PipelineProgressEvent> downsampledEvents = await DrainProgressEventsWithTimeoutAsync(progressNotifier, downsampledIdentifier, TimeSpan.FromSeconds(60));

        // Assert: Both completed all steps cleanly
        AssertStepStates(downsampledEvents, nameof(ExperimentalDataDownsamplerStep), PipelineStepState.Started, PipelineStepState.Running, PipelineStepState.Completed);
        AssertStepStates(downsampledEvents, "SimplifiedFungRelaxationOnlyCurveFitterStep", PipelineStepState.Started, PipelineStepState.Running, PipelineStepState.Completed);

        // Downsampling must forward strictly fewer items out of Downsampler into CurveSegmentBuilder than the baseline run
        int baselineSegmentBuilderMaxReceived = baselineEvents
            .Where(e => e.StepName == nameof(CurveSegmentBuilderStep))
            .Max(e => e.ReceivedItems);
        int downsampledSegmentBuilderMaxReceived = downsampledEvents
            .Where(e => e.StepName == nameof(CurveSegmentBuilderStep))
            .Max(e => e.ReceivedItems);

        Assert.True(
            downsampledSegmentBuilderMaxReceived < baselineSegmentBuilderMaxReceived,
            $"Expected downsampling (0.5s skip) to pass fewer points to CurveSegmentBuilderStep ({downsampledSegmentBuilderMaxReceived}) than baseline ({baselineSegmentBuilderMaxReceived}).");

        Assert.Contains(_persistedCurveFits, c => c.ExtenalIdentifier == downsampledIdentifier && c.MechanicalModelName == "SimplifiedFung");
        Assert.Contains(_persistedSimulations, s => s.ExternalIdentifier == downsampledIdentifier && s.MechanicalModelName == "SimplifiedFung");
    }

    /// <summary>
    /// No-op async initialization required by <see cref="IAsyncLifetime"/>.
    /// </summary>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Disposes the async service provider (including <see cref="ChannelPipelineProgressNotifier"/>)
    /// and cleans up temporary storage folders written to <c>%TEMP%\storage\&lt;identifier&gt;</c>.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();

        foreach (string identifier in _createdIdentifiers)
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

    private static (string StrainPath, string StressPath) GetSoftTissueRawPaths()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "MechanicalOfMaterials.Optimizations", "CurveFitterSteps", "TestData", "SoftTissueRaw");
        string strainPath = Path.Combine(directory, "LCA_02_07_21_L38_D11_strain.csv");
        string stressPath = Path.Combine(directory, "LCA_02_07_21_L38_D11_stress.csv");

        Assert.True(File.Exists(strainPath), $"Missing test dataset file: {strainPath}");
        Assert.True(File.Exists(stressPath), $"Missing test dataset file: {stressPath}");

        return (strainPath, stressPath);
    }

    private static void AssertStepStates(List<PipelineProgressEvent> events, string stepName, params PipelineStepState[] expectedStates)
    {
        List<PipelineStepState> observed = [.. events.Where(e => e.StepName == stepName).Select(e => e.State)];
        Assert.NotEmpty(observed);

        foreach (PipelineStepState state in expectedStates)
        {
            Assert.Contains(state, observed);
        }
    }

    private static async Task<List<PipelineProgressEvent>> DrainProgressEventsAsync(
        ChannelPipelineProgressNotifier notifier,
        string identifier,
        Task pipelineTask)
    {
        List<PipelineProgressEvent> events = [];
        bool pipelineCompletedBeforeChannelFound = false;

        while (true)
        {
            try
            {
                await foreach (PipelineProgressEvent progressEvent in notifier.ReadAllAsync(identifier))
                {
                    events.Add(progressEvent);
                }

                return events;
            }
            catch (KeyNotFoundException)
            {
                if (pipelineCompletedBeforeChannelFound)
                    return events;

                if (pipelineTask.IsCompleted)
                    pipelineCompletedBeforeChannelFound = true;
                else
                    await Task.Delay(10);
            }
        }
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
