# Feature Implementation Plans

This directory collects detailed technical and architectural implementation plans for capabilities identified in the ecosystem technical assessment as below *Partially Meets*.

Plans are structured by **logical phases and technical dependencies**, **without fixed calendar dates or artificial deadlines**, strictly adhering to the scientific and software engineering directives of the soft-tissue biomechanics research.

---

## Index of Implementation Plans

| Plan File | Target Capability | Primary Technical Focus |
|---|---|---|
| [`interactive-frontend-dynamic-charting-layer.md`](./interactive-frontend-dynamic-charting-layer.md) | **Module 08: Interactive Frontend Layer** | Scientific Blazor WebAssembly (`.NET 10`) workbench containerized in Docker, Canvas/WebGL rendering (Apache ECharts / Plotly.js) to prevent freezes on dense time series, real-time NDJSON progress streaming, and edge physical input validation with zero local business rules. *(Core workbench implemented in `src/MelloSilveiraTools.WebUi`.)* |
| [`febio-3d-tensorial-simulation-integration.md`](./febio-3d-tensorial-simulation-integration.md) | **Module 07: FEBio Integration & 3D Tensorial Simulation** | Closing the Data Triad (Experimental vs. 1D C# vs. 3D FEBio), deriving 3D tensorial cheat sheets, analytical $1\text{D} \to 3\text{D}$ parameter recalibration for uniaxial tension, C++ UDM subroutine for Schapery with strict effort contingency trigger, and headless FEBio solver automation with PostgreSQL persistence. |
| [`publication-charts-and-model-battle-tables-automation.md`](./publication-charts-and-model-battle-tables-automation.md) | **Module 09: Publication Charts & Model Battle Tables Automation** | Containerized Python pipeline (Matplotlib, Seaborn, Pandas) generating journal-grade static figures (vector PDF and TIFF $\ge 300\text{ DPI}$) and compiling LaTeX (`booktabs`) + Markdown Model Battle tables directly from PostgreSQL and generated CSVs. |

---

## Cross-Cutting Directives Applied to Plans

1. **The "Why" Foundation:** Every plan explicitly records the rationale behind each technology or method choice, ensuring epistemological traceability in the Scientific Ledger.
2. **Strictly Containerized Execution:** Custom non-solver tools run isolated in Docker containers (Nginx/Blazor or `WebApi.Host` container, Python 3.12 container for publication, PostgreSQL for persistence). Only third-party native binaries (FEBio solver) are permitted as host-installed tools.
3. **Zero Business Rules on the Edge:** The frontend layer strictly handles presentation and edge input-range checks. All optimization, residual evaluation, and constitutive equations reside in the `MelloSilveiraTools` backend.
4. **Numerical Precision & Full Traceability:** Zero tolerance for manual transcription of results into publication tables. Comparative tables are computed directly from database records and tracked CSV artifacts.
