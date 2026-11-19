# Decisions and Scientific Traceability Ledger

> **Consolidated Scientific Ledger** — Maintains the permanent triple-record traceability (`What / From where / Why`) of all scientific, methodological, bibliographic, and core architectural decisions for the PPEMM/UFCIRJ Master's Thesis and `MelloSilveiraTools` JOSS submission.
>
> *Transient software-sprint QA gates (Storage API phases, portability hotfixes, batch mutex orchestration) were pruned on 2026-11-19; implementation history remains tracked in Git and `CHANGELOG.md`. For thematic guides, see [`consolidated-thesis-guide.md`](../consolidated-thesis-guide.md) and [`consolidated-joss-and-publications.md`](../consolidated-joss-and-publications.md).*

---

## Index of Core Decisions

| Date | Task ID | Category | Summary |
|---|---|---|---|
| 2026-10-06 | `baseline-style-guide-and-previous-cycle` | Decision & Execution | Academic Style Guide (3 sig. figs. in text) & Schapery 1D baseline (81 joint scenarios, 0% vs. 3% pre-strain). |
| 2026-10-06 | `methodological-directive-evidence-funnel` | Decision | 4-stage Evidence Funnel (Soft Tissues → Knee Ligaments → FEA Tech → FEBio Validation) to prevent confirmation bias. |
| 2026-10-06 | `safeguard-anti-hallucination-guardrails` | Decision | Human-in-the-Loop QA gate, short loops, deterministic validation scripts, zero invented DOIs/URLs. |
| 2026-10-07 | `scope-uniaxial-and-architecture-pivot` | Decision | Focus strictly on isolated uniaxial porcine knee ligaments; C# 1D engine (`MelloSilveiraTools`) + 3D FEBio triad. |
| 2026-10-08 | `evidence-funnel-and-hgo-inclusion` | Decision & Execution | Completed 301-DOI Evidence Funnel (Tasks 1.1–1.3); confirmed FEBio; expanded guiding models to 6 (added HGO). |
| 2026-10-08 / 14 | `1d-fitters-and-sensitivity-engine` | Execution & Decision | Implemented Fung QLV 2-stage (Task 2.1), Schapery 3-stage (Task 2.2), and OAAT Sensitivity Engine (Task 2.7). |
| 2026-10-16 | `target-journals-and-dual-publication` | Decision | Audited 301 DOIs across 137 journals → Top 3 (*Scientific Reports*, *JBME*, *JMBBM*) + JOSS software paper strategy. |
| 2026-10-28 | `production-first-anti-tdd-directive` | Decision | Production-First implementation directive (validate clean production architecture before test scaffolding). |
| 2026-11-09 | `register-sensitivity-theory-basis` | Execution & Decision | Registered 10 CrossRef/ISBN-audited references for sensitivity gradients and continuation (`sensitivity-analysis-and-continuation.md`). |
| 2026-11-09 | `advisor-academic-title-standardization` | Decision & Style | Standardized advisor formal title as **Prof. Dr. Paulo Pedro Kenedi** (PPEMM/UFCIRJ). |
| 2026-11-09 | `focus-strictly-on-stress-relaxation` | Scope Refinement | Purged creep compliance and Kelvin-Voigt; aligned formulations and codebase strictly to stress relaxation. |
| 2026-11-09 | `streamline-axial-single-ligament-scope` | Scope Refinement | Pruned rotational kinematics (Polar Decomposition, Spin, dynamic inertia) for coaxial quasi-static uniaxial tension; retained dual Lagrangian/Eulerian kinematics, experimental bridge, and 4 major 3D continuum theories in `Consolidated_Biomechanical_Formulations.docx` (246 eqs). |
| 2026-11-10 | `soft-tissue-vs-polymers-comparative-mechanics` | Execution & Gap | 60 CrossRef-verified papers comparing native soft tissues vs. synthetic polymers (`comparative-mechanics-soft-tissues-vs-polymers.md`). |
| 2026-11-17 | `future-works-explainable-ai-traceability` | Future Work & Directive | Mandatory Explainable AI (XAI) step-by-step theoretical and experimental traceability in any AI-synthesized final analysis. |

---

## Detailed Scientific & Architectural Records

