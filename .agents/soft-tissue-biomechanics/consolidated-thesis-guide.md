# Consolidated Master's Thesis Guide — Soft Tissue Biomechanics (PPEMM/UFCIRJ)

> **Master Consolidated Document** — Unifies scientific decisions, the Evidence Funnel, analytical-numerical constitutive formulations, maturity diagnostics (Research, Backend, and Frontend), execution roadmap, and future works previously scattered across agent artifacts.
>
> *Last consolidated: 2026-11-26*

---

## 1. Identification, Scope & Scientific Philosophy

* **Provisional Title:** *Viscoelastic Characterization and Load-Sharing Modeling of Knee Ligaments: A Unified Computational Framework* (Thesis Focus: *Comparative Analysis of Non-Linear and Viscoelastic Constitutive Models Applied to Porcine Knee Ligaments under Uniaxial Tension*).
* **Program / Institution:** Graduate Program in Mechanical Engineering and Materials Technology (PPEMM), Universidade Federal de Ciência e Inovação do Rio de Janeiro (UFCIRJ, formerly CEFET/RJ).
* **Author:** Bruno Mello Silveira (`ORCID: 0000-0001-6793-9387`).
* **Advisor:** Prof. Dr. Paulo Pedro Kenedi (`ORCID: 0000-0001-5563-843X`).
* **Experimental Basis:** Uniaxial ramp-and-hold stress-relaxation tensile tests on isolated porcine knee ligaments (ACL, PCL, MCL, LCL — dataset from Dr. Rodrigo Rodarte's thesis).
* **Strict Physical-Mathematical Scope:**
  * **Loading:** Coaxial uniaxial tension under finite strain in a quasi-static regime ($\rho \ddot{\mathbf{u}} \approx 0$), with fixed principal axes (zero rotation $\mathbf{R} \equiv \mathbf{I}$, zero spin $\mathbf{W} \equiv \mathbf{0}$) and isochoric incompressibility ($J \approx 1$, $A(t) = A_0 / \lambda(t)$).
  * **Viscoelastic Phenomenon:** Strictly **stress relaxation**. Creep compliance formulations and Kelvin-Voigt were purged from the core scope because they do not match the experimental protocol.
  * **Infinitesimal Strain Theory:** Retained only as an asymptotic comparative baseline (quadratic error $\Delta = E_{11} - \varepsilon = \frac{1}{2}\varepsilon^2$: 5% at $\varepsilon = 10\%$ and 12.5% at $\varepsilon = 25\%$).
  * **Thermodynamics:** Invoked strictly where required by hyperelastic and non-linear constitutive free-energy potentials (e.g., Helmholtz free energy in Schapery's model), assuming isothermal assays.
* **Data Triad (Computational Architecture):**
  1. **Real Experimental Data** (force/displacement curves converted into true stress-strain);
  2. **1D Scalar Numerical Simulation** in C# / `.NET 10` (`MelloSilveiraTools`, avoiding mesh overhead);
  3. **3D Tensorial Finite Element Simulation** in open-source **FEBio**, with 1D→3D recalibration and C++ UDM subroutines only for non-native models.
* **Core Philosophy ("The Why") & Numerical Precision:** Every architectural or methodological decision must record its explicit rationale (*What / From where / Why*). Numbers retain full `double` precision in code/data and are rounded to **3 significant figures** only in thesis prose.

---

## 2. Unified 6-Model Constitutive Matrix

Consolidated outcome of the **Evidence Funnel** (358 audited records → **301 unique DOIs** in [`base-literature/evidence-funnel-bibliography.md`](./base-literature/evidence-funnel-bibliography.md)) crossed with codebase audit (`MelloSilveiraTools`) and **FEBio** support:

| Priority | Constitutive Model | Literature Usage (Knee) | 1D Status (`MelloSilveiraTools`) | 3D Support (FEBio) | Thesis Role / Cut Criterion |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **1 (Core)** | **Fung QLV (Continuous Spectrum $E_1$)** | High (Historical gold standard) | **Implemented** (`FungModelCalculator`, `FungRelaxationOnlyCurveFitterStep`, Sweep) | Native / via viscoelasticity | Historical analytical reference in *ex vivo* uniaxial tests. |
| **2 (Core)** | **Simplified Fung / Generalized Maxwell (Prony Series)** | Very High (FEA industry standard) | **Partial** (`SimplifiedFung` complete; `MaxwellModelCalculator` + `PronySeriesCurveFitter` exist, dedicated step/factory pending) | **Native** (internal variables) | FEA benchmark standard; core comparison of the primary scientific article. |
| **3 (Core)** | **Weiss Transversely Isotropic** | Very High (FEBio standard for ACL) | **Absent** (implement 1D uniaxial fiber-exponential law) | **Native** (`trans iso Mooney-Rivlin` / Weiss) | Pure hyperelastic baseline ($\sigma_{\text{initial}} = \sigma_{\text{final}}$, zero relaxation) for Discussion contrast. |
| **4 (Extended)** | **Schapery Non-Linear Viscoelastic** | Low / Emerging in 3D (Thesis novelty) | **Implemented** (`SchaperyModelCalculator`, `SchaperyRelaxationOnlyCurveFitterStep`, Sweep) | **Non-native** (requires C++ UDM) | Primary scientific differentiator. Strict 4-week / 4-cycle C++ UDM time-box with documented fallback to 1D-only comparison. |
| **5 (Extended)** | **Holzapfel-Gasser-Ogden (HGO)** | Very High (Fiber dispersion $\kappa$) | **Absent** (implement 1D uniaxial with dispersion $\kappa$) | **Native** | Modern structural model accounting for angular collagen fiber dispersion. |
| **6 (Cut / Future)** | **Poroviscoelasticity (Biphasic)** | Medium for ligaments (High for cartilage) | **Absent** | **Native** (biphasic) | **Top Future Work candidate** if schedule tightens; 1D reduction without lateral fluid drainage has weak physical justification in pure tensile tests. |

*Note on Soft Tissues vs. Polymers:* The comparative review of 60 CrossRef-verified studies in [`base-literature/comparative-mechanics-soft-tissues-vs-polymers.md`](./base-literature/comparative-mechanics-soft-tissues-vs-polymers.md) underpins why polymer-rheology formulations (notably **Schapery** and Prony series) transfer to biological ligaments, contrasting collagen *toe-region* crimp, preconditioning vs. Mullins effect, and fracture toughness.

---

## 3. Mathematical Basis, Parameter Identification & Sensitivity (Materials and Methods)

### 3.1. Canonical Formulations Document
* **Official File:** [`thesis/Consolidated_Biomechanical_Formulations.docx`](./thesis/Consolidated_Biomechanical_Formulations.docx) (generated via [`scripts/build-consolidated-thesis.py`](./scripts/build-consolidated-thesis.py) and validated via [`scripts/verify-consolidated-thesis.py`](./scripts/verify-consolidated-thesis.py)).
* **Structure:** **246 sequentially numbered equations** across 7 chapters (ABNT/PPEMM layout, native OMML, 100% auto-black typography):
  1. **Ch. 1:** Continuum Mechanics (dual Lagrangian and Eulerian formulations, experimental synthesis of $\mathbf{F}, \mathbf{C}, \mathbf{E}, \mathbf{e}$ from LVDT/load cell, tensor operators, and the four 3D frameworks: hyperelasticity, viscoelasticity, viscohyperelasticity, viscoporoelasticity);
  2. **Ch. 2:** Kinematic transformations and linear viscoelasticity (stress relaxation focus);
  3. **Ch. 3:** Quasi-Linear Viscoelasticity (didactic progression: general hereditary integral → step relaxation → exponential elastic law $\sigma^e(\varepsilon) = A(e^{B\varepsilon}-1)$ → continuous Fung $E_1(z)$ vs. Simplified Fung with Prony);
  4. **Ch. 4:** Non-Linear Viscoelasticity (Schapery relaxation with $h_e, h_2$ and Modified Superposition Method);
  5. **Ch. 5:** General analytical sensitivity gradients $\nabla_{\mathbf{p}}\sigma$, Levenberg-Marquardt, isocline continuation, and zero-crossing settling-time asymptote condition;
  6. **Ch. 6:** Anatomical 3D load sharing across the 4 knee ligaments;
  7. **Ch. 7:** CrossRef/ISBN-audited bibliographic registry.

### 3.2. Curve-Fitting Strategies Implemented in `MelloSilveiraTools`

1. **Fung QLV 1D (2-Stage Decoupling — `FungRelaxationOnlyCurveFitterStep`):**
   * *Stage 1 (Normalized Relaxation):* Minimizes $\text{SSR}_{\text{relax}}$ for $(c, \tau_1, \tau_2)$ in $G(t) = \frac{1 + c[E_1(t/\tau_2) - E_1(t/\tau_1)]}{1 + c\ln(\tau_2/\tau_1)}$ subject to $\tau_1 < \tau_2$ with bounds $c \in [0, 1]$, $\tau_1 \in [10^{-4}, 0.1]\,\text{s}$, $\tau_2 \in [1, 10^5]\,\text{s}$ (ALGLIB Differential Evolution + L-BFGS polishing).
   * *Stage 2 (Ramp):* With $(c^*, \tau_1^*, \tau_2^*)$ fixed, identifies elastic constants $A \in [10^{-6}, 10^6]\,\text{MPa}$ and $B \in [0, 100]$ via hereditary convolution (`Disregard` and `ConsiderWithViscoelasticEffect` validated; `ConsiderWithoutViscoelasticEffect` under refinement).
2. **Schapery Non-Linear 1D (3-Stage Hierarchical Decoupling — `SchaperyRelaxationOnlyCurveFitterStep`):**
   * *Stage 1 (Linear Anchor Segment, lowest strain $\varepsilon_1$):* Fixes $h_e = h_1 = h_2 = 1$ and identifies $(G_e, g_1, n)$ in $\sigma(t) = (G_e + g_1 t^{-n})\varepsilon_1$ via box-constrained L-BFGS (`minbleic`).
   * *Stage 2 (Subsequent Non-Linear Segments):* With $(G_e^*, g_1^*, n^*)$ fixed, identifies pointwise Helmholtz multipliers $h_e(\varepsilon_j), h_2(\varepsilon_j) \in [0, 10]$ per strain level.
   * *Stage 3 (Continuous Multiplier Fitting):* Competes candidate functions (`Logarithmic`, `Exponential`, `Polynomial`) by minimum SSR to construct closed-form $h_e(\varepsilon)$ and $h_2(\varepsilon)$ (falling back to `ConstantFunction` for single-segment data).
   * *Combined Metric:* $\text{Precision} = \frac{1}{3}\left(\frac{\sum w_j R_j^2}{\sum w_j} + R_{h_e}^2 + R_{h_2}^2\right)$.
3. **1D OAAT Sensitivity Analysis Engine (`ParametricSweepPipeline`):**
   * Evaluates all **4 mandatory scenarios** via concurrent TPL Dataflow for Fung QLV, Simplified Fung, and Schapery:
     1. Initial stress $\sigma(t_0; \theta_i)$;
     2. Final stress $\sigma(t_f; \theta_i)$;
     3. Asymptote time $t_{\text{asymptote}}$ (asymptotic derivative / root-finding criterion);
     4. Absolute and percentage stress variation $\Delta\sigma = \sigma(t_f) - \sigma(t_0)$ and $\%\Delta\sigma$.
   * Compiles typed input adapters in `MechanicalModelTypeCache` (zero runtime reflection in hot paths) and writes variable-partitioned CSVs.

---

## 4. Unified Maturity Diagnostic & Prioritized Backlog

Consolidated from independent Research, Backend, and Frontend audits (scale: **1** fully meets · **5** does not meet at all):

| Axis | Strengths (Level 1–2) | Critical Gaps (Level 4–5) | Next Priority Actions |
| :--- | :--- | :--- | :--- |
| **Research & Theory** | 301 audited DOIs from the Evidence Funnel; `Consolidated_Biomechanical_Formulations.docx` (246 eqs); Fung QLV 3D & Sensitivity cheat sheets; soft-tissue vs. polymer review. | Missing standalone 3D cheat sheets (Schapery, Maxwell/Prony, Weiss, HGO — Tasks 1.5–1.9); missing documented 1D→3D conversion method (Task 3.3); thesis chapters not yet drafted (Tasks 4.3–4.7). | 1) Finalize model list & cut order;<br>2) Derive analytical 1D→3D uniaxial isochoric mapping + inverse FEA fallback;<br>3) Begin incremental drafting of Chapters 1–2. |
| **Backend (`MelloSilveiraTools` + `SoftTissue`)** | Optimization engine (ALGLIB/MathNet, confidence intervals), TPL Dataflow pipelines, NDJSON streaming, pure-`Npgsql` PostgreSQL persistence with non-partitioned RLS (`storage_entry_metadata`) + DbUp migrations, executable `WebApi.Host` + `Launcher`, distributed health checks, NuGet package validation, and high-coverage unit/acceptance suites. | Missing dedicated Maxwell 1D step/factory (Task 2.3); missing Weiss and HGO 1D models (Tasks 2.4, 2.6); **RMSE, persisted $R^2$, AIC/BIC, and execution metadata** (runtime, memory, iterations) missing on entity; missing Dr. Rodarte anatomical import profile; FEBio connector not yet built. | 1) Persist `RMSE`, `RSquared`, `AIC/BIC`, and `ExecutionMetadata` (time/memory);<br>2) Implement `MaxwellRelaxationOnlyCurveFitterStep`, `Weiss` 1D, and `HGO` 1D;<br>3) Add Dr. Rodarte specimen metadata profile;<br>4) Implement headless FEBio connector (`.feb` → solver → ingestion). |
| **Frontend & Visualization** | REST contracts, NDJSON streaming, and file listing/download available (`ExperimentalDataEndpoints`, `ParametricSweepEndpoints`, `PipelineProgressEndpoints`, `StorageEntryMetadataEndpoints` `GET /api/v1/storage-entry-metadata` and `/{id}/download`), plus Blazor WebAssembly + Apache ECharts frontend (`MelloSilveiraTools.WebUi`) served by `WebApi.Host` and containerized via `docker-compose.yml`. | 3D FEBio tensorial series slot in the triad overlay and publication-grade static Python plotting scripts (Module 09) remain pending. | 1) Add 3D FEBio series slot into triad comparison UI once Phase 3 lands;<br>2) Build Python plotting pipeline (Matplotlib/Seaborn) for $\ge 300$ DPI figures and LaTeX (`booktabs`) tables from PostgreSQL. |

