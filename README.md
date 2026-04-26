# MelloSilveiraTools

A suite of .NET 10 NuGet packages with helpers for system development: extension methods, infrastructure (database, logging, resilience, encryption), web-API helpers, a runtime plugin system, and engineering toolkits (mathematics, mechanics of materials).

## Packages

Pick the contextual packages your application actually needs, or install the meta-package for everything.

| Package | What it ships | Depends on |
|---|---|---|
| `MelloSilveiraTools.Core` | Extension methods, `ILogger` + file logger, in-memory single/two-level caches, Polly resilience pipelines, encryption (PBKDF2), SMTP email | — |
| `MelloSilveiraTools.Database` | `IRepository`, `PostgresRepository`, `ISqlProvider`, attribute-driven SQL generation, filter clauses, Npgsql/Dapper extensions, dedicated Postgres resilience pipeline | Core |
| `MelloSilveiraTools.WebApi` | `CustomControllerBase`, `CrudController<TEntity, TFilter>`, `ExceptionHandlingMiddleware`, NDJSON streaming, JWE bearer authentication, Swagger bootstrap, `ApiServiceAgent`, `OperationBase` / `OperationResponse` | Core, Database |
| `MelloSilveiraTools.Plugins` | File-based plugin runtime, two-level cache, dynamic DI, persistence (file/database), HTTP-friendly operations, background orchestrator | Core, Database, WebApi |
| `MelloSilveiraTools.Mathematics` | Differential equation solvers (Newmark, Newmark-β) and supporting types. Future home for expressions, functions, derivatives, integrals | Core |
| `MelloSilveiraTools.MechanicsOfMaterials` | Fatigue (Goodman/Marin), constitutive equations, geometric properties (circular/rectangular profiles), 3D vector/force models | — |
| `MelloSilveiraTools` | Meta-package: ProjectReferences for every package above; install once and get everything | all of the above |

## Quick start

### Everything in one go

```csharp
services.AddToolsServices(
    databaseSettings,
    encryptionSettings,
    resiliencePipelineSettings,
    pluginSettings,
    loggerSettings);
```

`AddToolsServices` (from the meta-package) chains `AddCoreServices`, `AddDatabaseServices`, `AddMathematicsServices`, `AddMechanicsOfMaterialsServices`, `AddPluginServices` and `AddWebApiServices`. JWE auth and Swagger remain opt-in.

### Granular wire-up

```csharp
services
    .AddCoreServices(encryptionSettings, resiliencePipelineSettings, loggerSettings)
    .AddDatabaseServices(databaseSettings, resiliencePipelineSettings)
    .AddWebApiServices(resiliencePipelineSettings)
    .AddJweAuthentication(jwtSettings)
    .AddSwaggerWithBearerSecurity()
    .AddMathematicsServices()
    .AddMechanicsOfMaterialsServices()
    .AddPluginServices(pluginSettings);
```

A worker that just talks to PostgreSQL and writes file logs only needs:

```csharp
services
    .AddCoreServices(encryptionSettings, resiliencePipelineSettings)
    .AddDatabaseServices(databaseSettings, resiliencePipelineSettings);
```

— no ASP.NET, JWT, Swagger, plugins or Polly-for-SMTP transitive dependencies.

## HTTP endpoints

Both MVC controllers and minimal-API endpoint extensions ship side-by-side; consumers pick whichever style matches their host. The classic controllers (`CustomControllerBase`, `CrudController<TEntity, TFilter>`, `PluginController`) remain available for hosts that already wire `app.MapControllers()`. New projects can use the equivalent minimal-API extensions instead:

```csharp
// Generic CRUD over an entity + filter pair (alongside CrudController<TEntity, TFilter>):
app.MapCrud<MyEntity, MyFilter>(pattern: "/api/v1/things", resourceName: "thing");

// Plugin management (alongside PluginController):
app.MapPluginEndpoints("/api/v1/plugins");
```

`MapCrud` exposes the same surface the controller does: `POST /`, `GET /{id:long}`, `GET /` (filter + pagination), `PUT /{id:long}`, `DELETE /{id:long}` and `GET /stream` (NDJSON). NDJSON streaming is also available standalone via `httpContext.WriteNdjsonAsync(asyncSequence, logger, resourceName)`.

## AOT compatibility

Every package opts in to `<IsAotCompatible>true</IsAotCompatible>`. The trim/AOT diagnostic warnings (`IL2026`, `IL3050`, etc.) are silenced via `<NoWarn>` at the project level, so the toolkit doesn't propagate `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` annotations on its public surface. Where reflection-target preservation matters, `[DynamicallyAccessedMembers]` annotations are kept so the trimmer keeps the right members.

In prose: **Core**, **Mathematics** and **MechanicsOfMaterials** are AOT-clean. **Database** and **WebApi** build AOT-clean but exercise reflection at runtime (Dapper materialization, attribute-driven SQL, Swashbuckle's Newtonsoft adapter); consumers publishing AOT will see those trim warnings in their own analyzers when they call into those paths and must accept them or replace with source-generated equivalents. **Plugins** is fundamentally not AOT-publishable: `AssemblyLoadContext.LoadFromAssemblyPath` cannot work in a published AOT app.

## Plugin system

A complete plugin architecture that lets a host application discover, load and hot-swap external assemblies dropped into a folder, with their services automatically registered into the DI container.

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
services.AddPluginServices(new PluginSettings
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

## Mathematics

`MelloSilveiraTools.Mathematics` currently ships time-domain integration solvers (`NewmarkMethod`, `NewmarkBetaMethod`) and a factory. The package name is intentionally broad — future additions like `Expression`, `Function`, derivatives and integrals will land here without renaming.

```csharp
services.AddMathematicsServices();
```

## Mechanics of Materials

`MelloSilveiraTools.MechanicsOfMaterials` provides fatigue analysis (Goodman/Marin), constitutive equations, geometric property calculators (circular and rectangular profiles), and immutable 3D vector/force models.

```csharp
services.AddMechanicsOfMaterialsServices();
```
