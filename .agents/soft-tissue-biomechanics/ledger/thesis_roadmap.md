# Macro Research Planning (1-Year Restriction)

**Provisional Title:** Comparative Analysis of Non-Linear and Viscoelastic Constitutive Models Applied to Porcine Knee Ligaments under Uniaxial Tension.
**Deadline Restriction:** 12 months until defense.
**Architectural Directive:** Strict focus on uniaxial loading, anatomical isolation, and deep statistical comparison between theoretical models against empirical experimental data.
**Core Philosophy (The "Why"):** The documentation of the *reasons* and rationale behind every decision is the single most critical aspect of this research. At every step, the "Why" must be explicitly recorded and justified in the ledger (e.g., why use C#, why choose FEBio, why select a particular mathematical formulation). The justification is always more important than the action itself.

---

## PHASE 1: Review and Foundation (The Theoretical Base)
- **Objective:** Establish a solid theoretical framework from an "Evidence Funnel".
- **Deliverables:**
  - Survey of constitutive models used for soft tissues and knee ligaments.
  - Theoretical mathematical Cheat Sheets of the selected models.

## PHASE 2: Computational Engineering (C# Architecture and Processing)
- **Objective:** Establish a C# ecosystem focused on orchestrating data and processing scalar curves.
- **Deliverables:**
  - **Frontend:** Interactive visual layer for dynamic plot generation and exploration.
  - **Backend (MelloSilveiraTools):** Reusable 1D math, curve fitting, and a sensitivity analysis engine via NuGet.
  - **Backend (SoftTissue):** Orchestrating core of the 3 data sources.

## PHASE 3: External 3D Simulation and Data Aggregation
- **Objective:** Close the data triad (1. Real Data, 2. C# 1D Simulation, 3. FEBio 3D Simulation) and process them for the frontend.
- **Deliverables:**
  - 1D Curve Fitting of porcine ligament experimental data (obtained from Dr. Rodrigo Rodarte's thesis).
  - UDM plugin compilation for missing models in FEBio.
  - 3D tensorial simulation in open-source software (FEBio).

## PHASE 4: Comparative Analysis and Writing (The Core of the Thesis)
- **Objective:** Contrast the models and define limitations.
- **Deliverables:**
  - Model Battle Tables comparing execution time, machine resources, iterations, and mathematical fit (RMSE, $R^2$).
  - Python scripts for generating high-resolution, static publication-ready graphs (complementing the dynamic frontend).
  - Thesis defense.

---

## The Atomic Backlog (Micro-Tasks)

## PHASE 1: Review and Foundation
*Status: In progress*

- **[x] TASK 1.1:** [FUNNEL - STEP 1] Survey the main constitutive models used for soft tissues. *(Ensure "Why" is logged)*. (COMPLETED)
- **[x] TASK 1.2:** [FUNNEL - STEP 2] Narrow the research to models used for knee ligaments. *(Ensure "Why" is logged)*. (COMPLETED)
- **[x] TASK 1.3:** [FUNNEL - STEP 3] Survey FEA technologies. Document the formal justification for using FEBio (open-source) over others. (COMPLETED)
- **[x] TASK 1.4:** Write the mathematical Cheat Sheet for Fung's QLV 3D model. (COMPLETED)
- **[ ] TASK 1.5:** Write the mathematical Cheat Sheet for the **Schapery Non-Linear Model in 3D Tensorial formulation** (uniaxial tension). *(Note: 1D scalar equations are already held by the researcher. Focus strictly on 3D tensorial math).*
- **[ ] TASK 1.6:** Write the mathematical Cheat Sheet for the 3D tensorial formulation of the **Generalized Maxwell / Prony Series**. *(Note: 1D scalar equations are already held by the researcher).*
- **[ ] TASK 1.7:** Write the mathematical Cheat Sheet for the 3D tensorial formulation of the **Weiss Transversely Isotropic Hyperelastic Model**. *(Note: 1D scalar equations are already held by the researcher).*
- **[ ] TASK 1.8:** Write the mathematical Cheat Sheet for the 3D tensorial formulation of the **Poroviscoelasticity** model. *(Note: Typically used for cartilage; investigate and justify application to ligaments. 1D equations are held by the researcher).*
- **[ ] TASK 1.9:** Write the mathematical Cheat Sheet for the **Holzapfel-Gasser-Ogden (HGO) structural model**, highlighting its application to collagen fiber dispersion in knee ligaments.

---

## PHASE 2: Computational Engineering (C# Architecture)
*Status: To Do (MelloSilveiraTools + SoftTissue)*

- **[x] TASK 2.1:** Develop the *Curve Fitting* objective functions for the **Fung QLV 1D** model within the **MelloSilveiraTools** package. (COMPLETED)
- **[ ] TASK 2.2:** Develop the *Curve Fitting* objective functions for the **Schapery Non-Linear 1D** model within **MelloSilveiraTools**.
- **[ ] TASK 2.3:** Develop the *Curve Fitting* objective functions for the **Generalized Maxwell / Prony Series 1D** within **MelloSilveiraTools**.
- **[ ] TASK 2.4:** Develop the *Curve Fitting* objective functions for the **Weiss Transversely Isotropic 1D** model within **MelloSilveiraTools**.
- **[ ] TASK 2.5:** Develop the *Curve Fitting* objective functions for the **Poroviscoelasticity 1D** model within **MelloSilveiraTools**.
- **[ ] TASK 2.6:** Develop the *Curve Fitting* objective functions for the **Holzapfel-Gasser-Ogden (HGO) 1D** model within **MelloSilveiraTools**.
- **[ ] TASK 2.7:** Develop and validate the native **1D Sensitivity Analysis Engine** (stress x time, stress x variation, asymptotes) within **MelloSilveiraTools**.
- **[ ] TASK 2.8:** Inspect and clean the legacy code of the **SoftTissue** project to centralize business rules.
- **[ ] TASK 2.9:** Create the logic, pipelines, and routes in **SoftTissue** aggregating the data triad.
- **[ ] TASK 2.10:** Structure a **Minimalist Frontend** focused exclusively on dynamic data exploration and triggering C# pipelines, strictly avoiding complex UI/UX engineering that drains research time.

---

## PHASE 3: 3D Simulation and Data Aggregation
*Status: To Do*

- **[ ] TASK 3.1:** Ingest real raw porcine experimental data (from Dr. Rodrigo Rodarte) into the C# API and run the numerical optimizer (MelloSilveiraTools) to generate the scalar constants.
- **[ ] TASK 3.2:** Run the step-by-step 1D numerical simulation in C# to extract the "Scalar" curve.
- **[ ] TASK 3.3:** Investigate literature methods to convert/recalibrate the extracted 1D scalar constants into valid 3D tensorial parameters.
- **[ ] TASK 3.4:** Program custom User-Defined Material (UDM) plugins in C++ for missing models in FEBio (e.g., Schapery). *(Safety Directive: Establish a strict 4-week Time-Box. If numerical convergence in C++ fails, restrict Schapery to C# 1D comparison and proceed with native FEBio models for 3D).* 
- **[ ] TASK 3.5:** Configure geometries, boundaries, and the converted 3D constants within **FEBio**.
- **[ ] TASK 3.6:** Run the FEBio Solver and export the results. Ingest back into SoftTissue Database.

---

## PHASE 4: Comparative Analysis and Writing (The Thesis Document)
*Status: To Do*

- **[ ] TASK 4.1:** Calculate statistical error metrics (RMSE, $R^2$) and extract execution metadata (machine resources, execution time, precision, number of iterations) comparing the 1D, 3D, and experimental results for all 5 models.
- **[ ] TASK 4.2:** Plot static Comparative Graphs (via Python script for high-res publication) superimposing experimental data and theoretical curves for **Fung, Schapery, Maxwell, Weiss, Poroviscoelasticity, and HGO**.
- **[ ] TASK 4.3:** Write the **"Introduction and Theoretical Framework"** chapters.
- **[ ] TASK 4.4:** Write the **"Materials and Methods"** chapter.
- **[ ] TASK 4.5:** Write the **"Results"** chapter.
- **[ ] TASK 4.6:** Write the **"Discussion and Conclusion"** chapters. *(Explicitly discuss Weiss hyperelasticity showing initial stress = final stress with zero relaxation).*
- **[ ] TASK 4.7:** Pass all chapters through the *High-Standard Paper Reviewer*.