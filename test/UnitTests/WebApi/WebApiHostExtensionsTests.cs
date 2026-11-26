using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Endpoints;
using MelloSilveiraTools.Plugins.Application.Endpoints;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Application.HealthChecks;
using MelloSilveiraTools.WebApi.Application.Models;
using MelloSilveiraTools.WebApi.Application.RateLimiting;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace UnitTests.WebApi;

public sealed class WebApiHostExtensionsTests
{
    [Fact]
    public void ConfigureRequestBodyLimits_ConfiguresKestrelAndFormOptions_AndRejectsNonPositiveValues()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.ConfigureRequestBodyLimits(0L));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.ConfigureRequestBodyLimits(-10L));

        const long limitBytes = 20L * 1024 * 1024;
        builder.ConfigureRequestBodyLimits(limitBytes);

        using WebApplication app = builder.Build();
        KestrelServerOptions kestrelOptions = app.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;
        FormOptions formOptions = app.Services.GetRequiredService<IOptions<FormOptions>>().Value;

        Assert.Equal(limitBytes, kestrelOptions.Limits.MaxRequestBodySize);
        Assert.Equal(limitBytes, formOptions.MultipartBodyLengthLimit);
    }

    [Fact]
    public void AddDefaultCors_And_ConfigureToolsHttpJsonOptions_ApplyExpectedSettings()
    {
        ServiceCollection explicitServices = new();
        explicitServices.AddDefaultCors(["https://app.example.com"], allowAnyOriginWhenEmpty: false);
        explicitServices.ConfigureToolsHttpJsonOptions();

        using ServiceProvider explicitProvider = explicitServices.BuildServiceProvider();
        CorsOptions explicitCors = explicitProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        CorsPolicy? explicitPolicy = explicitCors.GetPolicy(explicitCors.DefaultPolicyName);
        Assert.NotNull(explicitPolicy);
        Assert.Contains("https://app.example.com", explicitPolicy.Origins);
        Assert.False(explicitPolicy.AllowAnyOrigin);

        JsonOptions jsonOptions = explicitProvider.GetRequiredService<IOptions<JsonOptions>>().Value;
        Assert.True(jsonOptions.SerializerOptions.AllowOutOfOrderMetadataProperties);
        Assert.Contains(jsonOptions.SerializerOptions.Converters, c => c is JsonStringEnumConverter);

        ServiceCollection devServices = new();
        devServices.AddDefaultCors([], allowAnyOriginWhenEmpty: true);
        using ServiceProvider devProvider = devServices.BuildServiceProvider();
        CorsOptions devCors = devProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        CorsPolicy? devPolicy = devCors.GetPolicy(devCors.DefaultPolicyName);
        Assert.NotNull(devPolicy);
        Assert.True(devPolicy.AllowAnyOrigin);
    }

    [Fact]
    public async Task AddDefaultRateLimiting_EnforcesPolicies_AndPartitionsPluginUploadPerAuthenticatedUser()
    {
        JwtSettings jwtSettings = new()
        {
            Issuer = "MelloSilveiraTools.Tests",
            Audience = "MelloSilveiraTools.Tests",
            EncryptionKey = "MelloSilveiraToolsQuickstartKey!",
            SigningKey = "MelloSilveiraToolsSigningKey2026!!",
            ClockSkewInSeconds = 30,
            SecurityKeyType = SecurityKeyType.Symmetric,
            TokenExperitationTimeInMinutes = 60
        };

        Mock<IRepository> repoMock = new();
        repoMock.Setup(r => r.GetAsync<UserEntity>(10L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity { Id = 10L, Username = "user10", PasswordHash = "h", IsActive = true, IsSuperuser = false });
        repoMock.Setup(r => r.GetAsync<UserEntity>(20L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity { Id = 20L, Username = "user20", PasswordHash = "h", IsActive = true, IsSuperuser = false });

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();
        builder.Services.AddSingleton(repoMock.Object);
        builder.Services.AddJweAuthentication(jwtSettings);
        builder.Services.AddDefaultRateLimiting();

        await using WebApplication app = builder.Build();
        app.UseWebApiSecurityPipeline();
        app.MapPost("/api/v1/auth/register", () => Results.Ok()).AllowAnonymous().RequireRateLimiting(RateLimitPolicies.Auth);
        app.MapPost("/api/v1/plugins/submissions", () => Results.Ok()).RequireAuthorization().RequireRateLimiting(RateLimitPolicies.PluginUpload);
        await app.StartAsync();

        AuthenticationJweTokenService tokenService = new(TimeProvider.System, jwtSettings);
        string user10Token = tokenService.Generate(10L).Value;
        string user20Token = tokenService.Generate(20L).Value;

        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

        // Auth /register allows 5 requests per minute, 6th returns 429
        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/auth/register", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/api/v1/auth/register", null)).StatusCode);

        // PluginUpload partitions per authenticated user: exhausting user 10's 5-req budget must NOT block user 20
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user10Token);
        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins/submissions", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/api/v1/plugins/submissions", null)).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user20Token);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/plugins/submissions", null)).StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task AddDatabaseHealthCheck_And_MapHealthEndpoint_UseConfiguredTimeoutExposeSwaggerAndHideErrorsInProduction()
    {
        DatabaseSettings invalidSettings = new()
        {
            ConnectionString = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1",
            UnitOperationTimeoutInMilliseconds = 0,
            BulkOperationTimeoutInMilliseconds = 1000
        };
        ServiceCollection invalidServices = new();
        Assert.Throws<ArgumentNullException>(() => invalidServices.AddDatabaseHealthCheck(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidServices.AddDatabaseHealthCheck(invalidSettings));

        DatabaseSettings unreachableSettings = invalidSettings with { UnitOperationTimeoutInMilliseconds = 2500 };

        await VerifyHealthStatusAsync(Environments.Development, unreachableSettings, expectErrorField: true);
        await VerifyHealthStatusAsync(Environments.Production, unreachableSettings, expectErrorField: false);
    }

    [Fact]
    public async Task EnsureDatabaseRlsSafetyAsync_RespectsAllowFlagAndEnvironment()
    {
        DatabaseSettings unreachableSettings = new()
        {
            ConnectionString = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1",
            UnitOperationTimeoutInMilliseconds = 1000,
            BulkOperationTimeoutInMilliseconds = 1000,
            AllowSuperuserDatabaseConnection = true
        };

        // Explicit opt-out skips DB query completely even in Production
        WebApplicationBuilder allowedBuilder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        allowedBuilder.Services.AddSingleton(unreachableSettings);
        allowedBuilder.Services.AddSingleton(_ => DatabaseDependencyInjection.CreateDataSource(unreachableSettings));
        await using (WebApplication allowedApp = allowedBuilder.Build())
        {
            await allowedApp.EnsureDatabaseRlsSafetyAsync();
        }

        // Development ignores transient NpgsqlException while PostgreSQL is booting
        DatabaseSettings strictSettings = unreachableSettings with { AllowSuperuserDatabaseConnection = false };
        WebApplicationBuilder devBuilder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        devBuilder.Services.AddSingleton(strictSettings);
        devBuilder.Services.AddSingleton(_ => DatabaseDependencyInjection.CreateDataSource(strictSettings));
        await using (WebApplication devApp = devBuilder.Build())
        {
            await devApp.EnsureDatabaseRlsSafetyAsync();
        }

        // Production fails fast on connectivity errors when opt-out is false
        WebApplicationBuilder prodBuilder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        prodBuilder.Services.AddSingleton(strictSettings);
        prodBuilder.Services.AddSingleton(_ => DatabaseDependencyInjection.CreateDataSource(strictSettings));
        await using (WebApplication prodApp = prodBuilder.Build())
        {
            await Assert.ThrowsAsync<NpgsqlException>(() => prodApp.EnsureDatabaseRlsSafetyAsync());
        }
    }

    [Fact]
    public void MapCrud_InfersConventionsFromTableAttribute_AndOptimizationsCrudEndpointsRegisterCanonicalRoutesAndTags()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        using WebApplication app = builder.Build();

        app.MapCrud<StorageEntryMetadataEntity, StorageEntryMetadataFilter>();
        app.MapOptimizationsCrudEndpoints(requireAuthForMutations: true);
        app.MapPluginEndpoints();

        List<RouteEndpoint> endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        RouteEndpoint inferredStorageGet = endpoints.First(e => e.RoutePattern.RawText == "/api/v1/storage-entry-metadata/{id:long}");
        ITagsMetadata? inferredTags = inferredStorageGet.Metadata.GetMetadata<ITagsMetadata>();
        Assert.NotNull(inferredTags);
        Assert.Contains("StorageEntryMetadata", inferredTags.Tags);

        RouteEndpoint curveFitGet = endpoints.First(e => e.RoutePattern.RawText == "/api/v1/curve-fit-outputs/{id:long}");
        Assert.Contains("CurveFitOutputs", curveFitGet.Metadata.GetMetadata<ITagsMetadata>()!.Tags);

        RouteEndpoint simGet = endpoints.First(e => e.RoutePattern.RawText == "/api/v1/mechanical-model-simulations/{id:long}");
        Assert.Contains("MechanicalModelSimulations", simGet.Metadata.GetMetadata<ITagsMetadata>()!.Tags);

        RouteEndpoint sweepGet = endpoints.First(e => e.RoutePattern.RawText == "/api/v1/parametric-sweeps/{id:long}");
        Assert.Contains("ParametricSweeps", sweepGet.Metadata.GetMetadata<ITagsMetadata>()!.Tags);

        RouteEndpoint pluginsGet = endpoints.First(e => e.RoutePattern.RawText == "/api/v1/plugins/");
        Assert.Contains("Plugins", pluginsGet.Metadata.GetMetadata<ITagsMetadata>()!.Tags);
    }

    private static async Task VerifyHealthStatusAsync(string environmentName, DatabaseSettings settings, bool expectErrorField)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(_ => DatabaseDependencyInjection.CreateDataSource(settings));
        builder.Services
            .AddDatabaseHealthCheck(settings, tags: ["ready"])
            .AddCheck("self", () => HealthCheckResult.Healthy("Process alive."), tags: ["live"]);

        await using WebApplication app = builder.Build();
        HealthCheckServiceOptions healthOptions = app.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration dbRegistration = Assert.Single(healthOptions.Registrations, r => r.Name == PostgresHealthCheck.DefaultName);
        Assert.Equal(TimeSpan.FromMilliseconds(settings.UnitOperationTimeoutInMilliseconds), dbRegistration.Timeout);

        app.MapHealthEndpoint();
        app.MapHealthEndpoint("/health/live", r => r.Tags.Contains("live"));
        await app.StartAsync();

        // Discoverable in ApiExplorer / Swagger regardless of Controller vs. Minimal API host style
        IApiDescriptionGroupCollectionProvider apiExplorer = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        List<string?> relativePaths = apiExplorer.ApiDescriptionGroups.Items
            .SelectMany(g => g.Items)
            .Select(d => d.RelativePath)
            .ToList();
        Assert.Contains("health", relativePaths);

        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };

        // Full /health evaluates PostgreSQL + self -> 503 Unhealthy when Postgres is unreachable
        HttpResponseMessage response = await client.GetAsync("/health");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Unhealthy", body);
        Assert.Contains(PostgresHealthCheck.DefaultName, body);
        if (expectErrorField)
            Assert.Contains("\"error\"", body, StringComparison.OrdinalIgnoreCase);
        else
            Assert.DoesNotContain("\"error\"", body, StringComparison.OrdinalIgnoreCase);

        // Filtered /health/live evaluates only "live" checks -> 200 Healthy
        HttpResponseMessage liveResponse = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        HealthCheckResponse? livePayload = await liveResponse.Content.ReadFromJsonAsync<HealthCheckResponse>();
        Assert.NotNull(livePayload);
        Assert.Equal("Healthy", livePayload.Status);
        Assert.Single(livePayload.Checks);
        Assert.Equal("Healthy", livePayload.Checks["self"].Status);

        await app.StopAsync();
    }
}
