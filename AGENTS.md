# AGENTS.md

Project notes and developer guidelines for AI agents working on **MelloSilveiraTools** — a .NET 10 suite of NuGet packages split into contextual libraries: core utilities, relational database provider, web-API helpers, dynamic plugin runtime, mathematics, mechanics of materials, and experimental data optimizations.

---

## Solution Layout

```
src/MelloSilveiraTools/                                — Meta-package (aggregates all packages; meta DI entry)
src/MelloSilveiraTools.Core/                           — Extensions, in-memory caches, Polly v8 pipelines, encryption, email, file management, TPL Dataflow & fluent pipelines
src/MelloSilveiraTools.Database/                       — IRepository, PostgresRepository, ISqlProvider, attributes, FilterClauses, Npgsql/Dapper
src/MelloSilveiraTools.WebApi/                         — Controllers (Custom/Crud), minimal endpoints, NDJSON streaming, Swagger, JWE auth, ApiServiceAgent, Commands (Crud)
src/MelloSilveiraTools.Plugins/                        — File-based plugin runtime, two-level cache, dynamic DI, persistence, background orchestrator
src/MelloSilveiraTools.Mathematics/                    — Differential equation solvers (Newmark, Newmark-β), univariate Function hierarchy, expressions, numerical calculus, root-finding, 3D geometry, statistics
src/MelloSilveiraTools.MechanicsOfMaterials/           — Fatigue, constitutive equations, geometric profiles, force/vector models, viscoelastic models
src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ — TPL Dataflow pipelines, experimental data segmentation (Ramp/Relaxation/Descent/Recovery), curve fitting via MathNet.Numerics & ALGLIB
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

Run tests:
```powershell
dotnet test test/UnitTests/UnitTests.csproj
```

---

## Package Dependency Graph

```
Core ←─── Mathematics ←─── MechanicsOfMaterials ←─── MechanicsOfMaterials.Optimizations
  ↑
  └── Database ←── WebApi ←── Plugins

MelloSilveiraTools (meta) → Core, Database, WebApi, Plugins, Mathematics, MechanicsOfMaterials, MechanicsOfMaterials.Optimizations
```

### Notable Constraints
- `WebApi → Database`: `CrudController` and `MapCrud` consume `IRepository`, `EntityBase`, and `FilterBase`.
- `Plugins → WebApi`: `PluginController` extends `ControllerBase`; `PluginEndpoints` uses minimal API routing.
- `MechanicsOfMaterials → Mathematics`: Calculator interfaces use `Vector3D`, `MathExpression`, `PronySeries` from Mathematics.
- `MechanicsOfMaterials.Optimizations → MechanicsOfMaterials`: Builds on model calculators, curve segments, and numerical routines.

---

## Target Frameworks & Dependencies

### Common (Directory.Build.props)
- `TargetFramework`: `net10.0`
- `ImplicitUsings`: `enable`
- `Nullable`: `enable`
- `GeneratePackageOnBuild`: `true`
- `GenerateDocumentationFile`: `true`
- `VersionPrefix`: `1.5.0-rc046`

### Per-Package NuGet Dependencies

| Package | Key Dependencies |
|---|---|
| **Core** | `Polly` 8.7.0, `Polly.RateLimiting` 8.7.0, `Microsoft.Extensions.Logging` 10.0.11, `Microsoft.AspNetCore.Cryptography.KeyDerivation` 10.0.11, `Serilog.Extensions.Logging` 10.0.0, `Serilog.Sinks.File` 7.0.0, `Serilog.Enrichers.Environment` 3.0.1, `Microsoft.AspNet.Mvc` 5.3.0, `FrameworkReference: Microsoft.AspNetCore.App` |
| **Database** | `Dapper` 2.1.79, `Npgsql` 10.0.3, `Serilog.Sinks.Postgresql.Alternative` 4.3.0, ProjectRef → Core |
| **WebApi** | `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.11, `Microsoft.IdentityModel.JsonWebTokens` 8.22.0, `Swashbuckle.AspNetCore` 10.2.3, `Swashbuckle.AspNetCore.Newtonsoft` 10.2.3, ProjectRef → Core, Database |
| **Plugins** | ProjectRef → WebApi (transitively: Core, Database) |
| **Mathematics** | ProjectRef → Core (zero external NuGet deps) |
| **MechanicsOfMaterials** | `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.11, ProjectRef → Mathematics |
| **Optimizations** | `MathNet.Numerics` 5.0.0, ProjectRef → MechanicsOfMaterials, `FrameworkReference: Microsoft.AspNetCore.App` |
| **Meta** | `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.11, ProjectRef → all above |
| **UnitTests** | `xunit` 2.9.3, `Moq` 4.20.72, `Microsoft.NET.Test.Sdk` 18.9.0, `coverlet.collector` 10.0.1, `Npgsql` 10.0.3, ProjectRef → Meta |

