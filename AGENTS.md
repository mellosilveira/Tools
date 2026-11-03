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
src/MelloSilveiraTools.Mathematics/                    — Differential equation solvers (Newmark, Newmark-β), Function hierarchy, expressions, numerical calculus, root-finding, 3D geometry, statistics
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
- `VersionPrefix`: `2.0.0`

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

- `MelloSilveiraTools.Core.*` (e.g. `.Caching`, `.Services.Encryption`, `.Managers.File`, `.Pipelines`, `.Pipelines.Dataflow`, `.Pipelines.Fluent`, `.Pipelines.Notifiers`, `.Pipelines.Telemetry`, `.Providers.Dynamics`)
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
- **Properties, indexers, accessors, lambdas, and single-expression methods** use expression bodies (`=>`).
- **Multi-statement methods and constructors** use standard block bodies `{ ... }`.

### 3. Layout, Formatting & Syntax
- **Indentation:** 4 spaces.
- **Line endings:** CRLF (`end_of_line = crlf`).
- **Braces:** Single-line `if` statements without braces are allowed and preferred; multi-line blocks require braces.
- **Parameter Ordering:** 1. `ILogger` / `ILoggerFactory`, 2. interfaces (with `IPipelineProgressNotifier` first among interfaces), 3. concrete classes and primitives (with `TimeProvider` preceding other instances), ..., last: `CancellationToken`.
- **Temporal Operations:** Mandatory use of `TimeProvider` (non-optional, non-nullable). Ambient `DateTimeOffset.UtcNow` or `DateTime.UtcNow` is forbidden in production code to guarantee testability.
- **Namespaces:** File-scoped or block-scoped declarations consistent with surrounding files.
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
- Settings classes are `record`s (or classes) whose mandatory properties are marked `required`. Settings classes must never assume default values (`= default;`); all values must be explicitly provided by the consumer.
- Optional settings properties are declared nullable (`?`) without default initialization.
- Registered via `services.AddSingleton(settings)` when explicitly configured by the consumer.
- **Eager Validation:** Settings with platform constraints or critical prerequisites (e.g. `DfsStorageSettings` in `CoreDependencyInjection`) must be validated eagerly during DI container registration so misconfigurations fail fast during host startup.

### 6. Public Documentation
- Comprehensive XML documentation comments are **mandatory** on all public types and members.
- Must include `<summary>`, `<param>`, `<returns>`, and where applicable `<exception>`, `<remarks>`, and `<example>`.

---

## Dependency Injection Entry Points

| Package | Class | Method |
|---|---|---|
| Core | `CoreDependencyInjection` | `AddCoreServices(defaultResiliencePipelineSettings, encryptionSettings, emailSettings, fileStorageSettings, loggerSettings, enablePipelineProgressNotifications)` |
| Database | `DatabaseDependencyInjection` | `AddDatabaseServices(defaultResiliencePipelineSettings, databaseSettings, loggerSettings)` |
| WebApi | `WebApiDependencyInjection` | `AddWebApiServices(resiliencePipelineSettings)`, `AddJweAuthentication(jwtSettings)`, `AddSwaggerWithBearerSecurity()`, `UseSwaggerDocs()`, `UseCustomMiddlewares()` |
| Plugins | `PluginsDependencyInjection` | `AddPluginServices(pluginSettings)` |
| Mathematics | `MathematicsDependencyInjection` | `AddMathematicsServices()` |
| MechanicsOfMaterials | `DependencyInjection` | `AddMechanicsOfMaterialsServices(addMechanicalModels)` |
| Meta | `DependencyInjection` | `AddToolsServices(defaultResiliencePipelineSettings, databaseSettings, encryptionSettings, pluginSettings, emailSettings, fileStorageSettings, experimentalDataSettings, loggerSettings, addMechanicalModels, enablePipelineProgressNotifications)` |

### DI Registration Summary by Lifetime

