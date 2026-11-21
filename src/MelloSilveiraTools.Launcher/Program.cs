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

logger.LogInformation("=================================================================");
logger.LogInformation("  MelloSilveiraTools Quickstart Launcher (.NET 10)");
logger.LogInformation("=================================================================");

LauncherInfrastructureOptions? infrastructure = LauncherInfrastructureResolver.Resolve(logger, args);
if (infrastructure is null)
    return 1;

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

string? repoRoot = await LauncherInfrastructureResolver.ResolveOrCloneRepositoryRootAsync(logger, infrastructure.RepositoryRoot, cts.Token).ConfigureAwait(false);
if (repoRoot is null)
    return 1;

logger.LogInformation("Execution Mode    : {Mode}", infrastructure.Mode);
logger.LogInformation("Repository Root   : {RepoRoot}", repoRoot);
logger.LogInformation("PostgreSQL Target : {ConnectionString}", infrastructure.BuildMaskedConnectionString());
logger.LogInformation("Migration Context : {Context} (timeout: {TimeoutSeconds}s)", infrastructure.MigrationContext, infrastructure.MigrationTimeoutSeconds);
logger.LogInformation("WebApi.Host URL   : {BaseUrl}", infrastructure.BuildWebApiBaseUrl());

if (infrastructure.Mode == ExecutionMode.Docker)
{
    DockerComposeStep dockerComposeStep = new(loggerFactory.CreateLogger<DockerComposeStep>(), repoRoot, infrastructure);
    if (!await dockerComposeStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
        return 1;

    await dockerComposeStep.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
    return 0;
}

(string host, int port) = infrastructure.ExtractHostAndPort();
DatabaseInfrastructureStep dbInfraStep = new(loggerFactory.CreateLogger<DatabaseInfrastructureStep>(), 1, 3, repoRoot, host, port, infrastructure.AllowDockerInstall);
if (!await dbInfraStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
    return 1;

DatabaseMigrationStep migrationStep = new(loggerFactory, 2, 3, infrastructure.ConnectionString, infrastructure.MigrationContext, infrastructure.MigrationTimeoutSeconds);
if (!migrationStep.Execute())
    return 1;

WebApiHostStep webApiStep = new(loggerFactory.CreateLogger<WebApiHostStep>(), 3, 3, repoRoot, infrastructure.ConnectionString, infrastructure.BuildWebApiBaseUrl(), infrastructure.OpenBrowser);
if (!await webApiStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
    return 1;

await webApiStep.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
return 0;
