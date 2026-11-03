# Decisions and Scientific Traceability Ledger

## Execution: Smoke Test Validation
- **Date**: 2026-10-06T01:00:06Z
- **Task IDs**: `smoke-test-task-1`, `smoke-test`
- **What**: Successful execution of the Biomechanics Research ecosystem smoke test using Docker.
- **From where**: Executed by `senior-backend-software-engineer-net` (`docker-compose`) and `experimental-data-analyst` (`docker run`).
- **Why**: To validate the infrastructure setup, ensuring isolated and reproducible execution via Docker, thereby complying with the absolute prohibition of non-containerized (local) installations and tool executions.
- **Category**: Execution

### 2026-10-06 Task: baseline-style-guide-and-previous-cycle
**Category**: Decision & Execution

#### Style Guide (Decision)
- **What**: Established standard Style Guide for the research ecosystem.
  - **Tone**: Academic, impersonal, fluid, rigorous, and with impeccable logical linking.
  - **Methodological Approach**: Emphasizes strict geometric and kinematic boundary conditions. Acknowledges limitations openly.
  - **Mathematical Exposition**: Defines all variables immediately after equations. Addresses singularities explicitly. Moves heavy deductions to appendices.
  - **Terminology**: Employs rigorous continuum mechanics and viscoelastic terminology.
  - **Formatting**: Uses `$...$` and `$$...$$`. Limits significant figures in descriptive text.
- **From where**: Extracted by the Academic Writer from the user's baseline paper `Texto base para revista_Schapery.docx`.
- **Why**: To ensure all future research outputs and written texts strictly respect this foundation and maintain high editorial and scientific standards.

#### Previous Cycle Baseline (Execution)
- **What**: Registered the achievements and findings of the previous cycle.
  - **Methodology**: Developed a 1D non-linear viscoelastic load-sharing framework for porcine knee ligaments based on Schapery’s theory. Evaluated 81 combinatorial joint scenarios under a 75 N step-like vertical distraction, comparing 0% and 3% initial pre-strain.
  - **Results**: Demonstrated viscoelasticity reduces joint load capacity over time. 0% pre-strain dropped to 43.07 N; 3% pre-strain mitigated decay to 60.48 N. MCL experienced most relative force reduction.
- **From where**: Extracted by the Academic Writer from the user's baseline paper `Texto base para revista_Schapery.docx`.
- **Why**: To serve as the state-of-the-art reference and methodological starting point for the next iterations of the study.

### 2026-10-06 Task: methodological-directive-evidence-funnel
**Category**: Decision (Methodological Directive for Deep Research)

#### Evidence Funnel Approach
- **What**: Established the "Evidence Funnel" approach for theoretical research and literature reviews. The process strictly follows:
  1. **Broad search**: Identify which constitutive models the state-of-the-art uses to predict soft tissue mechanical behavior in general.
  2. **Narrowing**: Focus specifically on knee ligaments.
  3. **Computational Analysis**: Investigate which software and computational technologies are being used for these simulations.
  4. **Specific validation**: Look at how specific target tools (e.g., FEBio) fit into that landscape.
- **From where**: User directive (received 2026-10-06).
- **Why**: To avoid confirmation bias in literature reviews (i.e., preventing searches that only target pre-chosen tools or models).

### 2026-10-06 Task: theoretical-advance-3d-continuum-febio
**Category**: Decision & Future Work

#### Transition to 3D Continuum Mechanics (Decision)
- **What**: Expanding the 1D baseline modeling into a 3D Tensorial Continuum Mechanics framework.
- **From where**: Biomechanics Co-advisor.
- **Why**: 1D models are insufficient for capturing 3D behavior. 3D tensorial generalizations provide the necessary rigor.

#### Action Plan for Theoretical Advance
- **What**: Formulate the 3D tensorial generalization of the existing 1D models (defining Helmholtz free energy and Cauchy stress tensor) to run in a 3D open-source FEA software.
- **From where**: Biomechanics Co-advisor.
- **Why**: To bridge the gap between 1D simplifications and full 3D continuum mechanics required by advanced models.

#### Mandatory Human-in-the-Loop QA Gate for Automated Parsing
- **What**: For any automated text generation or parsing loop (e.g., Vision LaTeX extraction), the system is FORBIDDEN from writing directly to the final markdown file. 
  1. The agent must process only a small batch (short loop).
  2. The output must be saved to a temporary file (e.g., draft.md).
  3. The system MUST halt execution and explicitly prompt the human user to validate the temporary file.
  4. Only after the user expressly approves the content will the changes be migrated and merged into the final target file.
- **From where**: User directive after observing LLM loop fatigue and lazy scripting behaviors.
- **Why**: To prevent data corruption, AI hallucinations, "lazy shortcut" scripting, and unnecessary token burn. Ensures absolute human control over the final repository state.

### 2026-10-06 Task: safeguard-anti-hallucination-guardrails
**Category**: Decision (System Safeguards against LLM Fatigue & Shortcuts)

#### The 4 Pillars of Bulk Processing Defense
To ensure data integrity and prevent runaway token consumption during automated text/math parsing, the ecosystem is bound by these 4 interconnected guardrails:
1. **Architectural Guardrail (Short Loops & Mandatory Validation)**: Massive batching is strictly forbidden. 
2. **Deterministic QA Gate (Draft Verification Script)**: A deterministic Python script will inspect drafts for valid mathematical syntax before presenting to humans.
3. **Anti-Laziness Prompt Clause**: Vision and Parsing Workers are injected with a strict cognitive penalty clause prohibiting blind regex wrapping.
4. **Token Budgeting & Circuit Breaker**: Orchestrator terminates agents bypassing the "short loop" rule.
- **From where**: User directive following the failure of blind bulk processing in Chapter 7.
- **Why**: To lock the system into a deterministic workflow that prevents AI hallucinations.

## [2026-10-07] - Deadline Restructuring and Focus on Uniaxial Tension
**Category:** Decision (Scope Reduction)
- **What:** Focus exclusively on isolated uniaxial loading for the 4 porcine knee ligaments.
- **From where:** Researcher / Human directive.
- **Why:** Full-knee 3D simulations with complex articular loading present a high risk of non-convergence in a 12-month timeframe. Complex kinematics are moved to post-thesis future works.

## [2026-10-07] - The 5 Guiding Constitutive Models
**Category:** Decision (Methodological Baseline)
- **What:** Selected 5 models to guide the initial research development:
  1. *Fung QLV* (Historical gold-standard)
  2. *Schapery Tensorial* (Mathematical advance)
  3. *Generalized Maxwell / Prony Series* (Industry standard)
  4. *Weiss / Transversely Isotropic* (Hyperelastic anatomical baseline)
  5. *Poroviscoelasticity* (Fluid-solid interaction)
- **From where:** Prior preliminary analyses by the research group.
- **Why:** To serve as a guiding hypothesis for the computational development. *Note: These are not set in stone and may oscillate or be altered as new information emerges from the evidence funnel.*

