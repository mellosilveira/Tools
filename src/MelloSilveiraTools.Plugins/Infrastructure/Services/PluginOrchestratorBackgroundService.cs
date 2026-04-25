using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MelloSilveiraTools.Plugins.Infrastructure.Services;

/// <summary>
/// Periodically inspects the plugin folder, loads newer plugin versions through
/// <see cref="IPluginService.LoadPluginsOnRuntime(string?, PluginVersion?)"/> and evicts obsolete
/// cached versions according to <see cref="PluginSettings.PreviousVersionRetention"/>.
/// </summary>
public sealed class PluginOrchestratorBackgroundService(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    PluginFileProcessor fileProcessor,
    PluginCache cache,
    PluginSettings settings) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(settings.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await InspectAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { logger.Error("Plugin orchestrator pass failed.", ex); }
        }
    }

    private async Task InspectAsync(CancellationToken cancellationToken)
    {
        foreach (IGrouping<string, DiscoveredPlugin> pluginGroup in fileProcessor.Scan().GroupBy(discovered => discovered.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // One snapshot per plugin name, kept in sync as we promote new versions in-loop.
            List<CachedVersion> cachedVersions = await SnapshotCacheAsync(pluginGroup.Key, cancellationToken);
            PluginVersion? highestLoaded = cachedVersions
                .Where(cachedVersion => cachedVersion.IsFullyLoaded)
                .Max(cachedVersion => (PluginVersion?)cachedVersion.Version);

            // Descending: the highest folder version is processed first; lower files in the same
            // group are then automatically warn-ignored against the just-promoted snapshot, which
            // avoids redundant LoadPluginsOnRuntime calls (e.g. folder has 1.4 and 1.5 while only
            // 1.2 is loaded → only 1.5 is promoted, 1.4 is ignored).
            foreach (DiscoveredPlugin discovered in pluginGroup.OrderByDescending(discovered => discovered.Version))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Process(discovered, cachedVersions, highestLoaded);
            }
        }
    }

    private void Process(DiscoveredPlugin discovered, List<CachedVersion> cachedVersions, PluginVersion? highestLoaded)
    {
        // Newer version already in place → ignore the old file.
        if (highestLoaded is PluginVersion highest && discovered.Version < highest)
        {
            logger.Warn($"Ignoring plugin file '{Path.GetFileName(discovered.FullPath)}': version {highest.Name} of '{discovered.Name}' is already loaded.");
            return;
        }

        // Already fully loaded at this exact version → nothing to do.
        if (cachedVersions.Any(cachedVersion => cachedVersion.Version == discovered.Version && cachedVersion.IsFullyLoaded))
            return;

        if (!TryLoad(discovered))
            return;

        EvictOlderVersions(discovered, cachedVersions, previousVersion: highestLoaded);
        cachedVersions.Add(new CachedVersion(discovered.Version, DateTimeOffset.UtcNow));
    }

    private void EvictOlderVersions(DiscoveredPlugin discovered, List<CachedVersion> cachedVersions, PluginVersion? previousVersion)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cachedVersions.RemoveAll(cachedVersion =>
        {
            if (cachedVersion.Version >= discovered.Version)
                return false;

            bool keepInRetention = cachedVersion.Version == previousVersion
                && cachedVersion.FullyLoadedAt is DateTimeOffset loadedAt
                && now - loadedAt < settings.PreviousVersionRetention;

            if (keepInRetention)
                return false;

            cache.Clear(discovered.Name, cachedVersion.Version);
            return true;
        });
    }

    private bool TryLoad(DiscoveredPlugin discovered)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<IPluginService>().LoadPluginsOnRuntime(discovered.Name, discovered.Version);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load plugin '{discovered.Name}' {discovered.Version.Name}.", ex);
            return false;
        }
    }

    private ValueTask<List<CachedVersion>> SnapshotCacheAsync(string pluginName, CancellationToken cancellationToken)
        => cache
            .Stream(pluginName, version: null, cancellationToken)
            .Select(entry => new CachedVersion(entry.Version, entry.State))
            .ToListAsync(cancellationToken);

    private readonly record struct CachedVersion
    {
        public CachedVersion(string version, DiscoveredPlugin state)
        {
            Version = PluginVersion.Parse(version);

            if (state is RegisteredPlugin registered)
            {
                FullyLoadedAt = registered.FullyLoadedAt;
                IsFullyLoaded = registered.IsFullyLoaded;
            }
        }

        public CachedVersion(PluginVersion version, DateTimeOffset fullyLoadedAt)
        {
            Version = version;
            FullyLoadedAt = fullyLoadedAt;
            IsFullyLoaded = true;
        }

        public PluginVersion Version { get; }
        public DateTimeOffset? FullyLoadedAt { get; }
        public bool IsFullyLoaded { get; }
    }
}
