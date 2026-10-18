# MelloSilveiraTools

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [UNRELEASED]
- Adapted `FitCurveCommand` and Associated Types for Segmented File Uploads (`task-fitcurve-command-adaptation`):
  - Created `SegmentFilesGroup` DTO (`SegmentType SegmentType`, `IReadOnlyList<IFormFile> Files`) in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands` to group uploaded experimental data files by deformation phase type.
  - Updated `FitCurveRequest` to receive `IReadOnlyList<SegmentFilesGroup> SegmentFiles` instead of pre-parsed `CurveSegment` collections.
  - Refactored `FitCurveCommand` to inherit from `ListedCommandBase<FitCurveRequest, MechanicalModelCurveFitOutput>`, streaming and parsing each CSV file via `CsvStreamReader` (col 0: Time, col 1: Strain, col 2: Stress) into `CurveSegment` instances, executing `fitterStep.ExecuteAsync`, and emitting a sentinel segment with `SegmentType.Unknown` to drain batch fitters (e.g., Schapery).
  - Eliminated `FitCurveResultData` and `ParameterGroupResultData` types, returning pure `ListedResult<MechanicalModelCurveFitOutput>` via `Result.CreateListedSuccessOk(outputs)`.
  - Updated `FitCurveValidator` to ensure `SegmentFiles` is not null or empty, every group specifies `SegmentType != SegmentType.Unknown`, and files are non-empty.
  - Updated `CurveFittingController` to receive `[FromForm] FitCurveRequest request` for multipart form data support and return `ActionResult<ListedResult<MechanicalModelCurveFitOutput>>`.
  - Updated and verified unit tests in `FitCurveCommandTests` and `FitCurveValidatorTests`.
  - Harmonized cooperative `CancellationToken` parameters across application command hierarchies (`AddEntity`, `DeleteEntity`, `ReadEntityById`, `UpdateEntity`, `ExecuteParametricSweepCommand`, `ClearPluginCache`, `GetPlugins`, `PersistPluginCache`, `LoadPlugins`, `RestorePluginCache`, `ReloadPlugins`).
- Comprehensive Unit Test Suite in `test/UnitTests` for Storage, Pipelines, Persistence, Real-time Progress, Commands, and Validators (46 new unit tests, bringing the total passing test suite to 362 tests):
  - `ParametricSweepPersistenceStepTests` in `test/UnitTests/MechanicsOfMaterials.Optimizations/Pipelines/SensitivityAnalysis/Steps`: Verifies database persistence of `ParametricSweepEntity` and `StoredFileEntity` (`Context = "SensitivityAnalysis"`, `StorageProvider.Local`, `ContentType = "text/csv"`), conflict handling (already exists without failure), error handling (`InvalidOperationException` on db failure), and graceful `DisposeAsync`.
  - `ProcessExperimentalDataCommandTests` and `ProcessExperimentalDataValidatorTests` in `test/UnitTests/MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Commands`: Verifies request validation (missing model name, missing streams/files, invalid TargetRSquared, negative thresholds) and command orchestration (validation short-circuiting, pipeline delegation, and deterministic stream disposal in `finally` blocks for `IFormFile` inputs).
  - `ExecuteParametricSweepCommandTests` and `ExecuteParametricSweepValidatorTests` in `test/UnitTests/MechanicsOfMaterials.Optimizations/SensitivityAnalyses/Commands`: Verifies validation rules (model name, baseline parameters, positive time steps, positive asymptote tolerances, swept variable presence) and command execution.
  - `FitCurveCommandTests` and `FitCurveValidatorTests` in `test/UnitTests/MechanicsOfMaterials.Optimizations/CurveFitting/Commands`: Verifies curve fitting request validation, step factory instantiation, async stream execution, and parameter group / `FinalError` aggregation.
  - `PipelineProgressEventTests` and `PipelineProgressEmissionTests` in `test/UnitTests/Core/Pipelines`: Verifies `PipelineProgressEvent` record immutability and equality, `PipelineStepState` enum values, and real-time progress reporting through `IProgress<PipelineProgressEvent>` across synchronous (`HandleExecution`), asynchronous with retries, streaming (`IAsyncEnumerable`), and full TPL Dataflow pipelines (`PipelineFactory.StartDataflow`).
  - Hardened Polly resilience pipeline configuration with safe defaults in `ResiliencePipelineSettings` (`MaxRetryAttempts = 3`, `DelayInMilliseconds = 200`, `BackoffType = Constant`, `UseJitter = true`) and fallback guards in `DefaultResiliencePipeline` to prevent validation exceptions when default settings are supplied.
- `PipelineStepState` and `PipelineProgressEvent` in `MelloSilveiraTools.Core.Pipelines.Models`, integrated into `PipelineFactory.StartDataflow`, `DataflowPipelineBuilder`, and `TelemetryExtensions`: Real-time progress notification infrastructure emitting `Started`, `Running`, `Completed`, and `Faulted` step events across TPL Dataflow pipelines, exposed on `ExperimentalDataProcessingInput.Progress` and `ParametricSweepInput.Progress`.
- Application Commands, Validators, and Web API Controllers in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`:
  - `ProcessExperimentalDataCommand`, `ProcessExperimentalDataRequest`, `ProcessExperimentalDataResponse`, and `ProcessExperimentalDataValidator` for experimental raw data processing.
  - `ExecuteParametricSweepCommand`, `ExecuteParametricSweepRequest`, `ExecuteParametricSweepResponse`, `ExecuteParametricSweepValidator`, and `IParametricSweepPipeline` for parametric sweep sensitivity analyses.
  - Updated `FitCurve`, `FitCurveRequest`, `ParameterGroupResultData`, and `FitCurveValidator` using `IMechanicalModelStepFactory`.
  - `StoredFileFilter` in `MelloSilveiraTools.Database.RelationalDatabase.Models.Filters` for querying `StoredFileEntity` records by `ExternalIdentifier`, `Context`, and `FileName`.
  - `ExperimentalDataController` (`api/v1/experimental-data`), `ParametricSweepController` (`api/v1/parametric-sweeps`), `CurveFittingController` (`api/v1/curve-fitting`), and `StoredFilesController` (`api/v1/stored-files`) supporting standard `POST` execution, real-time NDJSON progress streaming (`POST .../stream` via `PipelineProgressStreamWriter`), and file listing/download (`GET`) by `ExternalIdentifier` via `IFileStorageService.OpenRead`.