## [2026-10-07] - Computational Architecture Pivot (Backend/Frontend and C# Engine)
**Category:** Decision (Software Architecture)
- **What:** The C# engine (`MelloSilveiraTools`) will be restricted to scalar (1D) curve fitting and data orchestration. 3D tensorial simulations will be delegated to open-source FEA software (priority: FEBio). The architecture will orchestrate 3 data sources: experimental, 1D C#, and 3D FEBio.
- **From where:** Researcher / Human directive.
- **Why:** To automate and accelerate research. Expanding C# to 3D tensors (meshes) would consume critical time, missing the 12-month deadline. C# serves best as an orchestrator and 1D baseline, leveraging the researcher's existing fluency.

## [2026-10-07] - FEBio as the Primary 3D FEA Tool
**Category:** Decision (Tooling)
- **What:** Preferential selection of FEBio over other software.
- **From where:** Prior preliminary analyses.
- **Why:** FEBio is free, open-source, and specifically tailored for biomechanics (supporting soft tissues natively). *Note: Like the models, this is a guiding hypothesis that can change if future evidence requires.*

## [2026-10-07] - Strategy for Missing Native FEA Models
**Category:** Decision (Computational Implementation)
- **What:** Any constitutive model lacking native support in the chosen 3D FEA software will be custom-implemented.
- **From where:** Researcher / Human directive.
- **Why:** To ensure all 5 hypothesized models can be compared. The ecosystem will compile User-Defined Material (UDM) plugins (e.g., in C++) to inject the custom 3D tensorial math into the solver.

## Ledger Entry: 2026-10-08

### Decision & Execution
* **Task_ID:** task-1.1
* **Date:** 2026-10-08
* **What:** Selection of primary constitutive models for soft tissue mechanics, specifically hyperelastic and viscoelastic models (Fung QLV, Mooney-Rivlin, Neo-Hookean, Ogden, Holzapfel-Gasser-Ogden).
* **From where:** senior-applied-biomechanics-researcher-deep-research (Evidence includes DOIs: 10.1115/1.2834307, 10.1098/rsif.2015.0486, 10.1111/j.1475-1305.2006.00257.x, etc.)
* **Why:** These models are chosen because they map well to the hierarchical structure of soft tissues like ligaments and tendons, representing large deformations, hysteresis, non-linear, time-dependent, and anisotropic behaviors.

### Decision & Execution
* **Task_ID:** task-1.2
* **Date:** 2026-10-08
* **What:** Application of Hyperelastic and viscoelastic constitutive models for knee ligaments.
* **From where:** senior-applied-biomechanics-researcher-deep-research (Evidence includes DOIs: 10.1038/s41598-018-20739-w, 10.3389/fbioe.2014.00054, 10.1016/s0736-0266(03)00113-x, etc.)
* **Why:** They correctly model the inherent nonlinearity, anisotropy (due to collagen bundles), and time-dependent behaviors (such as creep, hysteresis, and stress relaxation) under physiological and supra-physiological loads.

### Decision & Execution
* **Task_ID:** task-1.3
* **Date:** 2026-10-08
* **What:** Selection of FEBio as the core finite element solver instead of general-purpose solvers like Abaqus and Ansys.
* **From where:** senior-applied-biomechanics-researcher-deep-research (Evidence includes DOIs: 10.1007/s10439-022-03074-0, 10.1111/os.13980, etc.)
* **Why:** FEBio natively handles complex biphasic, multiphasic, and poroelastic materials essential for hydrated soft tissues. As open-source, it allows complete transparency, rigorous validation, and custom plugins for new constitutive models without licensing constraints.

### Opportunity/Gap & Future Work
* **Task_ID:** task-1.1, task-1.2, task-1.3
* **Date:** 2026-10-08
* **What:** Implementation of FEBio UDM (User Defined Material) and converting 1D constants to 3D models.
* **From where:** Traceability QA identification / human input logic based on limitations of standard models in 3D settings.
* **Why:** To fully leverage FEBio's open-source architecture by implementing custom plugins for specific constitutive models, moving beyond simple 1D constants to accurately model 3D biomechanical behaviors.

### Execution & Decision
* **Task_ID:** task-2.1
* **Date:** 2026-10-08
* **What:** Validated and maintained Fung QLV 1D model objective function logic using inline lambdas in FungRelaxationOnlyCurveFitterStep.cs.
* **From where:** senior-backend-software-engineer-net (File: .agents\soft-tissue-biomechanics\outputs\task-2.1-fung-analytical-detailing.md)
* **Why:** To strictly adhere to the "Do Not Reinvent the Wheel" directive and maintain architectural consistency across the MelloSilveiraTools ecosystem. Avoided creating redundant custom ObjectiveFunction classes, ensuring a unified approach to curve-fitting constraints.
### 2026-10-08 Task: roadmap-revision-phase1-findings
**Category**: Decision & Future Work

#### Inclusion of Holzapfel-Gasser-Ogden (HGO) Model
- **What**: Added the HGO structural model to the thesis scope (Tasks 1.9, 2.6, 4.2).
- **From where**: Outputs of deep research Tasks 1.1 and 1.2.
- **Why**: The literature strongly supports that soft tissues (especially knee ligaments) require anisotropic and structural constitutive models to account for collagen fiber dispersion in a ground matrix. Purely phenomenological polynomial models (Mooney-Rivlin, Neo-Hookean) were deemed insufficient, whereas HGO is a direct biological fit and is highly supported by modern FEA solvers like FEBio.

#### Validation of FEBio and Poroviscoelasticity
- **What**: Confirmed the selection of FEBio and the inclusion of Poroviscoelasticity in the roadmap.
- **From where**: Outputs of deep research Task 1.3.
- **Why**: FEBio natively supports complex biphasic and poroelastic materials critical for hydrated tissues. Its open-source nature allows for writing custom User-Defined Materials (UDMs) in C++ without licensing constraints, validating Task 3.4 of the roadmap.

## Ledger Entry: 2026-10-13

### Decision & Execution
* **Task_ID:** task-2.2
* **Date:** 2026-10-13
* **What:** Validated, executed, and formally documented the objective functions for the Schapery Non-Linear 1D model within MelloSilveiraTools (SchaperyRelaxationOnlyCurveFitterStep.cs).
* **From where:** senior-backend-software-engineer-net (File: outputs/task-2.2-result.json)
* **Why:** In Schapery's non-linear formulation, decoupling the linear viscoelastic baseline ($G_e, g_1, n$) at the anchor segment from the non-linear Helmholtz multipliers ($h_e, h_2$) at higher strain segments guarantees numerical stability, eliminates parameter correlation, and permits robust continuous function fitting (Logarithmic vs Exponential) for the academic writer.

## Ledger Entry: 2026-10-14

### Decision & Execution
* **Task_ID:** task-2.7
* **Date:** 2026-10-14
* **What:** Developed, integrated, and validated the native 1D Sensitivity Analysis Engine (OAAT) in MelloSilveiraTools via TPL Dataflow (`ParametricSweepPipeline`), covering Fung QLV, Simplified Fung, and Schapery models across all 4 mandatory sensitivity scenarios (stress at $t_0$, stress at $t_f$, asymptote time $t_{\text{asymptote}}$, and stress variation $\Delta\sigma$).
* **From where:** senior-backend-software-engineer-net (File: outputs/task-2.7-result.md)
* **Why:** In compliance with the experimental-data-analyst skill directive and thesis roadmap, the engine provides an automated, reactive, and non-blocking pipeline for evaluating parametric sensitivity. Dynamically compiles parameter adaptations in `MechanicalModelTypeCache` to bridge generic simulation envelopes (`GenericMechanicalModelInput`) with strongly typed constitutive calculators without reflection overhead. Output is serialized into partitioned CSVs for downstream academic visualization and thesis reporting.

