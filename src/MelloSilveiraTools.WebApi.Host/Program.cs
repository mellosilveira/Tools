using MelloSilveiraTools;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using Polly;
using System.Text.Json.Serialization;
using System.Threading.Channels;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string dbConnectionString = builder.Configuration["Database:ConnectionString"]
    ?? throw new InvalidOperationException("Configuration key 'Database:ConnectionString' is required.");

int unitTimeoutMs = builder.Configuration.GetValue<int?>("Database:UnitOperationTimeoutInMilliseconds") ?? 30000;
int bulkTimeoutMs = builder.Configuration.GetValue<int?>("Database:BulkOperationTimeoutInMilliseconds") ?? 120000;

string configuredStorageDir = builder.Configuration["FileStorage:Local:BaseDirectory"]
    ?? throw new InvalidOperationException("Configuration key 'FileStorage:Local:BaseDirectory' is required.");

string storageDir = Path.IsPathRooted(configuredStorageDir)
    ? configuredStorageDir
    : Path.Combine(AppContext.BaseDirectory, configuredStorageDir);

Directory.CreateDirectory(storageDir);

JwtSettings jwtSettings = new()
{
    Issuer = builder.Configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Configuration key 'Jwt:Issuer' is required."),
    Audience = builder.Configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Configuration key 'Jwt:Audience' is required."),
    EncryptionKey = builder.Configuration["Jwt:EncryptionKey"]
        ?? throw new InvalidOperationException("Configuration key 'Jwt:EncryptionKey' is required."),
    SigningKey = builder.Configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Configuration key 'Jwt:SigningKey' is required."),
    ClockSkewInSeconds = builder.Configuration.GetValue<int?>("Jwt:ClockSkewInSeconds") ?? 30,
    SecurityKeyType = Enum.Parse<SecurityKeyType>(builder.Configuration["Jwt:SecurityKeyType"] ?? nameof(SecurityKeyType.Symmetric), ignoreCase: true),
    TokenExperitationTimeInMinutes = builder.Configuration.GetValue<int?>("Jwt:TokenExperitationTimeInMinutes") ?? 120
};

PipelineStepOptions defaultStepOptions = new(maxWorkers: 1, maxBufferSize: 256, keepOrder: true);

builder.Services.AddToolsServices(
    defaultResiliencePipelineSettings: new ResiliencePipelineSettings
    {
        BackoffType = DelayBackoffType.Exponential,
        DelayInMilliseconds = 200,
        MaxRetryAttempts = 3,
        UseJitter = true
    },
    databaseSettings: new DatabaseSettings
    {
        ConnectionString = dbConnectionString,
        UnitOperationTimeoutInMilliseconds = unitTimeoutMs,
        BulkOperationTimeoutInMilliseconds = bulkTimeoutMs
    },
    encryptionSettings: new EncryptionSettings
    {
        SaltSize = 16,
        HashSize = 32,
        Iterations = 100_000
    },
    fileStorageSettings: new FileStorageSettings
    {
        Local = new LocalFileStorageSettings { BaseDirectory = storageDir }
    },
    curveFitterOptions: new CurveFitterSettings
    {
        DefaultCurveFitter = CurveFitterType.Alglib
    },
    experimentalDataSettings: new ExperimentalDataSettings
    {
        SegmenterOptions = defaultStepOptions,
        FileWriterOptions = defaultStepOptions,
        PersistanceOptions = defaultStepOptions,
        SegmentBuilderOptions = defaultStepOptions,
        CurveFitterOptions = defaultStepOptions,
        SimulationInputBuilderOptions = defaultStepOptions,
        IngestionBufferCapacity = 256
    },
    parametricSweepSettings: new ParametricSweepSettings
    {
        IngestionBufferCapacity = 256
    },
    pipelineProgressNotifierSettings: new PipelineProgressNotifierSettings
    {
        ChannelCapacity = 512,
        FullMode = BoundedChannelFullMode.DropOldest,
        MaxChannels = 64
    },
    addMechanicalModels: true);

builder.Services.AddJweAuthentication(jwtSettings);
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(ExperimentalDataController).Assembly)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSwaggerWithBearerSecurity();

WebApplication app = builder.Build();

app.UseCustomMiddlewares();
app.UseSwaggerDocs();
app.UseCors();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (CancellationToken cancellationToken) =>
{
    try
    {
        await using NpgsqlConnection connection = new(dbConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using NpgsqlCommand command = new("SELECT 1;", connection);
        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Results.Ok(new { Status = "Healthy", Database = "PostgreSQL" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { Status = "Unhealthy", Error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous().WithTags("Health");

app.MapControllers();

app.MapCrud<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>("/api/v1/curve-fit-outputs", "Curve fit output")
    .RequireAuthorization()
    .WithTags("CurveFitOutputs");

app.MapCrud<MechanicalModelSimulationEntity, MechanicalModelSimulationFilter>("/api/v1/mechanical-model-simulations", "Mechanical model simulation")
    .RequireAuthorization()
    .WithTags("MechanicalModelSimulations");

app.MapCrud<ParametricSweepEntity, ParametricSweepFilter>("/api/v1/parametric-sweeps", "Parametric sweep")
    .RequireAuthorization()
    .WithTags("ParametricSweeps");

app.MapFallbackToFile("index.html").AllowAnonymous();

await app.RunAsync();