- `ParametricSweepEntity` (`[Table("parametric_sweeps")]`), `ParametricSweepPersistenceInput`, and `ParametricSweepPersistenceStep` in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`: Terminal database persistence step for `ParametricSweepPipeline` persisting sweep configuration snapshots (`BaselineParametersJson`, `VariableRangesJson` formatted via `OptimizationJsonOptions.SignificantFigures7`) and registering all partitioned sensitivity CSV files in `stored_files` (`StoredFileEntity` with `Context = "SensitivityAnalysis"`).
- `IEnumerablePipelineStep<in TIn, out TOut>` in `src/MelloSilveiraTools.Core/Pipelines/Steps/IEnumerablePipelineStep.cs`: Synchronous collection and streaming 1-to-many execution step contract (`IEnumerable<TOut> Execute(TIn input, CancellationToken cancellationToken)`) implementing `IDisposable`. Designed for synchronous 1-to-many fan-out transformations via TPL Dataflow's `TransformManyBlock`, with cooperative cancellation token propagation, safe Dead-Letter Queue (DLQ) routing, and automatic resource disposal.
- TPL Dataflow Pipelines (`IDataflowPipelineBuilder<T>`, `IDataflowPipeline<T>`): `.AddStep()` overload supporting `IEnumerablePipelineStep<TIn, TOut>` with cooperative cancellation, DLQ error handling, and `IDisposable` lifecycle management via `TransformManyBlock`.
- `CsvFileWriter` in `src/MelloSilveiraTools.Core/Managers/File/CsvFileWriter.cs`: High-performance, domain-agnostic buffered asynchronous CSV writer encapsulating stream creation, sequential/async file flags, 128 KB buffer, UTF-8 non-BOM, 1,000-row batching threshold, immediate mandatory header emission, and graceful async disposal with `DisposeAllAsync` aggregate exception handling.
- Formalized and documented Schapery Non-Linear 1D curve fitting objective functions and three-stage hierarchical identification strategy (anchor linear baseline, pointwise non-linear Helmholtz multipliers, and continuous function fitting) in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` for Task 2.2 (`outputs/task-2.2-result.json`).
- Re-enabled and enhanced `SchaperyRelaxationOnlyCurveFitterStepAcceptanceTests` validating 3-stage curve fitting against real experimental multi-strain ligament data (ACL specimen `02-07-21 - L38 - D11` from `SoftTissue/experimental results/Schapery/Stress`), asserting strict biomechanical physical validity ($G_e > 0$, $g_1 > 0$, $0 < n < 1$, discrete $h_e, h_2 > 0$, continuous positive Helmholtz functions across $[0.03, 0.06]$) and proving direct resolution of unphysical legacy parameters from `Populate_database.sql`.
- Fixed array sizing bug in `SchaperyRelaxationOnlyCurveFitterStep.cs` where `strainAndHelmholtzVariables` allocated `[relaxations.Count - 1]` instead of `[relaxations.Count]`, preventing an `IndexOutOfRangeException` when iterating across all segments.
- Fixed `LogarithmicCurveFitter.cs` where `ZeroBased = true` optimization returned parameters without the leading `0.0`, scrambling coefficient indexing in `LogarithmicFunction` and resulting in `NaN` log arguments.
- Implemented downsampling with $\Delta t \ge 1.0\text{ s}$ across all multi-strain relaxation test segments in `TestData/SchaperyRelaxationOnly/` (`01_Relaxation_Strain003.csv`, `02_Relaxation_Strain004.csv`, `03_Relaxation_Strain005.csv`, `04_Relaxation_Strain006.csv`), achieving $R^2 \ge 0.95 > 0.90$ across all four individual strain levels consistent with `Populate_database.sql` benchmark cases.
- Added `VandermondeLeastSquares` in `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/CurveFitting/MathematicalFunctions/VandermondeLeastSquares.cs`: High-performance analytical polynomial curve fitting engine constructing the Vandermonde matrix and solving the linear least-squares problem via QR decomposition with Modified Gram-Schmidt orthogonalization.
- Enhanced `PolynomialCurveFitter.cs` with closed-form analytical least-squares fitting and optimal initial parameter estimation via `VandermondeLeastSquares`, preserving full decoupling from third-party libraries and delegating to `innerFitter` for constrained optimization.
- Injected `[FromKeyedServices(FunctionType.Polynomial)] IMathematicalFunctionCurveFitter` into `SchaperyRelaxationOnlyCurveFitterStep.cs` and evaluated polynomial fitting candidates in `FitHelmholtzVariable`, guaranteeing $R^2 \ge 0.90$ for continuous Helmholtz functions $h_e(\varepsilon)$ and $h_2(\varepsilon)$ and lifting the consolidated composite fit to $R^2 = 0.9880 \ge 0.90$.
- Fixed argument order inversion in `ConstantFunction` constructor calls across `SchaperyRelaxationOnlyCurveFitterStep.cs`, `ConstantFunction.CreateDerivative()`, and `PolynomialFunction.CreateDerivative()` where interval boundaries were incorrectly passed into the `coefficient` parameter.
- **Task 2.7: Native 1D Sensitivity Analysis Engine (OAAT)** in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`:
  - Added reactive TPL Dataflow pipeline (`ParametricSweepPipeline`) orchestrating parameter variation ingestion, concurrent 3-way stress evaluation ($\sigma(t_0)$, $\sigma(t_f)$, and $t_{\text{asymptote}}$), delta metrics calculation, and partitioned CSV writing.
  - Ensured strict coverage of all 4 mandatory sensitivity scenarios defined in `experimental-data-analyst/SKILL.md`: Stress at initial time ($t_0$), stress at final time ($t_f$), asymptote time ($t_{\text{asymptote}}$), and stress variation ($\Delta\sigma$ and $\% \Delta\sigma$).
  - Implemented domain-specific numerical asymptote detection in `AsymptoteCalculationStep` based on the time derivative of stress approaching zero ($|\frac{d\sigma}{dt}| \le \epsilon$) via strictly mandatory `IDifferentiation` and user-informed `tolerance` parameters (central and boundary forward differences), eliminating artificial time bounds and using exponential bracket search followed by binary search refinement.
  - Added constitutive parameter alias support (e.g., $A$, $B$, $c$, $\tau_1$, $\tau_2$, $g_0$, $G_e$, $g_1$, $n$) in `ParametricSweepGeneratorBase`, `FungParametricSweepGenerator`, `SimplifiedFungParametricSweepGenerator`, and `SchaperyParametricSweepGenerator`, ensuring seamless mapping between mathematical symbols and programmatic property names in partitioned CSV generation.
  - Fixed floating-point step truncation in `RangeParametersExtensions.GenerateValues` with relative tolerance `Math.Abs(range.Step.Value) * 1e-6`.
  - Added dynamic polymorphic parameter adaptation in `MechanicalModelTypeCache.CompileMethodInvoker` via `ConvertMechanicalModelInput<T>`, bridging generic simulation envelopes (`GenericMechanicalModelInput`) to strongly typed `MechanicalModelInput<T>` without reflection overhead.
  - Implemented `ParametricSweepInputJsonParser` and factory methods `ParametricSweepInput.FromFile(filePath)` / `ParametricSweepInput.FromJson(json)` in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Serialization`, enabling deserialization of parametric sweep configurations directly from JSON files with polymorphic resolution of constitutive parameters (`Fung`, `SimplifiedFung`, `Schapery`, `Maxwell`, `Elastic`, `ModifiedSuperpositionMethod`) and mathematical functions (`PolynomialFunction`, `PowerLaw`, `PronySeries`, `ConstantFunction`).
