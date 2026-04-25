# MelloSilveiraTools

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-04-25
### Added
- **Plugin architecture** for discovering, loading and hot-swapping external assemblies dropped into a configured folder, with their services automatically wired into the DI container:
  - File-based discovery: DLLs named `{name}.v{major}.{minor}.{patch}.dll` are scanned by `PluginFileProcessor`. Loaded files are moved to a `loaded/` subfolder.
  - `PluginVersion` (readonly record struct) with `Parse`/`TryParse`/`SafeParse` and full ordering operators.
  - Two-level `PluginCache` (level 1 = name, level 2 = version) tracking the pipeline `DiscoveredPlugin` -> `LoadedPlugin` -> `RegisteredPlugin` (`IsFullyLoaded`, `FullyLoadedAt`).
  - `IPluginTypeProcessor` extension point for consumer-supplied registration logic per base type, receiving a `PluginRegistrationContext` (`ForStartup` / `ForRuntime`) so the same processor can target either the static `IServiceCollection` or an `IDynamicServiceProvider`.
  - `IPluginService` (`PluginService`) with `LoadPluginsOnStartup` / `LoadPluginsOnRuntime`, `ReloadPluginsOnStartup` / `ReloadPluginsOnRuntime`, `GetPlugins`, `Clear`, `PersistCacheAsync`, `RestoreCacheAsync`.
  - `IPluginCachePersistence` registered as keyed services; the `{target}` route segment on cache endpoints picks the implementation at runtime. Built-in keys: `PluginCacheTargets.File` (JSON file) and `PluginCacheTargets.Database`. Consumers can register additional keyed implementations without touching the endpoints.
  - HTTP-friendly application operations under `Application/Operations/Plugins/`: `GetPlugins`, `LoadPlugins`, `ReloadPlugins`, `ClearPluginCache`, `PersistPluginCache`, `RestorePluginCache`.
  - `AddPluginServices(pluginSettings)` registers the whole stack and eagerly runs `LoadPluginsOnStartup` against the supplied `IServiceCollection` before returning, because the collection is sealed once the host is built.
  - `PluginOrchestratorBackgroundService` (registered automatically) periodically inspects the plugin folder, promotes newer versions through `LoadPluginsOnRuntime`, ignores files older than the currently-loaded version (logging a warning), and evicts obsolete cached versions, keeping the immediately-previous version until `PluginSettings.PreviousVersionRetention` elapses.
  - `PluginSettings`: `Directory`, `PollInterval` (default 30 s), `PreviousVersionRetention` (default 24 h), `DefaultCacheTarget` (default `PluginCacheTargets.File`, used when no HTTP `{target}` route value is available, e.g. inside the orchestrator).
