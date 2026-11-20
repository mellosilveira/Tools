```json
{
  "task_id": "eval-backend-vs-research",
  "skill": "senior-backend-software-engineer-net",
  "status": "success",
  "retry_count": 0,
  "data_payload": {
    "levels": "1=atende completamente; 2=parcialmente; 3=indefinido; 4=não atende parcialmente; 5=não atende completamente",
    "scope": "Tools/src + Tools/test (MelloSilveiraTools.*), read-only. SoftTissue/ and FEBio/ sibling repos NOT audited in depth.",
    "items": [
      {"item":"Fung QLV 1D fitting (TASK 2.1)","level":2,"task":"2.1"},
      {"item":"Schapery 1D fitting (TASK 2.2)","level":2,"task":"2.2"},
      {"item":"Maxwell/Prony 1D fitting (TASK 2.3)","level":4,"task":"2.3"},
      {"item":"Weiss 1D (TASK 2.4)","level":5,"task":"2.4"},
      {"item":"Poroviscoelasticity 1D (TASK 2.5)","level":5,"task":"2.5"},
      {"item":"HGO 1D (TASK 2.6)","level":5,"task":"2.6"},
      {"item":"Sensitivity analysis engine (TASK 2.7)","level":2,"task":"2.7"},
      {"item":"Fitting engine/optimizers (infra)","level":1,"task":"2.1-2.6"},
      {"item":"Pipelines/API/streaming/persistence (infra)","level":1,"task":"2.9 / 3.1"},
      {"item":"Rodarte data ingestion + optimizer run (TASK 3.1)","level":2,"task":"3.1"},
      {"item":"1D step-by-step simulation (TASK 3.2)","level":2,"task":"3.2"},
      {"item":"1D to 3D parameter conversion (TASK 3.3)","level":5,"task":"3.3"},
      {"item":"FEBio integration (TASKs 3.4-3.6)","level":5,"task":"3.4-3.6"},
      {"item":"RMSE / R2 metrics (TASK 4.1)","level":4,"task":"4.1"},
      {"item":"Execution metadata (time, resources, iterations) (TASK 4.1)","level":4,"task":"4.1"},
      {"item":"SoftTissue cleanup/orchestration of data triad (TASKs 2.8-2.9)","level":3,"task":"2.8, 2.9"},
      {"item":"Data triad comparison/aggregation endpoint","level":5,"task":"2.9, 4.1"},
      {"item":"3D tensorial extensibility of entities","level":4,"task":"guideline 7"}
    ]
  },
  "output_summary": "18 items: level 1 = 2, level 2 = 5, level 3 = 1, level 4 = 4, level 5 = 6. Fung, Schapery and sensitivity are implemented; Maxwell fitting is partial; Weiss/Poro/HGO/FEBio/3D are absent.",
  "justification_for_decisions": "Levels derive from grep and reading of real code (Weiss, HGO, Poro, FEBio, Rodarte: 0 hits in src/test; Maxwell only as a calculator; fitter steps only for Fung, SimplifiedFung, Schapery; CurveFitOutput has RSquared, FinalError (SSR), Iterations; no RMSE, no wall-time or resource capture; ledger claims were not trusted blindly).",
  "artifacts": [".agents/soft-tissue-biomechanics/outputs/eval-backend-vs-research.md"],
  "warnings": [
    "Roadmap marks 2.1, 2.2 and 2.7 as COMPLETED, but the fitters only cover relaxation segments (SegmentType.Relaxation).",
    "SoftTissue (183 .cs files) and FEBio (C++ source) are sibling repos outside the audited scope: levels for 2.8/2.9 are 3 (undefined).",
    "Unit tests were not executed in this evaluation (read-only inspection)."
  ],
  "token_usage": 0,
  "theoretical_precision": "medium"
}
```

# Backend (MelloSilveiraTools) vs. Research: Evaluation

Levels: **1** fully meets · **2** partially meets · **3** undefined · **4** partially does not meet · **5** does not meet at all.

