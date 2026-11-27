# MelloSilveiraTools.MechanicsOfMaterials.Optimizations

High-throughput experimental data processing, phase segmentation, constrained numerical curve fitting, and parametric sensitivity analysis pipelines for mechanics of materials.

---

## 1. Key Capabilities

- **Automated Phase Segmentation (`ExperimentalDataSegmenterStep`):**
  - Streams raw experimental CSV files and classifies deformation data points into mechanical phases: `Ramp`, `Relaxation`, `Descent`, and `Recovery` via sliding-window numerical differentiation.
- **Adaptive Downsampling & Segment Accumulation:**
  - `ExperimentalDataDownsamplerStep` and `CurveSegmentBuilderStep` group contiguous deformation segments into clean batches for optimization.
- **Constrained Constitutive Curve Fitting (`ICurveFitter`):**
  - **`AlglibCurveFitter`:** Bundles ALGLIB Free Edition routines. Supports fast local gradient convergence via bounded L-BFGS (`alglib.minbleic`) and derivative-free global optimization via adaptive Differential Evolution (`alglib.mindf` GDEMO) with penalty barriers for physical/thermodynamic rules. Computes $R^2$, RMSE, and Student-$t$ parameter confidence intervals.
  - **`MathNetCurveFitter`:** Alternative unconstrained local minimizer using BFGS and Nelder-Mead simplex.
- **Parametric Sensitivity Analysis (`ParametricSweepPipeline`):**
  - Executes One-At-A-Time (OAAT) sweeps across scalar and array-indexed constitutive parameters, computing initial/final stress responses and steady-state relaxation asymptotes.
- **Five Core Computational Workflows:**
  1. *Experimental Dataset Upload (`UploadExperimentalDataCommand`):* Stores paired `strain.csv` and `stress.csv` files and persists `StorageEntryMetadataEntity` rows.
  2. *Single-Dataset Processing (`ExecuteExperimentalDataPipeline`):* Ingests synchronized `strain.csv` and `stress.csv` pairs for segmentation, downsampling, curve fitting, simulation, and persistence.
  3. *Batch Multi-Dataset Execution (`ExecuteBatchExperimentalDataPipeline`):* Executes multiple specimen groups in `Sequential` or `Parallel` modes with non-blocking `SemaphoreSlim` throttling (`MaxDegreeOfParallelism`).
  4. *Single Baseline Parametric Sweep (`ExecuteParametricSweepPipeline`):* Evaluates OAAT sensitivity on a single constitutive baseline.
  5. *Multi-Model Sweep by Identifier (`ExecuteParametricSweepFromExperimentalDataCommand`):* Evaluates sensitivity sweeps across all calibrated models generated from an experimental run.

---

## 2. Installation

```bash
dotnet add package MelloSilveiraTools.MechanicsOfMaterials.Optimizations
```

---

## 3. Quick Start

### Non-Linear Curve Fitting Example

```csharp
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging.Abstractions;

double[] strains = [0.00, 0.01, 0.02, 0.03, 0.04, 0.05];
double[] stresses = [0.00, 2.46, 5.42, 8.98, 13.25, 18.39];

AlglibCurveFitter fitter = new(NullLogger<AlglibCurveFitter>.Instance, new CurveFitterSettings { Seed = 42 });

CurveFitInput fitInput = new(
    IndependentVariable: strains,
    DependentVariable: stresses,
    ModelFunction: (eps, p) => p[0] * (Math.Exp(p[1] * eps) - 1.0),
    InitialParameters: [5.0, 10.0],
    LowerBounds: [0.001, 0.001],
    UpperBounds: [500.0, 200.0],
    Profile: CurveFitProfile.Standard);

CurveFitOutput output = fitter.Fit(fitInput);
// output.OptimizedParameters -> [A ≈ 12.5, B ≈ 18.0]
// output.RSquared -> > 0.9999
```

### Batch Experimental Data Pipeline Example