## Ledger Entry: 2026-10-16

### Decision (Pre-Execution Strategy QA Gate Approved)
* **Task_ID:** task-storage-api-pre-qa
* **Date:** 2026-10-16T10:33:02Z
* **Category:** Decision
* **What:** Approved the 6-phase architectural and implementation plan for Agnostic File Storage, Pipeline Persistence, Real-Time Progress Streaming, and Web API Exposure:
  1. **Phase 1 & 2 (Agnostic File Storage & Strict SRP in CSV Writer Steps):** `IFileStorageService` (registered via Keyed DI with `StorageProvider` enum keys and default resolution to `StorageProvider.Local`) is the sole component responsible for resolving storage paths (`{BaseDirectory}/{context}/{identifier}/{fileName}`), opening read/write `Stream` instances, and providing `FileMetadata`. `CsvFileWriter(Stream stream, string header)` strictly formats and writes CSV content to a stream without path awareness or row counting. Legacy `IFileManager`, `FileManager`, `FileData`, `RowCount`, and `Checksum` are strictly prohibited. User-supplied output directories (`OutputFileUri`, `OutputDirectory`) and manual path resolution (`ResolveLocation`) are removed from `ExperimentalDataProcessingInput`, `ParametricSweepInput`, `ExperimentalDataFileWriterStep`, `MechanicalModelOutputFileWriterStep`, and `ParametricSweepCsvWriterStep`.
  2. **Phase 3 (Database Persistence in Pipelines):** `ExperimentalDataProcessingPipeline` persists `ExternalIdentifier` in `MechanicalModelSimulationEntity` and records both experimental segmented CSVs and numerical simulation CSVs in `stored_files` (`StoredFileEntity`). `ParametricSweepPipeline` introduces `ParametricSweepEntity` (`parametric_sweeps` table, storing `BaselineParametersJson` and `VariableRangesJson` serialized with `OptimizationJsonOptions.SignificantFigures7`) and a terminal `ParametricSweepPersistenceStep` that persists both the sweep entity and all generated CSV files in `stored_files` (`Context = "SensitivityAnalysis"`).
  3. **Phase 4 (Real-Time Pipeline Progress Notification):** Introduces `PipelineStepState` (`Started`, `Running`, `Completed`, `Faulted`) and `PipelineProgressEvent` in `MelloSilveiraTools.Core.Pipelines`, integrating `IProgress<PipelineProgressEvent>?` into the pipelines to emit real-time step lifecycle telemetry.
  4. **Phase 5 (Web API Commands, Validators, and Streaming Endpoints):** Implements `ProcessExperimentalDataCommand`, `ExecuteParametricSweepCommand` (pure JSON DTO without `IFormFile` or output directory), and aligns `FitCurveCommand` inheriting from `CommandBaseWithData<TRequest, TResponse>`. Exposes standard REST `POST` endpoints (`Result<TResponse>`), real-time streaming endpoints (`POST .../stream` via `WriteNdjsonAsync` / SSE), and file download endpoints backed by `StoredFileEntity` and `IFileStorageService.OpenRead`.
  5. **Phase 6 (Unit & Integration Testing):** Comprehensive unit test suite covering the refactored CSV writer steps, `ParametricSweepPersistenceStep`, commands, validators, and real-time progress notifications with zero build errors.
* **From where:** Human user directive and architectural consolidation (`c:\Pessoal\Projetos\Tools\.agents\prompts\experimental-data-and-storage-api-plan.md`), audited by `documentation-and-tracking-secretary` (`task-storage-api-pre-qa`).
* **Why:** To enforce strict Single Responsibility Principle (SRP) between storage location resolution (`IFileStorageService`) and stream writing (`CsvFileWriter`), prevent directory traversal or arbitrary path injection by governing output paths exclusively within the application, ensure full relational traceability of generated artifacts (`StoredFileEntity`, `ParametricSweepEntity`), and provide non-blocking real-time execution visibility to Web API consumers while reusing existing `MelloSilveiraTools` abstractions (`CommandBaseWithData`, TPL Dataflow, `IRepository`, and `WriteNdjsonAsync`).


