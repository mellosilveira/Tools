using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Plugins.Infrastructure.Services;

/// <summary>
/// Periodically inspects the plugin folder, loads newer plugin versions through
/// <see cref="IPluginService.LoadPluginsOnRuntimeAsync(string?, PluginVersion?, CancellationToken)"/> and evicts obsolete
/// cached versions according to <see cref="PluginSettings.PreviousVersionRetention"/>.
/// </summary>
public sealed class PluginOrchestratorBackgroundService(
    ILogger<PluginOrchestratorBackgroundService> logger,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
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
                await InspectAsync(stoppingToken).ConfigureAwait(false);
                await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Plugin orchestrator timer canceled.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Plugin orchestrator pass failed.");
            }
        }
    }

    private async Task InspectAsync(CancellationToken cancellationToken)
    {
        List<DiscoveredPlugin> discoveredPlugins = [];
        await foreach (DiscoveredPlugin discovered in fileProcessor.ScanAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            discoveredPlugins.Add(discovered);
        }

        foreach (IGrouping<string, DiscoveredPlugin> pluginGroup in discoveredPlugins.GroupBy(discovered => discovered.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // One snapshot per plugin name, kept in sync as we promote new versions in-loop.
            List<CachedVersion> cachedVersions = await SnapshotCacheAsync(pluginGroup.Key, cancellationToken).ConfigureAwait(false);
            PluginVersion? highestLoaded = cachedVersions
                .Where(cachedVersion => cachedVersion.IsFullyLoaded)
                .Max(cachedVersion => (PluginVersion?)cachedVersion.Version);

            // Descending: the highest folder version is processed first; lower files in the same
            // group are then automatically warn-ignored against the just-promoted snapshot.
            foreach (DiscoveredPlugin discovered in pluginGroup.OrderByDescending(discovered => discovered.Version))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessAsync(discovered, cachedVersions, highestLoaded, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessAsync(
        DiscoveredPlugin discovered,
        List<CachedVersion> cachedVersions,
        PluginVersion? highestLoaded,
        CancellationToken cancellationToken)
    {
        // Newer version already in place → ignore the old file.
        if (highestLoaded is PluginVersion highest && discovered.Version < highest)
        {
            logger.LogWarning(
                "Ignoring plugin file '{PluginFileName}': version {PluginVersion} of '{PluginName}' is already loaded.",
                Path.GetFileName(discovered.FullPath),
                highest.Name,
                discovered.Name);
            return;
        }

        // Already fully loaded at this exact version → nothing to do.
        if (cachedVersions.Any(cachedVersion => cachedVersion.Version == discovered.Version && cachedVersion.IsFullyLoaded))
            return;

        if (!await TryLoadAsync(discovered, cancellationToken).ConfigureAwait(false))
            return;

        await EvictOlderVersionsAsync(discovered, cachedVersions, previousVersion: highestLoaded, cancellationToken).ConfigureAwait(false);
        cachedVersions.Add(new CachedVersion(discovered.Version, timeProvider.GetUtcNow()));
    }

    private async Task EvictOlderVersionsAsync(
        DiscoveredPlugin discovered,
        List<CachedVersion> cachedVersions,
        PluginVersion? previousVersion,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<CachedVersion> versionsToRemove = [];

        foreach (CachedVersion cachedVersion in cachedVersions)
        {
            if (cachedVersion.Version >= discovered.Version)
                continue;

            bool keepInRetention = cachedVersion.Version == previousVersion
                && cachedVersion.FullyLoadedAt is DateTimeOffset loadedAt
                && now - loadedAt < settings.PreviousVersionRetention;

            if (keepInRetention)
                continue;

            cache.Clear(discovered.Name, cachedVersion.Version);
            logger.LogInformation("Evicted obsolete version {PluginVersion} of '{PluginName}'.", cachedVersion.Version.Name, discovered.Name);

            await foreach (DiscoveredPlugin obsoletePlugin in fileProcessor.ScanLoadedAsync(discovered.Name, cachedVersion.Version, cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await fileProcessor.MoveToArchiveFolderAsync(obsoletePlugin, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("Archived obsolete plugin file '{FileName}'.", Path.GetFileName(obsoletePlugin.FullPath));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to archive obsolete plugin file '{FileName}'.", Path.GetFileName(obsoletePlugin.FullPath));
                }
            }

            versionsToRemove.Add(cachedVersion);
        }

        foreach (CachedVersion toRemove in versionsToRemove)
        {
            cachedVersions.Remove(toRemove);
        }
    }

    private async Task<bool> TryLoadAsync(DiscoveredPlugin discovered, CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            await scope.ServiceProvider
                .GetRequiredService<IPluginService>()
                .LoadPluginsOnRuntimeAsync(discovered.Name, discovered.Version, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to load plugin '{PluginName}' {PluginVersion}.",
                discovered.Name,
                discovered.Version.Name);
            return false;
        }
    }

    private async ValueTask<List<CachedVersion>> SnapshotCacheAsync(string pluginName, CancellationToken cancellationToken)
    {
        Dictionary<PluginVersion, CachedVersion> dict = [];
        await foreach (PluginCacheEntry entry in cache.Stream(pluginName, version: null, cancellationToken).ConfigureAwait(false))
        {
            CachedVersion current = new(entry.Version, entry.State);
            if (!dict.TryGetValue(current.Version, out CachedVersion existing) || (!existing.IsFullyLoaded && current.IsFullyLoaded))
                dict[current.Version] = current;
        }

        return [.. dict.Values];
    }

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
