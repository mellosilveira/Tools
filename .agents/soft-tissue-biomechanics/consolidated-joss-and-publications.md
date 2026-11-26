# Consolidated Publication Guide — JOSS & Primary Scientific Article

> **Master Publication Document** — Consolidates the dual publication strategy for `MelloSilveiraTools` + PPEMM/UFCIRJ Master's Thesis: (1) software paper submission to *Journal of Open Source Software* (**JOSS**) / *SoftwareX*, and (2) complete 4-stage systematic journal selection for the comparative soft-tissue knee-ligament biomechanics paper.
>
> *Last consolidated: 2026-11-26*

---

## 1. Dual Publication Strategy

The engineering and scientific depth achieved supports **two complementary, synergistic publications**:

1. **Software Paper (Short, 3–5 pages) — *Journal of Open Source Software* (JOSS)** *(secondary alternative: **SoftwareX — Elsevier**)*:
   * **Focus:** Software engineering architecture of the `.NET 10 / C# 13` `MelloSilveiraTools` suite, modularity across 8 NuGet packages, developer ergonomics, independence from proprietary licenses (MATLAB Optimization/Curve Fitting Toolboxes, OriginLab Pro, Isight, Abaqus), constrained hybrid optimization (ALGLIB Differential Evolution + L-BFGS), backpressure-aware TPL Dataflow pipelines, and scientific traceability.
   * **Manuscript:** [`paper/paper.md`](../../paper/paper.md) and [`paper/paper.bib`](../../paper/paper.bib) (automated PDF compilation via `.github/workflows/draft-pdf.yml`).
2. **Primary Scientific Article — International Biomechanics Journal (*Scientific Reports*, *JBME*, or *JMBBM*):**
   * **Focus:** Rigorous comparison between **Fung QLV (Continuous Spectrum $E_1$)** (`FungModelCalculator`) and **Simplified Fung / Generalized Maxwell (Prony Series)** (`SimplifiedFungModelCalculator`), both using the exponential elastic response $\sigma^e(\varepsilon) = A(e^{B\varepsilon}-1)$ across the three ramp regimes (`Disregard`, `ConsiderWithoutViscoelasticEffect`, and `ConsiderWithViscoelasticEffect`), executed end-to-end on knee ligaments (ACL, PCL, MCL, LCL) using `MelloSilveiraTools`.

---

## 2. JOSS Submission — `MelloSilveiraTools`

### 2.1. Canonical Metadata & Legal Compliance
* **Manuscript Title (`paper/paper.md`):** *MelloSilveiraTools: A .NET 10 Computational Framework for Viscoelastic Constitutive Modeling, Numerical Analysis, and Experimental Data Optimization*
* **Authors:**
  * **Bruno Mello Silveira** (`ORCID: 0000-0001-6793-9387`) — Corresponding Author (`affiliation: "1, 2"`)
  * **Prof. Dr. Paulo Pedro Kenedi** (`ORCID: 0000-0001-5563-843X`) — Coauthor / Thesis Advisor (`affiliation: 1`)
* **Affiliations:**
  1. *Graduate Program in Mechanical Engineering and Materials Technology (PPEMM), Universidade Federal de Ciência e Inovação do Rio de Janeiro (UFCIRJ), Rio de Janeiro, Brazil*
  2. *Mello Silveira Serviços LTDA, Pinheiros, São Paulo, Brazil*
