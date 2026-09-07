# AGENTS.md

Project notes and developer guidelines for AI agents working on **MelloSilveiraTools** — a .NET 10 suite of NuGet packages split into contextual libraries: core utilities, relational database provider, web-API helpers, dynamic plugin runtime, mathematics, mechanics of materials, and experimental data optimizations.

---

## Solution Layout

```
src/MelloSilveiraTools/                                — Meta-package (aggregates all packages; meta DI entry)
src/MelloSilveiraTools.Core/                           — Extensions, in-memory caches, Polly v8 pipelines, encryption, email, file management
src/MelloSilveiraTools.Database/                       — IRepository, PostgresRepository, ISqlProvider, attributes, FilterClauses, Npgsql/Dapper
src/MelloSilveiraTools.WebApi/                         — Controllers (Custom/Crud), minimal endpoints, NDJSON streaming, Swagger, JWE auth, ApiServiceAgent, OperationBase
src/MelloSilveiraTools.Plugins/                        — File-based plugin runtime, two-level cache, dynamic DI, persistence, background orchestrator
src/MelloSilveiraTools.Mathematics/                    — Differential equation solvers (Newmark, Newmark-β), univariate Function hierarchy, expressions, numerical calculus, root-finding, 3D geometry
src/MelloSilveiraTools.MechanicsOfMaterials/           — Fatigue, constitutive equations, geometric profiles, force/vector models, viscoelastic models
src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ — TPL Dataflow pipelines, experimental data segmentation (Ramp/Relaxation/Descent/Recovery), curve fitting via MathNet.Numerics
test/UnitTests/                                        — xUnit test suite, references the meta-package and Optimizations
build/                                                 — Centralized bin/obj output redirected via Directory.Build.props
Directory.Build.props                                  — Common metadata: net10.0, Authors, License, VersionPrefix, AOT/trim warning suppressions
CHANGELOG.md                                           — Keep-a-Changelog format, SemVer
```

Build the entire solution:
```powershell
dotnet build MelloSilveiraTools.sln
```
Each project generates its own `.nupkg` and `.snupkg` under `build/bin/Debug/<PackageId>/`.

---

## Package Dependency Graph

```
Core ←─── Mathematics ←─── MechanicsOfMaterials ←─── MechanicsOfMaterials.Optimizations
  ↑
  └── Database ←── WebApi ←── Plugins

MelloSilveiraTools (meta) → Core, Database, WebApi, Plugins, Mathematics, MechanicsOfMaterials
```

### Notable Constraints
- `WebApi → Database`: `CrudController` and `MapCrud` consume `IRepository`, `EntityBase`, and `FilterBase`.
- `Plugins → Database`: `DatabasePluginCachePersistence` ships alongside `JsonFilePluginCachePersistence`.
- `MechanicsOfMaterials → Mathematics`: `ILoadSharingCalculator` uses `Vector3D` from Mathematics.
- `MechanicsOfMaterials.Optimizations → MechanicsOfMaterials`: Builds on model calculators, curve segments, and numerical routines.

---

## Namespaces

Each project owns a namespace that mirrors its package ID:

- `MelloSilveiraTools.Core.*` (e.g. `MelloSilveiraTools.Core.Caching`, `MelloSilveiraTools.Core.Services.Encryption`, `MelloSilveiraTools.Core.Managers.File`)
- `MelloSilveiraTools.Database.*` (e.g. `MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider`, `MelloSilveiraTools.Database.RelationalDatabase.Repositories`)
- `MelloSilveiraTools.WebApi.*`
- `MelloSilveiraTools.Plugins.*`
- `MelloSilveiraTools.Mathematics.*`
- `MelloSilveiraTools.MechanicsOfMaterials.*`
- `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.*`
- `MelloSilveiraTools` (meta DI only)

---

## Coding Conventions & Style (.editorconfig Rules)

AI agents modifying or generating code in this repository **must strictly adhere** to the following rules:

### 1. Type Declarations: No `var`
- **Explicit types are mandatory:** `csharp_style_var_for_built_in_types = false`, `csharp_style_var_when_type_is_apparent = false`, `csharp_style_var_elsewhere = false`.
- Never use `var` for local variable declarations. Always write explicit type names (e.g., `int count = 0;`, `string name = string.Empty;`, `List<T> items = new();`).
- Target-typed `new()` is encouraged when the type is explicitly stated on the left-hand side (e.g., `MyService service = new(options);`).

### 2. Constructor & Method Body Styles
- **Primary constructors** are preferred for dependency injection and classes/records: `csharp_style_prefer_primary_constructors = true`.
- **Properties, indexers, accessors, and lambdas** use expression bodies (`=>`).
- **Methods and constructors** must use standard block bodies `{ ... }` (`csharp_style_expression_bodied_methods = false`, `csharp_style_expression_bodied_constructors = false`).

### 3. Layout, Formatting & Syntax
- **Indentation:** 4 spaces.
- **Line endings:** CRLF (`end_of_line = crlf`).
- **Braces:** Always required for all control flow statements (`csharp_prefer_braces = true`).
- **Namespaces:** Block-scoped declarations (`namespace Foo { ... }`) are configured in `.editorconfig`. Keep consistency with surrounding files.
- **`using` directives:** Place outside of namespaces.
- **Private fields:** Always prefix with underscore (`_privateField`).
- **Modern C# 13 features:**
  - Collection expressions `[...]` instead of `new[] { ... }` or `new List<T>()`.
  - `System.Threading.Lock` over `object` monitor locks.
  - Pattern matching with switch expressions and extended property patterns.
  - Slices and spans (`span[..index]`, `AsSpan()`).

### 4. Logging
- In v1.5.0+, the in-house logging abstraction was removed.
- Use standard **`Microsoft.Extensions.Logging.ILogger<T>`** across all packages.
- Telemetry is backed by Serilog for structured JSON file logging.

### 5. Settings & DI
- Settings classes are `record`s with safe defaults.
- Registered via `services.AddSingleton(settings ?? new TSettings())` to preserve fluent chaining and consumer overrides.

### 6. Public Documentation
- Comprehensive XML documentation comments are **mandatory** on all public types and members.
- Must include `<summary>`, `<param>`, `<returns>`, and where applicable `<exception>` and `<example>`.

---

## Domain Architecture & Patterns

### 1. Result vs. Output (Calculation Engine)
- Mathematical and mechanical calculators emit raw projection data classes named **`*Output`** (e.g. `MechanicalModelOutput`), never `*Result`.
- Calculator blocks are decoupled from application/API status monads (`Result<T>` / `OperationResponse`).

### 2. Constitutive Modeling (Mechanics of Materials)
- Physical material parameters inherit from the **`ConstitutiveParameters`** abstract base record (e.g., `MaxwellConstitutiveParameters`, `SchaperyConstitutiveParameters`, `FungConstitutiveParameters`).
- Calculator execution uses the strongly-typed wrapper `MechanicalModelInput<TConstitutiveParameters>`.
- Models: Elastic, Linear Viscoelastic (Maxwell), Non-Linear Viscoelastic (Schapery, Modified Superposition), Quasi-Linear Viscoelastic (Fung, Simplified Fung).
- `Force` is immutable after construction; derive new instances with `Sum()`, `Subtract()`, `Round()`, `Divide()`, `Abs()`, `Create()`.

### 3. Experimental Data & Optimizations
- Located in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
- Multi-stage TPL Dataflow pipelines process high-frequency sensor curves (stress/strain vs. time).
- Segment types: `Ramp`, `Relaxation`, `Descent`, `Recovery`.
- Mathematical optimization via MathNet.Numerics (Levenberg-Marquardt curve fitting).

