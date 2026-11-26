using MelloSilveiraTools;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Endpoints;
using MelloSilveiraTools.Plugins.Application.Endpoints;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Application.RateLimiting;
using MelloSilveiraTools.WebApi.Host;
using MelloSilveiraTools.WebApi.Host.Commands;
using MelloSilveiraTools.WebApi.Host.Endpoints;
using MelloSilveiraTools.WebApi.Host.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Settings settings = builder.Configuration.Get<Settings>() ?? throw new InvalidOperationException("Failed to bind root 'Settings' from configuration.");

Result settingsValidation = settings.Validate(builder.Environment.IsDevelopment());
if (!settingsValidation.Success)
    throw new InvalidOperationException($"Invalid WebApi.Host settings:{Environment.NewLine}{settingsValidation.JoinedMessages}");

long maxRequestBodySizeBytes = Math.Max(settings.ExperimentalData.MaxCsvSizeBytes * 2, settings.Plugins.MaxPluginSizeBytes);
builder.ConfigureRequestBodyLimits(maxRequestBodySizeBytes);

builder.Services.AddSingleton(settings);
builder.Services.AddToolsServices(
    defaultResiliencePipelineSettings: settings.DefaultResiliencePipeline,
    databaseSettings: settings.Database,
    encryptionSettings: settings.Encryption,
    pluginSettings: settings.Plugins,
    emailSettings: settings.Email,
    fileStorageSettings: settings.FileStorage,
    loggerSettings: settings.Logger,
    curveFitterOptions: settings.CurveFitter,
    experimentalDataSettings: settings.ExperimentalData,
    parametricSweepSettings: settings.ParametricSweep,
    pipelineProgressNotifierSettings: settings.PipelineProgressNotifier,
    addMechanicalModels: settings.AddMechanicalModels);

builder.Services.AddWebApiHealthChecks(settings.Database);
builder.Services.AddJweAuthentication(settings.Jwt);
builder.Services.AddDefaultRateLimiting();
builder.Services.AddDefaultCors(settings.AllowedCorsOrigins, builder.Environment.IsDevelopment());
builder.Services.ConfigureToolsHttpJsonOptions();

builder.Services.AddSingleton<GetSampleDatasetsCommand>();
builder.Services.AddHostedService<SampleDatasetsStartupSeeder>();
builder.Services.AddSwaggerWithBearerSecurity();

WebApplication app = builder.Build();

await app.EnsureDatabaseRlsSafetyAsync().ConfigureAwait(false);

app.UseCustomMiddlewares();
app.UseSwaggerDocs();
app.UseCors();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseWebApiSecurityPipeline();

app.MapHealthEndpoint();
app.MapAuthEndpoints().RequireRateLimiting(RateLimitPolicies.Auth);
app.MapStorageEntryMetadataEndpoints(requireAuthForMutations: true).RequireRateLimiting(RateLimitPolicies.AnonymousCompute);
app.MapSamplesEndpoints();
app.MapExperimentalDataEndpoints().RequireRateLimiting(RateLimitPolicies.AnonymousCompute);
app.MapParametricSweepEndpoints().RequireRateLimiting(RateLimitPolicies.AnonymousCompute);
app.MapPipelineProgressEndpoints();
app.MapOptimizationsCrudEndpoints(requireAuthForMutations: true);
app.MapPluginEndpoints(uploadRateLimitPolicy: RateLimitPolicies.PluginUpload);
app.MapMechanicsOfMaterialsEndpoints().RequireRateLimiting(RateLimitPolicies.AnonymousCompute);

app.MapFallbackToFile("index.html").AllowAnonymous();

await app.RunAsync();
