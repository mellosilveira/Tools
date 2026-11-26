# Architectural & Implementation Plan — Module 08: Interactive Frontend & Dynamic Charting Layer

> [!NOTE]
> **Current Status (2026-11-26):** The Blazor WebAssembly (`.NET 10`) + Apache ECharts frontend layer is implemented in `src/MelloSilveiraTools.WebUi` (served by `src/MelloSilveiraTools.WebApi.Host` and containerized via `docker-compose.yml`), covering `ExperimentalWorkbenchTab`, `MechanicsWorkbenchTab`, `StorageWorkbenchTab`, `PluginsWorkbenchTab`, `BiomechanicsChart`, and `PipelineProgressMonitor`. Remaining future extensions are the 3D FEBio comparison slot (after Phase 3) and a dedicated *Iso-Stress* view.

## 1. Overview & Objectives

Specifies the architecture, component design, and implementation plan for the **Interactive Frontend Layer** (Blazor WebAssembly) supporting the soft-tissue biomechanics research (porcine knee ligaments under uniaxial tension):

1. Support uploading raw mechanical test CSV pairs and visualizing automated deformation phase segmentation (`Ramp`, `Relaxation`, `Descent`, `Recovery`).
2. Trigger and monitor multi-model constitutive curve fitting (`Fung QLV`, `Simplified Fung`, `Schapery`, `Maxwell`, `HGO`, etc.) in real time via continuous NDJSON streaming.
3. Interactively overlay the **Data Triad**: real experimental data, 1D analytical-numerical C# simulations (`MelloSilveiraTools`), and 3D tensorial FEBio simulations.
4. Visually explore One-At-A-Time (OAAT) parametric sensitivity sweeps and *Iso-Stress* variable relationships.

---

## 2. Strategic & Methodological Rationale (The "Why")

* **Blazor WebAssembly (.NET 10):** Directly shares DTOs, records, enums (`SegmentType`, `MechanicalModel`), and validation patterns with `MelloSilveiraTools`, avoiding TypeScript interface duplication and contract drift while handling dense arrays client-side without burdening server memory.
* **Canvas / WebGL Rendering (Apache ECharts via JSInterop):** **Anti-Visual-Crash Directive.** High-frequency relaxation assays contain $20{,}000\text{–}50{,}000$ data points (time, strain, force). Pure SVG/DOM charting libraries create tens of thousands of DOM nodes and freeze browsers; Canvas/WebGL maintains hardware-accelerated pan, zoom, and point inspection at 60 FPS.
* **Inviolable "Zero Business Rules on the Edge" Rule:** Zero optimization, residual calculation, non-linear regression, or constitutive equations execute in the UI. The frontend strictly handles rendering and edge input sanity checks (e.g., rejecting negative stiffness moduli or invalid bounds).
* **Containerized Hosting (`WebApi.Host` + Docker Compose):** Compliant with containerized execution policies; compiled static WASM assets are served directly by `MelloSilveiraTools.WebApi.Host` inside `docker-compose.yml`.

---

## 3. Local Engineering Guidelines

* **Strict C# Typing:** `var` is forbidden; explicit types are mandatory everywhere (see [`AGENTS.md`](../../../AGENTS.md)).
* **Primary Constructors:** Preferred in components, services, and models.
* **Contract Alignment:** Direct alignment with `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` and `MelloSilveiraTools.WebApi` HTTP contracts.
* **Streaming Telemetry:** Socket-timeout-free NDJSON (`application/x-ndjson`) consumption via `IAsyncEnumerable<T>` / `StreamReader`.

---

## 4. Component Architecture

### 4.1 Folder Structure (`src/MelloSilveiraTools.WebUi/`)
* Implemented workbench structure under `src/MelloSilveiraTools.WebUi/`:
  * `App.razor`, `Pages/Home.razor`, `Models/ApiContracts.cs`, `Services/ToolsApiClient.cs`
  * `Components/BiomechanicsChart.razor`, `Components/PipelineProgressMonitor.razor`
  * `Components/ExperimentalWorkbenchTab.razor` (JWE auth, samples, CSV upload, pipeline execution, segmentation & model-fit overlays, OAAT sensitivity, persisted CRUD explorer)
  * `Components/MechanicsWorkbenchTab.razor` (constitutive model metadata, deterministic specimen analysis, Goodman/Marin fatigue)
  * `Components/StorageWorkbenchTab.razor` (`storage_entry_metadata` CRUD, upload, download, preview)
  * `Components/PluginsWorkbenchTab.razor` (public catalog, community submissions, superuser inspection/moderation/cache lifecycle)
  * `wwwroot/js/` (Apache ECharts JSInterop bridge)

### 4.2 Core Component Contracts
* **`BiomechanicsChart.razor`:** Agnostic Canvas charting bridge via ECharts JSInterop supporting experimental curves, 1D C# fits/simulations, OAAT sensitivity curves, interactive zoom/pan, and high-resolution image export.
* **`PipelineProgressMonitor.razor`:** Consumes `GET /api/v1/pipelines/{pipelineId}/progress` NDJSON `PipelineProgressEvent` streams, tracking `Pending`, `Started`, `Completed`, `Faulted` step transitions, `UpstreamSteps`, `ReceivedItems`, and `ProcessedItems`.

---

## 5. Implementation Phases & Current Milestones

1. **[Done] Phase 1 — Blazor WASM Host & Docker Packaging:** `src/MelloSilveiraTools.WebUi` hosted by `MelloSilveiraTools.WebApi.Host` and wired into `docker-compose.yml` + `MelloSilveiraTools.Launcher`.
2. **[Done] Phase 2 — Reactive HTTP Client & NDJSON Streaming:** Typed client `ToolsApiClient` + `PipelineProgressMonitor`.
3. **[Done] Phase 3 — Canvas Charting Engine:** Apache ECharts JSInterop (`BiomechanicsChart.razor`).
4. **[Done for 1D] Phase 4 — Workbench Tabs & Edge Validations:** Experimental, Mechanics, Storage, and Plugins tabs with edge input guards.
5. **[Pending Future Extensions]:** Integrate 3D FEBio series into the triad comparison view after Phase 3 and add stress-test validation (> 50,000 raw temporal samples) + optional *Iso-Stress* explorer.
