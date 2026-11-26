using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.Migrator;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Database;

public sealed class DatabaseMigratorSmokeTests
{
    [Theory]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Common.001_init.sql", MigrationContext.All, true)]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Common.001_init.sql", MigrationContext.Common, true)]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Optimizations.002_opt.sql", MigrationContext.Common, false)]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Common.001_init.sql", MigrationContext.Optimizations, true)]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Optimizations.002_opt.sql", MigrationContext.Optimizations, true)]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Plugins.003_plug.sql", MigrationContext.Optimizations, false)]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Plugins.003_plug.sql", MigrationContext.Plugins, true)]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Common.001_init.sql", MigrationContext.Plugins, false)]
    [InlineData("MelloSilveiraTools.Database.Migrator.Scripts.Common.001_init.sql", (MigrationContext)999, false)]
    public void FilterScriptByContext_MatchesExpectedScripts(string scriptName, MigrationContext context, bool expected)
    {
        bool matched = MigratorEngine.FilterScriptByContext(scriptName, context);
        Assert.Equal(expected, matched);
    }

    [Fact]
    public void UpgradeDatabase_WithEmptyConnectionString_ReturnsBadRequest()
    {
        Result result = MigratorEngine.UpgradeDatabase(NullLogger.Instance, new MigratorOptions("   ", MigrationContext.Common, 30));

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Theory]
    [InlineData(new[] { "--help" }, 0)]
    [InlineData(new[] { "-h" }, 0)]
    [InlineData(new[] { "--unknown-flag" }, 1)]
    [InlineData(new[] { "--context" }, 1)]
    [InlineData(new[] { "--context", "InvalidContext" }, 1)]
    [InlineData(new[] { "--connection-string" }, 1)]
    [InlineData(new[] { "--timeout", "0" }, 1)]
    public void ProgramMain_HandlesHelpAndInvalidCliArguments(string[] args, int expectedExitCode)
    {
        int exitCode = MelloSilveiraTools.Database.Migrator.Program.Main(args);
        Assert.Equal(expectedExitCode, exitCode);
    }
}
