# CLAUDE.md

Project notes for AI agents working on **MelloSilveiraTools** — a .NET 10 NuGet package with helpers, extensions, infrastructure (DB/auth/logging/resilience) and a plugin system.

## Solution layout

```
src/MelloSilveiraTools/                       — main package
src/MelloSilveiraTools.MechanicsOfMaterials/  — companion package (fatigue, geometric properties, etc.)
test/                                         — unit tests
build/                                        — build/pack output (Directory.Build.props redirects bin/obj here)
Directory.Build.props                         — shared metadata: Authors, License, Repository, VersionPrefix, net10.0
CHANGELOG.md                                  — Keep-a-Changelog format, SemVer
```

Build: `dotnet build src/MelloSilveiraTools/MelloSilveiraTools.csproj`. Output `.nupkg` lands under `build/bin/Debug/MelloSilveiraTools/`.

## DI entry points (`src/MelloSilveiraTools/DependencyInjection.cs`)

- `AddToolsServices(databaseSettings, encryptionSettings, resiliencePipelineSettings, loggerSettings = null)` — settings, resilience pipelines, SQL provider, repository, logger, encryption.
- `AddPluginServices(pluginSettings)` — plugin infrastructure + eager `LoadPluginsOnStartup`. Must run during DI configuration; an `IApplicationBuilder` hook would be too late because the collection is sealed once the host is built.
- `AddNumericalMethods()` — Newmark / Newmark-β + factory.
- `AddJweAuthentication(jwtSettings)` — JWE bearer auth.
- `AddSwaggerWithBearerSecurity()` / `UseSwaggerDocs()` — Swagger bootstrap.

## Plugin system

Discovers DLLs from `PluginSettings.Directory`, matched by the regex `^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$`.

### Pipeline (`PluginCache` keys by name + `PluginVersion`)

```
DiscoveredPlugin -> LoadedPlugin -> RegisteredPlugin (IsFullyLoaded, FullyLoadedAt)
```

- `PluginFileProcessor` — scans the main folder and the `loaded/` subfolder, parses filenames into `DiscoveredPlugin`, moves files between the two folders.
- `PluginAssemblyProcessor` — loads assemblies via `AssemblyLoadContext.Default`, finds processable types (those assignable to a registered `IPluginTypeProcessor.ProcessableType`), and dispatches each type to the matching `IPluginTypeProcessor.Process(type, context)`.
- `IPluginTypeProcessor` — consumer-supplied registration logic per base type. Receives a `PluginRegistrationContext` built by the service:
  - `ForStartup(IServiceCollection)` — startup mode, mutates the root collection.
  - `ForRuntime(IDynamicServiceProvider)` — runtime mode, registers in the dynamic provider. Exactly one of `Services` / `DynamicProvider` is non-null.
- `PluginCache` — wraps `ITwoLevelCache<DiscoveredPlugin>` (level 1 = name, level 2 = `PluginVersion.Name`). Methods: `GetOrAdd`, `Update`, `TryGet<T>`, `Clear()`, `Clear(name, version?)`, `Stream(...)`.

### `IPluginService` (scoped, `PluginService` impl)

- `LoadPluginsOnStartup(name?, version?)` — scan + load + register against the root `IServiceCollection`.
- `LoadPluginsOnRuntime(name?, version?)` — same but against `IDynamicServiceProvider`.
- `ReloadPluginsOnStartup(forceLoad, name?, version?)` / `ReloadPluginsOnRuntime(...)` — moves plugins back from `loaded/` to the main folder, optionally evicting **only** that exact (name, version) cache entry before reloading.
- `Clear()`, `GetPlugins(name, version?)`, `PersistCacheAsync(...)`, `RestoreCacheAsync(...)`.

`PluginService` is **scoped** because `IPluginCachePersistence` is resolved per request from a route value. The non-keyed scoped factory reads `{target}` from `HttpContext.Request.RouteValues` and falls back to `PluginSettings.DefaultCacheTarget` when there is no HTTP context (e.g. inside the orchestrator).

### Persistence

`IPluginCachePersistence` is registered as **keyed** singletons:

- `PluginCacheTargets.File` -> `JsonFilePluginCachePersistence`
- `PluginCacheTargets.Database` -> `DatabasePluginCachePersistence`

Consumers can add their own keys (`AddKeyedSingleton<IPluginCachePersistence, X>("redis")`) and the existing cache endpoints route by the `{target}` segment without further changes.

### Background orchestrator

`PluginOrchestratorBackgroundService` (singleton hosted service, registered automatically by `AddPluginServices`):

1. Every `PluginSettings.PollInterval`, calls `PluginFileProcessor.Scan()` on the main folder.
2. Groups results by plugin name; takes one `PluginCache` snapshot per group.
3. For each group, processes files **in descending version order** so the highest version is promoted first; lower files in the same pass automatically fall into the warn-ignore branch against the just-promoted snapshot — avoiding redundant `LoadPluginsOnRuntime` calls.
4. Per file:
   - `file.Version < highestLoaded` -> log `Warn` and skip.
   - `file.Version` already fully loaded -> skip.
   - otherwise -> `IPluginService.LoadPluginsOnRuntime(name, version)` (resolved through a fresh DI scope), then evict every cached version older than the new one **except** the one immediately below — that one is kept until `PluginSettings.PreviousVersionRetention` elapses (timed by `RegisteredPlugin.FullyLoadedAt`).
5. The in-memory snapshot is updated synchronously with each promotion/eviction so subsequent files in the same group see the new state without re-querying the cache.

### Application operations (HTTP-friendly wrappers)

`Application/Operations/Plugins/`:

- `Get/GetPlugins` — list plugins matching a name/version filter.
- `Load/LoadPlugins`, `Reload/ReloadPlugins` — runtime invocations.
- `Cache/ClearPluginCache`, `PersistPluginCache`, `RestorePluginCache` — cache lifecycle. Persist/restore route the `{target}` segment to the keyed `IPluginCachePersistence`.

## Conventions

- Private fields use `_` prefix (linter rewrites otherwise).
- Settings are records with safe defaults registered via plain `AddSingleton(settings ?? new ...)` rather than `TryAddSingleton<TSettings>()` — keeps the fluent chain intact and lets consumers override by passing their own instance.
- Public XML docs are mandatory; `<exception>` and `<example>` tags are expected on the public surface.
- `ILogger` is the in-house abstraction (`Infrastructure/Logger/ILogger.cs`) — not `Microsoft.Extensions.Logging.ILogger`. The default implementation is `LocalFileLogger` (JSON-line, daily/size rotation).
- CHANGELOG entries go under `## [Unreleased]` until release; sections are `### Added / Changed / Fixed / Breaking / Removed`.

## Companion package

`MelloSilveiraTools.MechanicsOfMaterials` ships fatigue (Goodman/Marin) analysis, constitutive equations, geometric properties, 3D vector/force models. `Force` is immutable after construction (use `Sum`/`Subtract`/`Round`/`Divide`/`Abs`/`Create` to derive new instances).
