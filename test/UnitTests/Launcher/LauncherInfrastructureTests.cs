using MelloSilveiraTools.Launcher.Infrastructure;
using MelloSilveiraTools.Launcher.Steps;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Launcher;

public sealed class LauncherInfrastructureTests
{
    [Fact]
    public void Resolve_WithNoArgs_UsesAutoDetectDefaultsAndMainBranch()
    {
        LauncherInfrastructureOptions? options = LauncherInfrastructureResolver.Resolve(NullLogger.Instance, []);

        Assert.NotNull(options);
        Assert.False(options.ModeExplicitlySet);
        Assert.Equal("main", options.Branch);
        Assert.False(options.BranchExplicitlySet);
        Assert.Equal(26324, options.WebApiPort);
        Assert.Equal(24263, options.WebApiHttpsPort);
        Assert.Equal("http://localhost:26324", options.BuildWebApiBaseUrl());
        Assert.Equal("https://localhost:24263", options.BuildWebApiHttpsUrl());
        Assert.Null(options.AllowDockerInstall);
        Assert.Null(options.AllowPostgresInstall);
        Assert.Null(options.OpenBrowser);
        Assert.False(options.NonInteractive);
    }

    [Fact]
    public void Resolve_WithExplicitFlags_BindsBranchInstallAndBrowserOverrides()
    {
        string[] args =
        [
            "--branch", "develop",
            "--no-docker-install",
            "--install-postgres",
            "--open-browser",
            "--port", "25000",
            "--https-port", "25443",
            "--cert-path", "custom.pfx",
            "--cert-password", "secret",
            "--non-interactive"
        ];

        LauncherInfrastructureOptions? options = LauncherInfrastructureResolver.Resolve(NullLogger.Instance, args);

        Assert.NotNull(options);
        Assert.Equal("develop", options.Branch);
        Assert.True(options.BranchExplicitlySet);
        Assert.False(options.AllowDockerInstall);
        Assert.True(options.AllowPostgresInstall);
        Assert.True(options.OpenBrowser);
        Assert.Equal(25000, options.WebApiPort);
        Assert.Equal(25443, options.WebApiHttpsPort);
        Assert.Equal("custom.pfx", options.HttpsCertificatePath);
        Assert.Equal("secret", options.HttpsCertificatePassword);
        Assert.True(options.NonInteractive);
    }

    [Fact]
    public async Task HttpsCertificateStep_WithCustomCertPath_CopiesCertificateToAppRoot()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"launcher-cert-test-{Guid.NewGuid():N}");
        string sourceCert = Path.Combine(tempDir, "source.pfx");
        string targetDir = Path.Combine(tempDir, "app-root");
        Directory.CreateDirectory(tempDir);

        try
        {
            byte[] expectedBytes = [1, 2, 3, 4];
            await File.WriteAllBytesAsync(sourceCert, expectedBytes);

            LauncherInfrastructureOptions? options = LauncherInfrastructureResolver.Resolve(
                NullLogger.Instance,
                ["--cert-path", sourceCert, "--cert-password", "test-pass", "--non-interactive"]);
            Assert.NotNull(options);

            HttpsCertificateStep step = new(NullLogger<HttpsCertificateStep>.Instance, options);
            (bool success, string? certPath, string? certPassword) = await step.PrepareCertificateInDirectoryAsync(targetDir, allowDockerSdkFallback: false);

            Assert.True(success);
            Assert.Equal(Path.Combine(targetDir, HttpsCertificateStep.RootCertificateFileName), certPath);
            Assert.Equal("test-pass", certPassword);
            Assert.True(File.Exists(certPath));
            Assert.Equal(expectedBytes, await File.ReadAllBytesAsync(certPath!));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Theory]
    [InlineData(LauncherMigrationContext.Common, new[] { "schemaversions", "users", "storage_entry_metadata" })]
    [InlineData(LauncherMigrationContext.Optimizations, new[] { "schemaversions", "users", "storage_entry_metadata", "mechanical_model_curve_fit_outputs", "mechanical_model_simulations", "parametric_sweeps" })]
    [InlineData(LauncherMigrationContext.Plugins, new[] { "schemaversions", "plugin_cache", "plugin_submissions" })]
    [InlineData(LauncherMigrationContext.All, new[] { "schemaversions", "users", "storage_entry_metadata", "mechanical_model_curve_fit_outputs", "mechanical_model_simulations", "parametric_sweeps", "plugin_cache", "plugin_submissions" })]
    public void GetExpectedTables_ReturnsExpectedTablesForContext(LauncherMigrationContext context, string[] expectedTables)
    {
        string[] actualTables = DatabaseMigrationStep.GetExpectedTables(context);

        Assert.Equal(expectedTables, actualTables);
    }

    [Fact]
    public void WithPostgresPort_AndBuildDockerConnectionString_MapLoopbackAndHostGatewayCorrectly()
    {
        LauncherInfrastructureOptions? options = LauncherInfrastructureResolver.Resolve(NullLogger.Instance, ["--port", "26000"]);
        Assert.NotNull(options);

        LauncherInfrastructureOptions updated = options.WithPostgresPort(5433);
        string composeConn = updated.BuildDockerConnectionString(useHostGateway: false);
        string externalDockerConn = updated.BuildDockerConnectionString(useHostGateway: true);
        string externalAppConn = updated.BuildApplicationConnectionString(forDocker: true, useHostGateway: true);

        Assert.Contains("Port=5433", updated.ConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Host=postgres", composeConn, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Port=5432", composeConn, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Host=host.docker.internal", externalDockerConn, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Port=5433", externalDockerConn, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Username=tools_app", externalAppConn, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Host=host.docker.internal", externalAppConn, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseDockerPostgresContainer_PrioritizesRunningToolsPostgresAndExternalTargetPort()
    {
        string output = string.Join(
            Environment.NewLine,
            "other_app|redis:7|running|0.0.0.0:6379->6379/tcp",
            "pg_other|postgres:16-alpine|running|0.0.0.0:5433->5432/tcp",
            "pg_main|postgres:17|running|0.0.0.0:5432->5432/tcp");

        DockerPostgresContainerInfo? matched = DependencyInstaller.ParseDockerPostgresContainer(output, 5432);

        Assert.True(matched.HasValue);
        Assert.Equal("pg_main", matched.Value.Name);
        Assert.True(matched.Value.IsRunning);
        Assert.Equal(5432, matched.Value.HostPort);

        string withToolsOutput = output + Environment.NewLine + "tools_postgres|postgres:17-alpine|running|0.0.0.0:5432->5432/tcp";
        DockerPostgresContainerInfo? toolsMatch = DependencyInstaller.ParseDockerPostgresContainer(withToolsOutput, 5432);

        Assert.True(toolsMatch.HasValue);
        Assert.Equal("tools_postgres", toolsMatch.Value.Name);
    }
}