### 2026-10-06 — Baseline Style Guide, Previous Cycle & Evidence Funnel
* **Task_ID:** `baseline-style-guide-and-previous-cycle`, `methodological-directive-evidence-funnel`, `safeguard-anti-hallucination-guardrails`
* **Category:** Decision & Execution
* **What:**
  1. **Style Guide:** Academic, impersonal, rigorous continuum mechanics and viscoelastic terminology; LaTeX math (`$...$`, `$$...$$`); variables defined immediately after equations; heavy deductions in appendices; numbers rounded to **3 significant figures only in prose** (full precision in code/ledger).
  2. **Previous Cycle Baseline:** 1D Schapery load-sharing framework across 81 combinatorial porcine knee scenarios under 75 N vertical distraction: 0% pre-strain dropped to 43.07 N; 3% pre-strain mitigated decay to 60.48 N (MCL showed highest relative drop).
  3. **Evidence Funnel Directive:** Mandatory 4-step literature funnel (Broad Soft Tissue Models → Knee Ligaments → Computational/FEA Technologies → Specific Tool Validation) to eliminate confirmation bias.
  4. **Anti-Hallucination & Automated Parsing Guardrails:** Short processing loops, mandatory human-in-the-loop QA gate for automated PDF/OCR parsing (`markdown-books/` + `temp_images/` targeted formula healing — never blindly trust automated equation extraction), deterministic verification scripts, zero fabricated DOIs/URLs.
* **From where:** Researcher baseline manuscript (`Texto base para revista_Schapery.docx`) and explicit user directives.
* **Why:** Establishes editorial rigor, prevents confirmation bias in literature reviews, and guarantees verifiable scientific integrity.

---

### 2026-10-07 — Uniaxial Scope, Data Triad & FEBio Architecture Pivot
* **Task_ID:** `scope-uniaxial-and-architecture-pivot`
* **Category:** Decision (Scope & Software Architecture)
* **What:**
  1. **Uniaxial Isolated Focus:** Restricted thesis scope to uniaxial tensile testing of the 4 isolated porcine knee ligaments (ACL, PCL, MCL, LCL) from Dr. Rodrigo Rodarte's dataset; full-knee articular contact deferred to post-thesis work.
  2. **1D C# Engine + 3D FEBio Triad:** `MelloSilveiraTools` (C# / .NET 10) remains strictly **1D scalar** (curve fitting, simulation, sensitivity, data orchestration) to avoid reimplementing finite element meshing/solvers. 3D tensorial simulation is delegated to **FEBio** (open-source, native biphasic/soft-tissue support, C++ UDM extensibility).
  3. **Guiding Constitutive Models:** Initially 5 models (Fung QLV, Schapery, Generalized Maxwell/Prony, Weiss Transversely Isotropic, Poroviscoelasticity); expanded to **6 models** on 2026-10-08 with **Holzapfel-Gasser-Ogden (HGO)** based on Evidence Funnel findings.
  4. **UDM Time-Box Safety:** C++ UDM development for non-native FEBio models (e.g., Schapery) capped at a strict 4-week time-box with documented fallback to 1D comparison.
* **From where:** Researcher / Human directive and `biomechanics-co-advisor`.
* **Why:** Achieves a defensible, high-rigor Master's thesis within a 12-month window by leveraging C# fluency for 1D automation and open-source FEBio for 3D continuum validation.

---

### 2026-10-08 — Evidence Funnel Validation (301 Unique DOIs)
* **Task_ID:** `task-1.1`, `task-1.2`, `task-1.3`, `roadmap-revision-phase1-findings`
* **Category:** Decision & Execution
* **What:** Audited 358 reference records consolidated into **301 unique verified entries** ([`base-literature/evidence-funnel-bibliography.md`](../base-literature/evidence-funnel-bibliography.md)):
  * Confirmed hyperelastic + viscoelastic formulations (Fung QLV, Prony/Maxwell, Weiss, HGO) as dominant for knee ligaments;
  * Added **HGO** to capture collagen fiber angular dispersion ($\kappa$);
  * Validated **FEBio** over Abaqus/ANSYS for hydrated soft-tissue biomechanics and open-source reproducibility.
* **From where:** `senior-applied-biomechanics-researcher-deep-research` (validated via CrossRef/EuropePMC APIs).
* **Why:** Grounds constitutive model selection and solver choice in systematic empirical literature rather than intuition.

---

