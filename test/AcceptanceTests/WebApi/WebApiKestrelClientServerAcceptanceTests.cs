using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcceptanceTests.WebApi;

/// <summary>
/// End-to-end client-server acceptance tests running a real Kestrel HTTP server bound to an
/// ephemeral loopback TCP port (<c>127.0.0.1:0</c>), validating full CRUD round-trips,
/// <see cref="ApiServiceAgentResiliencePipeline"/> Polly retries against real 503 responses,
/// JWE bearer authentication and token refresh, and <c>ExceptionHandlingHttpMiddleware</c>.
/// </summary>
public sealed class WebApiKestrelClientServerAcceptanceTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ConcurrentDictionary<long, StorageEntryMetadataEntity> _store = new();
    private long _nextId = 1L;
    private int _flakyAttempts;
    private WebApplication? _app;
    private string _baseAddress = string.Empty;
    private string _httpsBaseAddress = string.Empty;
    private X509Certificate2? _serverCertificate;
    private JwtSettings _jwtSettings = null!;

    /// <summary>
    /// Starts a live Kestrel host bound to <c>http://127.0.0.1:0</c> with real WebApi DI wiring,
    /// JWE authentication, exception middleware, CRUD endpoints for <see cref="StorageEntryMetadataEntity"/>,
    /// and deterministic transient-503 / unhandled-exception diagnostic endpoints.
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
        _store[seeded.Id] = seeded;

        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataEntity entity, CancellationToken _) =>
            {
                long id = Interlocked.Increment(ref _nextId);
                _store[id] = entity with { Id = id };
                return id;
            });

        repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => _store.TryGetValue(id, out StorageEntryMetadataEntity? entity) ? entity : null);

        repositoryMock
            .Setup(r => r.CountAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(
                It.IsAny<StorageEntryMetadataFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataFilter filter, CancellationToken _) => FilterEntities(filter).Count());

        repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(
                It.IsAny<StorageEntryMetadataFilter>(),
                It.IsAny<Pagination?>(),
                It.IsAny<CancellationToken>()))
            .Returns((StorageEntryMetadataFilter filter, Pagination? pagination, CancellationToken ct) =>
                StreamFilteredAsync(filter, pagination, ct));

        repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataEntity entity, CancellationToken _) =>
            {
                if (!_store.ContainsKey(entity.Id))
                    return false;

                _store[entity.Id] = entity;
                return true;
            });

        repositoryMock
            .Setup(r => r.DeleteAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long id, CancellationToken _) =>
            {
                _store.TryRemove(id, out StorageEntryMetadataEntity? _);
                return Task.CompletedTask;
            });

        ResiliencePipelineSettings resilienceSettings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            DelayInMilliseconds = 20,
            MaxRetryAttempts = 3,
            UseJitter = false
        };

        _jwtSettings = new JwtSettings
        {
            Issuer = "MelloSilveiraTools.Tests",
            Audience = "MelloSilveiraTools.Clients",
            EncryptionKey = "12345678901234567890123456789012",
            SigningKey = "super-secret-signing-key-32-bytes!",
            ClockSkewInSeconds = 5,
            SecurityKeyType = SecurityKeyType.Symmetric,
            TokenExperitationTimeInMinutes = 30
        };

        _serverCertificate = CreateSelfSignedLoopbackCertificate();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, 0);
            kestrel.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps(_serverCertificate));
        });

        builder.Services.AddCoreServices(resilienceSettings);
        builder.Services.AddWebApiServices(resilienceSettings);
        builder.Services.AddJweAuthentication(_jwtSettings);
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(repositoryMock.Object);

        _app = builder.Build();
        _app.UseCustomMiddlewares();
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapCrud<StorageEntryMetadataEntity, StorageEntryMetadataFilter>("/api/v1/storage-metadata", "StorageMetadata");

        _app.MapGet("/api/v1/protected/profile", (ClaimsPrincipal user) =>
        {
            string? subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            return Results.Ok(Result.CreateSuccessOk(subject ?? "authenticated"));
        }).RequireAuthorization();

        _app.MapGet("/api/v1/resilience/flaky", () =>
        {
            int attempt = Interlocked.Increment(ref _flakyAttempts);
            if (attempt <= 2)
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

            StorageEntryMetadataEntity[] payload = [seeded];
            return Results.Ok(payload);
        });

        _app.MapGet("/api/v1/fault", () =>
        {
            throw new TimeoutException("Simulated unhandled Kestrel server timeout.");
        });

        await _app.StartAsync();
        string[] urls = [.. _app.Urls];
        _baseAddress = urls.First(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        _httpsBaseAddress = urls.First(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Stops and disposes the Kestrel host instance and ephemeral TLS certificate cleanly after test execution.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        _serverCertificate?.Dispose();
    }

    /// <summary>
    /// Executes the full HTTP CRUD lifecycle (POST, GET by id, GET paged, PUT, DELETE, GET after delete)
    /// over real Kestrel TCP sockets using <see cref="ApiServiceAgentBase"/> against
    /// <c>MapCrud&lt;StorageEntryMetadataEntity, StorageEntryMetadataFilter&gt;</c>.
    /// </summary>
    [Fact]
    public async Task ApiServiceAgent_OverRealKestrelTcp_ShouldExecuteFullCrudLifecycleForStorageEntryMetadataEntityAsync()
    {
        // Arrange
        using TestApiServiceAgent agent = CreateAgent();

        StorageEntryMetadataEntity newEntity = new()
        {
            ExternalIdentifier = "EXP-KNEE-002",
            StorageProvider = StorageProvider.Local,
            Location = "optimizations/experiments/knee_ligament_02.csv",
            Directory = "optimizations/experiments",
            FileName = "knee_ligament_02.csv",
            SizeBytes = 4096,
            ContentType = "text/csv"
        };

        // Act 1: POST create
        HttpResponseMessage postResponse = await agent.Client.PostAsJsonAsync("/api/v1/storage-metadata", newEntity);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        AddResult? addResult = await postResponse.Content.ReadFromJsonAsync<AddResult>(WebJsonOptions);
        Assert.NotNull(addResult);
        Assert.True(addResult.Success);
        Assert.NotNull(addResult.Data);
        long createdId = addResult.Data.Id;
        Assert.True(createdId > 1L);

        // Act 2: GET by id via agent ExecuteAsync
        HttpResponseMessage getByIdRaw = await agent.Client.GetAsync($"/api/v1/storage-metadata/{createdId}");
        Assert.Equal(HttpStatusCode.OK, getByIdRaw.StatusCode);
        Result<StorageEntryMetadataEntity>? getByIdResult = await getByIdRaw.Content.ReadFromJsonAsync<Result<StorageEntryMetadataEntity>>(WebJsonOptions);
        Assert.NotNull(getByIdResult?.Data);
        Assert.Equal("EXP-KNEE-002", getByIdResult.Data.ExternalIdentifier);

        // Act 3: GET paged (seeded + newly created)
        PagedResult<StorageEntryMetadataEntity>? pagedResult = await agent.Client.GetFromJsonAsync<PagedResult<StorageEntryMetadataEntity>>(
            "/api/v1/storage-metadata?Offset=0&Limit=10",
            WebJsonOptions);
        Assert.NotNull(pagedResult);
        Assert.True(pagedResult.Success);
        Assert.NotNull(pagedResult.Data);
        Assert.True(pagedResult.Data.Count >= 2);

        // Act 4: PUT update
        StorageEntryMetadataEntity updatedEntity = getByIdResult.Data with
        {
            SizeBytes = 8192,
            FileName = "knee_ligament_02_updated.csv"
        };
        Result putResult = await agent.CallExecuteAsync(agent.Client.PutAsJsonAsync($"/api/v1/storage-metadata/{createdId}", updatedEntity));
        Assert.True(putResult.Success);
        Assert.Equal(StatusCode.OK, putResult.StatusCode);

        Result<StorageEntryMetadataEntity>? refreshed = await agent.Client.GetFromJsonAsync<Result<StorageEntryMetadataEntity>>(
            $"/api/v1/storage-metadata/{createdId}",
            WebJsonOptions);
        Assert.Equal(8192, refreshed?.Data?.SizeBytes);

        // Act 5: DELETE & verify 404
        Result deleteResult = await agent.CallExecuteAsync(agent.Client.DeleteAsync($"/api/v1/storage-metadata/{createdId}"));
        Assert.True(deleteResult.Success);

        HttpResponseMessage afterDelete = await agent.Client.GetAsync($"/api/v1/storage-metadata/{createdId}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    /// <summary>
    /// Verifies that <see cref="ApiServiceAgentResiliencePipeline"/> automatically retries twice when the real
    /// Kestrel endpoint returns HTTP <c>503 Service Unavailable</c> and succeeds cleanly on the 3rd attempt.
    /// </summary>
    [Fact]
    public async Task ApiServiceAgent_WhenServerReturnsTransient503Twice_ShouldRetryViaPollyPipelineAndSucceedOnThirdAttemptAsync()
    {
        // Arrange
        Interlocked.Exchange(ref _flakyAttempts, 0);
        using TestApiServiceAgent agent = CreateAgent();

        // Act
        Result outcome = await agent.ResiliencePipeline.ExecuteAsync(
            async context => await agent.CallExecuteAsync(
                agent.Client.GetAsync("/api/v1/resilience/flaky", context.CancellationToken),
                context.CancellationToken));

        // Assert
        Assert.True(outcome.Success);
        Assert.Equal(StatusCode.OK, outcome.StatusCode);
        Assert.Equal(3, Volatile.Read(ref _flakyAttempts));

        ListedResult<StorageEntryMetadataEntity> listedOutcome = await agent.CallGetAsync<StorageEntryMetadataEntity>("/api/v1/resilience/flaky");
        Assert.True(listedOutcome.Success);
        Assert.NotNull(listedOutcome.Data);
        StorageEntryMetadataEntity returned = Assert.Single(listedOutcome.Data);
        Assert.Equal("EXP-KNEE-001", returned.ExternalIdentifier);
    }

    /// <summary>
    /// Verifies encrypted JWE token generation, real Kestrel <c>JwtBearer</c> middleware enforcement
    /// (401 on missing/tampered token, 200 on valid JWE token), and token refresh lifecycle.
    /// </summary>
    [Fact]
    public async Task JweAuthentication_OverRealKestrel_ShouldRejectMissingOrTamperedTokenWith401AndAcceptValidJweTokenAsync()
    {
        // Arrange
        using IServiceScope scope = _app!.Services.CreateScope();
        IAuthenticationTokenService tokenService = scope.ServiceProvider.GetRequiredService<IAuthenticationTokenService>();

        AuthenticationToken token = tokenService.Generate("user-42");
        using HttpClient client = new() { BaseAddress = new Uri(_baseAddress) };

        // Act & Assert 1: Missing bearer header -> 401
        HttpResponseMessage anonymousResponse = await client.GetAsync("/api/v1/protected/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        // Act & Assert 2: Tampered token -> 401
        using HttpRequestMessage tamperedRequest = new(HttpMethod.Get, "/api/v1/protected/profile");
        tamperedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value + "tampered");
        HttpResponseMessage tamperedResponse = await client.SendAsync(tamperedRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, tamperedResponse.StatusCode);

        // Act & Assert 3: Valid JWE token -> 200 OK
        using HttpRequestMessage authorizedRequest = new(HttpMethod.Get, "/api/v1/protected/profile");
        authorizedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        HttpResponseMessage authorizedResponse = await client.SendAsync(authorizedRequest);
        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);

        Result<string>? profileResult = await authorizedResponse.Content.ReadFromJsonAsync<Result<string>>(WebJsonOptions);
        Assert.True(profileResult?.Success);
        Assert.Equal("user-42", profileResult?.Data);

        // Act & Assert 4: Expired JWE token (beyond ClockSkew) -> 401
        AuthenticationJweTokenService expiredGenerator = new(
            new FixedTimeProvider(DateTimeOffset.UtcNow.AddMinutes(-60)),
            _jwtSettings);
        AuthenticationToken expiredToken = expiredGenerator.Generate("user-42");
        using HttpRequestMessage expiredRequest = new(HttpMethod.Get, "/api/v1/protected/profile");
        expiredRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken.Value);
        HttpResponseMessage expiredResponse = await client.SendAsync(expiredRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResponse.StatusCode);

        // Act & Assert 5: Refresh JWE token and re-call protected endpoint
        AuthenticationToken refreshedToken = await tokenService.RefreshAsync(token.Value);
        Assert.False(string.IsNullOrWhiteSpace(refreshedToken.Value));
        using HttpRequestMessage refreshedRequest = new(HttpMethod.Get, "/api/v1/protected/profile");
        refreshedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken.Value);
        HttpResponseMessage refreshedResponse = await client.SendAsync(refreshedRequest);
        Assert.Equal(HttpStatusCode.OK, refreshedResponse.StatusCode);
    }

    /// <summary>
    /// Establishes a real TLS 1.2/1.3 HTTPS connection over Kestrel using an ephemeral self-signed X.509 certificate
    /// and verifies authenticated JWE bearer access over HTTPS.
    /// </summary>
    [Fact]
    public async Task HttpsWithSelfSignedCertificate_OverRealKestrel_ShouldEstablishTlsAndServeProtectedJweEndpointAsync()
    {
        // Arrange
        using IServiceScope scope = _app!.Services.CreateScope();
        IAuthenticationTokenService tokenService = scope.ServiceProvider.GetRequiredService<IAuthenticationTokenService>();
        AuthenticationToken token = tokenService.Generate("tls-user-7");

        using HttpClientHandler tlsHandler = new()
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                certificate is not null && string.Equals(certificate.GetCertHashString(), _serverCertificate!.Thumbprint, StringComparison.OrdinalIgnoreCase)
        };
        using HttpClient httpsClient = new(tlsHandler) { BaseAddress = new Uri(_httpsBaseAddress) };
        httpsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        // Act
        HttpResponseMessage response = await httpsClient.GetAsync("/api/v1/protected/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Result<string>? payload = await response.Content.ReadFromJsonAsync<Result<string>>(WebJsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("tls-user-7", payload.Data);
    }

    /// <summary>
    /// Verifies that unhandled exceptions raised inside Kestrel request execution are intercepted by
    /// <c>ExceptionHandlingHttpMiddleware</c> and mapped to an HTTP 500 JSON <see cref="Result"/> response.
    /// </summary>
    [Fact]
    public async Task ExceptionHandlingHttpMiddleware_WhenEndpointThrows_ShouldReturnStructured500JsonResultAsync()
    {
        // Arrange
        using HttpClient client = new() { BaseAddress = new Uri(_baseAddress) };

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/fault");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);

        Result? payload = await response.Content.ReadFromJsonAsync<Result>(WebJsonOptions);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
        Assert.Equal(StatusCode.UnknownError, payload.StatusCode);
        Assert.NotEmpty(payload.Messages);
    }

    private TestApiServiceAgent CreateAgent()
    {
        ILogger<TestApiServiceAgent> logger = _app!.Services.GetRequiredService<ILogger<TestApiServiceAgent>>();
        ApiServiceAgentResiliencePipeline pipeline = _app.Services.GetRequiredService<ApiServiceAgentResiliencePipeline>();
        TestAgentSettings settings = new()
        {
            BaseAddress = _baseAddress,
            DefaultTimeoutInSeconds = 10
        };

        return new TestApiServiceAgent(logger, settings, pipeline);
    }

    private IEnumerable<StorageEntryMetadataEntity> FilterEntities(StorageEntryMetadataFilter filter)
    {
        foreach (StorageEntryMetadataEntity entity in _store.Values.OrderBy(e => e.Id))
        {
            if (!string.IsNullOrWhiteSpace(filter.ExternalIdentifier)
                && !string.Equals(entity.ExternalIdentifier, filter.ExternalIdentifier, StringComparison.Ordinal))
            {
                continue;
            }

            yield return entity;
        }
    }

    private async IAsyncEnumerable<StorageEntryMetadataEntity> StreamFilteredAsync(
        StorageEntryMetadataFilter filter,
        Pagination? pagination,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        IEnumerable<StorageEntryMetadataEntity> query = FilterEntities(filter);
        if (pagination is not null && pagination.Offset > 0)
            query = query.Skip((int)pagination.Offset);

        if (pagination is not null && pagination.Limit > 0)
            query = query.Take((int)pagination.Limit);

        foreach (StorageEntryMetadataEntity entity in query)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entity;
        }
    }

    private static X509Certificate2 CreateSelfSignedLoopbackCertificate()
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        SubjectAlternativeNameBuilder sanBuilder = new();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddDnsName("localhost");
        request.CertificateExtensions.Add(sanBuilder.Build());

        using X509Certificate2 ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pkcs12), password: null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestAgentSettings : ApiServiceAgentSettings
    {
    }

    private sealed class TestApiServiceAgent(
        ILogger logger,
        ApiServiceAgentSettings settings,
        ApiServiceAgentResiliencePipeline resiliencePipeline)
        : ApiServiceAgentBase(logger, settings, resiliencePipeline)
    {
        public override string ServiceName => nameof(TestApiServiceAgent);

        public HttpClient Client => HttpClient;

        public Task<ListedResult<T>> CallGetAsync<T>(string uri, int timeoutMs = 5000) where T : class
            => GetAsync<T>(uri, timeoutMs);

        public Task<Result> CallExecuteAsync(Task<HttpResponseMessage> httpTask, CancellationToken cancellationToken = default)
            => ExecuteAsync(httpTask, nameof(CallExecuteAsync), cancellationToken);
    }
}