### Dependency Policy
- Do NOT add NuGet dependencies without confirming they don't overlap with existing ones.
- Core, Mathematics, and MechanicsOfMaterials are AOT-clean — avoid adding reflection-heavy dependencies.
- Optimizations bundles ALGLIB source files directly (`Algorithms/CurveFitting/Alglib/`), not as a NuGet reference.

---

## Namespaces

Each project owns a namespace that mirrors its package ID:

- `MelloSilveiraTools.Core.*` (e.g. `.Caching`, `.Services.Encryption`, `.Managers.File`, `.Pipelines`, `.Pipelines.Dataflow`, `.Pipelines.Fluent`, `.Providers.Dynamics`)
- `MelloSilveiraTools.Database.*` (e.g. `.RelationalDatabase.Sql.Provider`, `.RelationalDatabase.Repositories`, `.RelationalDatabase.Attributes`)
- `MelloSilveiraTools.WebApi.*` (e.g. `.Authentication`, `.Application.Commands.Crud.*`, `.Application.Controllers`, `.Application.Endpoints`)
- `MelloSilveiraTools.Plugins.*` (e.g. `.Infrastructure`, `.Infrastructure.Models`, `.Infrastructure.Persistences`, `.Infrastructure.Services`)
- `MelloSilveiraTools.Mathematics.*` (e.g. `.Functions`, `.Expressions`, `.NumericalMethods.DifferentialEquation`, `.Models`)
- `MelloSilveiraTools.MechanicsOfMaterials.*` (e.g. `.Calculators.MechanicalModels`, `.Models.MechanicalModels`)
- `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.*` (e.g. `.Services.ExperimentalData`, `.Algorithms.CurveFitting`)
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
  - `extension(Type)` blocks for extension methods (C# 13 extension members).
  - Collection expressions `[...]` instead of `new[] { ... }` or `new List<T>()`.
  - `System.Threading.Lock` over `object` monitor locks (`csharp_prefer_system_threading_lock = true`).
  - Pattern matching with switch expressions and extended property patterns.
  - Slices and spans (`span[..index]`, `AsSpan()`).
  - `readonly record struct` for value-type immutable DTOs.
  - `ref struct` where stack-only allocation semantics are needed (`SpanStringBuilder`).

### 4. Logging
- Use standard **`Microsoft.Extensions.Logging.ILogger<T>`** across all packages.
- Telemetry is backed by Serilog for structured JSON file logging (rolling file sink) and PostgreSQL sink (`Serilog.Sinks.Postgresql.Alternative`).

### 5. Settings & DI
- Settings classes are `record`s with safe defaults.
- Registered via `services.AddSingleton(settings ?? new TSettings())` to preserve fluent chaining and consumer overrides.

### 6. Public Documentation
- Comprehensive XML documentation comments are **mandatory** on all public types and members.
- Must include `<summary>`, `<param>`, `<returns>`, and where applicable `<exception>`, `<remarks>`, and `<example>`.

---

## Dependency Injection Entry Points

| Package | Class | Method |
|---|---|---|
| Core | `CoreDependencyInjection` | `AddCoreServices(encryptionSettings, smtpResiliencePipelineSettings, emailSettings, loggerSettings, useDefaultLogger)` |
| Database | `DatabaseDependencyInjection` | `AddDatabaseServices(databaseSettings, resiliencePipelineSettings, loggerSettings)` |
| WebApi | `WebApiDependencyInjection` | `AddWebApiServices(resiliencePipelineSettings)`, `AddJweAuthentication(jwtSettings)`, `AddSwaggerWithBearerSecurity()`, `UseSwaggerDocs()`, `UseCustomMiddlewares()` |
| Plugins | `PluginsDependencyInjection` | `AddPluginServices(pluginSettings)` |
| Mathematics | `MathematicsDependencyInjection` | `AddMathematicsServices()` |
| MechanicsOfMaterials | `DependencyInjection` | `AddMechanicsOfMaterialsServices(addMechanicalModels)` |
| Meta | `DependencyInjection` | `AddToolsServices(databaseSettings, encryptionSettings, resiliencePipelineSettings, pluginSettings, emailSettings, loggerSettings, useDefaultLogger, addMechanicalModels)` |

### DI Registration Summary by Lifetime

**Singletons (stateless/thread-safe):**
- Core: `IFileManager`, `ISingleLevelCache`, `ITwoLevelCache`, `ServiceLocator`, `IDynamicServiceProvider`, `SmtpResiliencePipeline`, `EncryptionSettings`
- Database: `DatabaseSettings`, `ResiliencePipelineSettings`, `PostgresResiliencePipeline`, `ISqlProvider` → `PostgresSqlProvider`, `IRepository` → `PostgresRepository`
- WebApi: `ApiServiceAgentResiliencePipeline`, `JwtSettings`
- Plugins: `PluginSettings`, `PluginFileProcessor`, `PluginAssemblyProcessor`, `PluginCache`, `PluginValidator` (all variants); keyed: `JsonFilePluginCachePersistence` (key: `"file"`), `DatabasePluginCachePersistence` (key: `"database"`)
- Mathematics: `IDifferentiation`, `IIntegration`, `FunctionFactory`, `IStatisticsCalculator`; keyed: `IDifferentialEquationMethod` (Newmark/NewmarkBeta), `IRootFinding` (Bisection/Brent/StepByStep)
- MechanicsOfMaterials: all model calculators (Elastic, Maxwell, Fung, SimplifiedFung, Schapery, ModifiedSuperposition), `IConstitutiveEquationsCalculator`, `IFatigueCalculator`, `IGeometricPropertyCalculator<>`, `IMechanicalModelTypeCache`

**Scoped:**
- Core: `IEncryptionService`, `IEmailService`
- WebApi: `IAuthenticationTokenService`, CRUD commands (`AddEntity<>`, `ReadEntityById<>`, `ReadEntityPaged<,>`, `UpdateEntity<>`, `DeleteEntity<>`)
- Plugins: `IPluginService`, `IPluginCachePersistence` (resolved via HTTP route `{target}`), all plugin commands

**Hosted:**
- Plugins: `PluginOrchestratorBackgroundService`

---

## Domain Architecture & Patterns

### 1. Result Monad (`MelloSilveiraTools.Core.Models`)
- `ResultBase` → `Result` → `Result<T>` → `ListedResult<T>` → `PagedResult<T>`.
- Factory methods: `Result.CreateSuccessOk()`, `Result.CreateBadRequest(msg)`, `Result.CreateNotFound(msg)`, etc.
- Fluent validation: `.AddErrorIf()`, `.AddErrorIfNull()`, `.AddErrorIfNegativeOrZero()`, etc.
- Monadic branching: `.OnSuccess()`, `.OnError()`, `.Match()`.
- HTTP projection: `.ToHttpResult()` → `IResult` (minimal APIs), `.BuildHttpResponse()` → `JsonResult` (MVC).
- Async equivalents: `.ToHttpResultAsync()`, `.BuildHttpResponseAsync()`.

### 2. Pipeline Architecture (`MelloSilveiraTools.Core.Pipelines`)
**Step contracts:**
- **`IPipelineStep`**: Base metadata contract defining `string Name { get; }` for telemetry, distributed tracing, and fault localization.
- **`IAsyncPipelineStep<in TIn, TOut>`**: Asynchronous step contract (`Task<TOut> ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
- **`ISyncPipelineStep<in TIn, out TOut>`**: Synchronous step contract (`TOut Execute(TIn input)`) implementing `IDisposable` (does not inherit from `IAsyncDisposable`).
- **`IAsyncEnumerablePipelineStep<in TIn, out TOut>`**: Streaming step contract (`IAsyncEnumerable<TOut> ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.

**Two pipeline flavors:**
- **TPL Dataflow (streaming/push):** `PipelineFactory.StartDataflow<T>()` → `IDataflowPipelineBuilder` → `IDataflowPipeline<T>`.
  - Supports: `AddStep` (sync, async, and `IAsyncEnumerable`), `AddDataMapping`, `AddForkingStep`, `AddBatchStep`, `AddFilterStep`, `AddGroupWhileStep`, `AddBroadcastBlock`, `AddBroadcastStep`, `BuildTerminal`.
  - Dead-Letter Queue (DLQ): `WithDeadLetterQueue()` isolates faulted items via `ActionBlock<FailedPayload>`.
  - Backpressure: `PipelineStepOptions.MaxBufferSize` → `BoundedCapacity`.
  - Telemetry: OpenTelemetry `ActivitySource` tracing + structured Serilog logging via `TelemetryExtensions`.
  - Retry: `RetryOptions(MaxAttempts, InitialDelayMs, BackoffFactor)` with exponential backoff.
- **Fluent (request/response/pull):** `PipelineFactory.StartFluent<T>()` → `IFluentPipelineBuilder` → `IFluentPipeline<TIn, TOut>.ExecuteAsync()`.
  - Supports: `AddStep` (sync, async, and `IAsyncEnumerable`), `AddDataMapping`.
  - Sequential step chain; type-erased internal delegates.

### 3. Command Pattern (`MelloSilveiraTools.Core.Application.Commands`)
- `CommandBase<TRequest, TResult>` with optional `IValidator<TRequest>`.
- Hierarchy: `CommandBaseWithData`, `ListedCommandBase`, `PagedCommandBase`, `CommandBaseWithDefaultResponse`, `CommandBaseWithoutRequest`, `DefaultCommandBase`.
- All commands execute via `ExecuteAsync(request)` → validates → calls `ExecuteCommandAsync`.

### 4. Result vs. Output (Calculation Engine)
- Mathematical and mechanical calculators emit raw projection data classes named **`*Output`** (e.g. `MechanicalModelOutput`, `ElasticModelOutput`, `FatigueOutput`), never `*Result`.
- Calculator blocks are decoupled from application/API status monads (`Result<T>` / `OperationResponse`).

### 5. Constitutive Modeling (Mechanics of Materials)
- Physical material parameters inherit from the **`ConstitutiveParameters`** abstract base record.
- Concrete types: `ElasticConstitutiveParameters`, `MaxwellConstitutiveParameters`, `SchaperyConstitutiveParameters`, `ModifiedSuperpositionMethodConstitutiveParameters`, `FungConstitutiveParameters`, `SimplifiedFungConstitutiveParameters`.
- Quasi-linear models use `QuasiLinearConstitutiveParameters<TReducedRelaxationFunction>` intermediate base.
- Calculator execution uses the strongly-typed wrapper `MechanicalModelInput<TConstitutiveParameters>`.
- Calculator interface hierarchy:
  ```
  IMechanicalModelCalculator<T> ← IViscoelasticModelCalculator<T>
      ← IQuasiLinearModelCalculator<T, TRelax> ← IFungModelCalculator, ISimplifiedFungModelCalculator
      ← IMaxwellModelCalculator
      ← ISchaperyModelCalculator, IModifiedSuperpositionMethodCalculator
  ```
- `IMechanicalModelCalculatorFacade`: Resolves calculators by `ConstitutiveParameters` type at runtime via `ServiceLocator` + `IMechanicalModelTypeCache`.

### 6. Experimental Data & Optimizations
- Located in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
- Multi-modal pipeline steps:
  - `ExperimentalDataSegmenterStep`: Ingestion step implementing `IAsyncEnumerablePipelineStep<(Stream StrainStream, Stream StressStream), SegmentedDataPoint>`, parsing CSV streams and categorizing points across deformation phases via sliding-window numerical differentiation.
  - `ExperimentalDataFileWriterStep`: Persistence step implementing `IAsyncPipelineStep<SegmentedDataPoint, SegmentedDataPoint>`, streaming valid points to disk via CSV format.
  - `CurveSegmentBuilderStep`: Assembly step implementing `ISyncPipelineStep<SegmentedDataPoint[], CurveSegment>`, constructing segments from grouped arrays with configurable downsampling (`skipTimeStep`).
- `IExperimentalDataService.ProcessAsync(identifier, outputFileUri, strainStream, stressStream, options)` → `Result<(string OutputFileName, CurveSegment[] CurveSegments)>`.
  - Continuous stream topology via TPL Dataflow:
    - Ingestion: `ExperimentalDataSegmenterStep` converts raw stream pair into streaming `SegmentedDataPoint` sequence.
    - Branch 1 (Broadcast): `ExperimentalDataFileWriterStep` writes points to CSV via `AddBroadcastStep`.
    - Branch 2 (Aggregation): Adjacent points grouped via `.AddGroupWhileStep((prev, curr) => prev.SegmentType == curr.SegmentType)` and mapped to `CurveSegment` via `CurveSegmentBuilderStep`.
    - Terminal: Segments collected into result array.
  - Configured via `ExperimentalDataSettings` (`FileWriterOptions`, `GroupingOptions`, `SegmentBuilderOptions`, and `SegmenterOptions` with `PipelineStepOptions`).
  - Buffer pooling using `ArrayPool<ExperimentalDataPoint>.Shared` with safe return in `finally`.
  - Flushes remainder buffer on stream completion to prevent dropping trailing points.
  - Supports 3-phase interior transitions in `SliceBuffer` (`startIndex > 0 && endIndex < bufferCount - 1`).
- `IExperimentalDataService.SegmentPointsAsync()` → `IAsyncEnumerable<SegmentedDataPoint>` (delegates to `ExperimentalDataSegmenterStep.ExecuteAsync`).
- `IExperimentalDataService.ExtractSegments()` → sliding-window segment classification (delegates to `ExperimentalDataSegmenterStep.ExtractSegments`).
- Segment types: `Ramp`, `Relaxation`, `Descent`, `Recovery`.
- `ICurveFitter` interface with `MathNetCurveFitter` (Levenberg-Marquardt via MathNet.Numerics) and `AlglibCurveFitter` (bundled ALGLIB).
- `QuasiLinearModelCurveFitter` for specialized mechanical model curve fitting.
- Morris sensitivity analysis: `MorrisAnalyzer`, `MorrisInput`, `MorrisOutput`, `MorrisMetrics`.

### 7. Database & SQL Generation
- `PostgresSqlProvider` caches generated SQL in static `ConcurrentDictionary<(Type, Operation, int), string>`.
- Entity metadata discovery via `[Table]`, `[PrimaryKeyColumn]`, `[Column]`, `[UniqueColumn]`, `[ForeignKeyColumn]` attributes.
- SQL template placeholders (`#TABLE_NAME`, `#COLUMNS`, etc.) substituted statically; runtime placeholders (`#WHERE`, `#ORDERBY`, `#LIMIT`, `#OFFSET`) substituted dynamically by `PostgresRepository`.
- Supports single and multi-row batch insert with `ON CONFLICT (unique_columns) DO UPDATE SET ...`.
- `IRepository.TryInsertAsync<TEntity>`: Insert-or-get-existing single statement CTE returning `Result<long>`.
- `IRepository.GetByUniqueColumnAsync<TEntity>`: Typed lookup via single `[UniqueColumn]` property.
- `IRepository` is registered Singleton — completely stateless (opens its own connection per operation via `await using`).
- Filter system: `[Filter(typeof(TEntity))]` on filter class, `[FilterColumn(filterClause, propertyName, tableName)]` on properties. `BuildWhereClauseAndParameters` auto-generates parameterized WHERE clauses.

### 8. HTTP & Streaming Endpoints
- WebApi provides dual surfaces:
  - Minimal APIs: `MapCrud<TEntity, TFilter>`, `MapPluginEndpoints`.
  - MVC Controllers: `CustomControllerBase`, `CrudController<TEntity, TFilter>`, `PluginController`.
- NDJSON streaming: `HttpContext.WriteNdjsonAsync<T>(IAsyncEnumerable<T>, ...)` emits an `X-Stream-Status: true` trailer after consuming the full body. `ApiServiceAgentBase.GetStreamAsync<T>` verifies this trailer.
- `ExceptionHandlingHttpMiddleware`: Global error handler mapping exception types to HTTP status codes. Special-cases `NdjsonException` to avoid corrupting partial streams.
- Content type: `application/x-ndjson` with `X-Content-Type-Options: nosniff`.

### 9. JWE Authentication
- JWE (JSON Web Encryption) over JWS using `JsonWebTokenHandler`.
- Signing: `HmacSha256`. Encryption: `Aes256KW` key wrap + `Aes256CbcHmacSha512` content encryption.
- `IAuthenticationTokenService`: `Generate(long/string)`, `RefreshAsync(token)`, `IsValidAsync(token)`.
- `AddJweAuthentication(JwtSettings)` configures `JwtBearerDefaults` scheme.

### 10. Dynamic Plugin System
- Scans `PluginSettings.Directory` for assemblies matching `^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$`.
- Pipeline: `DiscoveredPlugin` → `LoadedPlugin` → `RegisteredPlugin` (`IsFullyLoaded`, `FullyLoadedAt`).
- Each plugin loads in isolated collectible `AssemblyLoadContext` for memory reclaimability.
- Two-level cache (`name` + `version`). Keyed persistence (`PluginCacheTargets.File` and `PluginCacheTargets.Database`).
- `PluginOrchestratorBackgroundService`: `PeriodicTimer`-based polling loop that auto-discovers, loads, promotes higher versions, and evicts older versions per `PreviousVersionRetention` (default: 24h).
- `PluginRegistrationContext`: `ForStartup(IServiceCollection)` mutates static DI; `ForRuntime(IDynamicServiceProvider)` enables hot-swap.

---

## Thread-Safety & Concurrency

### Static Caches (ConcurrentDictionary)
- `PostgresSqlProvider._metadataCache`: `ConcurrentDictionary<Type, EntityMetadata>` — deterministic, never evicted.
- `PostgresSqlProvider._sqlCache`: `ConcurrentDictionary<(Type, Operation, int), string>` — keyed on batch size.
- `ClassExtensions._columnMetaCache`, `_filterColumnMetaCache`, `_filterAttributeCache`: reflection metadata caches.
- `DictionaryExtensions._typeCache`: compiled `Action<object, object>` property setters for `IDataReader.ConvertTo<T>()`.
- `TypeExtensions._hierarchyCache`, `_hierarchyAttrCache`, `_propertyNamesCache`: reflection hierarchy caches.

### Connection Management
- `PostgresRepository`: Stateless singleton. Every operation opens its own `NpgsqlConnection` via `await using`. Safe for concurrent invocations.

### Memory Allocation
- `SpanStringBuilder`: `ref struct` using `ArrayPool<char>.Shared`. Stack-only; cannot cross `await` boundaries.
- `CsvStreamReader`: `PipeReader` + `MemoryPool<byte>.Shared`, `stackalloc` for lines ≤ 512 bytes, `ArrayPool<byte/double>.Shared` for larger buffers.
- `ExperimentalDataService`: `ArrayPool<ExperimentalDataPoint>.Shared` for sliding-window segmentation, returned in `finally`.

### Resilience
- `DefaultResiliencePipeline`: Uses `ResilienceContextPool.Shared.Get()/Return()` — zero per-call allocation.
- Polly v8 retry with exponential backoff, jitter, and caller context injection via `[CallerMemberName]` / `[CallerFilePath]`.

### Concurrency Primitives
- `SemaphoreSlim`: Used in `EnumerableExtensions.ForeachAsync` for bounded-parallel iteration.
- `Lazy<object>`: Used in `InMemoryDynamicServiceProvider` for thread-safe single-instantiation of plugin services.
- `ConcurrentDictionary` + `Lazy<T>`: Standard double-checked locking pattern throughout.

---

## AOT & Trimming Policy

- Projects declare `<IsAotCompatible>true</IsAotCompatible>`.
- Trim/AOT warning noise (`IL2026`, `IL2046`, `IL2050`, `IL2067`, `IL2070`–`IL2095`, `IL2111`, `IL3000`, `IL3050`–`IL3052`) is silenced at the solution level in `Directory.Build.props`.
- Do **not** pollute public API signatures with `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]`.
- Use `[DynamicallyAccessedMembers]` where reflection target preservation has functional runtime requirements.
- Core, Mathematics, and MechanicsOfMaterials are AOT-clean. Database and WebApi use reflection internally. Plugins is fundamentally non-AOT (`AssemblyLoadContext.LoadFromAssemblyPath`).

---

## Strict Constraints & Rules

### Breaking Changes
- This is a published NuGet library. Do NOT rename, remove, or change signatures of existing `public` members without explicit approval.
- Adding new `public` members is acceptable. Changing `internal` or `private` members is acceptable.
- Do not change DI lifetimes (Singleton → Scoped, etc.) without full impact analysis.

### Thread-Safety Requirements
- All services registered as **Singleton** MUST be thread-safe. This includes all calculators, caches, repositories, SQL providers, resilience pipelines, and file manager.
- `PostgresRepository` achieves thread-safety via per-call `NpgsqlConnection` — do NOT add instance-level mutable state.
- `PostgresSqlProvider` caches are static `ConcurrentDictionary` with deterministic factories — do NOT change to non-concurrent collections.

### SQL Generation
- SQL template placeholders are split into two stages: compile-time static substitution and runtime dynamic substitution. Do NOT mix them.
- Batch parameter suffixes are **1-based** (`@Name_1`, `@Name_2`), NOT 0-based. This is validated by unit tests.
- `BuildUniqueUpdates` updates non-PK, non-unique payload columns; falls back to unique column update if no payload columns exist.

### Naming Conventions
- Entity classes: PascalCase record inheriting `EntityBase`.
- Table names: `snake_case` via `[Table("table_name")]`.
- Column mapping: Property `Name` → column `name` via `ToSnakeCase()`.
- Calculator outputs: `*Output` suffix (never `*Result`).
- Constitutive parameter records: `*ConstitutiveParameters` suffix inheriting `ConstitutiveParameters`.

### Test Coverage
- Tests use xUnit 2.9.3 with `Moq` 4.20.72.
- Test patterns: AAA, `[Fact]`, `[Theory]` with `[InlineData]` and `[MemberData]`.
- Fake `IDataReader` via `FakeDataReader` for DB-free `ConvertTo<T>()` testing.
- SQL generation tests verify exact string content AND reference identity (`Assert.Same`) for cache validation.

---

## Versioning & Changelog

- Semantic Versioning (SemVer).
- Follow [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
- All unreleased modifications must be documented in `CHANGELOG.md` under `## [Unreleased]` using sections `### Added`, `### Changed`, `### Fixed`, `### Breaking`, or `### Removed`.