### Execution (Phase 2 & Phase 3: Agnostic CSV Writer Steps & Pipeline Database Persistence)
* **Task_ID:** task-storage-api-phase2-3
* **Date:** 2026-10-16T11:04:00Z
* **Category:** Execution
* **What:** Executed Phase 2 and Phase 3 of the Agnostic Storage and Pipeline Persistence plan:
  1. **Phase 2 (CSV Writer Steps & Input Alignment):** Removed `ExperimentalDataProcessingInput.OutputFileUri` and `ParametricSweepInput.OutputDirectory`. Standardized `ExperimentalDataFileWriterStep`, `MechanicalModelOutputFileWriterStep`, and `ParametricSweepCsvWriterStep` with primary constructors accepting `IFileStorageService` and `identifier`/`uniqueIdentifier`, eliminating `ResolveLocation` and legacy path properties while exposing `FileMetadata` / `GeneratedFilesMetadata`.
  2. **Phase 3 (Database Persistence in Pipelines):** Updated `ExperimentalDataProcessingPipeline` so `MechanicalModelSimulationEntity` stores `ExternalIdentifier` and persists `StoredFileEntity` records (`stored_files` table) for both raw processed data (`ExperimentalDataRawProcessed`) and numerical simulation outputs (`ExperimentalDataSimulation`) after stream disposal/flushing. Created `ParametricSweepEntity` (`[Table("parametric_sweeps")]`), `ParametricSweepPersistenceInput`, and `ParametricSweepPersistenceStep` in `ParametricSweepPipeline` to persist sweep metadata (`BaselineParametersJson` and `VariableRangesJson` serialized via `OptimizationJsonOptions.SignificantFigures7`) and all partitioned sensitivity CSV files in `stored_files` (`Context = "SensitivityAnalysis"`). Verified zero compilation errors in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.csproj`.
* **From where:** Executed by `senior-backend-software-engineer-net` (Delivery: `.agents/soft-tissue-biomechanics/outputs/task-storage-api-phase2-3.json`), audited and approved by `documentation-and-tracking-secretary` (`task-storage-api-phase2-3-qa`).
* **Why:** To enforce strict Single Responsibility Principle (SRP) by delegating all path resolution to `IFileStorageService`, guarantee accurate OS file size (`SizeBytes`) recording by flushing writer streams prior to extracting `FileMetadata`, and establish relational database traceability (`stored_files` and `parametric_sweeps`) with 7-significant-figure polymorphic JSON serialization for downstream reproducibility.


### Execution (Phase 4 & Phase 5: Real-Time Pipeline Progress Notification & Web API Architecture)
* **Task_ID:** task-storage-api-phase4-5
* **Date:** 2026-10-16T12:35:00Z
* **Category:** Execution
* **What:** Executed and integrated Phase 4 and Phase 5 of the Agnostic Storage and Web API Architecture:
  1. **Phase 4 (Real-Time Pipeline Progress Notification):**
     - Introduced `PipelineStepState` (`Started`, `Running`, `Completed`, `Faulted`) and `PipelineProgressEvent` in `MelloSilveiraTools.Core.Pipelines.Models`.
     - Integrated `IProgress<PipelineProgressEvent>?` parameter into `PipelineFactory.StartDataflow`, `DataflowPipelineBuilder`, and `TelemetryExtensions`, enabling automated real-time lifecycle event emissions during synchronous, asynchronous, and enumerable step executions without cluttering business logic or compromising OpenTelemetry traces.
     - Added `IProgress<PipelineProgressEvent>? Progress` property to `ExperimentalDataProcessingInput` and `ParametricSweepInput` (annotated with `[JsonIgnore]`) to propagate execution progress down both TPL Dataflow processing pipelines.
  2. **Phase 5 (Application Commands, Validators, and Web API Controllers):**
     - Implemented application commands inheriting from `CommandBaseWithData<TRequest, TResponse>`: `ProcessExperimentalDataCommand`, `ExecuteParametricSweepCommand`, and refactored `FitCurve`.
     - Implemented Fluent / contract validators: `ProcessExperimentalDataValidator`, `ExecuteParametricSweepValidator`, and `FitCurveValidator` implementing `IValidator<TRequest>`.
     - Added `StoredFileFilter` in `MelloSilveiraTools.Database.RelationalDatabase.Models.Filters` for querying `StoredFileEntity` records by `ExternalIdentifier`, `Context`, and `FileName`.
     - Implemented Web API Controllers in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers`:
       - `ExperimentalDataController` (`api/v1/experimental-data`): Traditional `POST` execution returning `ProcessExperimentalDataResponse`, streaming NDJSON `POST .../stream` (`application/x-ndjson`) via `PipelineProgressStreamWriter`, file listing (`GET .../files`), and file download (`GET .../files/{fileId}/download`) via `IFileStorageService.OpenRead`.
       - `ParametricSweepController` (`api/v1/parametric-sweeps`): Traditional `POST` execution returning `ExecuteParametricSweepResponse`, streaming NDJSON `POST .../stream` via `PipelineProgressStreamWriter`, file listing (`GET .../files`), and file download (`GET .../files/{fileId}/download`) via `IFileStorageService.OpenRead`.
       - `CurveFittingController` (`api/v1/curve-fitting`): Dedicated curve fitting endpoint executing `FitCurve`.
       - `StoredFilesController` (`api/v1/stored-files`): Unified query (`GET .../{externalIdentifier}`) and stream download (`GET .../{externalIdentifier}/download` or `GET .../{externalIdentifier}/{fileId}/download`) across all domain contexts.
     - Implemented `PipelineProgressStreamWriter`: Decoupled `System.Threading.Channels.Channel<PipelineProgressEvent>` bridge executing long-running pipeline commands concurrently while streaming UTF-8 newline-delimited JSON events to the HTTP client with immediate flushes and `X-Accel-Buffering: no`.
     - Registered all commands, validators, and `IParametricSweepPipeline` in `DependencyInjection.cs`.
     - Verified clean compilation with zero errors across all projects including `UnitTests`.
* **From where:** Executed by `senior-backend-software-engineer-net` (Delivery: `.agents/soft-tissue-biomechanics/outputs/task-storage-api-phase4-5.json`), audited and approved by `documentation-and-tracking-secretary` (`task-storage-api-phase4-5-qa`).
* **Why:** To fulfill the real-time execution visibility and REST API interaction requirements of the biomechanics computational platform. Providing NDJSON streaming (`POST .../stream`) prevents HTTP connection timeouts during computationally intensive simulations (such as non-linear viscoelastic optimization and full factorial parametric sweeps) and feeds frontend progress bars without polling overhead. Decoupling file downloads via `StoredFileEntity` and `IFileStorageService.OpenRead` prevents direct filesystem access by clients and enforces strict path encapsulation.


### Execution (Phase 6: Unit Testing Suite & Polly Resilience Hardening)
* **Task_ID:** task-storage-api-phase6
* **Date:** 2026-10-16T12:50:00Z
* **Category:** Execution
* **What:** Developed, verified, and executed the comprehensive unit test suite covering all agnostic file storage, pipeline persistence, real-time progress events, application commands, and validators across `test/UnitTests`:
  1. **Storage & Persistence Step Tests:**
     - `ParametricSweepPersistenceStepTests`: Validated database persistence of `ParametricSweepEntity` (table `parametric_sweeps`) and associated `StoredFileEntity` records (`Context = "SensitivityAnalysis"`, `StorageProvider.Local`, `ContentType = "text/csv"`), verifying conflict handling on pre-existing records, exception propagation on repository failures, and proper `DisposeAsync` handling.
     - `CsvWriterStepsTests`: Verified `ExperimentalDataFileWriterStep`, `MechanicalModelOutputFileWriterStep`, and `ParametricSweepCsvWriterStep` using `IFileStorageService` mocks, confirming stream opening under proper contexts (`ExperimentalData`, `SensitivityAnalysis`) and correct metadata emission (`FileMetadata`, `GeneratedFilesMetadata`).
  2. **Application Commands & Validators Tests:**
     - `ProcessExperimentalDataCommandTests` & `ProcessExperimentalDataValidatorTests`: Verified validation boundaries (model name, raw streams, target $R^2$, non-negative thresholds) and command orchestration (validation failure short-circuiting, pipeline delegation, and deterministic `Stream` / `FileStream` disposal in `finally` blocks).
     - `ExecuteParametricSweepCommandTests` & `ExecuteParametricSweepValidatorTests`: Validated sweep request parameters (model name, baseline parameters, positive time step, positive asymptote tolerance, sweep range presence) and pipeline execution delegation.
     - `FitCurveCommandTests` & `FitCurveValidatorTests`: Validated curve fitting requests, step factory instantiation, async stream execution, and parameter group / `FinalError` aggregation.
  3. **Real-Time Pipeline Progress Events & Telemetry Tests:**
     - `PipelineProgressEventTests` & `PipelineProgressEmissionTests`: Validated record immutability, `PipelineStepState` enum states, and real-time event reporting via `IProgress<PipelineProgressEvent>` across synchronous execution (`HandleExecution`), asynchronous Polly retry pipelines, asynchronous streaming (`IAsyncEnumerable`), and complete TPL Dataflow topologies (`PipelineFactory.StartDataflow`).
  4. **Polly Resilience Hardening:**
     - Configured safe non-zero defaults in `ResiliencePipelineSettings` (`MaxRetryAttempts = 3`, `DelayInMilliseconds = 200`, `BackoffType = Constant`, `UseJitter = true`) and added fallback guards in `DefaultResiliencePipeline` to prevent Polly v8 configuration validation exceptions when default settings are supplied.
  5. **Verification & Test Execution:**
     - All 362 unit tests passed with 100% success rate (362 passed, 0 failed, 0 skipped).
     - Solution compiled with 0 errors.
     - Updated `CHANGELOG.md` and formulated proposed `AGENTS.md` updates for human approval.
