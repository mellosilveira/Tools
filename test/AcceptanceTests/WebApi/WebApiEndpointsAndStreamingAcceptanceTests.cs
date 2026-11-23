using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Models;
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
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Application.Endpoints;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Application.Models;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace AcceptanceTests.WebApi;

/// <summary>
/// Full-stack HTTP client-server acceptance tests over real Kestrel TCP sockets validating:
/// NDJSON streaming with HTTP chunked trailers (<see cref="ApplicationConstants.StreamStatusTrailerName"/>),
/// <see cref="PluginEndpoints.MapPluginEndpoints"/> multipart upload/load/list/cache routes,
/// Optimizations controllers (<c>ExperimentalDataController</c>, <c>ParametricSweepController</c>,
/// <c>PipelineProgressController</c>, and <c>StorageEntryMetadataController</c> CSV downloads).
/// </summary>
public sealed class WebApiEndpointsAndStreamingAcceptanceTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _storageBaseDir = Path.Combine(Path.GetTempPath(), "storage");
    private readonly string _pluginDirectoryPrefix = $"accept-webapi-plugins-{Guid.NewGuid():N}";
    private readonly List<string> _createdPipelineIdentifiers = [];
    private readonly ConcurrentDictionary<long, StorageEntryMetadataEntity> _storedFiles = new();
    private readonly ConcurrentBag<MechanicalModelCurveFitOutputEntity> _storedCurveFits = [];
    private readonly ConcurrentBag<MechanicalModelSimulationEntity> _storedSimulations = [];
    private readonly ConcurrentBag<ParametricSweepEntity> _storedSweeps = [];
    private long _nextEntityId = 1L;
    private WebApplication? _app;
    private string _baseAddress = string.Empty;
    private string _http2BaseAddress = string.Empty;

    /// <summary>
    /// Starts a live Kestrel host bound to <c>http://127.0.0.1:0</c> hosting all MVC controllers,
    /// plugin minimal API endpoints, CRUD streaming endpoints, and diagnostic streaming error routes.
    /// </summary>
    public async Task InitializeAsync()
    {
        StorageEntryMetadataEntity seeded = new()
        {
            Id = 1L,
            ExternalIdentifier = "EXP-KNEE-001",
            StorageProvider = StorageProvider.Local,
            Location = "optimizations/experiments/knee_ligament_01.csv",
            Directory = "optimizations/experiments",
            FileName = "knee_ligament_01.csv",
            SizeBytes = 2048,
            ContentType = "text/csv"
        };
        _storedFiles[seeded.Id] = seeded;

        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref _nextEntityId);
                _storedFiles[id] = entity with { Id = id };
                return Result.CreateSuccessCreated(id);
            });

        repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => _storedFiles.TryGetValue(id, out StorageEntryMetadataEntity? entity) ? entity : null);

        repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(
                It.IsAny<StorageEntryMetadataFilter>(),
                It.IsAny<Pagination?>(),
                It.IsAny<CancellationToken>()))
            .Returns((StorageEntryMetadataFilter filter, Pagination? _, CancellationToken ct) =>
                StreamStorageMetadataAsync(filter, ct));

        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<MechanicalModelCurveFitOutputEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MechanicalModelCurveFitOutputEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref _nextEntityId);
                _storedCurveFits.Add(entity with { Id = id });
                return Result.CreateSuccessCreated(id);
            });

        repositoryMock
            .Setup(r => r.GetAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(
                It.IsAny<MechanicalModelCurveFitOutputFilter>(),
                It.IsAny<Pagination?>(),
                It.IsAny<CancellationToken>()))
            .Returns((MechanicalModelCurveFitOutputFilter filter, Pagination? _, CancellationToken ct) =>
                StreamCurveFitsAsync(filter, ct));

        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<MechanicalModelSimulationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MechanicalModelSimulationEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref _nextEntityId);
                _storedSimulations.Add(entity with { Id = id });
                return Result.CreateSuccessCreated(id);
            });

        repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParametricSweepEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref _nextEntityId);
                _storedSweeps.Add(entity with { Id = id });
                return Result.CreateSuccessCreated(id);
            });

        ResiliencePipelineSettings resilienceSettings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            DelayInMilliseconds = 20,
            MaxRetryAttempts = 2,
            UseJitter = false
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
            kestrel.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        builder.Services
            .AddCoreServices(
                resilienceSettings,
                fileStorageSettings: new FileStorageSettings
                {
                    Local = new LocalFileStorageSettings
                    {
                        BaseDirectory = _storageBaseDir
                    }
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
            .AddSingleton(provider => provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.Local))
            .AddWebApiServices(resilienceSettings)
            .AddSingleton<IPluginTypeProcessor, ParameterlessPluginTypeProcessor>()
            .AddPluginServices(new PluginSettings
            {
                StorageProvider = StorageProvider.Local,
                Directory = _pluginDirectoryPrefix,
                PollInterval = TimeSpan.FromMinutes(5),
                PreviousVersionRetention = TimeSpan.FromMinutes(30),
                DefaultCacheTarget = PluginCacheTargets.File
            })
            .AddSingleton(repositoryMock.Object)
            .ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new TypeJsonConverter()))
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .AddApplicationPart(typeof(ExperimentalDataController).Assembly);

        _app = builder.Build();
        _app.UseCustomMiddlewares();
        _app.MapControllers();
        _app.MapPluginEndpoints("/api/v1/plugins");
        _app.MapCrud<StorageEntryMetadataEntity, StorageEntryMetadataFilter>("/api/v1/storage-metadata-minimal", "StorageMetadataMinimal");
        _app.MapGet("/api/v1/stream/fault-midstream", async (HttpContext httpContext, ILogger<WebApiEndpointsAndStreamingAcceptanceTests> logger) =>
        {
            await FaultAfterOneItemAsync(seeded).WriteNdjsonAsync(httpContext, logger, "fault-stream", httpContext.RequestAborted);
        });

        await _app.StartAsync();
        string[] urls = [.. _app.Urls];
        _baseAddress = urls[0];
        _http2BaseAddress = urls[1];
    }

    /// <summary>
    /// Stops the Kestrel host and deletes temporary plugin and storage folders from disk.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        DeleteDirectoryIfExists(Path.Combine(_storageBaseDir, _pluginDirectoryPrefix));

        foreach (string id in _createdPipelineIdentifiers.Distinct(StringComparer.Ordinal))
        {
            DeleteDirectoryIfExists(Path.Combine(_storageBaseDir, id));
            DeleteDirectoryIfExists(Path.Combine(_storageBaseDir, "ExperimentalData", id));
        }
    }

    /// <summary>
    /// Validates real Kestrel HTTP/1.1 chunked NDJSON streaming: verifies <c>/stream</c> delivers all items
    /// and commits the <c>X-Stream-Status: success</c> trailer, while a mid-stream failure delivers initial
    /// chunks followed by <c>X-Stream-Status: Faulted</c> readable over <see cref="HttpClient"/>.
    /// </summary>
    [Fact]
    public async Task CrudStreamEndpoint_AndMidStreamError_OverRealKestrel_ShouldStreamNdjsonItemsAndEmitStatusTrailersAsync()
    {
        // Arrange
        using StreamTestAgent agent = CreateStreamAgent();
        using HttpClient http2Client = new()
        {
            BaseAddress = new Uri(_http2BaseAddress),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        // Act 1: Stream /api/v1/storage-metadata-minimal/stream via ApiServiceAgentBase.GetStreamAsync (HTTP/1.1)
        List<StorageEntryMetadataEntity> streamedViaAgent = [];
        await foreach (StorageEntryMetadataEntity item in agent.CallGetStreamAsync<StorageEntryMetadataEntity>("/api/v1/storage-metadata-minimal/stream"))
        {
            streamedViaAgent.Add(item);
        }

        // Assert 1: Seeded entity received cleanly
        StorageEntryMetadataEntity single = Assert.Single(streamedViaAgent);
        Assert.Equal("EXP-KNEE-001", single.ExternalIdentifier);

        // Act 2: Inspect real Kestrel HTTP/2 streaming response + trailing headers on success stream
        using HttpResponseMessage okResponse = await http2Client.GetAsync("/api/v1/storage-metadata-minimal/stream", HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        Assert.Equal(ApplicationConstants.NdjsonContentType, okResponse.Content.Headers.ContentType?.MediaType);
        string okBody = await okResponse.Content.ReadAsStringAsync();
        Assert.Contains("EXP-KNEE-001", okBody);
        Assert.True(okResponse.TrailingHeaders.TryGetValues(ApplicationConstants.StreamStatusTrailerName, out IEnumerable<string>? okTrailers));
        Assert.Contains(ApplicationConstants.StreamSuccessfullyStatus, okTrailers);

        // Act 3: Inspect mid-stream exception over Kestrel HTTP/2 trailers
        using HttpResponseMessage faultResponse = await http2Client.GetAsync("/api/v1/stream/fault-midstream", HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, faultResponse.StatusCode);

        List<string> faultLines = [];
        await using (Stream stream = await faultResponse.Content.ReadAsStreamAsync())
        using (StreamReader reader = new(stream, Encoding.UTF8))
        {
            while (await reader.ReadLineAsync() is string line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    faultLines.Add(line);
            }
        }

        Assert.Single(faultLines);
        Assert.Contains("EXP-KNEE-001", faultLines[0]);
        Assert.True(faultResponse.TrailingHeaders.TryGetValues(ApplicationConstants.StreamStatusTrailerName, out IEnumerable<string>? faultTrailers));
        Assert.Contains("Faulted", faultTrailers);
    }

    /// <summary>
    /// Validates <see cref="PluginEndpoints.MapPluginEndpoints"/> over real Kestrel HTTP requests:
    /// multipart DLL upload to physical disk, loading all staged plugins, querying the plugin registry,
    /// persisting cache to file, clearing cache, and restoring cache from disk.
    /// </summary>
    [Fact]
    public async Task PluginEndpoints_OverRealKestrel_ShouldUploadMultipartLoadPersistClearAndRestorePluginsAsync()
    {
        // Arrange
        using HttpClient client = new() { BaseAddress = new Uri(_baseAddress) };
        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();
        const string fileName = "RealKestrel.Analytics.v1.0.0.dll";

        using MultipartFormDataContent multipart = new();
        ByteArrayContent fileContent = new(assemblyBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        multipart.Add(fileContent, "file", fileName);

        // Act 1: Multipart upload
        HttpResponseMessage uploadResponse = await client.PostAsync("/api/v1/plugins/upload", multipart);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        string physicalIncomingPath = Path.Combine(_storageBaseDir, _pluginDirectoryPrefix, PluginStoragePartitions.Incoming, fileName);
        Assert.True(File.Exists(physicalIncomingPath), $"Expected uploaded plugin DLL on disk at '{physicalIncomingPath}'.");

        // Act 2: Load all staged plugins into runtime DI + move to loaded/
        HttpResponseMessage loadAllResponse = await client.PostAsync("/api/v1/plugins/load/all", null);
        Assert.Equal(HttpStatusCode.OK, loadAllResponse.StatusCode);

        string physicalLoadedPath = Path.Combine(_storageBaseDir, _pluginDirectoryPrefix, PluginStoragePartitions.Loaded, fileName);
        Assert.False(File.Exists(physicalIncomingPath));
        Assert.True(File.Exists(physicalLoadedPath));

        // Act 3: Persist cache to file, clear in-memory cache, and restore from physical disk JSON
        HttpResponseMessage persistResponse = await client.PostAsync("/api/v1/plugins/cache/file/persist", null);
        Assert.Equal(HttpStatusCode.OK, persistResponse.StatusCode);

        string cacheJsonPath = Path.Combine(_storageBaseDir, _pluginDirectoryPrefix, PluginStoragePartitions.Cache, "plugin-cache.json");
        Assert.True(File.Exists(cacheJsonPath), $"Expected persisted cache JSON at '{cacheJsonPath}'.");
        string cacheJson = await File.ReadAllTextAsync(cacheJsonPath);
        Assert.Contains("RealKestrel.Analytics", cacheJson);

        HttpResponseMessage clearResponse = await client.DeleteAsync("/api/v1/plugins/cache");
        Assert.Equal(HttpStatusCode.OK, clearResponse.StatusCode);

        HttpResponseMessage restoreResponse = await client.PostAsync("/api/v1/plugins/cache/file/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        // Assert GET plugins endpoint returns 200 OK with persisted plugin metadata
        HttpResponseMessage getResponse = await client.GetAsync("/api/v1/plugins");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        string pluginsJson = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains("RealKestrel.Analytics", pluginsJson);
    }

    /// <summary>
    /// Executes a complete Optimizations workflow over real Kestrel HTTP calls:
    /// submits <c>POST /api/v1/experimental-data/process</c> for "Fung" using the real <c>LCA_02_07_21_L38_D11</c>
    /// CSV files, streams live <see cref="PipelineProgressEvent"/> NDJSON chunks via
    /// <c>GET /api/v1/pipelines/{id}/progress</c>, chains <c>POST /api/v1/parametric-sweeps/process-from-experimental-data</c>,
    /// streams the sweep progress NDJSON, and downloads generated physical CSV artifacts via
    /// <c>GET /api/v1/storage-entry-metadata/{fileId}/download</c>.
    /// </summary>
    [Fact]
    public async Task OptimizationsControllers_OverRealKestrel_ShouldProcessExperimentalDataStreamProgressRunSweepAndDownloadCsvAsync()
    {
        // Arrange
        using HttpClient client = new() { BaseAddress = new Uri(_baseAddress) };
        (string strainPath, string stressPath) = GetSoftTissueRawPaths();
        string experimentalIdentifier = $"webapi-exp-fung-{Guid.NewGuid():N}";
        _createdPipelineIdentifiers.Add(experimentalIdentifier);

        ExecuteExperimentalDataPipelineRequest expRequest = new()
        {
            MechanicalModelName = "Fung",
            TargetSegments = [SegmentType.Relaxation],
            CurveFitterType = CurveFitterType.Alglib,
            TargetRSquared = 0.85,
            ConfidenceLevel = 0.95,
            AsymptoteConsecutivePointsThreshold = 5,
            Options = new ExperimentalDataProcessingOptions(BufferSize: 10, AccelerationTolerance: 2.0),
            ExperimentalDataGroupRequest = new ExperimentalDataGroupRequest
            {
                Identifier = experimentalIdentifier,
                StrainFileLocation = strainPath,
                StressFileLocation = stressPath,
                StartExperimentalTimeThreshold = 1202.0
            }
        };

        // Act 1: POST /api/v1/experimental-data/process -> 202 Accepted
        HttpResponseMessage expHttpResponse = await client.PostAsJsonAsync("/api/v1/experimental-data/process", expRequest, WebJsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, expHttpResponse.StatusCode);
        Result<string>? expResult = await expHttpResponse.Content.ReadFromJsonAsync<Result<string>>(WebJsonOptions);
        Assert.NotNull(expResult);
        Assert.True(expResult.Success);
        Assert.Equal(experimentalIdentifier, expResult.Data);

        // Act 2: Stream real-time NDJSON progress events from GET /api/v1/pipelines/{id}/progress
        List<PipelineProgressEvent> expProgressEvents = await ReadPipelineProgressStreamWithRetryAsync(client, experimentalIdentifier, TimeSpan.FromSeconds(60));
        Assert.NotEmpty(expProgressEvents);
        Assert.Contains(expProgressEvents, e => e.StepName == "ExperimentalDataSegmenter" && e.State == PipelineStepState.Completed);
        Assert.Contains(expProgressEvents, e => e.StepName == "FungRelaxationOnlyCurveFitterStep" && e.State == PipelineStepState.Completed);

        Assert.Contains(_storedCurveFits, c => c.ExtenalIdentifier == experimentalIdentifier && c.MechanicalModelName == "Fung");
        Assert.Contains(_storedSimulations, s => s.ExternalIdentifier == experimentalIdentifier && s.MechanicalModelName == "Fung");

        // Act 3: POST /api/v1/parametric-sweeps/process-from-experimental-data -> 202 Accepted NDJSON of sweep identifiers
        ExecuteParametricSweepFromExperimentalDataRequest sweepRequest = new()
        {
            ExperimentalDataIdentifier = experimentalIdentifier,
            MechanicalModelName = "Fung",
            ExecutionMode = PipelineExecutionMode.Sequential,
            BaseStrain = 0.04,
            StrainRange = new RangeParameters { InitialPoint = 0.03, FinalPoint = 0.05, Step = 0.01 },
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                ["ElasticStressConstant"] = new RangeParameters { InitialPoint = 8.0, FinalPoint = 12.0, Step = 2.0 }
            },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 1.0,
            TimeStep = 10.0,
            FinalTime = 51.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(1.0, 51.0, 1e-2, 50)
        };

        using HttpResponseMessage sweepHttpResponse = await client.PostAsJsonAsync(
            "/api/v1/parametric-sweeps/process-from-experimental-data",
            sweepRequest,
            WebJsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, sweepHttpResponse.StatusCode);

        List<string> sweepIds = [];
        await using (Stream bodyStream = await sweepHttpResponse.Content.ReadAsStreamAsync())
        using (StreamReader reader = new(bodyStream, Encoding.UTF8))
        {
            while (await reader.ReadLineAsync() is string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string? sweepId = JsonSerializer.Deserialize<string>(line, WebJsonOptions);
                if (!string.IsNullOrWhiteSpace(sweepId))
                {
                    _createdPipelineIdentifiers.Add(sweepId);
                    sweepIds.Add(sweepId);
                }
            }
        }

        Assert.NotEmpty(sweepIds);

        // Act 4: Stream sweep NDJSON progress and verify sweep persistence
        foreach (string sweepId in sweepIds)
        {
            List<PipelineProgressEvent> sweepProgressEvents = await ReadPipelineProgressStreamWithRetryAsync(client, sweepId, TimeSpan.FromSeconds(60));
            Assert.NotEmpty(sweepProgressEvents);
            Assert.Contains(sweepProgressEvents, e => e.StepName == "FungParametricSweepGeneratorStep" && e.State == PipelineStepState.Completed);
            Assert.Contains(_storedSweeps, s => s.ExternalIdentifier == sweepId);
        }

        // Act 5: Download and verify BOTH the experimental segmented CSV and a parametric sweep CSV
        StorageEntryMetadataEntity experimentalCsvEntity = _storedFiles.Values.First(f =>
            f.ExternalIdentifier == experimentalIdentifier
            && f.Location.Contains("ExperimentalData", StringComparison.OrdinalIgnoreCase));
        HttpResponseMessage expDownloadResponse = await client.GetAsync($"/api/v1/storage-entry-metadata/{experimentalCsvEntity.Id}/download");
        Assert.Equal(HttpStatusCode.OK, expDownloadResponse.StatusCode);
        Assert.Equal("text/csv", expDownloadResponse.Content.Headers.ContentType?.MediaType);
        string expCsv = await expDownloadResponse.Content.ReadAsStringAsync();
        Assert.StartsWith("Time,Strain,Stress,SegmentType,StrainRate,StrainAcceleration,StressRate,StressAcceleration", expCsv);

        StorageEntryMetadataEntity sweepCsvEntity = _storedFiles.Values.First(f =>
            f.ExternalIdentifier is not null && sweepIds.Contains(f.ExternalIdentifier));
        HttpResponseMessage sweepDownloadResponse = await client.GetAsync($"/api/v1/storage-entry-metadata/{sweepCsvEntity.Id}/download");
        Assert.Equal(HttpStatusCode.OK, sweepDownloadResponse.StatusCode);
        Assert.Equal("text/csv", sweepDownloadResponse.Content.Headers.ContentType?.MediaType);
        string sweepCsv = await sweepDownloadResponse.Content.ReadAsStringAsync();
        Assert.StartsWith("VariableValue,InitialStress,FinalStress,AbsoluteDeltaStress,PercentageDeltaStress,AsymptoteTime", sweepCsv);
    }

    private StreamTestAgent CreateStreamAgent()
    {
        ILogger<StreamTestAgent> logger = _app!.Services.GetRequiredService<ILogger<StreamTestAgent>>();
        ApiServiceAgentResiliencePipeline pipeline = _app.Services.GetRequiredService<ApiServiceAgentResiliencePipeline>();
        return new StreamTestAgent(logger, new StreamAgentSettings
        {
            BaseAddress = _baseAddress,
            DefaultTimeoutInSeconds = 15
        }, pipeline);
    }

    private static async Task<List<PipelineProgressEvent>> ReadPipelineProgressStreamWithRetryAsync(
        HttpClient client,
        string pipelineId,
        TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        while (!cts.IsCancellationRequested)
        {
            using HttpResponseMessage response = await client.GetAsync(
                $"/api/v1/pipelines/{pipelineId}/progress",
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await Task.Delay(25, cts.Token);
                continue;
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ApplicationConstants.NdjsonContentType, response.Content.Headers.ContentType?.MediaType);

            List<PipelineProgressEvent> events = [];
            await using Stream stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using StreamReader reader = new(stream, Encoding.UTF8);

            while (await reader.ReadLineAsync(cts.Token) is string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                PipelineProgressEvent? evt = JsonSerializer.Deserialize<PipelineProgressEvent>(line, WebJsonOptions);
                if (evt is not null)
                    events.Add(evt);
            }

            if (events.Count > 0)
                return events;

            await Task.Delay(25, cts.Token);
        }

        throw new TimeoutException($"Timed out polling /api/v1/pipelines/{pipelineId}/progress.");
    }

    private static (string StrainPath, string StressPath) GetSoftTissueRawPaths()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "MechanicalOfMaterials.Optimizations", "CurveFitterSteps", "TestData", "SoftTissueRaw");
        return (
            Path.Combine(directory, "LCA_02_07_21_L38_D11_strain.csv"),
            Path.Combine(directory, "LCA_02_07_21_L38_D11_stress.csv"));
    }

    private static async IAsyncEnumerable<StorageEntryMetadataEntity> FaultAfterOneItemAsync(StorageEntryMetadataEntity firstItem)
    {
        await Task.Yield();
        yield return firstItem;
        throw new InvalidOperationException("Simulated mid-stream failure over chunked HTTP/1.1.");
    }

    private static void DeleteDirectoryIfExists(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup of temp directory
        }
    }

    private async IAsyncEnumerable<StorageEntryMetadataEntity> StreamStorageMetadataAsync(
        StorageEntryMetadataFilter filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        foreach (StorageEntryMetadataEntity entity in _storedFiles.Values.OrderBy(e => e.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(filter.ExternalIdentifier)
                && !string.Equals(entity.ExternalIdentifier, filter.ExternalIdentifier, StringComparison.Ordinal))
            {
                continue;
            }

            yield return entity;
        }
    }

    private async IAsyncEnumerable<MechanicalModelCurveFitOutputEntity> StreamCurveFitsAsync(
        MechanicalModelCurveFitOutputFilter filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        foreach (MechanicalModelCurveFitOutputEntity entity in _storedCurveFits)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(filter.ExtenalIdentifier)
                && !string.Equals(entity.ExtenalIdentifier, filter.ExtenalIdentifier, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(filter.MechanicalModelName)
                && !string.Equals(entity.MechanicalModelName, filter.MechanicalModelName, StringComparison.Ordinal))
            {
                continue;
            }

            yield return entity;
        }
    }

    private sealed class StreamAgentSettings : ApiServiceAgentSettings
    {
    }

    private sealed class StreamTestAgent(
        ILogger logger,
        ApiServiceAgentSettings settings,
        ApiServiceAgentResiliencePipeline resiliencePipeline)
        : ApiServiceAgentBase(logger, settings, resiliencePipeline)
    {
        public override string ServiceName => nameof(StreamTestAgent);

        public IAsyncEnumerable<T> CallGetStreamAsync<T>(string uri, int timeoutMs = 15000, CancellationToken cancellationToken = default) where T : class
            => GetStreamAsync<T>(uri, timeoutMs, nameof(CallGetStreamAsync), cancellationToken);
    }

    private sealed class ParameterlessPluginTypeProcessor : IPluginTypeProcessor
    {
        public Type ProcessableType => typeof(object);

        public void Process(Type type, PluginRegistrationContext context)
        {
            if (type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) is null)
                return;

            if (context.IsStartup)
                context.Services!.AddSingleton(type);
            else
                context.DynamicProvider!.Add(type, type);
        }
    }
}
