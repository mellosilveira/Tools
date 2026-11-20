using MelloSilveiraTools.Launcher.Infrastructure;
using MelloSilveiraTools.Launcher.Steps;
using Microsoft.Extensions.Logging;

Console.WriteLine("=================================================================");
Console.WriteLine("  MelloSilveiraTools Quickstart Launcher (.NET 10)");
Console.WriteLine("=================================================================");

string? repoRoot = ProcessRunner.FindRepositoryRoot(Environment.CurrentDirectory)
    ?? ProcessRunner.FindRepositoryRoot(AppContext.BaseDirectory);

if (repoRoot is null)
{
    Console.WriteLine("[ERROR] Could not locate MelloSilveiraTools.sln in parent directories.");
    return 1;
}

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

string connectionString = Environment.GetEnvironmentVariable("TOOLS_DB_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=tools_db;Username=postgres;Password=postgres";

DatabaseInfrastructureStep dbInfraStep = new(repoRoot);
if (!await dbInfraStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
    return 1;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
DatabaseMigrationStep migrationStep = new(connectionString, loggerFactory);
if (!migrationStep.Execute())
    return 1;

WebApiHostStep webApiStep = new(repoRoot, connectionString);
if (!await webApiStep.ExecuteAsync(cts.Token).ConfigureAwait(false))
    return 1;

await webApiStep.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
return 0;