* **From where:** Executed by `senior-backend-software-engineer-net` (Delivery: `.agents/soft-tissue-biomechanics/outputs/task-storage-api-phase6.json`), audited and approved by `documentation-and-tracking-secretary` (`task-storage-api-phase6-qa`).
* **Why:** To ensure 100% test coverage and regression protection for the refactored agnostic file storage architecture, database pipeline persistence steps, application command contracts, and real-time progress notification infrastructure before production deployment, while adhering to the strict rule prohibiting direct edits to `AGENTS.md` without explicit human authorization.


### Execution (FitCurveCommand Adaptation for Segmented Multipart File Uploads)
* **Task_ID:** task-fitcurve-command-adaptation
* **Date:** 2026-10-17T22:25:00Z
* **Category:** Execution
* **What:** Adapted `FitCurveCommand` and associated Web API infrastructure to support multipart CSV uploads grouped by deformation segment type:
  1. **DTO & Request Refactoring:**
     - Created `SegmentFilesGroup` record (`SegmentType SegmentType`, `IReadOnlyList<IFormFile> Files`) in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands`, equipped with a parameterless constructor to support ASP.NET Core multipart form model binding.
     - Updated `FitCurveRequest` to receive `IReadOnlyList<SegmentFilesGroup> SegmentFiles`, replacing pre-parsed `Segments` collections.
  2. **Command & Batch Draining:**
     - Refactored `FitCurveCommand` to inherit from `ListedCommandBase<FitCurveRequest, MechanicalModelCurveFitOutput>`.
     - Streamed uploaded CSV files using `CsvStreamReader` (parsing numeric spans for Time, Strain, Stress without intermediate string allocations) into `CurveSegment` objects.
     - Emitted a sentinel segment with `SegmentType.Unknown` after exhausting all input files to drain stateful batch fitters (such as `SchaperyRelaxationOnlyCurveFitterStep` multi-stage identification).
     - Eliminated obsolete DTOs `FitCurveResultData` and `ParameterGroupResultData`, returning canonical `ListedResult<MechanicalModelCurveFitOutput>` via `Result.CreateListedSuccessOk(outputs)` without manual error accumulation.
  3. **Validation & Controller Integration:**
     - Updated `FitCurveValidator` to enforce valid non-empty `SegmentFilesGroup` collections, valid `SegmentType` values (rejecting `SegmentType.Unknown` on input), and non-empty files.
     - Configured `CurveFittingController` with `[FromForm]` parameter binding and `ActionResult<ListedResult<MechanicalModelCurveFitOutput>>` responses.
  4. **Verification & Test Execution:**
     - Verified 370/370 unit tests passing (100% success, 0 failed, 0 skipped).
     - Clean solution compilation with 0 errors.
     - Updated `CHANGELOG.md` under `[UNRELEASED]`.
* **From where:** Executed by `senior-backend-software-engineer-net` (Delivery: `.agents/soft-tissue-biomechanics/outputs/task-fitcurve-command-adaptation.json`), audited and approved by `documentation-and-tracking-secretary` (`task-fitcurve-command-adaptation-qa`).
* **Why:** To enable direct multipart file uploads of experimental deformation phases (`SegmentType`) from web clients and laboratory instruments without requiring client-side pre-parsing. Low-allocation CSV reading via `CsvStreamReader` and UTF8Parser ensures high throughput, while the sentinel `SegmentType.Unknown` guarantees complete drainage of multi-segment relaxation fitters before wrapping results in the standard monad `ListedResult<MechanicalModelCurveFitOutput>`.


### Execution (IFileStorageService Simplification: Partitioned Write Operations)
* **Task_ID:** task-storage-write-simplification
* **Date:** 2026-10-17T23:06:00Z
* **Category:** Execution
* **What:** Refactored `IFileStorageService` and `LocalFileStorageService` to expose exclusively two write methods:
  1. `Stream OpenWrite(string context, string identifier, string fileName)`: Opens a stream for writing to a deterministic partition-based file path.
  2. `Stream OpenTimebasedWrite(string context, string identifier, string filePrefix, string fileExtension)`: Opens a stream for writing to a timestamp-based partition-based file path (`{filePrefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{fileExtension}`).
  - Removed obsolete direct path overload `Stream OpenWrite(string location)`.
  - Added unit test `OpenTimebasedWrite_ShouldCreateTimestampedFileAndStreamContent` in `LocalFileStorageServiceTests.cs`.
  - Verified 100% test pass rate across all 371 unit tests in `UnitTests.csproj` and 0 solution build errors.
* **From where:** Executed by `senior-backend-software-engineer-net`, audited and approved by `documentation-and-tracking-secretary`.
* **Why:** To eliminate raw path bypasses and strictly enforce domain partitioning by `context` and `identifier` across all file writing operations, while preserving flexible support for both deterministic and timestamped file lifecycles.


### Execution (IFileStorageService Create/CreateTimebased and Optional Prefix)
* **Task_ID:** task-storage-create-and-optional-prefix
* **Date:** 2026-10-17T23:15:00Z
* **Category:** Execution
* **What:** Adapted `IFileStorageService` and `LocalFileStorageService`:
  1. Made `filePrefix` optional in `ResolveTimebasedPath` and `CreateTimebased` (`string? filePrefix = null`), omitting prefix and leading underscore when null or empty (`{timestamp}{fileExtension}`).
  2. Renamed write operations from `Open...` to `Create...` and updated return types to include both `Stream` and `FileMetadata`:
     - `(Stream Stream, FileMetadata Metadata) Create(string context, string identifier, string fileName)`
     - `(Stream Stream, FileMetadata Metadata) CreateTimebased(string context, string identifier, string fileExtension, string? filePrefix = null)`
  3. Propagated signature changes across consumer pipeline steps (`ExperimentalDataFileWriterStep`, `MechanicalModelOutputFileWriterStep`, `ParametricSweepCsvWriterStep`) and unit tests (`LocalFileStorageServiceTests`, `CsvFileWriterTests`).
  4. Verified 100% test pass rate (371/371 passed) and 0 errors across the solution.
* **From where:** User directive, implemented and verified in `MelloSilveiraTools.Core` and `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
* **Why:** To make initial file metadata (path, directory, filename) immediately accessible upon stream creation and simplify time-based file generation when an explicit prefix is not needed.


## Ledger Entry: 2026-10-27

### Decision (Pre-Execution Strategy QA Gate Approved: Phased Mutex Parallel Orchestration)
* **Task_ID:** task-orchestration-plan-and-pre-qa-gate
* **Date:** 2026-10-27
* **Category:** Decision
* **What:** Formally evaluated and approved the "Phased Mutex Strategy" (Esteira Desacoplada) for parallel execution of two major architectural plans:
  1. *Cross-Platform & Multi-Architecture (.NET 10) Portability Audit & Remediation Plan* (`.agents/prompts/cross-platform-portability-audit-and-remediation.md`).
  2. *Staff Technical Plan: Biomechanics Processing Pipelines, Topology Exposure, and Dynamic Notification* (`.agents/prompts/biomechanics-pipeline-and-sensitivity-plan.md`).
  - **Isolated Block 1A (Assigned to Senior Backend Engineer - Portability):** Zero-risk cross-platform hotfixes targeting `MelloSilveiraTools.Plugins` and `MelloSilveiraTools.Mathematics`:
    - Finding 01: Stream scoping and atomic rename in `JsonFilePluginCachePersistence.cs`.
    - Finding 05: Destination folder fix in `PluginFileProcessor.MoveToMainFolder`.
    - Finding 10: Invariant culture parsing (`CultureInfo.InvariantCulture`) in `Vector3D.cs`.
  - **Isolated Block 1B (Assigned to Senior Backend Engineer - Biomechanics):** Domain models definition in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models`:
    - `ParameterBound.cs`, `VariableRelationshipInput.cs`, `VariableCombinationPoint.cs`, and `VariableRelationshipOutput.cs`.
  - **Mutex Synchronization QA Gate:** Strict serialization and barrier synchronization enforced before any modifications to the shared critical core (`MelloSilveiraTools.Core`: `DataflowPipelineBuilder.cs`, `IDataflowPipeline.cs`, `CoreDependencyInjection.cs`, `MelloSilveiraTools.Core.csproj`), preventing merge conflicts, race conditions, and regressions.
* **From where:** Orchestrated and approved by `documentation-and-tracking-secretary` acting as Pre-Execution Strategy QA Gate, following consensus between the Human User and the Central Biomechanics Research Orchestrator.
* **Why:** Disjoint file set verification confirmed zero file overlap between Block 1A (`Plugins`, `Mathematics`) and Block 1B (`MechanicsOfMaterials.Optimizations`), completely eliminating concurrent modification hazards. Furthermore, the domain models in 1B conform to DDD conventions, C# 13 explicit typing (no `var`), primary constructors, and `*Output` calculation naming, while avoiding premature code reinvention. Protecting the shared `MelloSilveiraTools.Core` pipeline infrastructure behind a mutex gate ensures solution stability and deterministic build integrity.


### Execution: Zero-Risk Portability Hotfixes (Block 1A)
* **Task_ID:** task-portability-phase1-hotfixes
* **Date:** 2026-10-27
* **Category:** Execution
* **What:** Implemented and validated Phase 1 zero-risk cross-platform portability hotfixes across `MelloSilveiraTools.Plugins` and `MelloSilveiraTools.Mathematics`:
  1. **FINDING-01 (`JsonFilePluginCachePersistence.cs`):** Enclosed `Stream` and `Utf8JsonWriter` in explicit nested `await using` scopes within `SaveAsync` to guarantee deterministic flush and disposal ordering before file handle release, preventing Windows sharing lock violations (`ERROR_SHARING_VIOLATION`) and file locking latency.
  2. **FINDING-05 (`PluginFileProcessor.cs`):** Standardized `MoveToMainFolderAsync` destination routing to canonical `PluginStoragePartitions.Incoming` partition, preventing assembly isolation in the loaded partition and adhering to the storage-agnostic partition topology. Enriched method with complete XML documentation.
  3. **FINDING-10 (`Vector3D.cs` & `Vector3DTests.cs`):** Enforced `CultureInfo.InvariantCulture` across all coordinate parsing (`double.Parse` in `Parse` and `TryParse`) and standardized `ToString` formatting. Created comprehensive test suite in `test/UnitTests/Mathematics/Models/Vector3DTests.cs` validating culture invariance across comma-decimal locales (e.g., `pt-BR`, `de-DE`).
  4. Updated `CHANGELOG.md` under `[UNRELEASED]` with Phase 1 fixes. Verified 0 compilation errors across both modified packages.
* **From where:** Executed by `senior-backend-software-engineer-net` (Block 1A delivery: `.agents/soft-tissue-biomechanics/outputs/task-portability-phase1-hotfixes.json`), derived from the *Cross-Platform & Multi-Architecture Portability Audit Report* (`.agents/prompts/cross-platform-portability-audit-and-remediation.md`), audited and approved by `documentation-and-tracking-secretary` (`task-qa-ledger-phase1-deliveries`).
* **Why:** To eliminate Windows-specific file locking race conditions during dynamic plugin cache serialization, prevent partition drift in the plugin discovery lifecycle, and protect numerical calculations against locale-dependent string parsing corruption or format exceptions on systems using comma decimal separators.

### Execution: Domain Models & Notification Mode for Biomechanics Pipelines (Block 1B)
* **Task_ID:** task-biomechanics-phase3-models
* **Date:** 2026-10-27
* **Category:** Execution
* **What:** Defined domain models and notification mode types for biomechanics processing and variable relationship pipelines adhering strictly to .NET 10 / C# 13 conventions (explicit typing, primary constructors, comprehensive XML documentation, CRLF line endings):
  1. **`ProgressNotificationMode` (`MelloSilveiraTools.Core.Pipelines.Notifiers`):** Enum specifying execution progress delivery channels (`None = 0`, `StreamNdjson = 1`, `MessageQueue = 2`) for pipeline decoupling.
  2. **Domain Models (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models`):**
     - `ParameterBound`: Strongly-typed record encapsulating parameter boundaries (`LowerBound`, `UpperBound`) and discretization `Step`.
     - `VariableCombinationPoint`: Strongly-typed record encapsulating evaluated parameter combination coordinates, calculated stress (`CalculatedStress`), and difference residual (`Residual`).
     - `VariableRelationshipInput`: Strongly-typed request record encapsulating target stress (`TargetStress`), target time (`TargetTime`), relative tolerance (`ToleranceThreshold`), parameter bounds dictionary, and optional `NotificationMode`.
     - `VariableRelationshipOutput`: Strongly-typed calculation output record (`*Output`) encapsulating correlation identifier (`CorrelationId`), evaluated valid points collection, and CSV export metadata (`StorageEntryMetadata FileMetadata`).
  3. Documented additions in `CHANGELOG.md` under `[UNRELEASED]`. Verified 0 compilation errors in `MelloSilveiraTools.Core` and isolated compilation of newly introduced domain models.