- Refactored `ParametricSweepPipelineAcceptanceTests` into a data-driven test suite using xUnit `[Theory]` and `[MemberData(nameof(GetJsonInputFiles))]` that automatically discovers, parses, and executes all JSON input files residing in `MechanicalOfMaterials.Optimizations/SensitivityAnalysis/TestData/` (`01_Fung_SensitivityAnalysis.json`, `02_SimplifiedFung_SensitivityAnalysis.json`, `03_Schapery_SensitivityAnalysis.json`), verifying physical integrity, stress relaxation monotonicity, asymptote detection, and partitioned CSV outputs with complete test isolation per scenario.
- Simplified `AsymptoteCalculationStep` by eliminating threshold constants (`StationaryDerivativeThreshold`, `AbsoluteDerivativeFloor`) in favor of the user-informed `tolerance`, removing internal perturbation constants to directly utilize `input.TimeStep`, and streamlining stress derivative calculations via direct finite differences.
- Created end-to-end acceptance tests in `test/AcceptanceTests/MechanicalOfMaterials.Optimizations/SensitivityAnalysis/ParametricSweepPipelineAcceptanceTests.cs` and unit tests in `test/UnitTests/MechanicsOfMaterials.Optimizations/SensitivityAnalyses/`.

### Changed
- Standardized and decoupled CSV file writing across pipeline steps by introducing domain-agnostic `CsvFileWriter` and `IFileStorageService` in `MelloSilveiraTools.Core.Managers.File`, removing `BaseCsvWriterStep` and user-provided output paths (`OutputFileUri` / `OutputDirectory`):
  - `ExperimentalDataProcessingInput` and `ParametricSweepInput`: Removed `OutputFileUri` and `OutputDirectory` so storage resolution is governed exclusively by `IFileStorageService`.
  - `ExperimentalDataFileWriterStep`: Standardized primary constructor `(IFileStorageService fileStorageService, string identifier)` opening streams under `"ExperimentalData"` context and exposing `FileMetadata`.
  - `MechanicalModelOutputFileWriterStep`: Standardized primary constructor `(IFileStorageService fileStorageService, string uniqueIdentifier)` opening streams under `"ExperimentalData"` context and returning `FileMetadata` directly from `ExecuteAsync`.
  - `ParametricSweepCsvWriterStep`: Standardized primary constructor `(IFileStorageService fileStorageService, string identifier)` opening variable-partitioned streams under `"SensitivityAnalysis"` context and exposing `GeneratedFilesMetadata`.