### Active Detailed Plans & Live Backlog (Single Source of Truth)
* **Phases 1–4 atomic backlog:** maintained exclusively in [`ledger/thesis-roadmap.md`](./ledger/thesis-roadmap.md).
* **Technical feature plans:** maintained in [`.agents/prompts/feature-planning/`](../prompts/feature-planning/README.md) (Module 07 FEBio 3D, Module 08 Blazor WebGL/Canvas Frontend, Module 09 Python/LaTeX Publication Pipeline).

---

## 5. Future Works & Explainable AI (XAI) Directive

* **Single source of truth:** maintained exclusively in [`ledger/future-works.md`](./ledger/future-works.md) (covering 1D→3D inverse FEA, time-boxed C++ FEBio UDM plugins, strain rate sensitivity, ReadyToRun vs. NativeAOT migration, PgBouncer analysis, and mandatory step-by-step theoretical-experimental XAI traceability).

---

## 6. Quick Map of `.agents/soft-tissue-biomechanics/`

| Path | Contents |
| :--- | :--- |
| [`consolidated-joss-and-publications.md`](./consolidated-joss-and-publications.md) | Dual publication strategy: **JOSS** submission (`MelloSilveiraTools`) + target journal selection (*Scientific Reports*, *JBME*, *JMBBM*). |
| [`thesis/Consolidated_Biomechanical_Formulations.docx`](./thesis/Consolidated_Biomechanical_Formulations.docx) | Official thesis mathematical formulations reference (246 native OMML equations). |
| [`base-literature/evidence-funnel-bibliography.md`](./base-literature/evidence-funnel-bibliography.md) | Deduplicated catalog containing all **301 verified DOIs** from the Evidence Funnel and search queries. |
| [`base-literature/comparative-mechanics-soft-tissues-vs-polymers.md`](./base-literature/comparative-mechanics-soft-tissues-vs-polymers.md) | Comparative review of soft tissues vs. polymers (60 CrossRef-verified studies). |
| [`base-literature/theory-cheat-sheets/fung-qlv-3d.md`](./base-literature/theory-cheat-sheets/fung-qlv-3d.md) | Fung QLV 3D/1D mathematical cheat sheet. |
| [`base-literature/theory-cheat-sheets/sensitivity-analysis-and-continuation.md`](./base-literature/theory-cheat-sheets/sensitivity-analysis-and-continuation.md) | Sensitivity gradients, isocline continuation, and CrossRef-verified references cheat sheet. |
| [`base-literature/markdown-books/fung-biomechanics-base.md`](./base-literature/markdown-books/fung-biomechanics-base.md) | Y. C. Fung's *Biomechanics* converted to Markdown for formula verification. |
| [`ledger/decisions.md`](./ledger/decisions.md) | Concise scientific traceability ledger (*What / From where / Why*). |
| [`ledger/architecture.md`](./ledger/architecture.md) | Multi-agent governance rules and contracts. |
| [`ledger/thesis-roadmap.md`](./ledger/thesis-roadmap.md) | Atomic backlog for Phases 1–4. |
| [`ledger/future-works.md`](./ledger/future-works.md) | Future works & opportunities registry. |