* **From where:** Executed by `senior-backend-software-engineer-net` (Block 1B delivery: `.agents/soft-tissue-biomechanics/outputs/task-biomechanics-phase3-models.json`), derived from the *Staff Technical Plan: Biomechanics Processing Pipelines, Topology Exposure, and Dynamic Notification* (`.agents/prompts/biomechanics-pipeline-and-sensitivity-plan.md`), audited and approved by `documentation-and-tracking-secretary` (`task-qa-ledger-phase1-deliveries`).
* **Why:** To establish the foundational DDD value objects and data contracts required for multi-parameter sensitivity, variable relationship sweeps, and iso-stress surface generation in knee ligament constitutive modeling, while decoupling real-time progress transport (NDJSON vs MessageQueue) from pipeline execution logic.


## Ledger Entry: 2026-10-28

### Decision (Pre-Execution Strategy QA Gate Approved: Batch 2 Phased Mutex)
* **Task_ID:** task-pre-qa-gate-batch-2
* **Date:** 2026-10-28T01:00:00Z
* **Category:** Decision
* **What:** Formally evaluated, audited, and approved the Pre-Execution Strategy QA Gate for Batch 2 under the Phased Mutex Orchestration (Esteira Desacoplada):
  1. **Subtask 2A (Assigned to Agent 1 - Portability & Infrastructure):**
     - Target: BCL Cryptography Decoupling, Storage Sanitization, and Case-Insensitive Scanning (Findings 02 [partial], 06, 07, 08, 14).
     - Target Files:
       - `src/MelloSilveiraTools.Core/Services/Encryption/EncryptionService.cs` (migrate from `Microsoft.AspNetCore.Cryptography.KeyDerivation` to BCL `Rfc2898DeriveBytes.Pbkdf2`).
       - `src/MelloSilveiraTools.Core/Managers/File/LocalFileStorageService.cs` (path traversal sanitization, canonical path root verification, and `LastWriteTimeUtc`).
       - `src/MelloSilveiraTools.Core/Managers/File/FileStorageSettings.cs` (safe base directory resolution / container fallback).
       - `src/MelloSilveiraTools.Core/Logging/LoggerSettings.cs` (safe log directory resolution).
       - `src/MelloSilveiraTools.Plugins/Infrastructure/PluginFileProcessor.cs` (cross-platform case-insensitive enumeration via `MatchCasing.CaseInsensitive`).
  2. **Subtask 2B (Assigned to Agent 2 - Biomecânica):**
     - Target: Canonical `IPipeline<TIn, TOut>` Injection in Commands and Variable-Grouped CSV Partitioning for OAAT Sensitivity (Phase 2 of Biomechanics Plan).
     - Target Files:
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Commands/ProcessExperimentalDataCommand.cs` (consume canonical `IPipeline<ExperimentalDataProcessingInput, string>`).
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/SensitivityAnalyses/Commands/ExecuteParametricSweepCommand.cs` (consume canonical `IPipeline<ParametricSweepInput, string>`).
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/SensitivityAnalysis/Steps/ParametricSweepCsvWriterStep.cs` (group curve payloads by `VariableName` into partitioned `{VariableName}_SensitivityAnalysis.csv`).
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/DependencyInjection.cs` (register canonical `IPipeline<ExperimentalDataProcessingInput, string>` and `IPipeline<ParametricSweepInput, string>`).
  3. **Independence & QA Evaluation:**
     - Verified absolute disjointness of target file sets ($Files(2A) \cap Files(2B) = \emptyset$).
     - Confirmed package-level decoupling: 2A operates strictly on `MelloSilveiraTools.Core` and `MelloSilveiraTools.Plugins`, whereas 2B operates strictly on `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
     - Confirmed that neither subtask modifies the shared TPL Dataflow pipeline engine (`DataflowPipelineBuilder.cs`, `IDataflowPipeline.cs`, `PipelineFlowNode.cs`), keeping the critical core intact during this batch.
     - Confirmed zero code reinvention: 2A adopts standard BCL APIs (`Rfc2898DeriveBytes`, `EnumerationOptions`) and 2B converges on existing core pipeline abstractions (`IPipeline<,>`) and `CsvFileWriter`.
* **From where:** Evaluated and approved by `documentation-and-tracking-secretary` acting as Pre-Execution Strategy QA Gate, based on directives from the Central Biomechanics Research Orchestrator and user authorization for Phased Mutex execution.
* **Why:** To eliminate merge conflicts and concurrency race hazards between parallel development streams, enforce strict BCL security and cross-platform portability across Windows and Linux environments, eliminate redundant sub-interfaces (`IExperimentalDataProcessingPipeline`, `IParametricSweepPipeline`) in favor of canonical `IPipeline<TIn, TOut>`, and partition OAAT sensitivity curves by parameter to feed downstream academic analysis and thesis visualization without data corruption.


### Execution: Batch 2 - Security Hardening & Storage Portability (Subtask 2A)
* **Task_ID:** task-portability-phase2-security-storage
* **Date:** 2026-10-28
* **Category:** Execution
* **What:** Implemented security, storage sanitization, and cross-platform portability remediations across `MelloSilveiraTools.Core` and `MelloSilveiraTools.Plugins`:
  1. Migrated `EncryptionService` PBKDF2 key derivation from `Microsoft.AspNetCore.Cryptography.KeyDerivation` to BCL `System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2` [Finding 02].
  2. Implemented strict anti-path-traversal validation in `LocalFileStorageService` against traversal segments (`..`), path separators (`/`, `\`), control/dangerous characters, and Windows reserved device names (CON, PRN, AUX, NUL, COM1-9, LPT1-9) across identifier, context, and file names, validating containment via `Path.GetRelativePath` and canonical prefix checks [Finding 07].
  3. Added `FileStorageSettings.CanonicalizePath` and automatic canonicalization in `LocalFileStorageSettings` to resolve relative paths against a secure application data root (`Environment.SpecialFolder.LocalApplicationData` or `STORAGE_BASE_DIRECTORY`) instead of `System32` or root `/` [Finding 06].
  4. Configured `EnumerationOptions` with `MatchCasing.CaseInsensitive` in `LocalFileStorageService` (for directory segment resolution and file listing) and `PluginFileProcessor`, ensuring robust cross-platform directory matching on Linux case-sensitive filesystems (ext4) [Finding 08].
  5. Standardized `LocalFileStorageService` metadata timestamp extraction to use `LastWriteTimeUtc` instead of `CreationTimeUtc` to avoid birth time discrepancies in Linux POSIX/container overlayfs environments [Finding 14].
* **From where:** Executed by `senior-backend-software-engineer-net` (Delivery: `.agents/soft-tissue-biomechanics/outputs/task-portability-phase2-security-storage.json`), audited and approved by `documentation-and-tracking-secretary`.
* **Why:** Eliminates runtime dependency on ASP.NET Core in Core cryptographic services, prevents arbitrary filesystem traversal or device name collisions, prevents daemon writes to system folders, guarantees consistent filesystem matching across Linux and Windows, and ensures reliable UTC file timestamps across containerized environments.


### Execution: Batch 2 - Canonical IPipeline Injection and Sensitivity CSV Partitioning (Subtask 2B)
* **Task_ID:** task-biomechanics-phase2-canonical-pipeline-and-csv
* **Date:** 2026-10-28
* **Category:** Execution
* **What:** Implemented canonical pipeline architecture and OAAT sensitivity CSV partitioning in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`:
  1. Replaced domain sub-interfaces (`IExperimentalDataProcessingPipeline`, `IParametricSweepPipeline`) with canonical `IPipeline<ExperimentalDataProcessingInput, string>` and `IPipeline<ParametricSweepInput, string>` in `ProcessExperimentalDataCommand` and `ExecuteParametricSweepCommand`.
  2. Registered singleton canonical pipelines in `DependencyInjection.AddOptimizationsServices`.
  3. Refactored `ParametricSweepCsvWriterStep` to partition OAAT sensitivity results grouped by `VariableName`, writing `{VariableName}_SensitivityAnalysis.csv` under storage context `SensitivityAnalysis` with thread-safe file writer lifecycle and batch execution overload.
  4. Enforced standardized 6-column CSV schema (`VariableValue,InitialStress,FinalStress,AbsoluteDeltaStress,PercentageDeltaStress,AsymptoteTime`) formatted with `CultureInfo.InvariantCulture` and general `:G` numeric formatting.
  5. Fixed `StorageEntryMetadataEntity.Create` argument ordering in `ParametricSweepPersistenceStep` and aligned `ParametricSweepController` and `ExperimentalDataController` streaming invocations. Validated production build with 0 errors.
