using MelloSilveiraTools.Launcher.Infrastructure;
using MelloSilveiraTools.Launcher.Steps;
using Microsoft.Extensions.Logging;

if (LauncherInfrastructureResolver.IsHelpRequested(args))
{
    LauncherInfrastructureResolver.PrintHelp();
    return 0;
}

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
ILogger logger = loggerFactory.CreateLogger("Launcher");
TimeProvider timeProvider = TimeProvider.System;

logger.LogInformation("=================================================================");
logger.LogInformation("  MelloSilveiraTools Quickstart Launcher (.NET 10)");
logger.LogInformation("=================================================================");

LauncherInfrastructureOptions? parsedOptions = LauncherInfrastructureResolver.Resolve(logger, args);
if (parsedOptions is null)
    return 1;

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

DependencyInstaller dependencyInstaller = new(
    loggerFactory.CreateLogger<DependencyInstaller>(),
    parsedOptions.ConnectionString,
    parsedOptions.AllowDockerInstall,
    parsedOptions.AllowPostgresInstall,
    parsedOptions.NonInteractive,
    timeProvider);

(bool infraOk, ExecutionMode resolvedMode) = await dependencyInstaller
    .ResolveExecutionInfrastructureAsync(parsedOptions.Mode, parsedOptions.ModeExplicitlySet, cts.Token)
    .ConfigureAwait(false);
if (!infraOk)
    return 1;

LauncherInfrastructureOptions infrastructure = parsedOptions with { Mode = resolvedMode };

string? repoRoot = await LauncherInfrastructureResolver
    .ResolveOrCloneRepositoryRootAsync(logger, dependencyInstaller, infrastructure, cts.Token)
    .ConfigureAwait(false);
if (repoRoot is null)
    return 1;

LauncherRuntimeSecrets secrets = LauncherRuntimeSecrets.ResolveOrCreate();
PortConflictResolver portConflictResolver = new(loggerFactory.CreateLogger<PortConflictResolver>(), infrastructure.NonInteractive);
HttpsCertificateStep httpsCertificateStep = new(loggerFactory.CreateLogger<HttpsCertificateStep>(), infrastructure);

logger.LogInformation("Execution Mode    : {Mode}", infrastructure.Mode);
logger.LogInformation("Repository Root   : {RepoRoot}", repoRoot);
logger.LogInformation("PostgreSQL Target : {ConnectionString}", infrastructure.BuildMaskedConnectionString());
logger.LogInformation("Migration Context : {Context} (timeout: {TimeoutSeconds}s)", infrastructure.MigrationContext, infrastructure.MigrationTimeoutSeconds);
logger.LogInformation("WebApi/WebUi HTTP : {HttpUrl} (local)", infrastructure.BuildWebApiBaseUrl());
logger.LogInformation("WebApi/WebUi HTTPS: {HttpsUrl} (external/TLS)", infrastructure.BuildWebApiHttpsUrl());
logger.LogInformation("Runtime Secrets   : {SecretsSource} (Superuser username: admin)", secrets.SourceDescription);

if (infrastructure.Mode == ExecutionMode.Docker)
{
    DockerComposeStep dockerComposeStep = new(
        loggerFactory.CreateLogger<DockerComposeStep>(),
        repoRoot,
        infrastructure,
        secrets,
        dependencyInstaller,
        portConflictResolver,
        httpsCertificateStep,
        timeProvider);
    if (!await dockerComposeStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
        return 1;

    await dockerComposeStep.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
    return 0;
}

DatabaseInfrastructureStep dbInfraStep = new(loggerFactory.CreateLogger<DatabaseInfrastructureStep>(), 1, 3, dependencyInstaller);
if (!await dbInfraStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
    return 1;

DatabaseMigrationStep migrationStep = new(
    loggerFactory,
    2,
    3,
    repoRoot,
    infrastructure.ConnectionString,
    infrastructure.MigrationContext,
    infrastructure.MigrationTimeoutSeconds);
if (!await migrationStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
    return 1;

WebApiHostStep webApiStep = new(
    loggerFactory.CreateLogger<WebApiHostStep>(),
    3,
    3,
    repoRoot,
    infrastructure.BuildApplicationConnectionString(),
    secrets,
    portConflictResolver,
    httpsCertificateStep,
    infrastructure.WebApiPort,
    infrastructure.WebApiHttpsPort,
    infrastructure.OpenBrowser,
    infrastructure.NonInteractive,
    timeProvider);
if (!await webApiStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
    return 1;

await webApiStep.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
return 0;
