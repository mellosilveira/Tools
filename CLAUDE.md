# CLAUDE.md

Project notes for AI agents working on **MelloSilveiraTools** — a .NET 10 NuGet suite split into contextual packages: extensions, infrastructure (DB / auth / logging / resilience), web-API helpers, a plugin system, mathematics and mechanics of materials.

## Solution layout

```
src/MelloSilveiraTools/                       — meta-package (only ProjectReferences, no code beyond the meta DI entry)
src/MelloSilveiraTools.Core/                  — extension methods, in-memory caches, Polly pipelines, encryption, SMTP email
src/MelloSilveiraTools.Database/              — IRepository, PostgresRepository, ISqlProvider, attributes, FilterClauses, Npgsql/Dapper extensions
src/MelloSilveiraTools.WebApi/                — controllers (Custom/Crud), middleware, NDJSON, Swagger, JWE auth, ApiServiceAgent, OperationBase
src/MelloSilveiraTools.Plugins/               — file-based plugin runtime, two-level cache, dynamic DI, persistence, HTTP operations, background orchestrator
src/MelloSilveiraTools.Mathematics/           — differential equation solvers (Newmark, Newmark-β); home for future expressions/functions/derivatives
src/MelloSilveiraTools.MechanicsOfMaterials/  — fatigue, constitutive equations, geometric properties, force/vector models
test/UnitTests/                               — xUnit suite, references the meta-package
build/                                        — bin/obj output (Directory.Build.props redirects)
Directory.Build.props                         — shared metadata: Authors, License, Repository, VersionPrefix, net10.0
CHANGELOG.md                                  — Keep-a-Changelog format, SemVer
```

Build everything: `dotnet build MelloSilveiraTools.sln`. Each project produces its own `.nupkg` under `build/bin/Debug/<PackageId>/`.

## Package dependency graph

```
Core ←─── Mathematics ←─── MechanicsOfMaterials
  ↑
  └── Database ←── WebApi ←── Plugins

MelloSilveiraTools (meta)  →  Core, Database, WebApi, Plugins, Mathematics, MechanicsOfMaterials
```

Notable consequences:
- `WebApi → Database` because `CrudController` consumes `IRepository`, `EntityBase`, `FilterBase`.
- `Plugins → Database` because `DatabasePluginCachePersistence` is shipped alongside `JsonFilePluginCachePersistence`.
- `MechanicsOfMaterials → Mathematics` because `ILoadSharingCalculator` uses `Vector3D` from Mathematics.

## Namespaces

Each project owns a namespace that mirrors its package id:

- `MelloSilveiraTools.Core.*` (e.g. `MelloSilveiraTools.Core.Logger`, `MelloSilveiraTools.Core.Caching`, `MelloSilveiraTools.Core.Services.Encryption`)
- `MelloSilveiraTools.Database.*`
- `MelloSilveiraTools.WebApi.*`
- `MelloSilveiraTools.Plugins.*`
- `MelloSilveiraTools.Mathematics.*`
- `MelloSilveiraTools.MechanicsOfMaterials.*`
- `MelloSilveiraTools` (meta DI only)

This is a breaking change vs. the pre-split monolith, where everything sat under `MelloSilveiraTools.Application.*`, `MelloSilveiraTools.Domain.*`, `MelloSilveiraTools.Infrastructure.*`, `MelloSilveiraTools.Authentication.*` and `MelloSilveiraTools.ExtensionMethods.*`. Consumers must update their `using` directives.

## DI entry points

Each contextual package contributes a static class in the namespace it owns. Method names are stable.

| Package | Class | Methods |
|---|---|---|
| Core | `MelloSilveiraTools.Core.CoreDependencyInjection` | `AddCoreServices(encryptionSettings, resiliencePipelineSettings, loggerSettings = null)` |
| Database | `MelloSilveiraTools.Database.DatabaseDependencyInjection` | `AddDatabaseServices(databaseSettings, resiliencePipelineSettings)` |
| WebApi | `MelloSilveiraTools.WebApi.WebApiDependencyInjection` | `AddWebApiServices(resiliencePipelineSettings)`, `AddJweAuthentication(jwtSettings)`, `AddSwaggerWithBearerSecurity()`, `UseSwaggerDocs()` |
| Plugins | `MelloSilveiraTools.Plugins.PluginsDependencyInjection` | `AddPluginServices(pluginSettings)` — registers infra **and** eagerly runs `LoadPluginsOnStartup` (must happen during DI configuration; once the host is built the collection is sealed) |
| Mathematics | `MelloSilveiraTools.Mathematics.MathematicsDependencyInjection` | `AddMathematicsServices()` — Newmark, Newmark-β + factory |
| MechanicsOfMaterials | `MelloSilveiraTools.MechanicsOfMaterials.DependencyInjection` | `AddMechanicsOfMaterialsServices()` |
| Meta | `MelloSilveiraTools.DependencyInjection` | `AddToolsServices(databaseSettings, encryptionSettings, resiliencePipelineSettings, pluginSettings, loggerSettings = null)` — chains every contextual `Add*Services` |