* **From where:** Executed by `senior-backend-software-engineer-net` (Delivery: `.agents/soft-tissue-biomechanics/outputs/task-biomechanics-phase2-canonical-pipeline-and-csv.json`), audited and approved by `documentation-and-tracking-secretary`.
* **Why:** Enforces Interface Segregation Principle (ISP) and KISS by standardizing command execution on canonical `IPipeline<TIn, TOut>`, eliminates redundant interface bloat, structures sensitivity outputs for downstream academic visualization and thesis analysis, and guarantees numerical format consistency across locales.


### Decision: Production-First Development Principle / Anti-TDD Directive
* **Task_ID:** task-human-directive-anti-tdd
* **Date:** 2026-10-28
* **Category:** Decision
* **What:** Established the Inviolable Architectural Development Directive: "Production-First / Anti-TDD".
  - Testes de unidade não podem, sob nenhuma hipótese, guiar, ditar ou influenciar o design, a arquitetura ou o fluxo de desenvolvimento do código de produção.
  - O uso de Test-Driven Development (TDD) é estritamente proibido no ecossistema.
  - O código de produção deve ser projetado exclusivamente para atender aos requisitos de domínio, performance, robustez, conformidade com .NET 10 / C# 13 e integridade científica.
  - Os testes de unidade existem única e exclusivamente para assegurar, a posteriori (após a implementação de produção estar concluída), que a implementação funciona sem bugs e sem regressões.
  - Nenhum artifício, concessão arquitetural, acoplamento artificial ou simplificação no código de produção pode ser feito com o objetivo de "facilitar" ou "atender" a escrita de testes.
