# MelloSilveiraTools

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.5.0] - 2026-06-DD
### Added
- `CsvStreamReader` in `MelloSilveiraTools.Core.Managers.File`: High-performance, zero-allocation streaming CSV reader utilizing native `System.IO.Pipelines.PipeReader` and `System.Buffers.Text.Utf8Parser`. Returns parsed numerical rows as `double[]`, supporting arbitrary column counts, custom delimiters, empty line skipping, and header/invalid line filtering.
- **New Package: `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`**:
  - High-throughput experimental data processing and segmentation engine (`IExperimentalDataService`, `ExperimentalDataService`):
    - Stream processing with persistence and segmentation: `ProcessAsync(identifier, outputFileUri, strainStream, stressStream, options)` returns `Result<(string OutputFileName, CurveSegment[] CurveSegments)>`, consuming numerical streams via `CsvStreamReader` and delegating segment assembly and file writing to a reactive TPL Dataflow pipeline via `PipelineFactory.StartDataflow`, `AddBroadcastStep`, `ExperimentalDataFileWriterStep`, `AddGroupWhileStep`, and `CurveSegmentBuilderStep`.
    - Sliding-window segment classification supporting continuous stream processing of strain and stress tests into classified segments (`Ramp`, `Relaxation`, `Descent`, `Recovery`).
    - Buffer pooling using `ArrayPool<ExperimentalDataPoint>.Shared` with safe return in `finally`, eliminating heap allocations during continuous point streaming.
    - Remainder buffer processing on stream completion to prevent dropping trailing points that do not reach a full `BufferSize`.
    - Robust `SliceBuffer` algorithm supporting 3-phase interior transitions (`startIndex > 0 && endIndex < bufferCount - 1`), eliminating `InvalidOperationException` when phase boundaries occur interior to a single buffer window.
    - Precomputed single `timeDelta` per point in `BuildProcessedDataPoint`.
    - `ExperimentalDataSettings`: Topology execution settings configuring `FileWriterOptions`, `GroupingOptions`, and `SegmentBuilderOptions` via `PipelineStepOptions`.
    - `ExperimentalDataFileWriterStep`: Dedicated `IAsyncPipelineStep<SegmentedDataPoint, SegmentedDataPoint>` and `IAsyncDisposable` persisting processed data points to a CSV file.
    - `CurveSegmentBuilderStep`: Dedicated `ISyncPipelineStep<SegmentedDataPoint[], CurveSegment>` transforming grouped data points into classified curve segments with downsampling.
  - Extensible curve fitting suite: `ICurveFitter` interface with `MathNetCurveFitter` (Levenberg-Marquardt via MathNet.Numerics), `AlglibCurveFitter` (bundled ALGLIB), and `QuasiLinearModelCurveFitter` (fitting viscoelastic constitutive models to experimental data curves).
  - Global sensitivity analysis via Morris Elementary Effects Method: `MorrisAnalyzer`, `MorrisInput`, `MorrisOutput`, `MorrisMetrics`, `MorrisPoint`, `MorrisParameterBoundary`, and `ExpressionPathResolver`.
  - Optimization Web API commands & endpoints: `FitCurve`, `FitCurveRequest`, `FitCurveResultData`, `ParameterGroupResultData`, `OptimizationOptionsRequest`, and `CurveFittingController`.
  - Domain models for optimization: `CurveFitInput`, `CurveFitResult`, `CurveSegment`, `ExperimentalDataPoint`, `SegmentType`, `ExperimentalDataProcessingOptions`, `ProcessedDataPoint`, `SegmentedDataPoint`, `OptimizationOptions`, and parameter range models (`RangeFunction`, `RangeParameters`, `RangePowerLaw`, `RangePronySeries`, `RangeReducedRelaxationFunction`).