- `CsvStreamReader`: Default delimiter updated to `CsvSeparators.Main` (from `','`).
- `ParametricSweepGeneratorBase` and `FungParametricSweepGenerator`: Simplified OAAT variable mutation by introducing template method `GetPropertyVariations(input)` returning `(string PropertyName, Func<TConstitutiveParameters, double, TConstitutiveParameters> Mutate)` delegates, centralizing range iteration and simulation envelope construction in the base class.

## [2.0.0] - 2026-10-08
### Added
- `LogarithmicFunction` in `MelloSilveiraTools.Mathematics.Functions`.
- `CsvStreamReader` in `MelloSilveiraTools.Core.Managers.File`: High-performance, zero-allocation streaming CSV reader utilizing native `System.IO.Pipelines.PipeReader` and `System.Buffers.Text.Utf8Parser`. Returns parsed numerical rows as `double[]`, supporting arbitrary column counts, custom delimiters, empty line skipping, and header/invalid line filtering.
- **New Package: `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`**:
  - High-throughput experimental data processing and segmentation engine (`IExperimentalDataService`, `ExperimentalDataService`):
    - Stream processing with persistence and segmentation: `ProcessAsync(identifier, outputFileUri, strainStream, stressStream, options)` returns `Result<(string OutputFileName, CurveSegment[] CurveSegments)>`, consuming numerical streams and driving a reactive TPL Dataflow pipeline via `PipelineFactory.StartDataflow`, `ExperimentalDataSegmenterStep`, `AddBroadcastStep`, `ExperimentalDataFileWriterStep`, `AddGroupWhileStep`, and `CurveSegmentBuilderStep`.
    - Sliding-window segment classification supporting continuous stream processing of strain and stress tests into classified segments (`Ramp`, `Relaxation`, `Descent`, `Recovery`).
    - Buffer pooling using `ArrayPool<ExperimentalDataPoint>.Shared` with safe return in `finally`, eliminating heap allocations during continuous point streaming.
    - Remainder buffer processing on stream completion to prevent dropping trailing points that do not reach a full `BufferSize`.
    - Robust `SliceBuffer` algorithm supporting 3-phase interior transitions (`startIndex > 0 && endIndex < bufferCount - 1`), eliminating `InvalidOperationException` when phase boundaries occur interior to a single buffer window.
    - Precomputed single `timeDelta` per point in `BuildProcessedDataPoint`.
    - `ExperimentalDataSettings`: Topology execution settings configuring `FileWriterOptions`, `GroupingOptions`, `SegmentBuilderOptions`, and `SegmenterOptions` via `PipelineStepOptions`.
    - `ExperimentalDataSegmenterStep`: Dedicated `IAsyncEnumerablePipelineStep<(Stream StrainStream, Stream StressStream), SegmentedDataPoint>` and `IAsyncDisposable` ingesting CSV streams and yielding classified `SegmentedDataPoint` items.
    - `ExperimentalDataFileWriterStep`: Dedicated `IAsyncPipelineStep<SegmentedDataPoint, SegmentedDataPoint>` and `IAsyncDisposable` persisting processed data points to a CSV file.
    - `CurveSegmentBuilderStep`: Dedicated `ISyncPipelineStep<SegmentedDataPoint[], CurveSegment>` transforming grouped data points into classified curve segments with downsampling.
  - Extensible curve fitting suite: `ICurveFitter` interface with `MathNetCurveFitter` and `AlglibCurveFitter` (bundled ALGLIB):
    - `CurveFitProfile` enum (`Automatic`, `Standard`, `RuleConstrained`) providing domain-agnostic optimization routing.
    - `AlglibCurveFitter` multi-strategy engine:
      - `Standard`: Executes local gradient optimization via `alglib.minbleic` (L-BFGS with bounds) for fast quadratic convergence on smooth analytical curves.
      - `RuleConstrained`: Executes derivative-free global optimization via `alglib.mindf` with GDEMO (Success-History Based Differential Evolution / SHADE), supporting short-circuit boolean constraint rejection (`1e12` barrier) without numerical gradient distortion.
      - `Automatic`: Automatically resolves to `RuleConstrained` if boolean validation rules are present, defaulting to `Standard` otherwise.
    - Centralized determination coefficient computation (`CalculateRSquared`) in `CurveFitterBase` ($R^2 = 1 - \frac{SSR}{SST}$).
    - `CurveFitOutput`: Exposes `RSquared` metric alongside `OptimizedParameters`, `FinalError`, and `Iterations`.
    - `CurveFitInput` and `MathematicalCurveFitInput`: Added `Profile`, boolean domain rule validation delegate `ValidateParameters: Func<double[], bool>?`, `TargetRSquared: double?`, and `PopulationMultiplier: int`.
    - Integrated physical domain rules into `FungRelaxationOnlyCurveFitterStep` (fast relaxation time < slow relaxation time) and `SimplifiedFungRelaxationOnlyCurveFitterStep` (ordered Prony series decay times).
  - Global sensitivity analysis via Morris Elementary Effects Method: `MorrisAnalyzer`, `MorrisInput`, `MorrisOutput`, `MorrisMetrics`, `MorrisPoint`, `MorrisParameterBoundary`, and `ExpressionPathResolver`.
  - Optimization Web API commands & endpoints: `FitCurve`, `FitCurveRequest`, `FitCurveResultData`, `ParameterGroupResultData`, `OptimizationOptionsRequest`, and `CurveFittingController`.
  - Domain models for optimization: `CurveFitInput`, `CurveFitResult`, `CurveSegment`, `ExperimentalDataPoint`, `SegmentType`, `ExperimentalDataProcessingOptions`, `ProcessedDataPoint`, `SegmentedDataPoint`, `OptimizationOptions`, and parameter range models (`RangeFunction`, `RangeParameters`, `RangePowerLaw`, `RangePronySeries`, `RangeReducedRelaxationFunction`).
  - Added missing properties `Identifier`, `RampTime`, `ExperimentalStress`, `TimeStep`, `TimePoints`, and `Simulation` to `MechanicalModelCurveFitOutput` to support forward simulation.
  - Real-time streaming Experimental Data Processing Pipeline (`IExperimentalDataProcessingPipeline`, `ExperimentalDataProcessingPipeline`):
    - Reactive DAG stream topology in `ProcessAsync(input, cancellationToken)` returning a `Result<string>` containing the processing run identifier.
    - Continuous pipeline steps:
      - `ExperimentalDataDownsamplerStep`: Sequential streaming step (`IAsyncEnumerablePipelineStep<SegmentedDataPoint, SegmentedDataPoint>`) enforcing downsampling by `skipTimeStep`.
      - `CurveSegmentAccumulatorStep`: Real-time stateful accumulator step (`IAsyncEnumerablePipelineStep<SegmentedDataPoint, CurveSegment>`) grouping points by `SegmentType` without full-array batching overhead.
      - `IdentifierBuilderStep`: Deterministic identification step (`ISyncPipelineStep<MechanicalModelCurveFitOutput, string>`) computing a SHA-256 hash over model metadata, range, and polymorphic constitutive parameters.
      - `CurveFitOutputPersistenceStep`: Non-mutating database persistence step (`IAsyncPipelineStep<(string Identifier, MechanicalModelCurveFitOutput CurveFitOutput)>`) persisting fitted parameters via `IRepository.TryInsertAsync` using polymorphic `.GetType()` serialization.
      - `NumericalSimulationStep`: Forward numerical simulation step (`ISyncPipelineStep<MechanicalModelCurveFitOutput, MechanicalModelSimulationPayload>`) integrating material behavior through `IMechanicalModelCalculatorFactory`.
      - Parallel simulation analysis steps: `AsymptoteMonitoringStep` (detects steady-state relaxation asymptotes), `DeltaCalculatorStep` (computes comparative boundary deltas as `SimulationDeltaOutput`), and `MechanicalModelOutputFileWriterStep` (streams simulation time-histories to CSV).
      - `MechanicalModelOutputPersistenceStep`: Convergence persistence step (`IAsyncPipelineStep<MechanicalModelOutputPersistenceInput, string>`) computing a deterministic simulation hash and persisting `MechanicalModelSimulationEntity`.
    - `OptimizationJsonOptions`: Centralized JSON options provider (`OptimizationJsonOptions.SignificantFigures7`) using `SignificantFiguresDoubleJsonConverter(7)` across all optimization persistence steps.
    - Pipeline domain models: `ExperimentalDataProcessingInput`, `ExperimentalDataSegmenterInput`, `MechanicalModelOutputPersistenceInput`, and `SimulationDeltaOutput`.
