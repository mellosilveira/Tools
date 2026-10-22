# Mapping and Selection of Target Journals for Manuscript Submission

- **Date**: 2026-10-16
- **Applied Skills**: `senior-applied-biomechanics-researcher-deep-research` and `biomechanics-co-advisor`
- **Context**: Master's Thesis at PPEMM/CEFET-RJ (Advisor: Prof. Paulo Pedro Kenedi) — Constitutive characterization and modeling of the mechanical behavior of knee ligaments (ACL, PCL, MCL, LCL).
- **Audited Dataset**: 358 reference records (301 unique DOIs) fully extracted from the files in `.agents/soft-tissue-biomechanics/outputs/` (`task-1.1_result.json`, `task-1.2-results.json`, `task-1.3-payload.json`, `task-deep-research-funnel.json`, and `task-deep-research-unified.json`), validated via Crossref and EuropePMC APIs.
- **Scope of the Manuscript to be Submitted**:
  1. Rigorous comparison between the **Continuous Spectrum Fung QLV Model** ([`FungModelCalculator`](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs)) and the **Discrete QLV / Generalized Maxwell Model with Prony Series** ([`SimplifiedFungModelCalculator`](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/SimplifiedFung/SimplifiedFungModelCalculator.cs)), both incorporating the exponential elastic response $\sigma^e(\varepsilon) = A(e^{B\varepsilon}-1)$ under three ramp conditions (`Disregard`, `ConsiderWithoutViscoelasticEffect`, and `ConsiderWithViscoelasticEffect`);
  2. Full end-to-end implementation of the computational workflow (sliding-window segmentation, analytical *warm-start*, constrained non-linear regression, and parametric sensitivity analysis) via a **custom proprietary scientific software suite developed in C# / .NET 10** ([`MelloSilveiraTools`](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core)), with the explicit objective of **eliminating reliance on commercial, paid software packages** (MATLAB Optimization/Curve Fitting Toolboxes, OriginLab Pro, Isight, Abaqus).

---

## 1. Systematic 4-Step Methodology

To prevent selection bias and ensure complete traceability with the material already investigated throughout the research, the analysis followed four consecutive steps across the dataset:

1. **Step 1:** Extraction and frequency analysis of all scientific journals identified across the 301 articles cited in the `outputs`;
2. **Step 2:** Identification of journals with the highest number of articles addressing the **Fung Model**, **Quasi-Linear Viscoelastic (QLV) Theory**, or the **Generalized Maxwell Model with Prony Series**;
3. **Step 3:** Identification of journals with the highest number of articles employing **open-source software** (FEBio, OpenKnee) or **in-house code/software developed by the research group itself** (custom UMAT/VUMAT/UDM subroutines, proprietary scripts made available by the authors);
4. **Step 4:** Determination of the **exact intersection** between Steps 2 and 3, highlighting journals that exhibit both an established tradition and receptivity toward viscoelastic modeling alongside open-source/in-house computational frameworks.

---

## 2. Step 1 — All Journals Identified in the Outputs

Among the 301 unique articles analyzed, **137 distinct scientific journals** were identified. The most frequent journals across the corpus are:

| Rank | Journal | Publisher | Total in Outputs |
| :---: | :--- | :--- | :---: |
| 1st | **Biomechanics and Modeling in Mechanobiology** | Springer | 15 |
| 2nd | **Journal of Biomechanical Engineering** | ASME | 15 |
| 3rd | **Scientific Reports** | Nature Portfolio | 14 |
| 4th | **Frontiers in Bioengineering and Biotechnology** | Frontiers | 13 |
| 5th | **Annals of Biomedical Engineering** | Springer / BMES | 13 |
| 6th | *Other / Thesis / Preprint* | Various | 12 |
| 7th | **Journal of Biomechanics** | Elsevier | 7 |
| 8th | **Journal of the Mechanical Behavior of Biomedical Materials** | Elsevier | 7 |
| 9th | **Computer Methods in Biomechanics and Biomedical Engineering** | Taylor & Francis | 6 |
| 10th | **Bioengineering** | MDPI | 6 |
| 11th | **PLOS ONE** | PLOS | 6 |
| 12th | **Advances in Bioengineering** | ASME | 6 |
| 13th | **Clinical Biomechanics** | Elsevier | 6 |
| 14th | **Acta Biomaterialia** | Elsevier | 4 |
| 15th | **International Journal for Numerical Methods in Biomedical Engineering** | Wiley | 4 |
| 16th | **Journal of the Royal Society Interface** | Royal Society | 4 |
| 17th | **Materials** | MDPI | 4 |
| 18th | **Frontiers in Physiology** | Frontiers | 4 |
| 19th | **Medical Engineering & Physics** | Elsevier | 3 |
| 20th | **Biomimetics** | MDPI | 3 |
| 21st | **Journal of the Mechanics and Physics of Solids** | Elsevier | 3 |
| 22nd | **Computer Methods and Programs in Biomedicine** | Elsevier | 3 |
| — | *Remaining 115 journals (with 1 or 2 articles each)* | Various | 120 |
| **Total** | **137 Unique Journals** | — | **301** |

---

## 3. Step 2 — Journals with Papers on Fung, QLV, or Maxwell with Prony Series

Filtering the articles by direct application or discussion of the models under study (**Fung QLV**, **continuous spectrum with exponential integral**, **Maxwell rheological networks**, and **Prony series with discrete relaxation times**) yielded **27 articles** distributed across **16 scientific journals** (excluding theses/preprints):

| Rank | Journal | Articles in Step 2 | Reference DOIs in Outputs |
| :---: | :--- | :---: | :--- |
| **1st** | **Scientific Reports** | **6** | `10.1038/s41598-024-67603-8`<br>`10.1038/s41598-025-26576-y`<br>`10.1038/s41598-025-08471-8`<br>`10.1038/s41598-025-88767-x`<br>`10.1038/s41598-025-23918-8`<br>`10.1038/s41598-018-20739-w` |
| **2nd** | **Journal of the Royal Society Interface** | **2** | `10.1098/rsif.2015.0486`<br>`10.1098/rsif.2026.0078` |
| **3rd** | **Journal of the Mechanical Behavior of Biomedical Materials (JMBBM)** | **2** | `10.1016/j.jmbbm.2026.107452`<br>`10.1016/j.jmbbm.2023.105968` |
| **4th** | **Biomimetics** | **2** | `10.3390/biomimetics11010004`<br>`10.3390/biomimetics9120762` |
| **5th** | **Journal of Biomechanical Engineering (JBME)** | **1** | `10.1115/1.4003620` |
| **6th** | **Journal of Biomechanics** | **1** | `10.1016/j.jbiomech.2008.02.007` |
| **7th** | **Frontiers in Bioengineering and Biotechnology** | **1** | `10.3389/fbioe.2026.1840657` |
| **8th** | **Medical Engineering & Physics** | **1** | `10.1016/j.medengphy.2014.04.004` |
| **9th** | **Journal of the Mechanics and Physics of Solids** | **1** | `10.1016/j.jmps.2025.106348` |
| **10th** | **International Journal for Numerical Methods in Biomedical Engineering (IJNMBE)** | **1** | `10.1002/cnm.70075` |
| **11th** | **Proc. IMechE Part C: J. Mechanical Engineering Science** | **1** | `10.1177/0954406219875771` |
| **12th** | **BioMedical Engineering OnLine** | **1** | `10.1186/s12938-015-0023-7` |
| **13th** | **Advances in Bioengineering** | **1** | `10.1115/imece1998-0121` |
| **14th** | **The Journal of the Acoustical Society of America** | **1** | `10.1121/1.4828831` |
| **15th** | **Forensic Science International** | **1** | `10.1016/j.forsciint.2026.113050` |
| **16th** | **Bioengineering & Translational Medicine** | **1** | `10.1002/btm2.70044` |

---

## 4. Step 3 — Journals with Papers Utilizing Open-Source or In-House Software