### 2026-10-08 to 2026-10-14 — 1D Constitutive Fitters & Sensitivity Analysis Engine
* **Task_ID:** `task-2.1`, `task-2.2`, `task-2.7`
* **Category:** Execution & Decision
* **What:**
  1. **Fung QLV 1D (`FungRelaxationOnlyCurveFitterStep`):** Two-stage decoupling (Stage 1: normalized relaxation for $c, \tau_1, \tau_2$ with $\tau_1 < \tau_2$ via ALGLIB Differential Evolution; Stage 2: ramp elastic constants $A, B$).
  2. **Schapery Non-Linear 1D (`SchaperyRelaxationOnlyCurveFitterStep`):** Three-stage hierarchical identification (Stage 1: linear anchor segment for $G_e, g_1, n$; Stage 2: pointwise Helmholtz multipliers $h_e, h_2$; Stage 3: continuous function competition among `Logarithmic`, `Exponential`, `Polynomial` selected by min SSR).
  3. **OAAT Sensitivity Engine (`ParametricSweepPipeline`):** Concurrent TPL Dataflow pipeline evaluating all 4 mandatory scenarios ($\sigma(t_0)$, $\sigma(t_f)$, $t_{\text{asymptote}}$, $\Delta\sigma / \%\Delta\sigma$) across Fung, Simplified Fung, and Schapery, using pre-compiled expression trees in `MechanicalModelTypeCache`.
* **From where:** `senior-backend-software-engineer-net` and `experimental-data-analyst`.
* **Why:** Eliminates parameter collinearity during non-linear regression, removes commercial toolbox dependency (MATLAB/OriginLab), and automates high-throughput parametric sensitivity analyses.

---

### 2026-10-16 — Storage Abstraction (`IFileStorageService`) & NDJSON Progress Streaming Architecture
* **Task_ID:** `task-storage-api-phase2-3`, `task-storage-api-phase4-5`, `task-storage-write-simplification`, `task-storage-create-and-optional-prefix`
* **Category:** Decision & Execution (Software Architecture)
* **What:**
  1. Standardized `IFileStorageService` with storage-agnostic partitioned paths (`Create` / `CreateTimebased`, optional prefix, `StorageEntryMetadataEntity` persisted in PostgreSQL `stored_files`).
  2. Standardized CSV pipeline persistence via composition (`CsvFileWriter` — 128 KB buffer, 1,000-row batching, UTF-8 no-BOM, `CultureInfo.InvariantCulture`).
  3. Adopted real-time pipeline progress streaming via `IPipelineProgressNotifier` (`ChannelPipelineProgressNotifier`) exposed as `application/x-ndjson` (`WriteNdjsonAsync` with `X-Stream-Status: true` trailer).
* **From where:** `senior-backend-software-engineer-net` (details tracked in `CHANGELOG.md` and package `README.md` files).
* **Why:** Decouples scientific file outputs from local host paths (enabling Local/S3/Blob/DFS portability) and gives long-running curve-fitting/sweep campaigns timeout-free observability.

---

### 2026-10-16 — Target Journal Mapping & Dual-Publication Strategy (JOSS + Scientific Article)
* **Task_ID:** `target-journals-and-dual-publication`
* **Category:** Decision
* **What:** Systematic 4-step bibliometric intersection over the 301 verified DOIs (137 journals → 16 Fung/QLV/Prony → 27 open-source/in-house → **8 exact intersection journals**). Selected dual-publication roadmap:
  1. **Primary Scientific Paper:** *Scientific Reports* (Top 1), *JBME/ASME* (Top 2), or *JMBBM/Elsevier* (Top 3) comparing Fung QLV continuous vs. Simplified Fung/Prony on knee ligaments.
  2. **Software Paper:** *Journal of Open Source Software* (**JOSS**) presenting `MelloSilveiraTools` under `GPL-3.0-or-later` (compatible with bundled ALGLIB `GPL-2.0-or-later`).
* **From where:** `senior-applied-biomechanics-researcher-deep-research` + `biomechanics-co-advisor` (see [`consolidated-joss-and-publications.md`](../consolidated-joss-and-publications.md)).
* **Why:** Maximizes academic impact by separating software engineering architecture (JOSS) from empirical biomechanical characterization (Q1/Q2 biomechanics journal).

---

### 2026-10-28 — Production-First Development Directive
* **Task_ID:** `production-first-anti-tdd-directive`
* **Category:** Decision
* **What:** Established **Production-First** engineering workflow (implement and stabilize clean production code and contracts before building/updating unit and acceptance test suites), explicitly avoiding test-first scaffolding churn.
* **From where:** User directive.
* **Why:** Prevents wasted effort refactoring brittle tests during rapid scientific API evolution while maintaining high final test coverage (757 unit tests + Testcontainers acceptance tests).

---