`AddToolsServices` calls `AddCoreServices` → `AddDatabaseServices` → `AddMathematicsServices` → `AddMechanicsOfMaterialsServices` → `AddPluginServices` → `AddWebApiServices`. JWE auth (`AddJweAuthentication`) and Swagger (`AddSwaggerWithBearerSecurity` / `UseSwaggerDocs`) remain opt-in even when the meta-package is installed.

## HTTP endpoints

Both styles ship side-by-side; consumers pick whichever matches their host. The MVC controllers (`CustomControllerBase`, `CrudController<TEntity, TFilter>`, `PluginController`) remain available for hosts already wired through `app.MapControllers()`. New projects can instead use the minimal-API endpoint extensions:

```csharp
app.MapCrud<MyEntity, MyFilter>("/api/v1/things", resourceName: "thing");
app.MapPluginEndpoints("/api/v1/plugins");
```

| Extension | Project | Sits alongside |
|---|---|---|
| `MapCrud<TEntity, TFilter>(IEndpointRouteBuilder, string pattern, string resourceName)` | WebApi (`Application/Endpoints/CrudEndpoints.cs`) | `CrudController<TEntity, TFilter>` |
| `HttpContext.WriteNdjsonAsync<T>(IAsyncEnumerable<T>, ILogger, string resourceName)` | WebApi (`Application/Endpoints/StreamEndpoints.cs`) | `CustomControllerBase.Stream` |
| `MapPluginEndpoints(IEndpointRouteBuilder, string pattern = "/api/v1/plugins")` | Plugins (`Application/Endpoints/PluginEndpoints.cs`) | `PluginController` |

Handlers in the minimal-API path return `IResult` (`Results.Json(...)` driven by `OperationResponseBase.StatusCode`). `OperationResponseExtensions` exposes four conversion helpers — all equivalent in semantics, differing only in call site:

| Method | Extends | Returns | Used by |
|---|---|---|---|
| `ToHttpResult<T>(this T)` | `T : OperationResponseBase` | `IResult` | minimal-API handlers |
| `ToHttpResultAsync<T>(this Task<T>)` | `Task<T : OperationResponseBase>` | `Task<IResult>` | minimal-API handlers (async chain) |
| `BuildHttpResponse<T>(this T)` | `T : OperationResponseBase` | `JsonResult` | MVC controllers |
| `BuildHttpResponseAsync<T>(this Task<T>)` | `Task<T : OperationResponseBase>` | `Task<JsonResult>` | MVC controllers (async chain) |

The `*Async` variants extend `Task<T>` and chain directly after `operation.ExecuteAsync(...)` without an intermediate variable — the same pattern both endpoint styles now use consistently.

## AOT compatibility

Every csproj opts in to `<IsAotCompatible>true</IsAotCompatible>` (which implicitly enables `IsTrimmable`, `EnableTrimAnalyzer` and `EnableAotAnalyzer` — no need to set them explicitly). **WebApi** and **Plugins** additionally enable `<EnableRequestDelegateGenerator>true</EnableRequestDelegateGenerator>` so the minimal-API endpoints they define (`MapCrud<,>`, `MapPluginEndpoints`) get source-generated request delegates.

