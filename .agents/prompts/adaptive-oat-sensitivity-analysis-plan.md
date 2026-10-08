# Architectural and Implementation Plan: Adaptive OAT Sensitivity Analysis

## 1. Overview and Objectives
The goal of this development is to implement a high-performance computational system for Sensitivity Analysis in mechanical models (e.g., viscoelastic, hyperelastic). The focus is on generating data matrices for plotting charts that correlate the perturbation of a variable to physical metrics extracted from the simulation.

**Key Deliverables:**
*   **Adaptive Generator (OAT):** A One-At-A-Time sweep algorithm guided by the rate of change (gradient) of the function.
*   **Tabular Export (CSV):** Exported files crossing key data points (Stress, Stress Variation, Asymptote Time).
*   **Domain Agnosticism:** Complete isolation between the mathematical sensitivity package (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses`) and the simulations based on constitutive equations (`MechanicsOfMaterials.Models`).

---

## 2. Design Patterns (Local Guidelines)
*   **Strong Typing:** Absolute prohibition of the `var` keyword throughout the generated code.
*   **C# 13 & Syntax:** Preference for primary constructors in Dependency Injection (DI) and Records; simplified collections `[...]`; block-bodies `{}` for methods.
*   **Performance (CPU vs I/O):** Mathematical routines and extractions based on Reflection/Expression Trees will operate strictly synchronously. The use of `async/await`/`Task` will be exclusive for disk persistence operations.

---

## 3. Component Architecture

### 3.1 Generic Layer (`SensitivityAnalyses` Package)
These components will process `IReadOnlyDictionary<string, double>` without holding any physical logic.

*   `AdaptiveOatInput` (Record): Receives the baseline, limits (lower/upper bound), base step size, and exponential multiplier.
*   `SensitivityPoint` (Record): Encapsulates an execution, aggregating Parameters (Input) and Metrics (Output).
*   `AdaptiveOatGenerator` (Class): The mathematical engine (CPU-bound) responsible for managing the variable iterations and applying gradient assumptions to step sizes.
*   `SensitivityCsvExporter` (Class): I/O-bound class dedicated to optimized serialization to disk via C# streams and `StringBuilder`.

### 3.2 Mechanical Domain Layer (Integration/Optimizations Package)
Responsible for interpreting the math, instantiating the physics, and extracting resulting scalars from the time series.

*   `SensitivityMutatorStep` (Class): Uses Reflection or `ExpressionPathResolver` (in reverse) to clone and punctually update the property within concrete instances of `ConstitutiveParameters` (e.g., `MaxwellConstitutiveParameters`).
*   `SensitivityMetricsExtractorStep` (Class): Output Isolator. Processes the final `MechanicalModelSimulationPayload` looking for the specific responses requested by the user:
    *   `InitialStress` ($t = 0$)
    *   `FinalStress` ($t = t_f$)
    *   `StressVariation` (($T_f - T_i) / T_i$)
    *   `AsymptoteTime` (Processed via `AsymptoteMonitoringStep`)
*   `SensitivityAnalysisOrchestrator` (Class): Pipeline assembly point. Performs the "binding", connecting the `AdaptiveOatGenerator` iteration engine to the native calculation engines via a `SimulationEvaluatorDelegate`.

---

## 4. Implementation Phases

### Phase 1: Agnostic Foundations (Models and Adaptive Engine)
1.  Create the `AdaptiveOatInput` and `SensitivityPoint` records.
2.  Implement `AdaptiveOatGenerator` with strictly synchronous logic, incorporating adaptive control through gradient tolerances (linear vs. exponential).
3.  Write `xUnit` unit tests proving that arbitrary non-linear functions (e.g., $y = x^3$) trigger the engine's step reduction upon entering the "critical zone".

### Phase 2: Domain Translators and Mechanical Extraction
1.  Implement `SensitivityMutatorStep`. Focus on performance when cloning `ConstitutiveParameters` and using safe deep reflection.
2.  Implement `SensitivityMetricsExtractorStep`. Centralize the preexisting time series calculations and ensure tolerance to zero-division errors (in Stress Variation).

### Phase 3: Orchestration and Export
1.  Write `SensitivityAnalysisOrchestrator` linking the delegates to the preexisting `IMechanicalModelCalculatorFacade` API.
2.  Develop `SensitivityCsvExporter` configured for robust `File.WriteAllTextAsync` operations supporting proper separator encoding.
3.  Register all built instances as "Scoped" and "Singleton" (if thread-safe) components in the container via `DependencyInjection.cs`.

### Phase 4: Performance Profiling and Edge Case Validation
1.  Verify Garbage Collection accumulation in the adaptive generator and metrics extractor.
2.  Confirm behavior with mechanical models that generate exceptions due to physically unrealistic stresses. Asymptotic handling (values filled as `double.NaN`).