- **Mechanics of Materials Facade & Type Cache**:
  - `IMechanicalModelCalculatorFactory`: Re-implemented matching SoftTissue project pattern, using `IMechanicalModelTypeResolver` to decouple model mappings.
  - `IMechanicalModelTypeResolver`: Interface and concrete keyed singleton implementations in `TypeResolvers/` for every mechanical model.
  - Generic input architecture (`GenericMechanicalModelInput` and `MechanicalModelCalculatorFacade`) updated to use the factory with keyed DI resolvers.
- **Pipelines Engine (`MelloSilveiraTools.Core.Pipelines`)**:
  - `IPipelineStep`: Core non-generic metadata contract defining `string Name { get; }` for telemetry, distributed tracing, and fault localization.
  - `IAsyncPipelineStep<in TIn>`: Consumer/terminal asynchronous execution contract (`Task ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
  - `IAsyncPipelineStep<in TIn, TOut>`: Asynchronous execution contract (`Task<TOut> ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
  - `ISyncPipelineStep<in TIn, out TOut>`: Synchronous execution contract (`TOut Execute(TIn input)`) implementing `IDisposable` without `IAsyncDisposable` inheritance.
  - `IAsyncEnumerablePipelineStep<in TIn, out TOut>`: Streaming 1-to-many execution contract (`IAsyncEnumerable<TOut> ExecuteAsync(TIn input, CancellationToken ct)`) implementing `IAsyncDisposable`.
  - `PipelineFactory`: Unified factory entry point for building streaming push-based (TPL Dataflow) and request/response pull-based (Fluent) execution pipelines.
  - TPL Dataflow Pipelines (`IDataflowPipelineBuilder<T>`, `IDataflowPipeline<T>`):
    - Fluent stage configuration: `.AddStep()` (overloaded for sync, async, and `IAsyncEnumerable`), `.AddDataMapping()`, `.AddForkingStep()`, `.AddFilterStep()`, `.AddGroupWhileStep()`, `.AddCollectAllStep()`, `Fork` (2-way and 3-way, accepting branch delegates or step instances directly), `AddBroadcastStep` (side-effect observer and mapped transform observer), and `BuildTerminal()`.
    - Completion propagation: `DataflowExtensions.LinkTo` C# 13 extension members overriding TPL Dataflow defaults with `PropagateCompletion = true` on block linkages, guaranteeing automatic and graceful graph draining on `pipeline.Complete()`.
    - Resource cleanup: `DataflowPipeline<TIn>.DisposeAsync` encloses step disposal in a `try ... finally` block, ensuring all `IDisposable` and `IAsyncDisposable` step resources are reclaimed even if graph completion faults.
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
- Refactored `ICurveFitter` and its implementations to support functions with multiple independent variables.
- Updated `Schapery` and `Fung` constitutive model implementations in alignment with specialized bibliographies.
- Refactored mechanical model curve fitter steps (e.g., `SchaperyRelaxationOnlyCurveFitterStep`, `FungCurveFitterStep`) to streamline pipeline integration and yield `MechanicalModelCurveFitOutput`, replacing previous base abstractions.
- Validated and maintained Fung QLV 1D model objective function curve fitting mapping via inline lambdas in `FungRelaxationOnlyCurveFitterStep`, adhering to the established architectural standard without redundant file generation.
- All mechanical model calculators (e.g., `SchaperyModelCalculator`, `FungModelCalculator`, `ModifiedSuperpositionMethodCalculator`, `LinearModelCalculator`) now isolate and route physical properties through the `input.ConstitutiveParameters` property instead of reading them directly from a flattened input object.
- `Expression` abstract class in `MelloSilveiraTools.Mathematics` renamed to `MathExpression`.
- `Vector3DExtension` renamed to `Vector3DExtensions`.
- Renamed enum `MechanicalBehaviorType` to `LoadResponseRelationship` across the domain, attributes, steps, models, and caches.
- `MechanicalModelCurveFitOutput`: Renamed `Precision` property to `RSquared` for domain consistency with `CurveFitOutput` ($R^2 = 1 - \frac{SSR}{SST}$); updated XML doc comment replacing invalid HTML entity `&sup2;` with `²`.
- `MechanicalModelStepFactory`: Handled empty `TargetSegments` collection (`[]`) matching default model behavior instead of throwing `ArgumentException`.
- Refactored `ExperimentalDataProcessingPipeline` to log errors when `pipeline.SendAsync` fails instead of returning an error result, maintaining execution continuity.
- Extracted `ExperimentalDataProcessingInput` and `ExperimentalDataSegmenterInput` from `ExperimentalDataProcessingPipeline.cs` into standalone model files under `Pipelines/ExperimentalData/Models/`.
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
- **Curve Fitting Modernization**:
  - `CurveFitInput`: Replaced legacy scalar penalty delegate `EvaluateConstraintsAndPenalties` with short-circuit boolean rule validator `ValidateParameters`, added `Profile`, `TargetRSquared`, and `PopulationMultiplier`.
  - `CurveFitOutput`: Extended record constructor to include `double RSquared`.
  - `AlglibCurveFitter`: Primary constructor now requires `ILogger<AlglibCurveFitter>` for structured failure reporting and diagnostics.
  - `ExperimentalDataPersistenceStep`: Updated execution return type to `Task<MechanicalModelCurveFitOutput>` (returning `output with { Identifier = identifierHash }`) to maintain model pipeline continuity.
  - `LinearModelCalculator`, `ModifiedSuperpositionMethodCalculator`, and `SchaperyModelCalculator`: Corrected `IntegralInput` parameter ordering (`new IntegralInput(time, input.TimeStep)`).
### Removed
- `MelloSilveiraTools.WebApi.Application.Operations.*`: Removed legacy `OperationBase`, `OperationRequestBase`, `OperationResponse`, and old operation classes.
- `MelloSilveiraTools.WebApi.ExtensionMethods.OperationResponseExtensions` and `ResultExtensions`.
- `MelloSilveiraTools.MechanicsOfMaterials.Calculators.LoadSharing.*` and associated models.
- `DifferentialEquationMethodFactory` in `MelloSilveiraTools.Mathematics` (differential equation solvers are now resolved via Keyed DI or direct dependency injection).
- `MelloSilveiraTools.Core.ExtensionMethods.DoubleExtensions` — the canonical implementation now lives at `MelloSilveiraTools.Mathematics.Extensions.DoubleExtensions`. Consumers that imported the Core variant must add a reference to `MelloSilveiraTools.Mathematics` and update the `using` directive.
- **Experimental Data Pipeline cleanup:** Removed obsolete properties `OutputTypeName`, `InitialTime`, `InitialStrain`, `InitialStress`, `FinalTime`, `FinalStrain`, `FinalStress`, `DeltaStrain`, `DeltaStress`, `DeltaStrainPercentage`, `DeltaStressPercentage`, and `HasReachedAsymptote` from `MechanicalModelSimulationEntity`.
- Removed `Asymptote` complex type from `MechanicalModelSimulationOutput`, replaced entirely by `AsymptoteTime`.
- Removed obsolete `EvaluateConstraintsAndPenalties` delegate from `CurveFitInput` and `MathematicalCurveFitInput` in favor of short-circuit boolean rule validation.
- Removed orphaned dead code struct `CurveSegmentBuilderInput`.

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