Trim/AOT diagnostic warning IDs (`IL2026`, `IL2046`, `IL2050`, `IL2067`, `IL2070`–`IL2095`, `IL2111`, `IL3000`, `IL3050`–`IL3052`) are silenced once at the solution level via `<NoWarn>` in `Directory.Build.props`. We deliberately do **not** propagate `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` / `[UnconditionalSuppressMessage]` on the public surface — those attributes only exist to talk to the developer, and they polluted the code without changing runtime behaviour. Consumers who publish their own apps with `<PublishTrimmed>` or `<PublishAot>` will still receive trim warnings inside their own analyzer pass when they call into reflection-heavy paths (Dapper materialization, plugin loading, Swashbuckle's Newtonsoft adapter). Where reflection-target preservation has actual runtime impact, `[DynamicallyAccessedMembers]` annotations are kept (currently ~7 occurrences across `DictionaryExtensions`, `TypeExtensions`, `ClassExtensions`, `DbDataReaderExtensions`, `IDynamicServiceProvider`).

In prose: **Core**, **Mathematics** and **MechanicsOfMaterials** are AOT-clean. **Database** and **WebApi** build AOT-clean but exercise reflection at runtime; consumers publishing AOT must accept those trim warnings in their own builds. **Plugins** is fundamentally **not** AOT-publishable: `AssemblyLoadContext.LoadFromAssemblyPath` cannot work in a published AOT app.

Build noise that is **expected** and not actionable today:
- `RDG011` on `MapCrud<,>` (open-generic minimal-API endpoints — RDG falls back to runtime delegate generation).
- `AD0001` from `RouteHandlerAnalyzer` (.NET 10 preview SDK analyzer crash).
- `CS8620` on `CrudController.cs:173` (pre-existing nullability mismatch in the legacy controller; not introduced by the split).

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

Inside `MelloSilveiraTools.Plugins/Application/Operations/Plugins/`:

- `Get/GetPlugins` — list plugins matching a name/version filter.
- `Load/LoadPlugins`, `Reload/ReloadPlugins` — runtime invocations.
- `Cache/ClearPluginCache`, `PersistPluginCache`, `RestorePluginCache` — cache lifecycle. Persist/restore route the `{target}` segment to the keyed `IPluginCachePersistence`.

## Conventions

- Private fields use `_` prefix (linter rewrites otherwise).
- Settings are records with safe defaults registered via plain `AddSingleton(settings ?? new ...)` rather than `TryAddSingleton<TSettings>()` — keeps the fluent chain intact and lets consumers override by passing their own instance.
- Public XML docs are mandatory; `<exception>` and `<example>` tags are expected on the public surface.
- `ILogger` is the in-house abstraction (`MelloSilveiraTools.Core.Logger.ILogger`) — not `Microsoft.Extensions.Logging.ILogger`. The default implementation is `LocalFileLogger` (JSON-line, daily/size rotation).
- `OperationResponseBase` (and its derived `OperationResponse`) lives in WebApi because its `StatusCode` field is `System.Net.HttpStatusCode`. `OperationBase` therefore also lives in WebApi. Custom operations and `OperationResponseExtensions` helpers constrain on `OperationResponseBase`; the derived `OperationResponse` adds factory helpers (`CreateSuccessOk`, `CreateConflict`, `CreateUnprocessableEntity`).
- HTTP surfaces are exposed both as MVC controllers (`CustomControllerBase`/`CrudController`/`PluginController`) and as minimal-API endpoint extensions (`MapCrud`/`MapPluginEndpoints`). New projects should prefer minimal APIs but the controller path is supported for migration.
- `ApiServiceAgentBase` provides two protected HTTP-client helpers: `GetAsync<TResponse, TResponseData>` for standard JSON payloads (wrapped in `ResiliencePipeline`) and `GetStreamAsync<T>` for NDJSON-streaming endpoints (`IAsyncEnumerable<T>`, `HttpCompletionOption.ResponseHeadersRead`). `GetStreamAsync` validates the `X-Stream-Status: true` trailer emitted by `WriteNdjsonAsync` after consuming the full body, and logs an error if the trailer is absent.
- AOT/trim warnings are silenced via `<NoWarn>` at the project level. Reach for `[DynamicallyAccessedMembers]` when reflection target preservation matters; avoid `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` propagation.
- CHANGELOG entries go under `## [Unreleased]` until release; sections are `### Added / Changed / Fixed / Breaking / Removed`.

## Mathematics package

**DI-registered services** (`AddMathematicsServices`): `NewmarkMethod`, `NewmarkBetaMethod`, `DifferentialEquationMethodFactory`.

**Direct-use types** (no DI registration needed):

- *Functions* — `Function` abstract class with lazy `Derivative` / `Integral` properties. Implementations: `ConstantFunction`, `PolynomialFunction`, `ExponencialFunction`, `SineFunction`, `CosineFunction`, `PowerLaw`, `GenericFunction`. `FunctionFactory` dispatches by `FunctionType` enum.
- *Expressions* — `Expression` abstract class (sum of `Function` instances); `PronySeries` concrete expression.
- *Numerical methods* — `SimpsonRuleIntegration` (`IIntegration`); `Derivative` (`IDerivative`); `BisectionMethod`, `BrentMethod`, `RootFinding`, `StepByStepMethod` (all `IRootFinding`).
- *Statistics* — `IStatisticsCalculator` / `StatisticsCalculator`.
- *Geometry/utilities* — `Point3D`, `Vector3D`, `Vector3DExtension`, `DoubleExtensions`, `UnitConverter`, `CustomMath`, `MathematicConstants`.

Add new feature families as sibling namespaces inside the same csproj (e.g. `MelloSilveiraTools.Mathematics.Expressions`).

## Companion package: MechanicsOfMaterials

**DI-registered services** (`AddMechanicsOfMaterialsServices`): `IConstitutiveEquationsCalculator`, `IFatigueCalculator`, `IGeometricPropertyCalculator<CircularProfile>`, `IGeometricPropertyCalculator<RectangularProfile>`.

**Direct-use calculators** (no DI registration):

- *Mechanical models* — `IMechanicalModelCalculator<TInput>` (force / displacement / stress / strain). Families: elastic (`ElasticModelCalculator`), linear viscoelastic (`MaxwellModelCalculator`), non-linear viscoelastic (`SchaperyModelCalculator`, `ModifiedSuperpositionMethodCalculator`), quasi-linear viscoelastic (`FungModelCalculator`, `SimplifiedFungModelCalculator`).
- *Load sharing* — `ILoadSharingCalculator` / `LoadShare1DTissueThreeDimensionalSpaceCalculator`.
- *Converters* — `IMechanicalParameterConverter` / `MechanicalParameterConverter`.
- *Attributes* — `MechanicalModelParameterAttribute`, `MechanicalModelParameterCalculationAttribute`.

`Force` is immutable after construction; use `Sum`/`Subtract`/`Round`/`Divide`/`Abs`/`Create` to derive new instances. Depends on Mathematics (transitive) for `Vector3D`.