* **From where:** Diretriz Inviolável do Usuário Humano ("Os testes de unidade não podem influenciar no desenvolvimento, nunca deve-se usar TDD. Os testes de unidade devem única e exclusivamente assegurar que a implementação funciona sem bugs, portanto, nenhum desenvolvimento pode ser feito para facilitar ou atender testes.").
* **Why:** Garantir que o design do software permaneça limpo, robusto, idiomático e orientado estritamente aos problemas de domínio e desempenho da engenharia biomecânica, evitando distorções arquiteturais, sobrecargas de mocks artificiais, ou violações de encapsulamento frequentemente induzidas pelo desenvolvimento centrado em testes (TDD).


### Decision (Pre-Execution Strategy QA Gate Approved: Batch 3 Phased Mutex)
* **Task_ID:** task-pre-qa-gate-batch-3
* **Date:** 2026-10-28
* **Category:** Decision
* **What:** Formally evaluated, audited, and approved the Pre-Execution Strategy QA Gate for Batch 3 under Phased Mutex Orchestration (Esteira Desacoplada):
  1. **Subtask 3A (Assigned to Agent 1 - Portability & Infrastructure):**
     - Target: Plugin isolation, shadow copying, deterministic RNG seed, and newline normalization.
     - Scope:
       - `PluginAssemblyProcessor.cs` / `PluginLoadContext.cs`: Custom `AssemblyLoadContext` with `AssemblyDependencyResolver` and PE header validation (`IMAGE_DOS_SIGNATURE` 0x5A4D, `IMAGE_NT_SIGNATURE` 0x00004550) [Finding 04].
       - `PluginAssemblyProcessor.cs` / shadow copy: Shadow copying plugin assemblies to a temporary workspace prior to loading to prevent file locking on Windows during hot-reload [Finding 05].
       - `AlglibCurveFitter.cs`: Expose explicit RNG `Seed` in options and configure `alglib.mindfsetseed(state, seed)` [Finding 09].
       - `CsvFileWriter.cs` and `SpanStringBuilder.cs`: Standardize newline characters (`\r\n` or configurable RFC 4180 CRLF) across environments [Finding 12].
     - Target Files:
       - `src/MelloSilveiraTools.Plugins/Infrastructure/PluginAssemblyProcessor.cs`
       - `src/MelloSilveiraTools.Plugins/Infrastructure/PluginLoadContext.cs` (if needed)
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/CurveFitting/Algorithms/AlglibCurveFitter.cs`
       - `src/MelloSilveiraTools.Core/Managers/File/Csv/CsvFileWriter.cs`
       - `src/MelloSilveiraTools.Core/Models/SpanStringBuilder.cs`
  2. **Subtask 3B (Assigned to Agent 2 - Biomecânica):**
     - Target: Variable Relationships Pipeline Implementation (IsoStress Esteira) in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships`.
     - Scope:
       - `VariableGridGeneratorStep`: Cartesian discretization of $n-1$ parameters based on `ParameterBound` ranges.
       - `IsoStressNewtonRaphsonStep`: 1D numerical root-solving for parameter $p_k$ to achieve `TargetStress` at `TargetTime` using central difference gradient from `IDifferentiation`.
       - `IsoStressFilterStep`: Retention of valid combinations within parameter bounds and relative tolerance.
       - `IsoStressCsvWriterStep`: Partitioned CSV export emitting `{Identifier}_IsoStress_Combinations.csv` under storage context `VariableRelationships`.
       - `VariableRelationshipPersistenceStep`: Persistence and return of `VariableRelationshipOutput`.
       - `VariableRelationshipPipeline`: TPL Dataflow pipeline implementing canonical `IPipeline<VariableRelationshipInput, VariableRelationshipOutput>`.
       - `DependencyInjection.cs`: Registration of steps and pipeline as singletons.
     - Target Files:
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/VariableRelationships/Steps/VariableGridGeneratorStep.cs`
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/VariableRelationships/Steps/IsoStressNewtonRaphsonStep.cs`
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/VariableRelationships/Steps/IsoStressFilterStep.cs`
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/VariableRelationships/Steps/IsoStressCsvWriterStep.cs`
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/VariableRelationships/Steps/VariableRelationshipPersistenceStep.cs`
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/VariableRelationships/VariableRelationshipPipeline.cs`
       - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/DependencyInjection.cs`
  3. **Disjointness & Pre-Execution QA Evaluation:**
     - Confirmed zero file overlap between Subtask 3A and Subtask 3B ($Files(3A) \cap Files(3B) = \emptyset$).
     - 3A touches `Plugins/Infrastructure/`, `CurveFitting/Algorithms/AlglibCurveFitter.cs`, and `Core/Managers/File/Csv/CsvFileWriter.cs` + `SpanStringBuilder.cs`.
     - 3B touches exclusively the new folder `VariableRelationships/` and registers in `Optimizations/DependencyInjection.cs`.
     - Neither subtask modifies the shared TPL Dataflow core (`DataflowPipelineBuilder.cs`, `IDataflowPipeline.cs`).
     - Verified no code reinvention: 3A leverages BCL `AssemblyLoadContext` and ALGLIB native seeding; 3B builds on previously committed models (`ParameterBound`, `VariableRelationshipInput`, `VariableRelationshipOutput`), `IDifferentiation`, and canonical `IPipeline<,>`.
* **From where:** Evaluated and approved by `documentation-and-tracking-secretary` acting as Pre-Execution Strategy QA Gate, following consensus and directives from the Orchestrator.
* **Why:** Guarantees parallel execution safety with zero merge conflicts or race conditions across working trees, fulfills the cross-platform portability requirements for dynamic plugins and deterministic curve fitting, and delivers the IsoStress variable relationship optimization pipeline for biomechanical soft tissue modeling.