**Singletons (stateless/thread-safe):**
- Core: `TimeProvider` (`TimeProvider.System`), `IFileStorageService` (and keyed by `StorageProvider.Local`), `ISingleLevelCache`, `ITwoLevelCache`, `ServiceLocator`, `IDynamicServiceProvider`, `SmtpResiliencePipeline`, `EncryptionSettings`, `IPipelineProgressNotifier` → `NullPipelineProgressNotifier.Instance` (when `enablePipelineProgressNotifications: false`)
- Database: `DatabaseSettings`, `ResiliencePipelineSettings`, `PostgresResiliencePipeline`, `ISqlProvider` → `PostgresSqlProvider`, `IRepository` → `PostgresRepository`
- WebApi: `ApiServiceAgentResiliencePipeline`, `JwtSettings`
- Plugins: `PluginSettings`, `PluginFileProcessor`, `PluginAssemblyProcessor`, `PluginCache`, `PluginValidator` (all variants); keyed: `JsonFilePluginCachePersistence` (key: `"file"`), `DatabasePluginCachePersistence` (key: `"database"`)
- Mathematics: `IDifferentiation`, `IIntegration`, `FunctionFactory`, `IStatisticsCalculator`; keyed: `IDifferentialEquationMethod` (Newmark/NewmarkBeta), `IRootFinding` (Bisection/Brent/StepByStep)
- MechanicsOfMaterials: all model calculators (Elastic, Maxwell, Fung, SimplifiedFung, Schapery, ModifiedSuperposition), `IConstitutiveEquationsCalculator`, `IFatigueCalculator`, `IGeometricPropertyCalculator<>`, `IMechanicalModelTypeCache`