| # | Item | Level | Evidence (file/symbol) | Gap | Recommendation |
|---|---|---|---|---|---|
| 1 | Fung QLV 1D fitting (2.1) | **2** | `FungRelaxationOnlyCurveFitterStep`, `FungLogarithmicEstimator`, `SimplifiedFungRelaxationOnlyCurveFitterStep`, `MechanicalModelStepFactory` (switch on name and `SegmentType.Relaxation`) | Only the relaxation segment. No ramp or ramp+relaxation fit. No Fung hyperelastic (3D) formulation. | Add ramp and combined fitters. Reuse `FungModelCalculator` as the objective. Reopen 2.1 as "complete" only after this. |
| 2 | Schapery 1D fitting (2.2) | **2** | `SchaperyRelaxationOnlyCurveFitterStep`, `SchaperyConstitutiveParameters`, `SchaperyParametricSweepGeneratorStep` | Same limits: relaxation only, no ramp. | Same as item 1. |
| 3 | Maxwell/Prony fitting (2.3) | **4** | `MaxwellModelCalculator`, `MaxwellConstitutiveParameters`, `PronySeriesCurveFitter` (`IMathExpressionCurveFitter`), `PronySeries` | The calculator exists. The generic Prony fitter exists, but there is no `Maxwell…CurveFitterStep` and no factory case (factory throws `ArgumentException`). No sweep generator. | Add `MaxwellRelaxationOnlyCurveFitterStep` that wraps `PronySeriesCurveFitter`. Add a factory case and a generator. Low effort. |
| 4 | Weiss transversely isotropic (2.4) | **5** | 0 hits for "Weiss" in src/test | No model, parameters, calculator, fitter or resolver. | Add `WeissConstitutiveParameters`, a calculator (1D uniaxial fiber-exponential hyperelastic) and a fitter. Document that it has no relaxation (the roadmap requires this in 4.6). |
| 5 | Poroviscoelasticity (2.5) | **5** | 0 hits | Same as item 4. | Do it last. Justify the 1D reduction (no 1D equivalent without lateral drainage) and consider moving it to Future Works. |
| 6 | HGO (2.6) | **5** | 0 hits (only unrelated ALGLIB text) | Same as item 4. | Implement the 1D uniaxial HGO with a dispersion parameter κ. |
| 7 | Sensitivity analysis engine (2.7) | **2** | `ParametricSweepPipeline`, `ParametricSweepSimulationStep`, `SimulationMetrics` (initial stress, final stress, asymptote time, asymptote stress), generators for Fung, SimplifiedFung and Schapery, `ParametricSweepController` | Only 3 models. Strain rate not included (Future Works). Metrics are limited to stress and asymptote. | Add Maxwell, Weiss and HGO generators. Add strain rate as a sweep axis. |
| 8 | Optimizers and fitting infrastructure | **1** | `ICurveFitter` with Alglib/MathNet (`MathNetCurveFitter`), `CurveFitterBase`, `ParameterConfidenceInterval`, deterministic seed, `Logarithmic`/`Exponential`/`Polynomial` fitters | None relevant. | Keep. |
| 9 | Pipelines, API, streaming, persistence | **1** | `ExecuteExperimentalDataPipeline`, `ExecuteBatchExperimentalDataPipeline`, `ExperimentalDataController`, `CurveFittingController`, `StoredFilesController`, NDJSON `PipelineProgressStreamWriter`, entities `mechanical_model_curve_fit_outputs`, `mechanical_model_simulations`, `parametric_sweeps`, `stored_files`, `Database.Migrator` (SQL 0001–0003), `docker-compose.yml`, `.github`, 107 test files | None. Persisted parameters are serialized (JSON), which is flexible. | Keep. Re-run the tests before 3.1. |
| 10 | Rodarte ingestion and optimizer run (3.1) | **2** | `ExperimentalDataSegmenterStep` (generic CSV), `ProcessExperimentalData…`, batch command | No Rodarte-specific format or mapping. No specimen, ligament or anatomical metadata (`ExtenalIdentifier` only). Fits are limited to the 3 models. | Add specimen metadata: ligament type, donor, geometry and area for the stress conversion. Create a Rodarte import profile. |
| 11 | 1D step-by-step simulation (3.2) | **2** | `MechanicalModelSimulationStep`, `MechanicalModelSimulationEntity`, `SimulationDeltaOutput`, `IMechanicalModelCalculatorFacade` | Works only for the existing models. Persisted, but no pairing of the curve with the experimental run or fit. | Link simulation → fit → experimental run through a shared identifier. |
| 12 | 1D → 3D conversion (3.3) | **5** | No code. `Force`, `Vector3D` and `Profile` are geometric, not tensorial. | Non-existent. | Keep it as research (literature). Backend: only add a `ConstitutiveParameters3D` or a `TensorialMapping` abstraction after 3.3. |
| 13 | FEBio integration (3.4–3.6) | **5** | 0 hits for FEBio in Tools/src and Tools/test. The `FEBio/` sibling repo holds the source only. | No `.feb` writer, solver runner, result parser or ingestion. | Add a `FEBio` module: `.feb` XML generation (template per model), docker runner, and a logfile/`.xplt` or CSV parser feeding `stored_files` and a `febio_runs` table. Keep the 4-week time-box on UDM (3.4). |
| 14 | RMSE and R² (4.1) | **4** | `CurveFitterBase.CalculateRSquared`, `CurveFitOutput.RSquared`, `FinalError` (SSR). The entity persists `Error` and `Iterations` only. | R² is **not persisted** in `MechanicalModelCurveFitOutputEntity`. RMSE is absent (SSR only). No metric for sim-vs-experimental or 3D-vs-experimental. | Add `RMSE = sqrt(SSR/n)`, persist R² and RMSE (new migration). Create a generic `ComparisonMetrics(series A, series B)` service for the triad. |
| 15 | Execution metadata (4.1) | **4** | `Iterations` is persisted. Duration is only logged (`ObservabilityExtensions`). No Stopwatch, CPU or memory in the output. | Time, memory, CPU/machine, precision and convergence are not stored. | Add `ExecutionMetadata` (elapsed via `TimeProvider`, `Process`/GC metrics, machine name, optimizer, tolerance, seed, version). Persist it as JSONB. |
| 16 | SoftTissue cleanup and orchestration (2.8/2.9) | **3** | The `SoftTissue/api` sibling repo exists (183 `.cs`). Not audited. Tools only mentions SoftTissue in `DiscoveredPlugin`. | Cannot be confirmed from the Tools tree. | Run a separate audit of `SoftTissue/api/src` and `SoftTissue/api/test` (duplication versus MelloSilveiraTools). |
| 17 | Data triad aggregation endpoint (2.9/4.1) | **5** | No entity or endpoint joins experimental + 1D sim + FEBio 3D. | Non-existent. Belongs to SoftTissue. | Define `ComparisonRun` (experimental ref, fit ref, sim ref, FEBio ref, metrics) and a query endpoint returning aligned series. |
| 18 | 3D tensorial extensibility (guideline 7) | **4** | `ConstitutiveParameters` is a polymorphic record stored as JSON, which allows extension. `MechanicalModelOutput` is scalar. | No tensor or `Matrix3x3` types. No stress/strain tensor outputs. | Add a tensor output contract and `Tensor3x3` under Mathematics when entering 3.5/3.6 (requires approval to change AGENTS.md). |

