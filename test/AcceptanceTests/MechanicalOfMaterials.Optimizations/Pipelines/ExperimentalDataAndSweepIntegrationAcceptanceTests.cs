using MelloSilveiraTools;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
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
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.Pipelines;

/// <summary>
/// End-to-end acceptance tests integrating real SoftTissue ligament experimental CSV datasets across
/// segmentation, curve fitting, persistence, and experimental-data-driven parametric sweep pipelines.
/// </summary>
public sealed class ExperimentalDataAndSweepIntegrationAcceptanceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"mst-opt-accept-{Guid.NewGuid():N}");
    private readonly string _storageDir;
    private readonly ServiceProvider _serviceProvider;
    private readonly ConcurrentBag<MechanicalModelCurveFitOutputEntity> _savedCurveFits = [];
    private readonly ConcurrentBag<MechanicalModelSimulationEntity> _savedSimulations = [];
    private readonly ConcurrentBag<ParametricSweepEntity> _savedSweeps = [];
    private readonly ConcurrentBag<StorageEntryMetadataEntity> _savedFiles = [];
    private readonly ConcurrentQueue<string> _logs = [];

    /// <summary>
    /// Initializes an isolated DI container configured with real optimization pipelines, local storage, and an in-memory repository spy.
    /// </summary>
    public ExperimentalDataAndSweepIntegrationAcceptanceTests()
    {
        string pluginsDir = Path.Combine(_tempRoot, "plugins");
        _storageDir = Path.Combine(_tempRoot, "storage");
        Directory.CreateDirectory(pluginsDir);
        Directory.CreateDirectory(_storageDir);

        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<MechanicalModelCurveFitOutputEntity>(), It.IsAny<CancellationToken>()))
            .Callback<MechanicalModelCurveFitOutputEntity, CancellationToken>((entity, _) => _savedCurveFits.Add(entity))
            .ReturnsAsync(Result.CreateSuccessOk(1L));
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<MechanicalModelSimulationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<MechanicalModelSimulationEntity, CancellationToken>((entity, _) => _savedSimulations.Add(entity))
            .ReturnsAsync(Result.CreateSuccessOk(1L));
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ParametricSweepEntity, CancellationToken>((entity, _) => _savedSweeps.Add(entity))
            .ReturnsAsync(Result.CreateSuccessOk(1L));
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .Callback<StorageEntryMetadataEntity, CancellationToken>((entity, _) => _savedFiles.Add(entity))
            .ReturnsAsync(Result.CreateSuccessOk(1L));
        repositoryMock
            .Setup(r => r.GetAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(
                It.IsAny<MechanicalModelCurveFitOutputFilter>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns((MechanicalModelCurveFitOutputFilter filter, string? _, CancellationToken ct) => StreamSavedFitsAsync(filter, ct));

        PipelineStepOptions stepOptions = new() { MaxBufferSize = 32 };
        IServiceCollection services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information).AddProvider(new CaptureLoggerProvider(_logs)));
        services.AddToolsServices(
            defaultResiliencePipelineSettings: new ResiliencePipelineSettings
            {
                BackoffType = DelayBackoffType.Constant,
                DelayInMilliseconds = 50,
                MaxRetryAttempts = 1,
                UseJitter = false
            },
            databaseSettings: new DatabaseSettings
            {
                ConnectionString = "Host=localhost;Database=tests;Username=postgres;Password=postgres",
                UnitOperationTimeoutInMilliseconds = 30000,
                BulkOperationTimeoutInMilliseconds = 120000
            },
            encryptionSettings: new EncryptionSettings
            {
                SaltSize = 16,
                HashSize = 32,
                Iterations = 10_000
            },
            pluginSettings: new PluginSettings
            {
                StorageProvider = StorageProvider.Local,
                Directory = pluginsDir,
                DefaultCacheTarget = PluginCacheTargets.File,
                PollInterval = TimeSpan.FromSeconds(30),
                PreviousVersionRetention = TimeSpan.FromHours(1)
            },
            fileStorageSettings: new FileStorageSettings
            {
                Local = new LocalFileStorageSettings { BaseDirectory = _storageDir }
            },
            curveFitterOptions: new CurveFitterSettings
            {
                DefaultCurveFitter = CurveFitterType.Alglib
            },
            experimentalDataSettings: new ExperimentalDataSettings
            {
                IngestionBufferCapacity = 32,
                SegmenterOptions = stepOptions,
                FileWriterOptions = stepOptions,
                PersistanceOptions = stepOptions,
                SegmentBuilderOptions = stepOptions,
                CurveFitterOptions = stepOptions,
                SimulationInputBuilderOptions = stepOptions
            },
            parametricSweepSettings: new ParametricSweepSettings
            {
                IngestionBufferCapacity = 32
            },
            addMechanicalModels: true);

        services.AddSingleton(repositoryMock.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that the real ExperimentalDataProcessingPipeline ingests the SoftTissue raw strain/stress CSVs,
    /// writes processed experimental points, segments Ramp/Relaxation curves, and executes Fung curve fitting.
    /// </summary>
    [Fact]
    public async Task SoftTissue_ExperimentalDataProcessingPipeline_IngestsSegmentsAndFitsFungModel()
    {
        (string strainPath, string stressPath) = ResolveSoftTissueRawPaths();

        IPipeline<ExperimentalDataProcessingInput, string> pipeline = _serviceProvider.GetRequiredService<IPipeline<ExperimentalDataProcessingInput, string>>();
        string identifier = $"lca-fung-{Guid.NewGuid():N}";

        Result<string> result = await pipeline.ProcessAsync(new ExperimentalDataProcessingInput
        {
            Identifier = identifier,
            MechanicalModelName = nameof(MechanicalModel.Fung),
            StartExperimentalTimeThreshold = 1202.0,
            StrainFileLocation = strainPath,
            StressFileLocation = stressPath,
            Options = new ExperimentalDataProcessingOptions(BufferSize: 10, AccelerationTolerance: 2.0),
            DownsamplingSkipTimeStep = null,
            TargetSegments = [SegmentType.Relaxation],
            CurveFitterType = CurveFitterType.Alglib,
            TargetRSquared = 0.80,
            ConfidenceLevel = 0.95,
            SimulationTimeStep = 0.5,
            FinalSimulationTime = 10.0,
            AsymptoteConsecutivePointsThreshold = 3
        });

        Assert.True(result.Success);
        Assert.Equal(identifier, result.Data);
        Assert.Contains(_savedCurveFits, e => e.ExtenalIdentifier == identifier);
        Assert.Contains(_savedSimulations, e => e.ExternalIdentifier == identifier);
        Assert.True(_savedFiles.Count(f => f.ExternalIdentifier == identifier) >= 2);

        // Clean up unkeyed LocalFileStorageService default path created in CoreDependencyInjection
        string defaultTempStorage = Path.Combine(Path.GetTempPath(), "storage", identifier);
        if (Directory.Exists(defaultTempStorage))
            Directory.Delete(defaultTempStorage, recursive: true);
    }

    /// <summary>
    /// Verifies the full SoftTissue chain: segment/fit Fung relaxation CSVs -> persist curve fit entity ->
    /// execute <see cref="ExecuteParametricSweepFromExperimentalDataCommand"/> end-to-end via real DI.
    /// </summary>
    [Fact]
    public async Task SoftTissue_FungFit_DrivesExecuteParametricSweepFromExperimentalDataEndToEnd()
    {
        string testDataRoot = ResolveTestDataRoot();
        CurveSegment[] segments =
        [
            .. await CurveFitterSteps.AcceptanceTestHelpers.LoadSegmentsFromDirectoryAsync(Path.Combine(testDataRoot, "FungRelaxationOnly"))
        ];
        Assert.NotEmpty(segments);

        IMechanicalModelStepFactory stepFactory = _serviceProvider.GetRequiredService<IMechanicalModelStepFactory>();
        IRepository repository = _serviceProvider.GetRequiredService<IRepository>();
        ILoggerFactory loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

        string expId = $"softtissue-fung-{Guid.NewGuid():N}";
        await using IMechanicalModelCurveFitterStep fitterStep = stepFactory.CreateCurveFitterStep(
            nameof(MechanicalModel.Fung),
            [SegmentType.Relaxation],
            CurveFitterType.Alglib,
            targetRSquared: 0.85);
        using CurveFitOutputIdentifierBuilderStep idBuilder = new();
        await using CurveFitOutputPersistenceStep persistenceStep = new(
            loggerFactory.CreateLogger<CurveFitOutputPersistenceStep>(),
            repository,
            expId,
            nameof(MechanicalModel.Fung));

        List<MechanicalModelCurveFitOutput> outputs = [];
        foreach (CurveSegment segment in segments)
        {
            await foreach (MechanicalModelCurveFitOutput output in fitterStep.ExecuteAsync(segment, CancellationToken.None))
            {
                outputs.Add(output);
                string fitId = idBuilder.Execute(output);
                await persistenceStep.ExecuteAsync((fitId, output));
            }
        }

        await foreach (MechanicalModelCurveFitOutput output in fitterStep.ExecuteAsync(new CurveSegment
        {
            Type = SegmentType.Unknown,
            TimePoints = [],
            ExperimentalStrain = [],
            ExperimentalStress = []
        }, CancellationToken.None))
        {
            outputs.Add(output);
            string fitId = idBuilder.Execute(output);
            await persistenceStep.ExecuteAsync((fitId, output));
        }

        Assert.NotEmpty(outputs);
        Assert.Contains(_savedCurveFits, e => e.ExtenalIdentifier == expId);

        ExecuteParametricSweepFromExperimentalDataCommand command = _serviceProvider.GetRequiredService<ExecuteParametricSweepFromExperimentalDataCommand>();
        Result<IAsyncEnumerable<string>> sweepResult = await command.ExecuteAsync(new ExecuteParametricSweepFromExperimentalDataRequest
        {
            ExperimentalDataIdentifier = expId,
            MechanicalModelName = nameof(MechanicalModel.Fung),
            BaseStrain = 0.03,
            StrainRange = new RangeParameters { InitialPoint = 0.02, Step = 0.02, FinalPoint = 0.04 },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.5,
            FinalTime = 5.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 20.0, 1e-4, 50),
            ExecutionMode = PipelineExecutionMode.Sequential
        });

        Assert.True(sweepResult.Success);
        List<string> sweepIds = [];
        await foreach (string id in sweepResult.Data!)
            sweepIds.Add(id);

        Assert.NotEmpty(sweepIds);

        // Wait for the background Task.Run sweep pipeline launched by ExecuteParametricSweepFromExperimentalDataCommand
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline && _savedSweeps.Count < sweepIds.Count)
            await Task.Delay(50);

        Assert.True(_savedSweeps.Any(s => s.ExternalIdentifier is not null && sweepIds.Contains(s.ExternalIdentifier)), $"No saved sweeps. Logs:\n{string.Join("\n", _logs)}");

        foreach (string sweepId in sweepIds)
        {
            string defaultSweepDir = Path.Combine(Path.GetTempPath(), "storage", sweepId);
            if (Directory.Exists(defaultSweepDir))
                Directory.Delete(defaultSweepDir, recursive: true);
        }
    }

    private sealed class CaptureLoggerProvider(ConcurrentQueue<string> logs) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName, logs);
        public void Dispose() { }
    }

    private sealed class CaptureLogger(string categoryName, ConcurrentQueue<string> logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => logs.Enqueue($"[{logLevel}] {categoryName}: {formatter(state, exception)}{(exception is not null ? " | EX: " + exception.GetType().Name + ": " + exception.Message : string.Empty)}");
    }

    private async IAsyncEnumerable<MechanicalModelCurveFitOutputEntity> StreamSavedFitsAsync(
        MechanicalModelCurveFitOutputFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        foreach (MechanicalModelCurveFitOutputEntity entity in _savedCurveFits)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (filter.ExtenalIdentifier is null || entity.ExtenalIdentifier == filter.ExtenalIdentifier)
                yield return entity;
        }
    }

    private static string ResolveTestDataRoot()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "MechanicalOfMaterials.Optimizations", "CurveFitterSteps", "TestData");
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Acceptance TestData directory not found: {path}");
        return path;
    }

    private static (string StrainPath, string StressPath) ResolveSoftTissueRawPaths()
    {
        string rawDir = Path.Combine(ResolveTestDataRoot(), "SoftTissueRaw");
        return (
            Path.Combine(rawDir, "LCA_02_07_21_L38_D11_strain.csv"),
            Path.Combine(rawDir, "LCA_02_07_21_L38_D11_stress.csv"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}
