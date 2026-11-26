# Architectural & Implementation Plan — Module 07: FEBio Integration & 3D Tensorial Simulation

## 1. Overview & Objectives

Specifies the theoretical foundation, software architecture, and implementation phases for **3D Finite Element Tensorial Simulation via FEBio** integrated with the `MelloSilveiraTools` ecosystem:

1. Close the research **Data Triad**: (1) Real Experimental Data, (2) 1D Analytical-Numerical C# Simulation, and (3) 3D Tensorial FEBio Simulation.
2. Mathematically formulate the 3D Continuum Mechanics generalization of the investigated constitutive models.
3. Implement an analytical $1\text{D} \to 3\text{D}$ parameter recalibration routine converting 1D scalar parameters identified in C# into equivalent 3D material tensor constants for uniaxial ligament tension.
4. Develop a C++ User-Defined Material (UDM) subroutine for models lacking native FEBio support (focusing on Schapery's non-linear viscoelasticity) under a fixed effort budget.
5. Build an automated Headless Runner generating `.feb` files, invoking the solver in batch, extracting stress-time histories, and persisting outputs to PostgreSQL (`storage_entry_metadata` via `StorageEntryMetadataEntity`).

---

## 2. Strategic & Methodological Rationale (The "Why")

* **FEBio over proprietary solvers (Abaqus, Ansys, COMSOL):** Open-source, free platform tailored to soft-tissue biomechanics; allows full auditing of non-linear/biphasic source code and eliminates lab license-server barriers, ensuring unrestricted reproducibility.
* **3D simulation in FEBio rather than C#:** `MelloSilveiraTools` remains strictly a fast 1D scalar calibrator and data orchestrator. Rebuilding 3D meshers, isoparametric Gauss integration, contact mechanics, and non-linear sparse solvers in C# would derail research focus.
* **Uniaxial tension on isolated ligaments:** Full anatomical knee simulations with articular contact exhibit high non-convergence rates and prohibitive compute cost. Uniaxial loading on isolated ligaments reproduces the mechanical test bench and isolates constitutive effects cleanly.
* **Fixed 4-cycle C++ UDM budget & contingency trigger:** Custom FEBio materials require deriving and implementing the consistent tangent elasticity-viscoelasticity tensor ($C_{ijkl} = 2\frac{\partial \boldsymbol{S}}{\partial \boldsymbol{C}}$). To prevent endless low-level C++ debugging, effort is capped at 4 cycles; if convergence fails, activate the approved Scientific Ledger fallback (keep Schapery in 1D C# comparison and run FEBio with native Fung QLV and HGO models).

> Consolidated references: [`consolidated-thesis-guide.md`](../../soft-tissue-biomechanics/consolidated-thesis-guide.md) and [`ledger/decisions.md`](../../soft-tissue-biomechanics/ledger/decisions.md).

---

## 3. Architecture & Components

**Integration Flow:**
`MelloSilveiraTools (1D Fit + Analytical 1D→3D Recalibrator)` → `FebioAutomationRunner` (populate XML `.feb` template → `febio4 -i input.feb -silent` → parse `.xplt`/`.log` → extract 3D $\sigma(t)$ and $F(t)$ series) → `IFileStorageService` + `StorageEntryMetadataEntity` (`storage_entry_metadata`) → Frontend & Publication Pipeline (Data Triad).

### 3.1 Backend Extensions (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`)
* `ConstitutiveModels/Recalibration/IConstitutiveParameter3DRecalibrator.cs`: Contract mapping 1D scalar constants into 3D material tensors.
* `ConstitutiveModels/Recalibration/FungQlv3DRecalibrator.cs`: Analytical mapping for Fung QLV in FEBio.
* `ConstitutiveModels/Recalibration/Hgo3DRecalibrator.cs`: Structural fiber dispersion mapping for HGO.
* `ExternalSolvers/Febio/FebioInputGenerator.cs`: Parametric `.feb` XML generator.
* `ExternalSolvers/Febio/FebioLogParser.cs`: Parser extracting axial Cauchy stress and reaction force histories.

### 3.2 C++ UDM Subroutine (`external/febio-udm/`)
* `SchaperyMaterial.h`: Class derived from `FEViscoElasticMaterial` (or `FEMaterial`).
* `SchaperyMaterial.cpp`: Computes Cauchy stress $\boldsymbol{\sigma}(t)$, internal history variables, and consistent spatial tangent tensor $\mathbb{C}$.
* `CMakeLists.txt`: Cross-platform build producing `.dll` (Windows) / `.so` (Linux) plugins loadable by `febio4`.

---

## 4. Implementation Phases

### Phase 1: Formal 3D Tensorial Cheat Sheets
1. Author the 5 formal cheat sheets in `.agents/soft-tissue-biomechanics/base-literature/theory-cheat-sheets/`:
   * `schapery-3d-tensorial.md`
   * `generalized-maxwell-prony-3d.md`
   * `weiss-transversely-isotropic-3d.md`
   * `poroviscoelasticity-3d.md` *(conditional on retention in thesis scope)*
   * `hgo-collagen-dispersion-3d.md`
2. Validate thermodynamic consistency and variable definitions with `biomechanics-co-advisor`.

### Phase 2: $1\text{D} \to 3\text{D}$ Parameter Recalibration Routine
1. Formulate uniaxial isochoric equivalence along axis 1: $\lambda_1 = \lambda$, $\lambda_2 = \lambda_3 = \lambda^{-1/2}$; Green-Lagrange strain $\boldsymbol{E} = \frac{1}{2}(\boldsymbol{C} - \boldsymbol{I})$; map 1D constants ($G_e, g_1, n, c, A, B$) into FEBio material inputs.
2. Implement C# recalibration classes with full unit test coverage in `test/UnitTests`.
3. Verify numerically that, under identical uniaxial strain histories, the 3D analytical formulation degenerates exactly to the 1D scalar curve.

### Phase 3: Native Models & C++ UDM Budgeted Cycle
1. **Native Models:** Configure `.feb` templates for **Fung QLV** and **HGO** on a validated cylindrical uniaxial test geometry under identical ramp-and-hold time steps.
2. **Schapery Non-Linear Model (Strict 4-Cycle Cap):**
   * *Cycle 1:* Implement stress tensor and internal history state variables.
   * *Cycle 2:* Implement consistent tangent stiffness tensor.
   * *Cycle 3:* Compile `.dll`/`.so` and wire into FEBio plugin loader.
   * *Cycle 4:* Run large-strain uniaxial convergence tests.
   * **Stop & Contingency Trigger:** If stable after Cycle 4, promote UDM; if unstable/non-convergent, stop C++ debugging, retain Schapery in 1D C# analysis, execute FEBio exclusively with native models (Fung QLV and HGO), and **document physical/numerical causes thoroughly in Discussion**.

### Phase 4: Headless Automation & Result Ingestion
1. Implement `FebioAutomationRunner`: load recalibrated constants, generate `.feb`, invoke `febio4 -i input.feb -silent`, read nodal displacement and top reaction force, and compute isochoric Cauchy stress $\sigma(t) = F(t)/A(t)$.
2. Persist outputs via `IFileStorageService` and `StorageEntryMetadataEntity` in `storage_entry_metadata` (linked via `ExternalIdentifier`, e.g., `"FEBio3DSimulation_{SampleId}"`).

### Phase 5: Data Triad Validation
1. Run end-to-end across porcine ligament test sets.
2. Confirm convergence and compare 3D FEBio curves against 1D C# fits and raw experimental data.
3. Expose 3D series in Web API endpoints for frontend and publication consumption.

---

## 5. Acceptance & Validation Criteria

* [ ] All 5 3D tensorial cheat sheets derived and approved by `biomechanics-co-advisor`.
* [ ] Analytical $1\text{D} \to 3\text{D}$ recalibrator implemented in C# and covered by unit tests in `test/UnitTests`.
* [ ] Native FEBio simulations (Fung QLV and HGO) converging for uniaxial ramp-and-hold specimens.
* [ ] Schapery UDM contingency cap strictly enforced at 4 development cycles.
* [ ] `FebioAutomationRunner` executing headless and recording outputs in `storage_entry_metadata` (`StorageEntryMetadataEntity`) without manual intervention.
* [ ] 3D curves available for interactive overlay in WebUi and static plots in the publication pipeline.