- **Pipelines Engine (`MelloSilveiraTools.Core.Pipelines`)**:
  - `IPipelineStep`: Core non-generic metadata contract defining `string Name { get; }` for telemetry, distributed tracing, and fault localization.
  - `IAsyncPipelineStep<in TIn, TOut>`: Asynchronous execution contract (`Task<TOut> ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
  - `ISyncPipelineStep<in TIn, out TOut>`: Synchronous execution contract (`TOut Execute(TIn input)`) implementing `IDisposable` without `IAsyncDisposable` inheritance.
  - `IAsyncEnumerablePipelineStep<in TIn, out TOut>`: Streaming 1-to-many execution contract (`IAsyncEnumerable<TOut> ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
  - `PipelineFactory`: Unified factory entry point for building streaming push-based (TPL Dataflow) and request/response pull-based (Fluent) execution pipelines.
  - TPL Dataflow Pipelines (`IDataflowPipelineBuilder<T>`, `IDataflowPipeline<T>`):
    - Fluent stage configuration: `.AddStep()` (overloaded for sync, async, and `IAsyncEnumerable`), `.AddDataMapping()`, `.AddForkingStep()`, `.AddBatchStep()`, `.AddFilterStep()`, `.AddGroupWhileStep()`, `.AddBroadcastBlock()`, `.AddBroadcastStep()`, and `.BuildTerminal()`.
    - Integrated resilience: exponential backoff retry policies (`RetryOptions`), dead-letter queue routing (`WithDeadLetterQueue()`) to isolate faulted items without halting pipeline throughput, and bounded capacity backpressure (`PipelineStepOptions.MaxBufferSize`).
    - Telemetry and tracing: OpenTelemetry `ActivitySource` tracing and structured Serilog logging via `TelemetryExtensions`.
  - Fluent Pipelines (`IFluentPipelineBuilder`, `IFluentPipeline<TIn, TOut>`): Composable sequential step execution for request/response operations with `.AddStep()` supporting sync, async, and streaming steps.
- **Command Pattern Infrastructure (`MelloSilveiraTools.Core.Application.Commands`)**:
  - Unified command hierarchy replacing legacy `OperationBase`: `CommandBase<TRequest, TResult>`, `CommandBaseWithData<TRequest, TData>`, `ListedCommandBase<TRequest, TData>`, `PagedCommandBase<TRequest, TFilter, TData>`, `CommandBaseWithDefaultResponse<TRequest>`, `CommandBaseWithoutRequest<TResult>`, and `DefaultCommandBase`.
  - Integrated request validation via `IValidator<TRequest>`.
- **Result Monad (`MelloSilveiraTools.Core.Models`)**:
  - Domain-agnostic Result monad replacing `OperationResponse`: `ResultBase`, `Result`, `Result<T>`, `ListedResult<T>`, and `PagedResult<T>`.
  - Rich error tracking with `ResultError`, `StatusCode` enum, and functional flow branching (`OnSuccess`, `OnError`, `Match`).
  - Response projection extensions: `ToHttpResult` (ASP.NET Core minimal APIs `IResult`), `BuildHttpResponse` (MVC `JsonResult`), and asynchronous variants (`ToHttpResultAsync`, `BuildHttpResponseAsync`).
  - Asynchronous client result contract `AsyncResult<T>` for `ApiServiceAgentBase`.
- **Dynamic Service Providers & Service Locator (`MelloSilveiraTools.Core.Providers`)**:
  - Extracted `IDynamicServiceProvider` and `InMemoryDynamicServiceProvider` into `MelloSilveiraTools.Core.Providers.Dynamics` for runtime service registration and dynamic resolution.
  - Added thread-safe `ServiceLocator` singleton for global service lookup.
- **Web API & Middleware Modernization**:
  - `ExceptionHandlingHttpMiddleware` replacing `ExceptionHandlingMiddleware`, with dedicated handling for `NdjsonException` to preserve streaming response integrity.
  - Minimal API endpoint mappings: `AddEndpoints`, `CrudEndpoints`, `StreamEndpoints`.
  - Migrated CRUD operations to the new Command pattern (`AddEntity`, `DeleteEntity`, `ReadEntityById`, `ReadEntityPaged`, `UpdateEntity`).
- **Dynamic Plugins Subsystem**:
  - Migrated plugin operations to the new Command pattern (`ClearPluginCache`, `PersistPluginCache`, `RestorePluginCache`, `GetPlugins`, `LoadPlugins`, `ReloadPlugins`).
  - Added `PluginValidator` implementing `IValidator<PluginsRequest>`.
- **Mechanics of Materials Facade & Type Cache**:
  - `IMechanicalModelCalculatorFacade` and `MechanicalModelCalculatorFacade` for dynamically resolving model calculators based on runtime `ConstitutiveParameters` types.
  - `IMechanicalModelTypeCache` and `MechanicalModelTypeCache` for thread-safe caching of mechanical model types and calculators.
- `ConstitutiveParameters` abstract base record acting as the foundational domain constraint for all mechanical and viscoelastic material properties.
- `IRepository.TryInsertAsync<TEntity>(TEntity, CancellationToken)` — insert-or-get-existing semantics. Returns `(Inserted, Id)`; on unique-key conflict leaves the existing row intact and returns its primary key. Atomic single statement (CTE + UNION ALL). Requires the entity to declare at least one `[UniqueColumn]` property; otherwise throws `InvalidOperationException`.
- `ISqlProvider.GetTryInsertSql<T>()` plus the `TryInsertTemplate.sql` resource that backs it.
- `IRepository.GetByUniqueColumnAsync<TEntity>(object, CancellationToken)` — typed lookup by the entity's single `[UniqueColumn]`-annotated column. Returns the entity or `null`. Removes the need to declare a `FilterBase`-derived filter just for unique-column lookups (common pattern when the unique column is a hash-based identifier produced by a database trigger). Throws `InvalidOperationException` when the entity has zero or more than one `[UniqueColumn]` property.
- `ISqlProvider.GetSelectByUniqueColumnSql<T>()`. The generated SQL binds the value to the literal parameter name `@UniqueColumnValue` regardless of the underlying property name.
- `EnumerableExtensions.ForeachAsync<T>(...)` and `Foreach<T>(...)` overloads supporting structured telemetry via `ILogger` instead of using raw `Console.WriteLine`. Exceptions captured inside these overloads are logged as errors alongside a context dictionary containing the specific failed item, preventing complete loop degradation while maintaining tracking.
- Integrated standard `Microsoft.Extensions.Logging` across all packages, backed by Serilog for structured JSON file logging.
- `BoundaryStatisticalSummary`, `BoundaryValues` and `BoundaryValue` types to capture the statistical distribution of boundary conditions.
- FileManager service to build a file with timebased name.
### Changed
- All mechanical model calculators (e.g., `SchaperyModelCalculator`, `FungModelCalculator`, `ModifiedSuperpositionMethodCalculator`, `LinearModelCalculator`) now isolate and route physical properties through the `input.ConstitutiveParameters` property instead of reading them directly from a flattened input object.
- `Expression` abstract class in `MelloSilveiraTools.Mathematics` renamed to `MathExpression`.
- `Vector3DExtension` renamed to `Vector3DExtensions`.
- `IDerivative` / `Derivative` in `MelloSilveiraTools.Mathematics` renamed to `IDifferentiation` / `Differentiation`.
- Replaced `MechanicalRelationship` with `MechanicalBehaviorType`.
- **`EnumerableExtensions.ForeachAsync<T>(...)` and `Foreach<T>(...)` safety regression fallback**: The vanilla overload without an `ILogger` parameter no longer swallows and suppresses internal iteration exceptions; it now bubbles up failures directly to the caller, adhering to standard sequential execution expectations.
- Enums to inherit from int.
### Breaking
- **OperationBase Architecture Deprecation & Replacement with Command Pattern**: Removed `OperationBase`, `OperationRequestBase`, and `OperationResponse` in `MelloSilveiraTools.WebApi`. All application operations (CRUD and Plugins) now inherit from `CommandBase<TRequest, TResult>` (or its specialized variants in `MelloSilveiraTools.Core.Application.Commands`) and return `Result<T>` instead of `OperationResponse`.
- **OperationResponse Replacement with Result Monad**: Completely removed `OperationResponse` and `OperationResponseExtensions`. Migrated all controller endpoints, minimal APIs, and `ApiServiceAgentBase` to `Result<T>`, `ListedResult<T>`, and `PagedResult<T>`.
- **Dynamic Service Provider Relocation**: Moved `IDynamicServiceProvider` and `DynamicServiceProvider` (renamed `InMemoryDynamicServiceProvider`) from `MelloSilveiraTools.Plugins.Infrastructure.Providers` to `MelloSilveiraTools.Core.Providers.Dynamics`.
- **ExceptionHandlingMiddleware Replacement**: Replaced `ExceptionHandlingMiddleware` with `ExceptionHandlingHttpMiddleware`.
- **Removal of LoadSharing Module**: Removed `ILoadSharingCalculator`, `LoadShare1DTissueThreeDimensionalSpaceCalculator`, and related load-sharing models (`MechanicalSystem`, `LoadSharingConsideration`, `LoadSharingResult`, `SpecimenLoadSharingResult`, `FailureCondition`) from `MelloSilveiraTools.MechanicsOfMaterials`.
- **Mathematics Expression & Differentiation Renaming**: Renamed `Expression` to `MathExpression`, and `IDerivative` / `Derivative` to `IDifferentiation` / `Differentiation`.
- **Architectural Overhaul (Mechanical Models Input).** Introduced a strong-typed generic constraint for mechanical model inputs. All `IMechanicalModelCalculator` and `IViscoelasticModelCalculator` interfaces and their implementations now strictly require the wrapper `MechanicalModelInput<TConstitutiveParameters>` instead of specific inherited flat input classes.
- **Constitutive Parameters Renaming & Inheritance.** Specific model input classes were refactored into parameter records inheriting from the newly introduced `ConstitutiveParameters` base class:
  - `MaxwellModelInput` → `MaxwellConstitutiveParameters`
  - `ModifiedSuperpositionMethodInput` → `ModifiedSuperpositionMethodConstitutiveParameters`
  - `SchaperyModelInput` → `SchaperyConstitutiveParameters`
  - `QuasiLinearModelInput<T>` → `QuasiLinearConstitutiveParameters<T>`
  - `SimplifiedFungModelInput` → `SimplifiedFungConstitutiveParameters`
  - `FungModelInput` → `FungConstitutiveParameters`
- **Mechanical Model Facade Initialization.** `MechanicalModelCalculatorFacade`'s reflection engine was updated to unpack the new generic architecture. Consumers instantiating the Facade must now provide the non-generic base `MechanicalModelInput`, which internally carries the strongly-typed `ConstitutiveParameters`.
- `MechanicalBehaviorType` → `MechanicalBehaviorType`.  
- **Namespace flattening (Core).** Removed the `Domain.` and `Infrastructure.` segments from every `MelloSilveiraTools.Core` namespace:
  - `MelloSilveiraTools.Core.Domain.Models.*` → `MelloSilveiraTools.Core.Models.*`
  - `MelloSilveiraTools.Core.Domain.Services.*` → `MelloSilveiraTools.Core.Services.*`
  - `MelloSilveiraTools.Core.Infrastructure.Caching.*` → `MelloSilveiraTools.Core.Caching.*`
  - `MelloSilveiraTools.Core.Infrastructure.Logger.*` → `MelloSilveiraTools.Core.Logger.*`
  - `MelloSilveiraTools.Core.Infrastructure.ResiliencePipelines.*` → `MelloSilveiraTools.Core.ResiliencePipelines.*`
  - `MelloSilveiraTools.Core.Infrastructure.Services.Email.*` → `MelloSilveiraTools.Core.Services.Email.*`
  - `MelloSilveiraTools.Core.Infrastructure.Services.Encryption.*` → `MelloSilveiraTools.Core.Services.Encryption.*`
- **Namespace flattening + rename (Database).** Removed `Domain.` and `Infrastructure.` segments and renamed `Infrastructure.Database` → `RelationalDatabase` (signals room for non-relational stores in the future):
  - `MelloSilveiraTools.Database.Domain.Repositories.*` → `MelloSilveiraTools.Database.Repositories.*`
  - `MelloSilveiraTools.Database.Infrastructure.Database.Attributes.*` → `MelloSilveiraTools.Database.RelationalDatabase.Attributes.*`
  - `MelloSilveiraTools.Database.Infrastructure.Database.Models.*` → `MelloSilveiraTools.Database.RelationalDatabase.Models.*`
  - `MelloSilveiraTools.Database.Infrastructure.Database.Repositories.*` → `MelloSilveiraTools.Database.RelationalDatabase.Repositories.*`
  - `MelloSilveiraTools.Database.Infrastructure.Database.Settings.*` → `MelloSilveiraTools.Database.RelationalDatabase.Settings.*`
  - `MelloSilveiraTools.Database.Infrastructure.Database.Sql.*` → `MelloSilveiraTools.Database.RelationalDatabase.Sql.*`
  - `MelloSilveiraTools.Database.Infrastructure.ResiliencePipelines.*` → `MelloSilveiraTools.Database.ResiliencePipelines.*`
- **WebApi Commands folder restructure.** `Add*.cs`, `DeleteEntity*.cs`, `ReadEntity*.cs` and `UpdateEntity*.cs` moved from `Application/Commands/` (flat) into `Application/Commands/Crud/{Add,Delete,Read,Update}/`. Namespace updates required: `MelloSilveiraTools.WebApi.Application.Commands.Add` → `MelloSilveiraTools.WebApi.Application.Commands.Crud.Add` (and equivalents for Delete / Read / Update).
- **Calculator response contract realignment (Result vs Output).** Renamed all calculator execution response classes from `*Result` to `*Output` across the engine domain (e.g., `MechanicalModelResult` → `MechanicalModelOutput`, `FatigueResult` → `FatigueOutput`, `NumericalMethodResult` → `NumericalMethodOutput`). This breaking change decouples pure mathematical data structures from the application's Result pattern pipeline, establishing that calculator blocks emit raw numerical projections rather than operation-status monads. Consumers invoking calculator engines must update their variable declarations and type bindings to the new `*Output` contract.
- `IRepository.TryInsertAsync` to return `Result<long>` instead of tuple `(bool, long)`.
- **Custom Logger Abstraction Removed:** Removed the custom in-house logging abstraction (`MelloSilveiraTools.Core.Infrastructure.Logger.ILogger`, `LocalFileLogger`, `LoggerBase`, and `LoggerSettings`). Consumers must migrate their constructors to use the standard `Microsoft.Extensions.Logging.ILogger<T>`.
### Removed
- `MelloSilveiraTools.WebApi.Application.Operations.*`: Removed legacy `OperationBase`, `OperationRequestBase`, `OperationResponse`, and old operation classes.
- `MelloSilveiraTools.WebApi.ExtensionMethods.OperationResponseExtensions` and `ResultExtensions`.
- `MelloSilveiraTools.MechanicsOfMaterials.Calculators.LoadSharing.*` and associated models.
- `DifferentialEquationMethodFactory` in `MelloSilveiraTools.Mathematics` (differential equation solvers are now resolved via Keyed DI or direct dependency injection).
- `MelloSilveiraTools.Core.ExtensionMethods.DoubleExtensions` — the canonical implementation now lives at `MelloSilveiraTools.Mathematics.Extensions.DoubleExtensions`. Consumers that imported the Core variant must add a reference to `MelloSilveiraTools.Mathematics` and update the `using` directive.

## [1.4.0] - 2026-05-01
### Added
- **Mathematics — function system.** `Function` abstract class (unidimensional f(x)) with lazy `Derivative` and `Integral` properties. Concrete implementations: `ConstantFunction`, `PolynomialFunction`, `ExponencialFunction`, `SineFunction`, `CosineFunction`, `PowerLaw`, `GenericFunction`. `FunctionFactory` creates instances by `FunctionType`.
- **Mathematics — expressions.** `Expression` abstract class (sum of multiple `Function` instances). `PronySeries` concrete expression: `f(x) = c + Σ aₙ·e^(aₙx)`.
- **Mathematics — numerical integration and differentiation.** `IIntegration` + `SimpsonRuleIntegration`; `IDerivative` + `Derivative` (finite-difference numerical derivative).
- **Mathematics — root-finding.** `IRootFinding` interface + `BisectionMethod`, `BrentMethod`, `RootFinding` (composite dispatcher), `StepByStepMethod`. `RootFindingInput` carries interval, tolerance and max-iteration settings. `NonConvergenceException` is thrown when the algorithm fails to converge.
- **Mathematics — statistics.** `IStatisticsCalculator` + `StatisticsCalculator`; `StatisticalData` result record.
- **Mathematics — 3D geometry and utilities.** `Point3D` and `Vector3D` value types; `Vector3DExtension` and `DoubleExtensions`; `UnitConverter`; `CustomMath` and `MathematicConstants` constant holders.
- **MechanicsOfMaterials — mechanical models.** Generic `IMechanicalModelCalculator<TInput>` interface (CalculateForce / CalculateDisplacement / CalculateStress / CalculateStrain) and `MechanicalModelCalculatorBase`.
  - *Elastic:* `IElasticModelCalculator` / `ElasticModelCalculator` + `ElasticModelInput` / `ElasticModelOutput`.
  - *Linear viscoelastic:* `ILinearModelCalculator` / `LinearModelCalculator`; `IMaxwellModelCalculator` / `MaxwellModelCalculator` + `MaxwellModelInput` / `MaxwellModelResult`.
  - *Non-linear viscoelastic:* `ISchaperyModelCalculator` / `SchaperyModelCalculator` + `SchaperyModelInput` / `SchaperyModelResult`; `IModifiedSuperpositionMethodCalculator` / `ModifiedSuperpositionMethodCalculator` + corresponding input/result.
  - *Quasi-linear viscoelastic:* `IQuasiLinearModelCalculator` / `QuasiLinearModelCalculator`; `IFungModelCalculator` / `FungModelCalculator` + `FungModelInput`; `ISimplifiedFungModelCalculator` / `SimplifiedFungModelCalculator` + `SimplifiedFungModelInput`; `ReducedRelaxationFunction` helper type.
- **MechanicsOfMaterials — load sharing.** `ILoadSharingCalculator` / `LoadShare1DTissueThreeDimensionalSpaceCalculator` — computes specimen displacement, angle, force projection and their derivatives within a 3-D system.
- **MechanicsOfMaterials — supporting types.** `MechanicalParameter` (displacement / strain / force / stress descriptor), `SpecimenParameter`, `Asymptote`, `AnalysisType`, `AnalysisResult`, `MechanicalModelType`, `MechanicalBehaviorType`, `ParameterNameConstant`, `MechanicalModelConstants`, `RampTimeConsideration`, `ViscoelasticEffect`, `AcceptedRange`, `LoadSharingConsideration`, `MechanicalSystem`, `FailureCondition`, `LoadSharingResult`, `SpecimenLoadSharingResult`.
- **MechanicsOfMaterials — attributes.** `MechanicalModelParameterAttribute` and `MechanicalModelParameterCalculationAttribute` for annotating model-parameter properties.
- **MechanicsOfMaterials — converter.** `IMechanicalParameterConverter` + `MechanicalParameterConverter`.
### Changed
- `MelloSilveiraTools.MechanicsOfMaterials` now takes a `ProjectReference` on `MelloSilveiraTools.Mathematics` (required by `ILoadSharingCalculator` which uses `Vector3D`). Consumers that install `MechanicsOfMaterials` without the meta-package will pull in `Mathematics` as a transitive dependency.

## [1.3.0] - 2026-04-26
### Added
- `OperationResponseExtensions.BuildHttpResponseAsync<T>(this Task<T>)` — async counterpart to `BuildHttpResponse`: awaits a `Task<T> where T : OperationResponse` and returns a `JsonResult` with the matching HTTP status code. Mirrors `ToHttpResultAsync` (minimal APIs) on the controller side.
- `ApiServiceAgentBase.GetStreamAsync<T>(requestUri, timeoutInMilliseconds, methodName, cancellationToken)` — protected `async IAsyncEnumerable<T>` for consuming NDJSON-streaming endpoints. Opens the response body with `HttpCompletionOption.ResponseHeadersRead`, deserializes each newline-delimited JSON record on arrival, and after the body is fully consumed validates the `X-Stream-Status: true` trailer written by `WriteNdjsonAsync` — logging an error when the trailer is absent.
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
- `CrudController` (all four CRUD actions) and `CustomControllerBase.Add` now use the fluent `BuildHttpResponseAsync`.
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
