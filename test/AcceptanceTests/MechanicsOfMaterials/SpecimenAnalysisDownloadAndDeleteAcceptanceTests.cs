using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcceptanceTests.MechanicsOfMaterials;

/// <summary>
/// End-to-end Kestrel + physical disk acceptance tests verifying that <c>POST /api/v1/mechanics/specimen-analysis/execute</c>
/// computes and persists the generated CSV file and metadata record, streams the persisted CSV through
/// <c>GET /api/v1/storage-entry-metadata/{fileId}/download</c>, and cleanly deletes both metadata and physical file
/// via <c>DELETE /api/v1/storage-entry-metadata/{fileId}</c>.
/// </summary>
public sealed class SpecimenAnalysisDownloadAndDeleteAcceptanceTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Executes a deterministic elastic specimen analysis over HTTP, downloads the persisted CSV artifact,
    /// and deletes both metadata and physical file via the storage-entry-metadata endpoint.
    /// </summary>
    [Fact]
    public async Task ExecuteSpecimenAnalysis_OverHttp_PersistsCsvStreamsDownloadAndDeletesPhysicalFileFromDisk()
    {
        string tempStorageDir = Path.Combine(Path.GetTempPath(), $"mst-specimen-accept-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempStorageDir);

        try
        {
            ConcurrentDictionary<long, StorageEntryMetadataEntity> metadataStore = new();
            await using WebApplication app = await StartHostAsync(tempStorageDir, metadataStore);
            string baseAddress = GetServerBaseAddress(app);

            using HttpClient client = new() { BaseAddress = new Uri(baseAddress) };
            using JsonDocument constitutiveJson = JsonDocument.Parse("""{"youngModulus": 120.0}""");
            ExecuteSpecimenAnalysisRequest request = CreateElasticRequest("accept-download-1", constitutiveJson.RootElement.Clone());

            using HttpResponseMessage executeResponse = await client.PostAsJsonAsync("/api/v1/mechanics/specimen-analysis/execute", request, _jsonOptions);
            Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);

            Result<ExecuteSpecimenAnalysisResultData>? executePayload = await executeResponse.Content.ReadFromJsonAsync<Result<ExecuteSpecimenAnalysisResultData>>(_jsonOptions);
            Assert.NotNull(executePayload);
            Assert.True(executePayload.Success, executePayload.JoinedMessages);
            Assert.NotNull(executePayload.Data);
            Assert.True(executePayload.Data.OutputMetadataEntityId > 0L);
            Assert.NotNull(executePayload.Data.DeltaOutput);

            long fileId = executePayload.Data.OutputMetadataEntityId;
            string[] generatedFiles = Directory.GetFiles(tempStorageDir, "*.csv", SearchOption.AllDirectories);
            Assert.Single(generatedFiles);

            using HttpResponseMessage downloadResponse = await client.GetAsync($"/api/v1/storage-entry-metadata/{fileId}/download");
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
            Assert.Equal("text/csv", downloadResponse.Content.Headers.ContentType?.MediaType);
            Assert.EndsWith(".csv", downloadResponse.Content.Headers.ContentDisposition?.FileNameStar ?? downloadResponse.Content.Headers.ContentDisposition?.FileName?.Trim('"'), StringComparison.OrdinalIgnoreCase);

            string csvContent = await downloadResponse.Content.ReadAsStringAsync();
            Assert.Contains("Time", csvContent);
            Assert.True(csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length >= 4);

            using HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/v1/storage-entry-metadata/{fileId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            string[] remainingFiles = Directory.GetFiles(tempStorageDir, "*.csv", SearchOption.AllDirectories);
            Assert.Empty(remainingFiles);
            Assert.Empty(metadataStore);
        }
        finally
        {
            if (Directory.Exists(tempStorageDir))
                Directory.Delete(tempStorageDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that invalid ForceDisplacement geometry requests return HTTP 400 and unregistered mechanical models
    /// return HTTP 404 without writing orphan CSV files to physical disk.
    /// </summary>
    [Fact]
    public async Task ExecuteSpecimenAnalysis_WithInvalidRequestOrUnknownModel_ReturnsExpectedErrorAndWritesNoFile()
    {
        string tempStorageDir = Path.Combine(Path.GetTempPath(), $"mst-specimen-err-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempStorageDir);

        try
        {
            ConcurrentDictionary<long, StorageEntryMetadataEntity> metadataStore = new();
            await using WebApplication app = await StartHostAsync(tempStorageDir, metadataStore);
            string baseAddress = GetServerBaseAddress(app);

            using HttpClient client = new() { BaseAddress = new Uri(baseAddress) };
            using JsonDocument constitutiveJson = JsonDocument.Parse("""{"youngModulus": 120.0}""");

            ExecuteSpecimenAnalysisRequest missingSpecimenRequest = CreateElasticRequest("accept-invalid-1", constitutiveJson.RootElement.Clone()) with
            {
                LoadResponseRelationship = LoadResponseRelationship.ForceDisplacement,
                Specimen = null
            };
            using HttpResponseMessage badRequestResponse = await client.PostAsJsonAsync("/api/v1/mechanics/specimen-analysis/execute", missingSpecimenRequest, _jsonOptions);
            Assert.Equal(HttpStatusCode.BadRequest, badRequestResponse.StatusCode);

            ExecuteSpecimenAnalysisRequest unknownModelRequest = CreateElasticRequest("accept-unknown-1", constitutiveJson.RootElement.Clone()) with
            {
                MechanicalModelName = "NonExistentConstitutiveModel"
            };
            using HttpResponseMessage notFoundResponse = await client.PostAsJsonAsync("/api/v1/mechanics/specimen-analysis/execute", unknownModelRequest, _jsonOptions);
            Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);

            string[] filesOnDisk = Directory.GetFiles(tempStorageDir, "*.csv", SearchOption.AllDirectories);
            Assert.Empty(filesOnDisk);
            Assert.Empty(metadataStore);
        }
        finally
        {
            if (Directory.Exists(tempStorageDir))
                Directory.Delete(tempStorageDir, recursive: true);
        }
    }

    private static ExecuteSpecimenAnalysisRequest CreateElasticRequest(string identifier, JsonElement constitutiveParameters) => new()
    {
        MechanicalModelName = nameof(MechanicalModel.Elastic),
        AnalysisIdentifier = identifier,
        AnalysisParameter = new AnalysisParameter
        {
            InitialDependentMechanicalParameterValue = 0.0,
            IndependentMechanicalParameter = new MechanicalParameter(0.05)
        },
        LoadResponseRelationship = LoadResponseRelationship.StressStrain,
        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
        RampTimeConsideration = RampTimeConsideration.Disregard,
        InitialTime = 0.0,
        TimeStep = 0.5,
        FinalTime = 1.0,
        ConstitutiveParameters = constitutiveParameters
    };

    private static async Task<WebApplication> StartHostAsync(
        string baseDirectory,
        ConcurrentDictionary<long, StorageEntryMetadataEntity> metadataStore)
    {
        long nextId = 0L;
        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref nextId);
                metadataStore[id] = entity with { Id = id };
                return id;
            });
        repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => metadataStore.TryGetValue(id, out StorageEntryMetadataEntity? entity) ? entity : null);
        repositoryMock
            .Setup(r => r.DeleteAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long id, CancellationToken ct) =>
            {
                metadataStore.TryRemove(id, out _);
                return Task.CompletedTask;
            });

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        builder.Services.ConfigureToolsHttpJsonOptions();

        FileStorageSettings storageSettings = new()
        {
            DefaultStorageProvider = StorageProvider.Local,
            Local = new LocalFileStorageSettings { BaseDirectory = baseDirectory }
        };

        ResiliencePipelineSettings resilienceSettings = new()
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Constant,
            DelayInMilliseconds = 50,
            UseJitter = false
        };

        builder.Services.AddCoreServices(
            defaultResiliencePipelineSettings: resilienceSettings,
            fileStorageSettings: storageSettings);
        builder.Services.AddWebApiServices(resilienceSettings);
        builder.Services.AddMathematicsServices();
        builder.Services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);
        builder.Services.AddSingleton(repositoryMock.Object);
        builder.Services.AddSingleton(Mock.Of<ICurrentUserProvider>(u => u.IsSuperuser()));
        builder.Services.AddSingleton(provider => provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.Local));

        WebApplication app = builder.Build();
        app.MapMechanicsOfMaterialsEndpoints();
        app.MapStorageEntryMetadataEndpoints();
        await app.StartAsync();
        return app;
    }

    private static string GetServerBaseAddress(WebApplication app)
    {
        IServer server = app.Services.GetRequiredService<IServer>();
        IServerAddressesFeature addresses = server.Features.Get<IServerAddressesFeature>()!;
        return addresses.Addresses.First();
    }
}
