# Tools

NuGet package with helpers for .NET system development, providing extensions, utilities, and reusable patterns to speed up building clean and robust applications.

## Plugin system

The package ships a complete plugin architecture that lets a host application discover, load and hot-swap external assemblies dropped into a folder, with their services automatically registered into the DI container.

### Capabilities

- **File-based discovery.** DLLs named `{name}.v{major}.{minor}.{patch}.dll` are scanned from the configured plugin directory. Once loaded they are moved to a `loaded/` subfolder.
- **Two registration modes.**
  - *Startup:* services registered against the root `IServiceCollection` (`IPluginService.LoadPluginsOnStartup`).
  - *Runtime:* services registered against an `IDynamicServiceProvider` (`IPluginService.LoadPluginsOnRuntime`), so plugins can come and go without restarting the host.
- **Type processors.** Implement `IPluginTypeProcessor` to teach the system how to register a particular base type (interface or class) found inside a plugin assembly. The processor receives a `PluginRegistrationContext` that exposes either the `IServiceCollection` (startup) or the `IDynamicServiceProvider` (runtime).
- **Two-level cache.** `PluginCache` keys by plugin name and `PluginVersion`. Entries progress through `DiscoveredPlugin` -> `LoadedPlugin` -> `RegisteredPlugin`, with `RegisteredPlugin.IsFullyLoaded` / `FullyLoadedAt` marking the final stage.
- **Persistence.** `IPluginCachePersistence` snapshots the cache to non-volatile storage. Two implementations ship out of the box (`PluginCacheTargets.File`, `PluginCacheTargets.Database`); consumers can add their own keyed implementations and the existing endpoints route to them via the `{target}` route value. The fallback used outside an HTTP request is configurable via `PluginSettings.DefaultCacheTarget`.
- **Background orchestrator.** `PluginOrchestratorBackgroundService` polls the plugin folder periodically (`PluginSettings.PollInterval`) and:
  - ignores files whose version is lower than the highest currently-loaded version, logging a warning;
  - skips files already fully loaded;
  - loads higher versions through `LoadPluginsOnRuntime` and evicts older cached versions, keeping the version immediately below the new one until `PluginSettings.PreviousVersionRetention` elapses.
- **Application operations.** Ready-to-use operations (`GetPlugins`, `LoadPlugins`, `ReloadPlugins`, `ClearPluginCache`, `PersistPluginCache`, `RestorePluginCache`) wrap `IPluginService` for HTTP exposure.

### Wire-up

```csharp
services
    .AddToolsServices(databaseSettings, encryptionSettings, resiliencePipelineSettings, loggerSettings)
    .AddPluginServices(new PluginSettings
    {
        Directory = "/var/app/plugins",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24),
        DefaultCacheTarget = PluginCacheTargets.File
    });
```

`AddPluginServices` registers everything needed (cache, persistences, processors, orchestrator) **and** eagerly runs `LoadPluginsOnStartup` against the supplied `IServiceCollection` before returning. This must happen during DI configuration: once the host is built the collection is sealed, so any later registration would be discarded.

### File naming

```
MyCompany.Plugins.Reporting.v1.2.3.dll
^---------- name -----------^^ version ^
```

`PluginVersion` parses the `v{major}.{minor}.{patch}` suffix and supports comparison operators, so the orchestrator can decide whether a file is newer than what is already loaded.