### 4. Database & SQL Generation
- [`PostgresSqlProvider`](file:///D:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Database/RelationalDatabase/Sql/Provider/PostgresSqlProvider.cs) caches generated SQL in static `ConcurrentDictionary<(Type, Operation, int), string>`.
- Supports single and multi-row batch insert with `ON CONFLICT (unique_columns) DO UPDATE SET ...`.
- `BuildUniqueUpdates` updates non-PK and non-unique payload columns; falls back to unique column update if no payload columns exist.
- `IRepository.TryInsertAsync<TEntity>`: Insert-or-get-existing single statement CTE returning `Result<long>`.
- `IRepository.GetByUniqueColumnAsync<TEntity>`: Typed lookup via single `[UniqueColumn]` property.

### 5. HTTP & Streaming Endpoints
- WebApi provides dual surfaces:
  - Minimal APIs: `MapCrud<TEntity, TFilter>`, `MapPluginEndpoints`.
  - MVC Controllers: `CustomControllerBase`, `CrudController<TEntity, TFilter>`, `PluginController`.
- Minimal API handlers return `IResult` via `OperationResponseExtensions.ToHttpResult()`.
- Controllers return `JsonResult` via `OperationResponseExtensions.BuildHttpResponse()`.
- Async equivalents `ToHttpResultAsync()` and `BuildHttpResponseAsync()` chain directly onto `Task<T>`.
- NDJSON streaming: `HttpContext.WriteNdjsonAsync<T>(IAsyncEnumerable<T>, ...)` emits an `X-Stream-Status: true` trailer after consuming the full body. `ApiServiceAgentBase.GetStreamAsync<T>` verifies this trailer.

### 6. Dynamic Plugin System
- Scans `PluginSettings.Directory` for assemblies matching `^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$`.
- Pipeline: `DiscoveredPlugin` → `LoadedPlugin` → `RegisteredPlugin` (`IsFullyLoaded`, `FullyLoadedAt`).
- Two-level cache (`name` + `version`). Keyed persistence (`PluginCacheTargets.File` and `PluginCacheTargets.Database`).
- `PluginOrchestratorBackgroundService` automatically loads, promotes higher versions, and evicts older versions according to retention settings.

---

## Dependency Injection Entry Points

| Package | Class | Method |
|---|---|---|
| Core | `CoreDependencyInjection` | `AddCoreServices(encryptionSettings, resiliencePipelineSettings)` |
| Database | `DatabaseDependencyInjection` | `AddDatabaseServices(databaseSettings, resiliencePipelineSettings)` |
| WebApi | `WebApiDependencyInjection` | `AddWebApiServices(resiliencePipelineSettings)`, `AddJweAuthentication(jwtSettings)`, `AddSwaggerWithBearerSecurity()`, `UseSwaggerDocs()` |
| Plugins | `PluginsDependencyInjection` | `AddPluginServices(pluginSettings)` |
| Mathematics | `MathematicsDependencyInjection` | `AddMathematicsServices()` |
| MechanicsOfMaterials | `DependencyInjection` | `AddMechanicsOfMaterialsServices()` |
| Meta | `DependencyInjection` | `AddToolsServices(databaseSettings, encryptionSettings, resiliencePipelineSettings, pluginSettings)` |

---

## AOT & Trimming Policy

- Projects declare `<IsAotCompatible>true</IsAotCompatible>`.
- Trim/AOT warning noise (`IL2026`, `IL2046`, `IL2050`, `IL2067`, `IL2070`–`IL2095`, `IL2111`, `IL3000`, `IL3050`–`IL3052`) is silenced at the solution level in `Directory.Build.props`.
- Do **not** pollute public API signatures with `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]`.
- Use `[DynamicallyAccessedMembers]` where reflection target preservation has functional runtime requirements.
- Core, Mathematics, and MechanicsOfMaterials are AOT-clean. Database and WebApi use reflection internally. Plugins is fundamentally non-AOT (`AssemblyLoadContext.LoadFromAssemblyPath`).

---

## Versioning & Changelog

- Semantic Versioning (SemVer).
- Follow [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
- All unreleased modifications must be documented in `CHANGELOG.md` under `## [Unreleased]` using sections `### Added`, `### Changed`, `### Fixed`, `### Breaking`, or `### Removed`.