### 2026-11-09 — Canonical Biomechanical Formulations (`Consolidated_Biomechanical_Formulations.docx`)
* **Task_ID:** `register-sensitivity-theory-basis`, `advisor-academic-title-standardization`, `focus-strictly-on-stress-relaxation`, `chapter-3-didactic-progression-restructure`, `chapter-5-general-gradient-and-solver-formulations`, `streamline-axial-single-ligament-scope`, `dual-lagrangian-eulerian-experimental-bridge-and-operators`
* **Category:** Execution, Scope Refinement & Pedagogy
* **What:**
  1. Standardized advisor title to **Prof. Dr. Paulo Pedro Kenedi** (PPEMM/UFCIRJ).
  2. Consolidated all thesis equations into a single ABNT/PPEMM-formatted Word document ([`thesis/Consolidated_Biomechanical_Formulations.docx`](../thesis/Consolidated_Biomechanical_Formulations.docx), **246 sequential OMML equations**, 100% auto-black typography) generated deterministically by `scripts/build-consolidated-thesis.py`.
  3. **Strict Stress-Relaxation Scope:** Purged all creep compliance formulations and Kelvin-Voigt model.
  4. **Coaxial Uniaxial Specialization & Dual Formulations (Cap. 1):** Pruned rotational kinematics (Polar Decomposition, Spin, dynamic inertial terms); retained dual Lagrangian ($\mathbf{X}, \mathbf{U}, \mathbf{C}, \mathbf{E}$) and Eulerian ($\mathbf{x}, \mathbf{u}, \mathbf{B}, \mathbf{e}$) kinematics with explicit experimental LVDT/load-cell bridge, operator guide, isochoric area scaling ($A = A_0/\lambda$) backed by Flory/Ogden/Fung/Holzapfel/Humphrey/Weiss, and the 4 major 3D continuum theories (hyperelasticity, viscoelasticity, viscohyperelasticity, viscoporoelasticity).
  5. **Didactic Restructuring (Cap. 3 & Cap. 5):** Chapter 3 separates general hereditary convolution → step relaxation → elastic exponential law → continuous Fung ($E_1$) vs. discrete Simplified Fung (Prony). Chapter 5 isolates general sensitivity gradients, Levenberg-Marquardt, iso-stress continuation, and settling-time zero-crossing from model-specific partial derivatives (backed by 10 CrossRef/ISBN references in `theory-cheat-sheets/sensitivity-analysis-and-continuation.md`).
* **From where:** User directives executed by `academic-writer-biomechanics-specialist` and `senior-applied-biomechanics-researcher-deep-research`.
* **Why:** Eliminates duplicated/divergent Word drafts, aligns 100% of mathematical formulations with the uniaxial stress-relaxation experimental protocol, and delivers publication-ready thesis material.

---

### 2026-11-10 — Soft Tissues vs. Polymers Comparative Mechanics (60 Verified Studies)
* **Task_ID:** `task-deep-research-soft-tissue-vs-polymers-comparative-mechanics`
* **Category:** Execution & Opportunity/Gap
* **What:** Audited 60 peer-reviewed papers (100% CrossRef-validated DOIs) in [`base-literature/comparative-mechanics-soft-tissues-vs-polymers.md`](../base-literature/comparative-mechanics-soft-tissues-vs-polymers.md) across 5 domains: toe-region crimp vs. entropic elasticity; viscoelastic relaxation/creep; preconditioning vs. Mullins effect; multiscale fracture toughness vs. Lake-Thomas limit; anisotropic fibrous vs. isotropic hyperelastic modeling.
* **From where:** `senior-applied-biomechanics-researcher-deep-research`.
* **Why:** Provides empirical justification for adapting polymer-origin models (Schapery, Prony series) to knee ligaments and surfaces future research avenues (fractional viscoelasticity, damage vs. preconditioning, synthetic UHMWPE/PET grafts).

---

### 2026-11-17 — Explainable AI (XAI) & Theoretical-Experimental Traceability Directive
* **Task_ID:** `task-future-works-explainable-ai-traceability`
* **Category:** Future Work & Methodological Directive
* **What:** Any final analysis, conclusion, or report — even when synthesized by AI agents — must explicitly expose: (1) full step-by-step analytical/computational derivation; (2) formal theoretical backing (kinematic hypotheses, tensors/scalars, constitutive equations); and (3) contextualized experimental grounding (specimen, $L_0$, deformation phase segmentation, fitted parameters, $R^2$/RMSE).
* **From where:** User directive (2026-11-17).
* **Why:** Eliminates black-box outputs and guarantees full scientific auditability for the Master's defense and international peer-reviewed journals.