```csharp
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;

ExecuteBatchExperimentalDataPipelineRequest batchRequest = new()
{
    MechanicalModelName = "SimplifiedFung",
    ExecutionMode = PipelineExecutionMode.Parallel,
    MaxDegreeOfParallelism = 4,
    TargetRSquared = 0.98,
    ConfidenceLevel = 0.95,
    Groups =
    [
        new ExperimentalDataGroupRequest
        {
            Identifier = "Specimen_LCA_01",
            StrainFileLocation = "path/to/strain.csv",
            StressFileLocation = "path/to/stress.csv"
        }
    ]
};

// Execute command (injected via DI)
Result<IAsyncEnumerable<string>> result = await batchCommand.ExecuteAsync(batchRequest, cancellationToken);
```

---

## 4. Dependency Injection & Technical Invariants

- **DI Entry Point:** `DependencyInjection.AddOptimizationsServices(curveFitterSettings, experimentalDataSettings, parametricSweepSettings)`.
  - **Singletons (thread-safe):** `CurveFitterSettings`, `ICurveFitter` (default via `DefaultCurveFitter` + keyed `Alglib`/`MathNet`), `IMechanicalModelStepFactory`; keyed: `IMathExpressionCurveFitter` (`PronySeries`), `IFunctionCurveFitter` (`Logarithmic`/`Exponential`/`Polynomial`), `IParametricSweepGeneratorStep` (`Fung`/`SimplifiedFung`/`Schapery`); when configured: `ExperimentalDataSettings`, `ParametricSweepSettings`, `IPipeline<ExperimentalDataProcessingInput, string>`, `IPipeline<ParametricSweepPipelineInput, string>`, pipeline commands & validators.
- **Pipeline Topologies & Profiles:**
  - *Experimental Data DAG:* `ExperimentalDataSegmenterStep` (`ArrayPool<ExperimentalDataPoint>.Shared` sliding window) → broadcast `ExperimentalDataFileWriterStep` → `ExperimentalDataDownsamplerStep` → `CurveSegmentBuilderStep` → `IMechanicalModelCurveFitterStep` (`FungRelaxationOnlyCurveFitterStep`, `SimplifiedFungRelaxationOnlyCurveFitterStep`, `SchaperyRelaxationOnlyCurveFitterStep`) → 3-way Fork (`CurveFitOutput`, SHA-256 `CurveFitOutputIdentifierBuilderStep`, `MechanicalModelSimulationStep`) → broadcast `CurveFitOutputPersistenceStep` → `MechanicalModelOutputPersistenceStep`.
  - *Parametric Sweep DAG:* `ParametricSweepGeneratorStepBase<T>` → `ParametricSweepSimulationStep` → `ParametricSweepCsvWriterStep` → `AddCollectAllStep` → `ParametricSweepPersistenceStep`.
  - *Curve Fit Profiles:* `CurveFitProfile.Automatic` selects `RuleConstrained` (`alglib.mindf` GDEMO with `1e12` barrier) when `ValidateParameters` is set, otherwise `Standard` (`alglib.minbleic` L-BFGS).
  - *Commands, Serialization & HTTP Surface:* Commands `UploadExperimentalDataCommand`, `ExecuteExperimentalDataPipeline`, `ExecuteBatchExperimentalDataPipeline` (`SemaphoreSlim` throttling), `ExecuteParametricSweepPipeline`, `ExecuteParametricSweepFromExperimentalDataCommand`; serialization via `OptimizationJsonOptions.SignificantFigures7` (`SignificantFiguresDoubleJsonConverter(7)`); controllers (`ExperimentalDataController`, `ParametricSweepController`, `PipelineProgressController`) and Minimal API extensions (`MapExperimentalDataEndpoints`, `MapParametricSweepEndpoints`, `MapPipelineProgressEndpoints`, `MapOptimizationsCrudEndpoints`).

---

## 5. Dependencies & Licensing

- `MelloSilveiraTools.MechanicsOfMaterials`
- `MelloSilveiraTools.Database`
- `MelloSilveiraTools.WebApi`
- `MathNet.Numerics` (5.0.0)
- *Notice:* This package bundles C# source code from ALGLIB Free Edition (`GPL-2.0-or-later`). In compliance with ALGLIB, the entire suite is licensed under **GNU GPLv3 or later (`GPL-3.0-or-later`)**.