**Scoped:**
- Core: `IEncryptionService`, `IEmailService`, `IPipelineProgressNotifier` → `ChannelPipelineProgressNotifier` (when `enablePipelineProgressNotifications: true`)
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
- **`IAsyncPipelineStep<in TIn>`**: Consumer/terminal asynchronous step contract (`Task ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
- **`IAsyncPipelineStep<in TIn, TOut>`**: Asynchronous step contract (`Task<TOut> ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
- **`ISyncPipelineStep<in TIn, out TOut>`**: Synchronous step contract (`TOut Execute(TIn input)`) implementing `IDisposable` (does not inherit from `IAsyncDisposable`).
- **`IAsyncEnumerablePipelineStep<in TIn, out TOut>`**: Streaming step contract (`IAsyncEnumerable<TOut> ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
- **`IEnumerablePipelineStep<in TIn, out TOut>`**: Synchronous enumerable collection step contract (`IEnumerable<TOut> Execute(TIn input, CancellationToken ct)`) implementing `IDisposable`, designed for 1-to-many fan-out transformations via TPL Dataflow `TransformManyBlock`.

**Two pipeline flavors:**
- **TPL Dataflow (streaming/push):** `PipelineFactory.StartDataflow<T>(logger, progressNotifier, ...)` → `IDataflowPipelineBuilder` → `IDataflowPipeline<T>`.
  - Supports: `AddStep` (sync, async, `IAsyncEnumerable`, and `IEnumerable`), `AddDataMapping`, `AddForkingStep`, `AddFilterStep`, `AddGroupWhileStep`, `AddCollectAllStep`, `Fork` (2-way and 3-way, accepting branch delegates or step instances directly), `AddBroadcastStep` (side-effect observer and mapped transform observer), `AddBroadcastFork` (concurrent branch fork observer), and `BuildTerminal`.
  - Completion propagation: `DataflowExtensions.LinkTo` C# 13 extension members override TPL Dataflow defaults by setting `PropagateCompletion = true` on block linkages, ensuring graceful graph draining upon `pipeline.Complete()`.
  - Resource cleanup: `DataflowPipeline<TIn>.DisposeAsync` encloses step disposal in a `try ... finally` block, ensuring all `IDisposable` and `IAsyncDisposable` step resources are reclaimed even if graph completion faults.
  - Dead-Letter Queue (DLQ): `WithDeadLetterQueue()` isolates faulted items via `ActionBlock<FailedPayload>`.
  - Backpressure: `PipelineStepOptions.MaxBufferSize` → `BoundedCapacity`.
  - Telemetry & Real-Time Progress:
    - OpenTelemetry `ActivitySource` tracing + structured Serilog logging via `PipelineStepExecutor`.
    - `PipelineProgressEvent`: Immutable record in `MelloSilveiraTools.Core.Pipelines.Models` representing granular step execution progress (`StepName: string`, `Description: string`, `State: PipelineStepState`, `Timestamp: DateTimeOffset`, `UpstreamSteps: IReadOnlyList<string>`, `ReceivedItems: int`, `ProcessedItems: int`, `ProgressPercentage: double`, `Details: string?`).
    - `PipelineStepState`: Enum covering execution states (`Pending`, `Started`, `Running`, `Completed`, `Faulted`).
    - `IPipelineProgressNotifier` (in `MelloSilveiraTools.Core.Pipelines.Notifiers`): Mandatory, non-nullable dependency in `PipelineFactory.StartDataflow<T>(ILogger logger, IPipelineProgressNotifier progressNotifier, ...)`, resolved via DI (`ChannelPipelineProgressNotifier` for bounded async NDJSON/SSE streaming or `NullPipelineProgressNotifier.Instance` for zero-allocation no-op execution).
    - `PipelineProgressNotificationSettings` (in `MelloSilveiraTools.Core.Pipelines.Telemetry`): Externalized configuration settings defining `ImmediateNotificationItemLimit` and `HighVolumeNotificationInterval` thresholds for adaptive progress notification throttling, configurable via DI and optionally overridden in `PipelineFactory.StartDataflow`.
    - `PipelineFlowNode` & `PipelineStepTracker` (in `MelloSilveiraTools.Core.Pipelines.Telemetry`): Emit upfront `Pending` topology events on first `SendAsync`, propagate DAG dependencies (`UpstreamSteps`) and item arrival signals (`ReceivedItems` vs. `ProcessedItems`) across filters, batches, broadcasts, and forks (`JoinBlock`), and emit `Started`/`Completed` once per step lifecycle (`block.Completion`).
- **Fluent (request/response/pull):** `PipelineFactory.StartFluent<T>()` → `IFluentPipelineBuilder` → `IFluentPipeline<TIn, TOut>.ExecuteAsync()`.
  - Supports: `AddStep` (sync, async, and `IAsyncEnumerable`), `AddDataMapping`.
  - Sequential step chain; type-erased internal delegates.

### 3. Command Pattern (`MelloSilveiraTools.Core.Application.Commands`)
- `CommandBase<TRequest, TResult>` with optional `IValidator<TRequest>`.
- Hierarchy: `CommandBaseWithData`, `ListedCommandBase`, `PagedCommandBase`, `CommandBaseWithDefaultResponse`, `CommandBaseWithoutRequest`, `DefaultCommandBase`.
- All commands execute via `ExecuteAsync(request, cancellationToken)` → validates → calls `ExecuteCommandAsync(request, cancellationToken)`.
- **Request DTOs Rule:** Request classes (`*Request`) must never contain a `CancellationToken` property. Cooperative cancellation is passed explicitly to the command execution method (`command.ExecuteAsync(request, cancellationToken)`).

### 4. Result vs. Output (Calculation Engine)
- Mathematical and mechanical calculators emit raw projection data classes named **`*Output`** (e.g. `MechanicalModelOutput`, `ElasticModelOutput`, `FatigueOutput`), never `*Result`.
- Calculator blocks are decoupled from application/API status monads (`Result<T>` / `OperationResponse`).

### 5. Constitutive Modeling (Mechanics of Materials)
- Physical material parameters inherit from the **`ConstitutiveParameters`** abstract base record.
- Concrete types: `ElasticConstitutiveParameters`, `MaxwellConstitutiveParameters`, `SchaperyConstitutiveParameters`, `ModifiedSuperpositionMethodConstitutiveParameters`, `FungConstitutiveParameters`, `SimplifiedFungConstitutiveParameters`.
- Calculator execution uses the strongly-typed wrapper `MechanicalModelInput<TConstitutiveParameters>`.
- Calculator interface hierarchy:
  ```
  IMechanicalModelCalculator<T> ← IViscoelasticModelCalculator<T>
      ← IQuasiLinearModelCalculator<T, TRelax> ← IFungModelCalculator, ISimplifiedFungModelCalculator
      ← IMaxwellModelCalculator
      ← ISchaperyModelCalculator, IModifiedSuperpositionMethodCalculator
  ```
- **Type Resolvers & Factory:** The `IMechanicalModelCalculatorFactory` resolves the correct `ConstitutiveParameters`, `MechanicalModelOutput`, and `IMechanicalModelCalculator` types via `IMechanicalModelTypeResolver` keyed singletons (found in `TypeResolvers/` directory).
- `IMechanicalModelCalculatorFacade`: Uses `ServiceLocator` + `IMechanicalModelTypeCache` to wrap the dynamic reflection logic required for time-history numeric simulations, offering pre-compiled execution paths for high performance.

### 6. Experimental Data & Optimizations
- Located in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
- Multi-modal pipeline steps:
  - `ExperimentalDataSegmenterStep`: Ingestion step implementing `IAsyncEnumerablePipelineStep<ExperimentalDataSegmenterInput, SegmentedDataPoint>`, parsing CSV streams and categorizing points across deformation phases via sliding-window numerical differentiation.
  - `ExperimentalDataFileWriterStep`: Non-mutating persistence observer step implementing `IAsyncPipelineStep<SegmentedDataPoint>`, streaming valid points to disk via CSV format using `CsvFileWriter` and exposing `FileMetadata` (path resolved via `IFileStorageService`).
  - `ExperimentalDataDownsamplerStep`: Streaming downsampling step implementing `IAsyncEnumerablePipelineStep<SegmentedDataPoint, SegmentedDataPoint>`, filtering data points according to `skipTimeStep`.
  - `CurveSegmentAccumulatorStep`: Aggregation step implementing `IAsyncEnumerablePipelineStep<SegmentedDataPoint, CurveSegment>`, grouping points sequentially by `SegmentType` for continuous processing.
  - `IMechanicalModelCurveFitterStep`: Optimization step implementing `IAsyncEnumerablePipelineStep<CurveSegment, MechanicalModelCurveFitOutput>`. It yields fitted constitutive parameters and their analysis metrics sequentially as an async stream for real-time processing. Resolved via `IMechanicalModelStepFactory`.
  - `IdentifierBuilderStep`: Deterministic identification step implementing `ISyncPipelineStep<MechanicalModelCurveFitOutput, string>`, generating a SHA-256 hex hash from model metadata, range, and polymorphic constitutive parameters.
  - `CurveFitOutputPersistenceStep`: Persistence observer step implementing `IAsyncPipelineStep<(string Identifier, MechanicalModelCurveFitOutput CurveFitOutput)>`, writing fit parameters to the database via `TryInsertAsync` using polymorphic `.GetType()` serialization and `OptimizationJsonOptions.SignificantFigures7`.
  - `NumericalSimulationStep`: Simulation step implementing `ISyncPipelineStep<MechanicalModelCurveFitOutput, MechanicalModelSimulationPayload>`, executing forward numeric simulation using `IMechanicalModelCalculatorFactory`.
  - `AsymptoteMonitoringStep`: Analysis step implementing `ISyncPipelineStep<MechanicalModelSimulationPayload, double?>`, detecting steady-state relaxation asymptotes.
  - `DeltaCalculatorStep`: Analysis step implementing `ISyncPipelineStep<MechanicalModelSimulationPayload, SimulationDeltaOutput>`, computing initial, final, absolute, and percentage differences.
  - `MechanicalModelOutputFileWriterStep`: Output writer step implementing `IAsyncPipelineStep<MechanicalModelSimulationPayload, FileMetadata>`, streaming simulation time-histories to CSV using `CsvFileWriter` and returning `FileMetadata` (path resolved via `IFileStorageService`).
  - `MechanicalModelOutputPersistenceStep`: Convergence persistence step implementing `IAsyncPipelineStep<MechanicalModelOutputPersistenceInput, string>`, computing a deterministic simulation hash and persisting `MechanicalModelSimulationEntity`.
- `IExperimentalDataProcessingPipeline.ProcessAsync(input, cancellationToken)` → `Result<string>`:
  - Reactive DAG stream topology via TPL Dataflow:
    - Ingestion: `ExperimentalDataSegmenterStep` converts raw stream pair into streaming `SegmentedDataPoint` sequence.
    - Broadcast Fork: `ExperimentalDataFileWriterStep` and `StoredFilePersistenceStep` write raw points to disk and database via `.AddBroadcastFork(fileBranch => ...)`.
    - Downsampling & Accumulation: `ExperimentalDataDownsamplerStep` downsamples points sequentially, followed by `CurveSegmentAccumulatorStep`.
    - Fitting: `IMechanicalModelCurveFitterStep` yields `MechanicalModelCurveFitOutput`.
    - 3-Way Fork:
      - Branch 1: Pass-through preserving `MechanicalModelCurveFitOutput`.
      - Branch 2: `IdentifierBuilderStep` producing the deterministic SHA-256 fit identifier string.
      - Branch 3: `NumericalSimulationStep` followed by an inner 3-way fork (`AsymptoteMonitoringStep`, `DeltaCalculatorStep`, `MechanicalModelOutputFileWriterStep`).
    - Mapped Broadcast: `.AddBroadcastStep(curveFitPersistenceStep, t => (t.Item2, t.Item1))` persisting fitted parameters without mutating the primary stream.
    - Convergence: `.AddDataMapping(...)` constructs `MechanicalModelOutputPersistenceInput` from the joined tuple.
    - Terminal: `MechanicalModelOutputPersistenceStep` saves the simulation entity and returns the identifier.
  - Configuration: `ExperimentalDataSettings` (`FileWriterOptions`, `SegmenterOptions`, `CurveFitterOptions`, `SegmentBuilderOptions`, `SimulationInputBuilderOptions` via `PipelineStepOptions`).
  - Serialization: Centralized `OptimizationJsonOptions.SignificantFigures7` with `SignificantFiguresDoubleJsonConverter(7)`.
  - Segment types: `Ramp`, `Relaxation`, `Descent`, `Recovery`.
- `ICurveFitter` interface with `MathNetCurveFitter` and `AlglibCurveFitter` (bundled ALGLIB):
  - Domain-agnostic `CurveFitProfile` (`Automatic`, `Standard`, `RuleConstrained`):
    - `Standard`: Local gradient optimization via `alglib.minbleic` (L-BFGS with box bounds) for fast, quadratic convergence in smooth analytical problems.
    - `RuleConstrained`: Derivative-free global optimization via `alglib.mindf` with GDEMO (adaptive Differential Evolution / SHADE), executing short-circuit rejection (`1e12` barrier) on unphysical or forbidden parameter combinations.
    - `Automatic`: Automatically routes to `RuleConstrained` if `ValidateParameters` is provided; defaults to `Standard` otherwise.
  - `CurveFitInput`: Encapsulates independent/dependent variables, box bounds, initial parameters, `Profile`, `ValidateParameters: Func<double[], bool>?`, `TargetRSquared: double?`, and `PopulationMultiplier: int` (default 10).
  - `CurveFitOutput`: Exposes `OptimizedParameters: double[]`, `FinalError: double` (SSR), `RSquared: double` ($1 - \frac{SSR}{SST}$), and `Iterations: int`.
  - `MechanicalModelCurveFitOutput`: Exposes `RSquared: double` ($1 - \frac{SSR}{SST}$) consistent with `CurveFitOutput`.
  - `CurveFitterBase`: Centralized evaluation of objective function (SSR), numerical gradient (finite differences), and determination coefficient ($R^2$).
- Parametric Sweep sensitivity analysis (`ParametricSweepPipeline`):
  - Reactive TPL Dataflow stream: `SweepIngestionStep` (generating OAAT combinations via `IParametricSweepGenerator` resolved through `ServiceLocator`) → 2-way Fork:
    - Branch 1: Calculation branch mapping to `SimulationInput` and forking across `InitialStressCalculationStep`, `FinalStressCalculationStep`, and `AsymptoteCalculationStep`.
    - Branch 2: Pass-through preserving `VariableName` and `VariableValue`.
    - Convergence: `.AddDataMapping(...)` joins calculations into `ParametricSweepFinalPayload` computing absolute and percentage delta stresses.
    - Side-effect: `ParametricSweepCsvWriterStep` partitioning results into CSV files per variable via `ConcurrentDictionary<string, CsvFileWriter>` and exposing `GeneratedFilesMetadata`.
    - Terminal: `ParametricSweepPersistenceStep` persisting `ParametricSweepEntity` (table `parametric_sweeps`) with 7-significant-figure polymorphic JSON serialization and registering generated CSV artifacts in `StoredFileEntity` (table `stored_files`) under the `SensitivityAnalysis` context.
  - Generators inherit from `ParametricSweepGeneratorBase<T>` implementing `GetPropertyVariations` yielding `(PropertyName, Mutate)` delegates for centralized range iteration and envelope creation.
- Storage, Application Commands & API Endpoints:
  - `IFileStorageService`: Centralized file storage abstraction in `MelloSilveiraTools.Core.Managers.File` managing directory topology (`{BaseDirectory}/{context}/{identifier}/{fileName}`). Implemented by `LocalFileStorageService`, `AwsS3FileStorageService`, `BlobStorageFileStorageService`, and `DfsFileStorageService`. Implements both `IDisposable` and `IAsyncDisposable`. Replaces explicit `OutputFileUri` and `OutputDirectory` in pipeline inputs.
  - Application Commands & Validators:
    - `ProcessExperimentalDataCommand` / `ProcessExperimentalDataValidator`: Orchestrates ingestion, validation, stream lifetime, and execution of `ExperimentalDataProcessingPipeline`, returning the deterministic simulation SHA-256 identifier and `StorageEntryMetadata`.
    - `ExecuteParametricSweepCommand` / `ExecuteParametricSweepValidator`: Validates sweeping parameter variations and executes `ParametricSweepPipeline`.
    - `FitCurve` / `FitCurveValidator`: Validates curve segments and invokes `IMechanicalModelStepFactory` to fit constitutive parameters sequentially across experimental deformation phases.
    - `UploadPlugin` / `UploadPluginValidator`: Validates uploaded plugin DLLs (verifying file extension, semantic version naming pattern, and `PEReader.HasMetadata`) and streams payloads directly to storage.
  - Controllers & Streaming:
    - `ExperimentalDataController`, `ParametricSweepController`, `CurveFittingController`: Provide standard REST endpoints and real-time streaming endpoints (`POST .../stream`) utilizing `PipelineProgressStreamWriter` to emit NDJSON `PipelineProgressEvent` streams via `IPipelineProgressNotifier`.
    - `StoredFilesController`: Exposes file metadata queries by `ExternalIdentifier` and direct file downloads via `IFileStorageService.OpenRead`.
    - `PluginController` / `PluginEndpoints`: Exposes CRUD, lifecycle, and binary upload endpoints (`POST /api/v1/plugins/upload`).

### 7. CSV File Writing Architecture & Composition (`CsvFileWriter`)
- Located in `MelloSilveiraTools.Core.Managers.File`.
- All pipeline steps generating CSV files on disk use `CsvFileWriter` via **Composition over Inheritance** (no base class):
  - **Contracts & Composition:**
    - Steps implement their specific pipeline step contract directly (`IAsyncPipelineStep<TIn>` for terminal/observer steps, or `IAsyncPipelineStep<TIn, TOut>` for transforming steps), which natively inherits from `IPipelineStep` and `IAsyncDisposable`.
    - Steps encapsulate one or more `CsvFileWriter` instances without artificial abstract base class coupling.
  - **File Metadata & Path Tracking:**
    - Steps delegate path resolution and file creation to `IFileStorageService` and expose `FileMetadata` or `GeneratedFilesMetadata`, retained even after step disposal.
  - **Buffering & Stream Options (`CsvFileWriter`):**
    - `CsvFileWriter(Stream stream, string header)`:
      - Receives an open writable `Stream` and mandatory CSV header. Does not manage file paths directly (SRP).
      - Buffer size: 128 KB (`StreamWriter`).
      - Batch threshold: 1,000 rows before flushing the internal `StringBuilder` to disk.
      - Row buffer capacity: 16 KB initial capacity.
      - Encoding: UTF-8 without BOM (`Utf8Encoding = new UTF8Encoding(false)`).
      - Stream initialization: `CreateWriter(stream, header)` creates `StreamWriter` with `leaveOpen: false` and writes the mandatory header immediately.
      - High-throughput batching: `WriteRowAsync(row)` and zero-allocation delegate `WriteRowAsync(Action<StringBuilder>)` append rows and batch-flush to disk when the 1,000-row threshold is reached.
  - **Culture & Numerical Invariance:**
    - All numerical and floating-point values must be formatted using `CultureInfo.InvariantCulture` to prevent localization conflicts with decimal delimiters in non-US cultures.
  - **Sequential Writing & Resource Reclamation (`IAsyncDisposable`):**
    - File writing sessions are strictly sequential per stream. Upstream parallel steps feed writers via batches or pipeline streams.
    - In `DisposeAsync()`, `CsvFileWriter` flushes remaining buffered rows, flushes the writer, and asynchronously disposes the underlying stream before `GC.SuppressFinalize(this)`.
    - Partitioned multi-file writers manage a collection of `CsvFileWriter` instances and dispose them via `CsvFileWriter.DisposeAllAsync`, aggregating exceptions into `AggregateException`.

### 8. Database & SQL Generation
- `PostgresSqlProvider` caches generated SQL in static `ConcurrentDictionary<(Type, Operation, int), string>`.
- Entity metadata discovery via `[Table]`, `[PrimaryKeyColumn]`, `[Column]`, `[UniqueColumn]`, `[ForeignKeyColumn]` attributes.
- SQL template placeholders (`#TABLE_NAME`, `#COLUMNS`, etc.) substituted statically; runtime placeholders (`#WHERE`, `#ORDERBY`, `#LIMIT`, `#OFFSET`) substituted dynamically by `PostgresRepository`.
- Supports single and multi-row batch insert with `ON CONFLICT (unique_columns) DO UPDATE SET ...`.
- `IRepository.TryInsertAsync<TEntity>`: Insert-or-get-existing single statement CTE returning `Result<long>`.
- `IRepository.GetByUniqueColumnAsync<TEntity>`: Typed lookup via single `[UniqueColumn]` property.
- `IRepository` is registered Singleton — completely stateless (opens its own connection per operation via `await using`).
- Filter system: `[Filter(typeof(TEntity))]` on filter class, `[FilterColumn(filterClause, propertyName, tableName)]` on properties. `BuildWhereClauseAndParameters` auto-generates parameterized WHERE clauses.

### 9. HTTP & Streaming Endpoints
- WebApi provides dual surfaces:
  - Minimal APIs: `MapCrud<TEntity, TFilter>`, `MapPluginEndpoints`.
  - MVC Controllers: `CustomControllerBase`, `CrudController<TEntity, TFilter>`, `PluginController`.
- NDJSON streaming: `HttpContext.WriteNdjsonAsync<T>(IAsyncEnumerable<T>, ...)` emits an `X-Stream-Status: true` trailer after consuming the full body. `ApiServiceAgentBase.GetStreamAsync<T>` verifies this trailer.
- `ExceptionHandlingHttpMiddleware`: Global error handler mapping exception types to HTTP status codes. Special-cases `NdjsonException` to avoid corrupting partial streams.
- Content type: `application/x-ndjson` with `X-Content-Type-Options: nosniff`.

### 10. JWE Authentication
- JWE (JSON Web Encryption) over JWS using `JsonWebTokenHandler`.
- Signing: `HmacSha256`. Encryption: `Aes256KW` key wrap + `Aes256CbcHmacSha512` content encryption.
- `IAuthenticationTokenService`: `Generate(long/string)`, `RefreshAsync(token)`, `IsValidAsync(token)`.
- `AddJweAuthentication(JwtSettings)` configures `JwtBearerDefaults` scheme.

### 11. Dynamic Plugin System
- **Storage-Agnostic Partition Topology (`PluginStoragePartitions`):**
  - Completely decoupled from physical host disk via `IFileStorageService`, enabling plugin execution across Local, AWS S3, Azure Blob, and DFS.
  - Canonical partitions:
    - `incoming/`: Stage for newly placed or uploaded plugin DLLs awaiting inspection and promotion.
    - `loaded/`: Active loaded plugin assemblies.
    - `archive/`: Obsolete versions evicted according to retention policy, kept for rollback and telemetry.
    - `quarantine/`: Invalid binaries (failed naming pattern or non-.NET PE headers).
    - `cache/`: JSON cache persistence (`plugin-cache.json`).
- **Binary & Metadata Inspection:**
  - PE header validation via `System.Reflection.PortableExecutable.PEReader` (`peReader.HasMetadata`). Only managed .NET assemblies are permitted.
  - Naming pattern enforcement: `^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.dll$`.
  - Non-.NET binaries or ill-formed filenames are automatically moved to the `quarantine` partition with structured warnings.
- **Stream-Based Assembly Execution:**
  - `PluginAssemblyProcessor.LoadPluginAssemblyAsync` loads assemblies directly from memory streams via `pluginContext.LoadFromStream(streamToLoad)`, bypassing OS-level file locking on host operating systems and allowing direct runtime execution from cloud object storage.
- **Plugin Lifecycle Pipeline:**
  - Pipeline: `DiscoveredPlugin` → `LoadedPlugin` → `RegisteredPlugin` (`IsFullyLoaded`, `FullyLoadedAt`).
  - Each plugin loads in an isolated collectible `PluginAssemblyLoadContext` for deterministic memory reclaimability.
  - Two-level cache (`name` + `version`). Keyed persistence (`PluginCacheTargets.File` and `PluginCacheTargets.Database`).
  - `PluginOrchestratorBackgroundService`: `PeriodicTimer`-based polling loop that auto-discovers, loads, promotes higher versions, and evicts older versions to `archive/` per `PreviousVersionRetention` (default: 24h).
  - `PluginRegistrationContext`: `ForStartup(IServiceCollection)` mutates static DI; `ForRuntime(IDynamicServiceProvider)` enables hot-swap.
- **Upload Pipeline:**
  - `UploadPlugin` command and endpoint (`POST /api/v1/plugins/upload` in `PluginController` and `PluginEndpoints`) validates PE headers and writes uploaded plugins directly into `storage/{context}/incoming/`.

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