## Summary

- **Solid foundation (level 1):** the fitting infrastructure (Alglib/MathNet, confidence intervals), the dataflow pipelines, the REST API with NDJSON streaming, persistence with migrations, and Docker/CI.
- **Partial (level 2):**
  - Fung, SimplifiedFung and Schapery fit only the relaxation segment.
  - The sweep covers only 3 models and has no strain rate.
  - Ingestion is generic, with no Rodarte profile or specimen metadata.
- **Critical gaps for the thesis:**
  1. **Weiss, HGO and Poroviscoelasticity** are absent (2.4–2.6).
  2. **Maxwell fitter** is missing even though the calculator and Prony fitter exist (2.3, cheapest fix).
  3. **RMSE, persisted R² and execution metadata** (4.1) are absent.
  4. **FEBio integration and the triad comparison** are absent (3.4–3.6, 2.9).
- **Roadmap inconsistency:** 2.1, 2.2 and 2.7 are marked done, but are level 2 in the code. Suggest reopening them or recording the limits in the ledger.

**Suggested priority:** (a) Maxwell fitter, (b) metrics and metadata (RMSE, R², `ExecutionMetadata`, migration), (c) Weiss and HGO 1D, (d) Rodarte profile with specimen metadata, (e) FEBio runner and `ComparisonRun`, (f) Poroviscoelasticity (candidate for Future Works). Every code delivery must update `CHANGELOG.md`. Changes to `AGENTS.md` (for example, tensors or a FEBio module) need human approval.