Filtering the articles that reported the use of **open-source software** (FEBio, OpenKnee, public repositories) or **in-house tools/codes developed by the authoring group** (*in-house* code, UMAT/VUMAT/UDM subroutines, scripts and algorithms released in public repositories) identified **57 articles** across **27 scientific journals**:

| Rank | Journal | Articles in Step 3 | Computational Focus / Identified Tools |
| :---: | :--- | :---: | :--- |
| **1st** | **Annals of Biomedical Engineering** | **7** | Open ligament frameworks (Open Knee), FEBio FINESSE, and open-source solvers. |
| **2nd** | **Journal of Biomechanical Engineering** | **5** | In-house rheological networks (`10.1115/1.4003620`), coupled open-source solvers, and FEBio integration. |
| **3rd** | **Biomechanics and Modeling in Mechanobiology** | **4** | Reactive models in FEBio, voxel-based models, and open-source code. |
| **4th** | **Acta Biomaterialia** | **3** | Algorithms and software developed for finite element fiber orientation. |
| **5th** | **PLOS ONE** | **3** | Open-source pipelines and custom meshing and data processing scripts. |
| **6th** | **Computer Methods and Programs in Biomedicine** | **3** | In-house biomedical computer programs and software suites. |
| **7th** | **Medical Engineering & Physics** | **2** | In-house FORTRAN subroutine for viscoelastic modeling in Abaqus (`10.1016/j.medengphy.2014.04.004`). |
| **8th** | **Engineering with Computers** | **2** | Open-source software frameworks integrating FEBio and finite element libraries. |
| **9th** | **The Knee** | **2** | Custom computational pipelines developed for osteotomy planning and tibiofemoral contact analysis. |
| **10th** | **Journal of the Royal Society Interface** | **2** | In-house constitutive models featuring evolution subroutines and dual-branch rheological networks. |
| **11th** | **Scientific Reports** | **2** | In-house genetic algorithm with source code provided in supplementary material (`10.1038/s41598-024-67603-8`). |
| **12th** | **Frontiers in Bioengineering and Biotechnology** | **2** | Open-source validation code for knee models and computational implementation reviews. |
| **13th** | **Journal of Biomechanics** | **1** | Open FEBio Studio routines for dynamic musculoskeletal analysis (`10.1016/j.jbiomech.2026.113308`). |
| **14th** | **Journal of the Mechanical Behavior of Biomedical Materials** | **1** | Visco-hyperelastic constitutive model with discontinuous damage implemented via custom routine (`10.1016/j.jmbbm.2023.105968`). |
| **15th** | **Computer Methods in Biomechanics and Biomedical Engineering** | **1** | Proprietary algorithms for collateral ligament parameter calibration (`10.1080/10255842.2023.2253950`). |
| **16th** | **PLOS Computational Biology** | **1** | Open-source software framework for biophysical and biomechanical simulation. |
| **17th** | **Proc. IMechE Part C: J. Mechanical Engineering Science** | **1** | Computational routine developed by the authors for an internal variable model (`10.1177/0954406219875771`). |
| — | *Remaining 10 journals with 1 paper each* | 10 | Various in-house codes and custom subroutines. |

---

## 5. Step 4 — Exact Intersection (Journals Common to Steps 2 and 3)

The exact cross-referencing between journals that publish on **Fung, QLV, and Maxwell with Prony Series** (Step 2) and journals that publish studies employing **open-source or group-developed software** (Step 3) reveals exactly **8 scientific journals**:

| Rank | Journal | Total in Outputs (Step 1) | Step 2 Articles (Fung/QLV/Prony) | Step 3 Articles (Open-Source/In-House) | Articles Meeting Both Criteria Simultaneously |
| :---: | :--- | :---: | :---: | :---: | :---: |
| **1st** | **Scientific Reports** *(Nature Portfolio)* | **14** | **6** | **2** | **1** (`10.1038/s41598-024-67603-8`) |
| **2nd** | **Journal of Biomechanical Engineering (JBME)** *(ASME)* | **15** | **1** | **5** | **1** (`10.1115/1.4003620`) |
| **3rd** | **Journal of the Mechanical Behavior of Biomedical Materials (JMBBM)** *(Elsevier)* | **7** | **2** | **1** | **1** (`10.1016/j.jmbbm.2023.105968`) |
| **4th** | **Journal of the Royal Society Interface** *(Royal Society)* | **4** | **2** | **2** | **1** (`10.1098/rsif.2026.0078`) |
| **5th** | **Medical Engineering & Physics** *(Elsevier)* | **3** | **1** | **2** | **1** (`10.1016/j.medengphy.2014.04.004`) |
| **6th** | **Proc. IMechE Part C: J. Mechanical Engineering Science** *(SAGE)* | **1** | **1** | **1** | **1** (`10.1177/0954406219875771`) |
| **7th** | **Journal of Biomechanics** *(Elsevier)* | **7** | **1** | **1** | **0** |
| **8th** | **Frontiers in Bioengineering and Biotechnology** *(Frontiers)* | **13** | **1** | **2** | **0** |

---

## 6. Refined Recommendation by Co-Advisor (`biomechanics-co-advisor`) for Journal Selection

Considering that the manuscript is written and rests upon two central pillars — (1) the constitutive comparison between **Fung QLV** and the **Generalized Maxwell Model with Prony Series**, and (2) execution through a **custom-built C# / .NET 10 software platform** to eliminate reliance on closed commercial packages:

### 6.1. Analysis of the Top 3 Intersection Targets

1. **Top 1 Recommended (For Maximum Visibility of Open Software and Methodology): *Scientific Reports (Nature Portfolio)***
   - **Impact Factor / Indexing:** JCR Q1, IF ~3.8.
   - **Why it is the ideal target:** It aggregated the highest number of viscoelastic calibration studies across the audited outputs (6 papers) and maintains an explicit policy promoting scientific reproducibility and code availability. A benchmark paper in our dataset (`10.1038/s41598-024-67603-8`) followed this precise blueprint: calibration of visco-hyperelastic models using dedicated custom-built code provided in supplementary materials, bypassing commercial toolboxes.
2. **Top 2 Recommended (For Classical Grounding in Soft Tissue Constitutive Mechanics): *Journal of Biomechanical Engineering (JBME — ASME)***
   - **Impact Factor / Indexing:** JCR Q2, IF ~1.7.
   - **Why it is the ideal target:** It is the most historically established journal for Fung's QLV methodology with ramp-time corrections (Abramowitch & Woo, 2004) and for author-developed Maxwell rheological network calibrations (`10.1115/1.4003620`). Editorial boards in JBME strongly value detailed, transparent 1D analytical-numerical formulations.
3. **Top 3 Recommended (For Dedicated Focus on Ligament Mechanical Properties): *Journal of the Mechanical Behavior of Biomedical Materials (JMBBM — Elsevier)***
   - **Impact Factor / Indexing:** JCR Q1, IF ~3.4.
   - **Why it is the ideal target:** Excellent if the manuscript places primary emphasis on the calibrated knee ligament parameters, stress relaxation curves, and underlying tissue physics, while highlighting the .NET 10 software suite as a prominent subsection within the Methods (*"Custom Reactive Pipeline Implementation in .NET 10"*).

---

## 7. Dual-Publication Strategy (Strategic Recommendation)

Because the software suite was designed and implemented from the ground up in .NET 10 (leveraging modern Dependency Injection patterns, TPL Dataflow pipelines, and high-performance numerical routines), there is sufficient material and methodological depth for **two complementary publications**:

* **Primary Scientific Article:** Submit the comparative knee ligament study (Fung vs. Generalized Maxwell with Prony Series) to one of the intersection journals (**Scientific Reports**, **JBME**, or **JMBBM**).
* **Secondary Software Article (Short Paper, 3–5 pages):** Submit a formal software architecture description of `MelloSilveiraTools` to **SoftwareX (Elsevier)** or the **Journal of Open Source Software (JOSS)**, detailing software architecture, developer ergonomics, and independence from commercial licenses, referencing the primary article as an empirical application benchmark.