- `Infrastructure.Logger.LocalFileLogger` is now a working implementation: appends one JSON entry per line, rolls daily and by size, retains the last N files. Configurable via the new `LoggerSettings` record (directory, file name prefix, daily roll, max file size, max retained files). `AddToolsServices` accepts an optional `LoggerSettings` parameter and falls back to `new LoggerSettings()` when omitted.
- `<example>` snippets to `OperationBase.ProcessAsync`, `IRepository.GetAsync` and `IFatigueCalculator.CalculateFatigueResult`.
- `<exception>` documentation across public surfaces that throw (`IAuthenticationTokenService.RefreshAsync`, `EnumerableExtensions.FirstWithoutValidate`, `TypeExtensions.GetDbTypeFromPropertyType`, `IRepository`).
- Minimal-API endpoint extensions (`MapCrud<TEntity, TFilter>`, `MapPluginEndpoints`, `WriteNdjsonAsync`) added alongside the existing controllers. Both styles are supported; controllers remain for backward compatibility.
### Fixed
- Postgres repository now honors the configured `CommandTimeout` on every command path (previously defaulted on some repositories).
- `unique_hash` computation for filter clauses is now stable across process restarts.
- Swagger generation picks up the correct calling assembly when multiple assemblies are loaded in the host.
- `JOIN` column resolution no longer leaks columns from unrelated tables when two entities share column names.
- `FilterClause` expansion correctly flattens nested groups and preserves operator precedence.
- `Force` (Mechanics of Materials): magnitude (`AbsoluteValue`) is now always consistent with `X`/`Y`/`Z` after `Sum`/`Subtract`/`Round`/`Divide`/`Abs`/`Create`.
- `EnumerableExtensions.FirstWithoutValidate` exception message corrected ("No element matched the predicate.").
### Changed
- NuGet metadata populated on both `MelloSilveiraTools` and `MelloSilveiraTools.MechanicsOfMaterials` projects (PackageId, Description, Tags, README, license, symbols).
- Both packages now target `net10.0` (`MechanicsOfMaterials` was previously on `net9.0`).
- XML documentation is now generated and shipped inside the `.nupkg` so consumers get IntelliSense.
- Documented contracts that were previously implicit: `ISqlProvider` template placeholders (`#TABLE_NAME`, `#COLUMNS`, `#WHERE`, `#ORDERBY`, `#LIMIT`, `#OFFSET`, `#JOIN`, etc.); `PostgresSqlProvider` static cache lifetime and thread-safety; `PostgresRepository` resilience pipeline / timeout behavior; `ExceptionHandlingMiddleware` exception→status mapping; `AuthenticationJweTokenService` 32-byte UTF-8 key requirement; Newmark / Newmark-β stability characteristics in `IDifferentialEquationMethod`.
### Breaking
- `MelloSilveiraTools.MechanicsOfMaterials.Models.Force.AbsolutValue` renamed to `AbsoluteValue` (typo fix).
- `Force` `X`/`Y`/`Z`/`AbsoluteValue` setters are now `private`; `Force` instances are immutable after construction. Use `Sum`/`Subtract`/`Round`/`Divide`/`Abs`/`Create` to derive new instances.

## [1.2.1] - 2026-01-06
### Fixed
- `OperationBase` now uses a generic method instead of casting `OperationResponse` when handling unexpected errors.

## [1.2.0] - 2026-01-06
### Added
- `DifferentialEquationMethodFactory` and `CurveType` enum.
- `CreateSuccessOk` overload on `OperationResponseBase` that carries response data.
- `CreateConflict` and `CreateUnprocessableEntity` helpers on `OperationResponse`.
- `MelloSilveiraTools.MechanicsOfMaterials` companion package.
### Renamed
- `Fatigue` → `FatigueCalculator`.
- `GeometricProperty` → `GeometricPropertyCalculator`.
- `MechanicsOfMaterials` → `ConstitutiveEquationsCalculator`.

## [1.1.0] - 2025-11-29
### Added
- File-based `ILogger` implementation.
- `GetDeleteByPrimaryKeyQuery`, `GetSelectByPrimaryKeyQuery` and `GetUpdateByPrimaryKeyQuery` on `ISqlProvider`.
- Extension methods for Npgsql types, `IFormFile`, `Type` and `string`.
- JWE authentication (`AddJweAuthentication`, `AuthenticationJweTokenService`).
- Swagger bootstrap helpers (`AddSwaggerDocsWithJwtAuthentication`, `UseSwaggerDocs`).
- Encryption service.
- Pooled/stack-allocating string builder.
- Default resilience pipeline and PostgreSQL-specific pipeline (Polly).
- Base `IRepository` contract and `PostgresRepository` implementation.
- `OperationBase` and use-case operation scaffolding.
- Base HTTP service agent.
- Initial Mechanics of Materials primitives.
### Removed
- `ReferencedPropertyName` from `ForeignKeyColumnAttribute`.

## [1.0.3] - 2025-06-11
### Added
- `TableAttribute` constructor accepting name and alias.

## [1.0.2] - 2025-06-10
### Fixed
- Project metadata.

## [1.0.1] - 2025-06-10
### Removed
- Unnecessary build files.

## [1.0.0] - 2025-06-10
### Added
- First public release.
