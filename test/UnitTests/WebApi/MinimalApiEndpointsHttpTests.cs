using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Materials;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Endpoints;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Login;
using MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Register;
using MelloSilveiraTools.WebApi.Application.Commands.Users.ValidateToken;
using MelloSilveiraTools.WebApi.Application.Controllers;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Application.RateLimiting;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace UnitTests.WebApi;

public sealed class MinimalApiEndpointsHttpTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static JwtSettings CreateJwtSettings() => new()
    {
        Issuer = "MelloSilveiraTools.Tests",
        Audience = "MelloSilveiraTools.Tests",
        EncryptionKey = "MelloSilveiraToolsQuickstartKey!",
        SigningKey = "MelloSilveiraToolsSigningKey2026!!",
        ClockSkewInSeconds = 30,
        SecurityKeyType = SecurityKeyType.Symmetric,
        TokenExperitationTimeInMinutes = 60
    };

    private static ResiliencePipelineSettings CreateResilienceSettings() => new()
    {
        BackoffType = DelayBackoffType.Constant,
        DelayInMilliseconds = 1,
        MaxRetryAttempts = 1,
        UseJitter = false
    };

    [Fact]
    public async Task AuthEndpoints_AndAuthControllerMe_ExecuteFullHttpLifecycleAndMeBranches()
    {
        ConcurrentDictionary<long, UserEntity> users = new();
        long nextUserId = 100L;
        users[10L] = new UserEntity { Id = 10L, Username = "inactive_user", PasswordHash = "h", IsActive = false, IsSuperuser = false };

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetFirstOrDefaultAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFilter filter, SortOrder _, CancellationToken _) =>
                users.Values.FirstOrDefault(u => string.Equals(u.Username, filter.Username, StringComparison.OrdinalIgnoreCase)));
        repoMock
            .Setup(r => r.TryInsertAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref nextUserId);
                users[id] = entity with { Id = id };
                return Result.CreateSuccessCreated(id);
            });
        repoMock
            .Setup(r => r.GetAsync<UserEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => users.TryGetValue(id, out UserEntity? user) ? user : null);

        JwtSettings jwtSettings = CreateJwtSettings();
        ResiliencePipelineSettings resilience = CreateResilienceSettings();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services
            .AddCoreServices(
                resilience,
                encryptionSettings: new EncryptionSettings { SaltSize = 16, HashSize = 32, Iterations = 10_000 })
            .AddWebApiServices(resilience)
            .AddJweAuthentication(jwtSettings)
            .AddDefaultRateLimiting()
            .ConfigureToolsHttpJsonOptions()
            .AddSingleton(repoMock.Object);

        await using WebApplication app = builder.Build();
        app.UseWebApiSecurityPipeline();
        app.MapAuthEndpoints().RequireRateLimiting(RateLimitPolicies.Auth);
        app.MapGet("/api/v1/test/anon-compute", () => Results.Ok()).AllowAnonymous().RequireRateLimiting(RateLimitPolicies.AnonymousCompute);
        app.MapPost("/api/v1/test/plugin-anon", () => Results.Ok()).AllowAnonymous().RequireRateLimiting(RateLimitPolicies.PluginUpload);
        await app.StartAsync();

        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

        // 1. Register
        HttpResponseMessage registerResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserRequest { Username = "researcher1", Password = "Password123!" }, _jsonOptions);
        Assert.Equal(HttpStatusCode.OK, registerResp.StatusCode);
        using (JsonDocument regDoc = JsonDocument.Parse(await registerResp.Content.ReadAsStringAsync()))
        {
            Assert.True(regDoc.RootElement.GetProperty("success").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(regDoc.RootElement.GetProperty("data").GetProperty("value").GetString()));
        }

        // 2. Login (also exercises non-/register Auth rate-limit partition)
        HttpResponseMessage loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Username = "researcher1", Password = "Password123!" }, _jsonOptions);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        string activeToken;
        using (JsonDocument loginDoc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync()))
        {
            Assert.True(loginDoc.RootElement.GetProperty("success").GetBoolean());
            activeToken = loginDoc.RootElement.GetProperty("data").GetProperty("value").GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(activeToken));
        }

        // 3. Validate & Refresh
        HttpResponseMessage validateResp = await client.PostAsJsonAsync("/api/v1/auth/validate", new ValidateTokenRequest { Token = activeToken }, _jsonOptions);
        Assert.Equal(HttpStatusCode.OK, validateResp.StatusCode);
        HttpResponseMessage refreshResp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { Token = activeToken }, _jsonOptions);
        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);

        // 4. GET /me unauthenticated -> 401
        HttpResponseMessage meAnon = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAnon.StatusCode);

        // 5. GET /me authenticated active -> 200 OK
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", activeToken);
        HttpResponseMessage meActive = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meActive.StatusCode);
        Result<CurrentUserInfo>? mePayload = await meActive.Content.ReadFromJsonAsync<Result<CurrentUserInfo>>(_jsonOptions);
        Assert.True(mePayload?.Success);
        Assert.Equal("researcher1", mePayload?.Data?.Username);

        // 6. AnonymousCompute authenticated bypass + anonymous bucket + PluginUpload anonymous IP bucket
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/test/anon-compute")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/test/anon-compute")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/test/plugin-anon", null)).StatusCode);

        // 7. GET /me for inactive user -> 401 Unauthorized
        AuthenticationJweTokenService tokenGenerator = new(TimeProvider.System, jwtSettings);
        string inactiveToken = tokenGenerator.Generate(10L).Value;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", inactiveToken);
        HttpResponseMessage meInactive = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meInactive.StatusCode);

        // 8. AuthController.MeAsync direct unit assertions (null userId, inactive user, active user)
        AuthController controller = new();
        CurrentUserProvider currentUserProvider = new();

        JsonResult anonMeResult = Assert.IsType<JsonResult>(await controller.MeAsync(currentUserProvider, repoMock.Object));
        Assert.Equal(StatusCodes.Status401Unauthorized, anonMeResult.StatusCode);

        using (currentUserProvider.BeginScope(10L))
        {
            JsonResult inactiveMeResult = Assert.IsType<JsonResult>(await controller.MeAsync(currentUserProvider, repoMock.Object));
            Assert.Equal(StatusCodes.Status401Unauthorized, inactiveMeResult.StatusCode);
        }

        long activeId = users.Values.First(u => u.Username == "researcher1").Id;
        using (currentUserProvider.BeginScope(activeId))
        {
            JsonResult okMeResult = Assert.IsType<JsonResult>(await controller.MeAsync(currentUserProvider, repoMock.Object));
            Assert.Equal(StatusCodes.Status200OK, okMeResult.StatusCode);
        }

        await app.StopAsync();
    }

    [Fact]
    public async Task StorageEntryMetadataEndpoints_ExecuteCrudDownloadAuthAndEnforceRouteFileIdPrecedence()
    {
        string tempStorageDir = Path.Combine(Path.GetTempPath(), $"mst-sem-http-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempStorageDir);

        try
        {
            ConcurrentDictionary<long, StorageEntryMetadataEntity> store = new();
            long nextId = 0L;

            Mock<IRepository> repoMock = new();
            repoMock
                .Setup(r => r.GetAsync<UserEntity>(1L, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntity { Id = 1L, Username = "admin", PasswordHash = "h", IsActive = true, IsSuperuser = true });
            repoMock
                .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageEntryMetadataEntity entity, CancellationToken _) =>
                {
                    long id = Interlocked.Increment(ref nextId);
                    store[id] = entity with { Id = id };
                    return Result.CreateSuccessCreated(id);
                });
            repoMock
                .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageEntryMetadataEntity entity, CancellationToken _) =>
                {
                    long id = Interlocked.Increment(ref nextId);
                    store[id] = entity with { Id = id };
                    return id;
                });
            repoMock
                .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => store.TryGetValue(id, out StorageEntryMetadataEntity? e) ? e : null);
            repoMock
                .Setup(r => r.GetAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(
                    It.IsAny<StorageEntryMetadataFilter>(),
                    It.IsAny<Pagination?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((StorageEntryMetadataFilter _, Pagination? _, CancellationToken ct) => StreamEntitiesAsync(store.Values, ct));
            repoMock
                .Setup(r => r.TryUpdateAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageEntryMetadataEntity entity, CancellationToken _) =>
                {
                    if (!store.ContainsKey(entity.Id))
                        return false;
                    store[entity.Id] = entity;
                    return true;
                });
            repoMock
                .Setup(r => r.DeleteAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns((long id, CancellationToken ct) =>
                {
                    store.TryRemove(id, out _);
                    return Task.CompletedTask;
                });

            JwtSettings jwtSettings = CreateJwtSettings();
            ResiliencePipelineSettings resilience = CreateResilienceSettings();
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services
                .AddCoreServices(resilience, fileStorageSettings: new FileStorageSettings
                {
                    DefaultStorageProvider = StorageProvider.Local,
                    Local = new LocalFileStorageSettings { BaseDirectory = tempStorageDir }
                })
                .AddWebApiServices(resilience)
                .AddJweAuthentication(jwtSettings)
                .AddDefaultCors([], allowAnyOriginWhenEmpty: false)
                .ConfigureToolsHttpJsonOptions()
                .AddSingleton(repoMock.Object);

            await using WebApplication app = builder.Build();
            app.UseWebApiSecurityPipeline(useRateLimiter: false);
            app.MapStorageEntryMetadataEndpoints(requireAuthForMutations: true);
            await app.StartAsync();

            CorsOptions corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
            CorsPolicy? defaultCorsPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
            Assert.NotNull(defaultCorsPolicy);
            Assert.False(defaultCorsPolicy.AllowAnyOrigin);

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

            // 1. POST upload two files (append-only anonymous upload allowed even when requireAuthForMutations=true)
            long firstId = await UploadCsvAsync(client, "/api/v1/storage-entry-metadata", "id-1", "ctx-1", "file1.csv", "a,b\n1,2\n");
            long secondId = await UploadCsvAsync(client, "/api/v1/storage-entry-metadata", "id-2", "ctx-1", "file2.csv", "a,b\n3,4\n");
            Assert.NotEqual(firstId, secondId);

            // 2. GET list & verify anonymous DELETE is blocked with 401 Unauthorized when requireAuthForMutations=true
            HttpResponseMessage listResp = await client.GetAsync("/api/v1/storage-entry-metadata?Identifier=id-1&Context=ctx-1");
            Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);

            HttpResponseMessage unauthDelete = await client.DeleteAsync($"/api/v1/storage-entry-metadata/{firstId}");
            Assert.Equal(HttpStatusCode.Unauthorized, unauthDelete.StatusCode);

            // 3. PUT overwrite file1 (authenticated as superuser for public record mutations)
            AuthenticationJweTokenService tokenService = new(TimeProvider.System, jwtSettings);
            string adminToken = tokenService.Generate(1L).Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            using MultipartFormDataContent putContent = new();
            ByteArrayContent updatedBytes = new(Encoding.UTF8.GetBytes("a,b\n9,9\n"));
            updatedBytes.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            putContent.Add(updatedBytes, "file", "file1.csv");
            HttpResponseMessage putResp = await client.PutAsync($"/api/v1/storage-entry-metadata/{firstId}", putContent);
            Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
            Result<StorageEntryResultData>? putPayload = await putResp.Content.ReadFromJsonAsync<Result<StorageEntryResultData>>(_jsonOptions);
            Assert.True(putPayload?.Success);
            Assert.NotNull(putPayload?.Data);
            firstId = putPayload.Data.EntityId;

            // 4. GET download (200 OK & 404 NotFound)
            HttpResponseMessage dlOk = await client.GetAsync($"/api/v1/storage-entry-metadata/{firstId}/download");
            Assert.Equal(HttpStatusCode.OK, dlOk.StatusCode);
            Assert.Contains("9,9", await dlOk.Content.ReadAsStringAsync());

            HttpResponseMessage dlNotFound = await client.GetAsync("/api/v1/storage-entry-metadata/999999/download");
            Assert.Equal(HttpStatusCode.NotFound, dlNotFound.StatusCode);

            // 5. DELETE /{fileId} with conflicting FileId in body MUST delete route fileId, NOT body FileId
            using HttpRequestMessage deleteWithConflictingBody = new(HttpMethod.Delete, $"/api/v1/storage-entry-metadata/{firstId}")
            {
                Content = JsonContent.Create(new DeleteStorageEntryRequest { FileId = secondId }, options: _jsonOptions)
            };
            HttpResponseMessage delByIdResp = await client.SendAsync(deleteWithConflictingBody);
            Assert.Equal(HttpStatusCode.OK, delByIdResp.StatusCode);
            Assert.False(store.ContainsKey(firstId));
            Assert.True(store.ContainsKey(secondId));

            // 6. DELETE / with JSON body deletes secondId
            using HttpRequestMessage deleteByBody = new(HttpMethod.Delete, "/api/v1/storage-entry-metadata")
            {
                Content = JsonContent.Create(new DeleteStorageEntryRequest { FileId = secondId }, options: _jsonOptions)
            };
            HttpResponseMessage delBodyResp = await client.SendAsync(deleteByBody);
            Assert.Equal(HttpStatusCode.OK, delBodyResp.StatusCode);
            Assert.False(store.ContainsKey(secondId));

            await app.StopAsync();
        }
        finally
        {
            if (Directory.Exists(tempStorageDir))
                Directory.Delete(tempStorageDir, recursive: true);
        }
    }

    [Fact]
    public async Task MechanicsAndOptimizationsMinimalApiEndpoints_ExecuteRealHttpRequests()
    {
        string tempStorageDir = Path.Combine(Path.GetTempPath(), $"mst-opt-http-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempStorageDir);

        try
        {
            Mock<IRepository> repoMock = new();
            repoMock
                .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(50L);
            repoMock
                .Setup(r => r.GetAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(
                    It.IsAny<MechanicalModelCurveFitOutputFilter>(),
                    It.IsAny<Pagination?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((MechanicalModelCurveFitOutputFilter _, Pagination? _, CancellationToken ct) =>
                    StreamEntitiesAsync(Array.Empty<MechanicalModelCurveFitOutputEntity>(), ct));

            ResiliencePipelineSettings resilience = CreateResilienceSettings();
            PipelineStepOptions stepOptions = new(1, 32, true);

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services
                .AddCoreServices(
                    resilience,
                    fileStorageSettings: new FileStorageSettings
                    {
                        DefaultStorageProvider = StorageProvider.Local,
                        Local = new LocalFileStorageSettings { BaseDirectory = tempStorageDir }
                    },
                    pipelineProgressNotifierSettings: new PipelineProgressNotifierSettings
                    {
                        ChannelCapacity = 64,
                        FullMode = BoundedChannelFullMode.Wait,
                        MaxChannels = 16
                    })
                .AddMathematicsServices()
                .AddMechanicsOfMaterialsServices(addMechanicalModels: true)
                .AddOptimizationsServices(
                    new CurveFitterSettings(),
                    new ExperimentalDataSettings
                    {
                        SegmenterOptions = stepOptions,
                        FileWriterOptions = stepOptions,
                        PersistanceOptions = stepOptions,
                        SegmentBuilderOptions = stepOptions,
                        CurveFitterOptions = stepOptions,
                        SimulationInputBuilderOptions = stepOptions,
                        IngestionBufferCapacity = 16,
                        MaxCsvSizeBytes = 1024 * 1024
                    },
                    new ParametricSweepSettings { IngestionBufferCapacity = 16 })
                .AddWebApiServices(resilience)
                .ConfigureToolsHttpJsonOptions()
                .AddSingleton(repoMock.Object);

            await using WebApplication app = builder.Build();
            app.MapMechanicsOfMaterialsEndpoints();
            app.MapExperimentalDataEndpoints();
            app.MapParametricSweepEndpoints();
            app.MapPipelineProgressEndpoints();
            await app.StartAsync();

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

            // MechanicsOfMaterials endpoints
            HttpResponseMessage modelsResp = await client.GetAsync("/api/v1/mechanics/models");
            Assert.Equal(HttpStatusCode.OK, modelsResp.StatusCode);

            FatigueInput fatigueInput = new()
            {
                MinimumAppliedStress = -80.0,
                MaximumAppliedStress = 180.0,
                TensileStress = 700.0,
                FatigueLimit = 350.0,
                FatigueLimitFraction = 0.85,
                IsRotativeSection = true,
                Reliability = Reliability.NinetyNine,
                SurfaceFinish = SurfaceFinish.Machined,
                LoadingType = LoadingType.Bending,
                Temperature = 25.0,
                Profile = new CircularProfile { Diameter = 25.0 }
            };
            HttpResponseMessage fatigueResp = await client.PostAsJsonAsync("/api/v1/mechanics/fatigue/calculate", fatigueInput, _jsonOptions);
            Assert.Equal(HttpStatusCode.OK, fatigueResp.StatusCode);

            HttpResponseMessage seResp = await client.PostAsJsonAsync("/api/v1/mechanics/fatigue/calculate/endurance-limit", fatigueInput, _jsonOptions);
            Assert.Equal(HttpStatusCode.OK, seResp.StatusCode);

            using JsonDocument elasticDoc = JsonDocument.Parse("""{"youngModulus": 100.0}""");
            ExecuteSpecimenAnalysisRequest specimenRequest = new()
            {
                MechanicalModelName = nameof(MechanicalModel.Elastic),
                AnalysisIdentifier = "http-spec-1",
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
                ConstitutiveParameters = elasticDoc.RootElement.Clone()
            };
            HttpResponseMessage specResp = await client.PostAsJsonAsync("/api/v1/mechanics/specimen-analysis/execute", specimenRequest, _jsonOptions);
            Assert.Equal(HttpStatusCode.OK, specResp.StatusCode);

            // ExperimentalData endpoints (upload, process validation, batch validation)
            using MultipartFormDataContent uploadForm = new();
            uploadForm.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("Time,Strain\n0,0\n")), "strainFile", "strain.txt");
            uploadForm.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("Time,Stress\n0,0\n")), "stressFile", "stress.csv");
            HttpResponseMessage uploadBadResp = await client.PostAsync("/api/v1/experimental-data/upload", uploadForm);
            Assert.Equal(HttpStatusCode.BadRequest, uploadBadResp.StatusCode);

            HttpResponseMessage processBadResp = await client.PostAsJsonAsync("/api/v1/experimental-data/process", new ExecuteExperimentalDataPipelineRequest
            {
                MechanicalModelName = "",
                TargetSegments = [],
                ExperimentalDataGroupRequest = new ExperimentalDataGroupRequest { StrainFileLocation = "", StressFileLocation = "" }
            }, _jsonOptions);
            Assert.Equal(HttpStatusCode.BadRequest, processBadResp.StatusCode);

            HttpResponseMessage batchBadResp = await client.PostAsJsonAsync("/api/v1/experimental-data/process/batch", new ExecuteBatchExperimentalDataPipelineRequest
            {
                MechanicalModelName = "",
                TargetSegments = [],
                Groups = []
            }, _jsonOptions);
            Assert.Equal(HttpStatusCode.BadRequest, batchBadResp.StatusCode);

            // ParametricSweep endpoints (process validation & experimental-data validation)
            HttpResponseMessage sweepBadResp = await client.PostAsJsonAsync("/api/v1/parametric-sweeps/process", new ExecuteParametricSweepPipelineRequest
            {
                MechanicalModelName = "",
                Baseline = new MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity.ElasticConstitutiveParameters { YoungModulus = 100.0 },
                BaseStrain = 0.05,
                RampTimeConsideration = RampTimeConsideration.Disregard,
                InitialTime = 0.0,
                TimeStep = 0.5,
                FinalTime = 5.0,
                RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
                BaseRootFindingInput = new RootFindingInput(0.0, 5.0, 1e-3, 50)
            }, _jsonOptions);
            Assert.Equal(HttpStatusCode.BadRequest, sweepBadResp.StatusCode);

            HttpResponseMessage sweepFromExpBadResp = await client.PostAsJsonAsync("/api/v1/parametric-sweeps/process/experimental-data", new ExecuteParametricSweepFromExperimentalDataRequest
            {
                ExperimentalDataIdentifier = "",
                MechanicalModelName = "",
                BaseStrain = 0.05,
                RampTimeConsideration = RampTimeConsideration.Disregard,
                InitialTime = 0.0,
                TimeStep = 0.5,
                FinalTime = 5.0,
                RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
                BaseRootFindingInput = new RootFindingInput(0.0, 5.0, 1e-3, 50)
            }, _jsonOptions);
            Assert.Equal(HttpStatusCode.BadRequest, sweepFromExpBadResp.StatusCode);

            // PipelineProgress endpoint (emit event and stream NDJSON)
            IPipelineProgressNotifier notifier = app.Services.GetRequiredService<IPipelineProgressNotifier>();
            const string pipelineId = "http-progress-1";
            await notifier.NotifyAsync(new PipelineProgressEvent
            {
                PipelineIdentifier = pipelineId,
                StepName = "UnitTestStep",
                Description = "Done",
                State = PipelineStepState.Completed,
                Timestamp = TimeProvider.System.GetUtcNow(),
                ReceivedItems = 1,
                ProcessedItems = 1
            });
            notifier.TryComplete(pipelineId);

            HttpResponseMessage progressResp = await client.GetAsync($"/api/v1/pipelines/{pipelineId}/progress");
            Assert.Equal(HttpStatusCode.OK, progressResp.StatusCode);
            string progressBody = await progressResp.Content.ReadAsStringAsync();
            Assert.Contains("UnitTestStep", progressBody);

            await app.StopAsync();
        }
        finally
        {
            if (Directory.Exists(tempStorageDir))
                Directory.Delete(tempStorageDir, recursive: true);
        }
    }

    private static async Task<long> UploadCsvAsync(HttpClient client, string route, string identifier, string context, string fileName, string content)
    {
        using MultipartFormDataContent form = new();
        ByteArrayContent fileContent = new(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(identifier), "identifier");
        form.Add(new StringContent(context), "context");

        HttpResponseMessage response = await client.PostAsync(route, form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Result<StorageEntryResultData>? payload = await response.Content.ReadFromJsonAsync<Result<StorageEntryResultData>>(_jsonOptions);
        Assert.True(payload?.Success);
        Assert.NotNull(payload?.Data);
        return payload.Data.EntityId;
    }

    private static async IAsyncEnumerable<T> StreamEntitiesAsync<T>(
        IEnumerable<T> items,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        foreach (T item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}