* **License:** `GPL-3.0-or-later` (mandatory for OSI/JOSS compliance due to embedded C# source files from **ALGLIB Free Edition**, licensed under `GPL-2.0-or-later`).
* **AI Policy:** `# AI usage disclosure` section included in `paper/paper.md` following JOSS editorial guidelines.
* **Mandatory Community Files:** `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1), and `.github/ISSUE_TEMPLATE/` present in the repository root.

### 2.2. Modular Architecture & Technical Status
* **Target:** `.NET 10` (`net10.0`), C# 13, nullable enabled, 100% XML documentation generated on build (`GenerateDocumentationFile = true`), NuGet package validation enabled via `Directory.Build.targets` (`EnablePackageValidation = true`).
* **Packages and Applications (`src/` — 12 projects: 8 NuGet packages + `Database.Migrator` + `WebApi.Host` + `WebUi` + `Launcher`, building with 0 warnings and 0 errors):**
  * **Scientific Core (AOT-compatible / headless):** `MelloSilveiraTools.Mathematics`, `MelloSilveiraTools.MechanicsOfMaterials`.
  * **Scientific Orchestration & Optimization:** `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
  * **Integrated Infrastructure & Meta-Package:** `Core`, `Database`, `WebApi`, `Plugins`, `MelloSilveiraTools`.
  * **Executable Hosts, CLI & UI (exempt from broad coverage):** `Database.Migrator`, `WebApi.Host`, `WebUi` (Blazor WebAssembly + Apache ECharts), `Launcher`.
* **Tests and CI/CD:**
  * Fast in-memory unit tests (`test/UnitTests/UnitTests.csproj`) with coverage filtered via `coverlet.runsettings` (focused on the 8 NuGet packages; excluding third-party `ALGLIB` and `*.g.cs`);
  * End-to-end acceptance/integration tests on real knee ligament datasets (ACL, PCL, MCL, LCL) and Testcontainers PostgreSQL (`test/AcceptanceTests/AcceptanceTests.csproj`, using `[PostgresIntegrationFact]` and `[Trait("Category", "Slow")]`);
  * Active workflows in `.github/workflows/` (`publish_nuget_package.yml`, `publish_release_candidate_nuget_package.yml`, `draft-pdf.yml` — covering build, validation, Cobertura/TRX test execution, and draft PDF compilation on PRs and `main`, making an extra `ci.yml` unnecessary).

### 2.3. Demonstration / Quickstart Strategy (Architectural Decision)
> [!IMPORTANT]
> **No plain console-only Quickstart will be created.** The interactive demonstration experience for JOSS reviewers and end users is delivered via an **executable Web API host**, **lightweight Blazor Web UI**, and **cross-platform Launcher** (`src/MelloSilveiraTools.WebApi.Host`, `src/MelloSilveiraTools.WebUi`, `src/MelloSilveiraTools.Launcher`, plus `docker-compose.yml`).

### 2.4. Objective Pre-Submission Readiness Checklist

#### Pre-Submission
1. [x] **Executable Quickstart Web API (`src/MelloSilveiraTools.WebApi.Host`):** Configured with `AddToolsServices`, `AddWebApiHealthChecks`, mandatory JWE auth (`/api/v1/auth/*`), rate limiting, CORS, `/swagger`, and bundled sample datasets in `SamplesData/` (`LCA_02_07_21_L38_D11`), exposing:
   * `GET /api/health` and `GET /api/v1/samples`
   * `POST /api/v1/experimental-data/upload`, `/process` (`202 Accepted`), and `/process/batch` (`MaxDegreeOfParallelism`)
   * `POST /api/v1/parametric-sweeps/process` and `/process/experimental-data`
   * `GET /api/v1/pipelines/{pipelineId}/progress` (NDJSON `application/x-ndjson` streaming)
   * `GET /api/v1/storage-entry-metadata` and `/{id}/download`, plus CRUDs and `MechanicsOfMaterials`/`Plugins`.
2. [x] **Web Frontend (`src/MelloSilveiraTools.WebUi`) & `src/MelloSilveiraTools.Launcher`:** Blazor WebAssembly + Apache ECharts workbench with 1-click sample selection (`LCA_02_07_21_L38_D11`) or CSV upload, deformation phase segmentation (`Ramp`, `Relaxation`, `Descent`, `Recovery`), Experimental vs. Fung QLV / Simplified Fung / Schapery fit overlays, OAAT sensitivity, Mechanics, Storage, and Plugins tabs.
3. [ ] **Illustrative Figure in `paper/`:** Add a high-quality figure under `paper/` (TPL Dataflow architecture and/or Experimental vs. Fung QLV vs. Simplified Fung vs. Schapery comparison) and reference it in `paper/paper.md` (e.g., `![...](figure.png)`).
4. [ ] **Final Test Execution & PDF Verification:** Run full test suites (`UnitTests` + `AcceptanceTests` in `Release`) and trigger `.github/workflows/draft-pdf.yml` to inspect the final `paper.pdf`.
5. [ ] **Formal Submission:** Submit at [https://joss.theoj.org/papers/new](https://joss.theoj.org/papers/new).

> *(Note: CI build and test automation are already satisfied by `.github/workflows/publish_release_candidate_nuget_package.yml` and `.github/workflows/publish_nuget_package.yml`, without needing an additional `ci.yml`.)*

#### Post-Acceptance (After JOSS Peer Review)
* [ ] Create the final GitHub release tag (e.g., `v2.0.0`).
* [ ] Archive the repository via [Zenodo](https://zenodo.org/) (or figshare) to mint a permanent **DOI**.
* [ ] Report release version and DOI in the official JOSS review thread.

---

## 3. Systematic Target Journal Selection (Primary Scientific Article)

Complete **4-stage** bibliometric audit (2026-10-16, `senior-applied-biomechanics-researcher-deep-research` + `biomechanics-co-advisor`) over **358 records (301 unique DOIs)** from the Evidence Funnel ([`base-literature/evidence-funnel-bibliography.md`](./base-literature/evidence-funnel-bibliography.md)), verified via Crossref and EuropePMC APIs.

### 3.1. Stage 1 — All Journals Identified in the Corpus (137 journals / 301 articles)

| Rank | Journal | Publisher | Corpus Count |
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
| — | *Remaining 115 journals (1 or 2 articles each)* | Various | 120 |
| **Total** | **137 Unique Journals** | — | **301** |

### 3.2. Stage 2 — Journals Publishing Fung, QLV, or Generalized Maxwell with Prony Series (27 articles / 16 journals)

| Rank | Journal | Articles | Reference DOIs in Corpus |
| :---: | :--- | :---: | :--- |
| **1st** | **Scientific Reports** | **6** | `10.1038/s41598-024-67603-8`, `10.1038/s41598-025-26576-y`, `10.1038/s41598-025-08471-8`, `10.1038/s41598-025-88767-x`, `10.1038/s41598-025-23918-8`, `10.1038/s41598-018-20739-w` |
| **2nd** | **Journal of the Royal Society Interface** | **2** | `10.1098/rsif.2015.0486`, `10.1098/rsif.2026.0078` |
| **3rd** | **Journal of the Mechanical Behavior of Biomedical Materials (JMBBM)** | **2** | `10.1016/j.jmbbm.2026.107452`, `10.1016/j.jmbbm.2023.105968` |
| **4th** | **Biomimetics** | **2** | `10.3390/biomimetics11010004`, `10.3390/biomimetics9120762` |
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

### 3.3. Stage 3 — Journals Publishing Open-Source or *In-House* Computational Workflows (57 articles / 27 journals)

| Rank | Journal | Articles | Computational Focus / Identified Tools |
| :---: | :--- | :---: | :--- |
| **1st** | **Annals of Biomedical Engineering** | **7** | Open ligament frameworks (Open Knee), FEBio FINESSE, and open-source solvers. |
| **2nd** | **Journal of Biomechanical Engineering** | **5** | *In-house* rheological networks (`10.1115/1.4003620`), coupled open solvers, and FEBio integration. |
| **3rd** | **Biomechanics and Modeling in Mechanobiology** | **4** | Reactive FEBio formulations, voxel-based models, and open-source code. |
| **4th** | **Acta Biomaterialia** | **3** | Custom algorithms and software for finite-element fiber orientation. |
| **5th** | **PLOS ONE** | **3** | Open-source pipelines, custom meshing, and experimental processing scripts. |
| **6th** | **Computer Methods and Programs in Biomedicine** | **3** | *In-house* biomedical software suites and programs. |
| **7th** | **Medical Engineering & Physics** | **2** | *In-house* FORTRAN subroutine for Abaqus viscoelastic modeling (`10.1016/j.medengphy.2014.04.004`). |
| **8th** | **Engineering with Computers** | **2** | Open-source frameworks integrating FEBio and finite-element libraries. |
| **9th** | **The Knee** | **2** | Custom computational pipelines for osteotomy and tibiofemoral contact. |
| **10th** | **Journal of the Royal Society Interface** | **2** | *In-house* constitutive models with evolution subroutines and dual-branch rheological networks. |
| **11th** | **Scientific Reports** | **2** | *In-house* genetic algorithm with supplementary source code (`10.1038/s41598-024-67603-8`). |
| **12th** | **Frontiers in Bioengineering and Biotechnology** | **2** | Open-source knee model validation code and computational reviews. |
| **13th** | **Journal of Biomechanics** | **1** | Open FEBio Studio routines for dynamic musculoskeletal modeling (`10.1016/j.jbiomech.2026.113308`). |
| **14th** | **Journal of the Mechanical Behavior of Biomedical Materials** | **1** | Visco-hyperelastic damage model via custom routine (`10.1016/j.jmbbm.2023.105968`). |
| **15th** | **Computer Methods in Biomechanics and Biomedical Engineering** | **1** | Custom collateral ligament calibration algorithms (`10.1080/10255842.2023.2253950`). |
| **16th** | **PLOS Computational Biology** | **1** | Open-source framework for biophysical and biomechanical simulation. |
| **17th** | **Proc. IMechE Part C: J. Mechanical Engineering Science** | **1** | Custom internal-variable computational routine (`10.1177/0954406219875771`). |
| — | *Remaining 10 journals (1 article each)* | 10 | Various *in-house* codes and custom subroutines. |

### 3.4. Stage 4 — Exact Intersection (Stages 2 ∩ 3 — 8 journals)

| Rank | Journal (Publisher) | Total (Stage 1) | Fung/QLV/Prony (Stage 2) | Open-Source/*In-House* (Stage 3) | Matching Article (Reference DOI) |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **1st** | **Scientific Reports** *(Nature Portfolio)* | 14 | **6** | 2 | **1** (`10.1038/s41598-024-67603-8`) |
| **2nd** | **Journal of Biomechanical Engineering (JBME)** *(ASME)* | 15 | 1 | **5** | **1** (`10.1115/1.4003620`) |
| **3rd** | **Journal of the Mechanical Behavior of Biomedical Materials (JMBBM)** *(Elsevier)* | 7 | 2 | 1 | **1** (`10.1016/j.jmbbm.2023.105968`) |
| **4th** | **Journal of the Royal Society Interface** *(Royal Society)* | 4 | 2 | 2 | **1** (`10.1098/rsif.2026.0078`) |
| **5th** | **Medical Engineering & Physics** *(Elsevier)* | 3 | 1 | 2 | **1** (`10.1016/j.medengphy.2014.04.004`) |
| **6th** | **Proc. IMechE Part C: J. Mechanical Engineering Science** *(SAGE)* | 1 | 1 | 1 | **1** (`10.1177/0954406219875771`) |
| **7th** | **Journal of Biomechanics** *(Elsevier)* | 7 | 1 | 1 | 0 |
| **8th** | **Frontiers in Bioengineering and Biotechnology** *(Frontiers)* | 13 | 1 | 2 | 0 |

### 3.5. Top 3 Recommendations (`biomechanics-co-advisor`)
1. **1st Choice — *Scientific Reports* (JCR Q1, IF ~3.8):** Highest concentration of viscoelastic calibration studies in the corpus (6 papers) and strong reproducibility/open-code policy replacing commercial toolboxes (direct benchmark: `10.1038/s41598-024-67603-8`).
2. **2nd Choice — *Journal of Biomechanical Engineering — JBME* (ASME, JCR Q2, IF ~1.7):** Historical home of Fung QLV ramp-correction methodology (Abramowitch & Woo, 2004) and Maxwell rheological network calibrations (`10.1115/1.4003620`), valuing transparent 1D formulations.
3. **3rd Choice — *Journal of the Mechanical Behavior of Biomedical Materials — JMBBM* (Elsevier, JCR Q1, IF ~3.4):** Ideal if emphasizing ligament material characterization and tissue physics while positioning the `.NET 10` suite as a methods subsection (*"Custom Reactive Pipeline Implementation in .NET 10"*).
