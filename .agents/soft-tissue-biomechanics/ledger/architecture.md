# Biomechanics Research Agents Architecture
*Last revision: 2026-11-19 (v7.0 — High-Performance & Low-Cost Hybrid)*

## 1. Core Principles
- **Scope:** Uniaxial stress-relaxation modeling of isolated porcine knee ligaments (PPEMM/UFCIRJ Master's Thesis, Advisor: Prof. Dr. Paulo Pedro Kenedi) + `MelloSilveiraTools` (.NET 10 / JOSS). *Long-term post-thesis vision:* expand architecture to other soft tissues (e.g., spine) after mastering knee ligaments.
- **Thermodynamic Context:** Assays are isothermal (negligible temperature variation); invoke thermodynamic potentials (e.g., Helmholtz free energy in Schapery) strictly where required by constitutive formulations.
- **Data Triad:** (1) Real experimental data, (2) 1D scalar C# engine (`MelloSilveiraTools`, reusable scientific core + research orchestration in `SoftTissue`), (3) 3D tensorial FEA (**FEBio**).
- **Zero Unnecessary Hops:** Simple tasks (1–3 files, quick builds, single ledger entries) execute directly after mandatory `prompt-optimizer-gatekeeper` alignment — no multi-hop subagent ceremony or `Pre-Execution QA Gate`.
- **Pass-by-Reference & Zero Filler:** Subagents write deliverables directly to target files and return a compact summary (≤ 150 tokens), eliminating JSON-only bloat and manual `telemetry.jsonl`.
- **Numerical Precision:** Full `double` precision everywhere except academic prose (`formatting.significant_figures = 3`).

## 2. Optimized Workflow & Transversal Governance
`prompt-optimizer-gatekeeper` → Human Alignment → Direct Execution *(light tasks)* **or** Specialist Subagent with Model Tiering *(parallel/deep/isolated tasks)* → `high-standard-paper-reviewer` *(milestones only, clean context)* → `documentation-and-tracking-secretary` *(end-of-phase audit in `flash`)*.

- **Analyst → Backend Synergy:** Before proposing new code/infrastructure, the Analyst consults the Backend once on what already exists or can be extended in C# (`MelloSilveiraTools`). All sensitivity studies cover the 4 mandatory 1D scenarios and report RMSE, $R^2$, AIC/BIC, and multi-stage error propagation.
- **Backend Governance:** Read `AGENTS.md` before every task; update `CHANGELOG.md` on every feature/fix delivery; alter `AGENTS.md` architecture rules, introduce microservices, or permanently purge scientific data only with explicit justification and human approval.
- **Secretary Critical Directive ("The Why"):** Enforce documenting the **Why** alongside **What** and **From where** for every permanent decision, *future work*, and *opportunity/gap*.
- **Centralized Human Bridge:** Only the Coordinator/Orchestrator invokes `ask_question`; specialists surface decisions/approvals in their concise handoff.

## 3. Agents Roster & Model Tiering

| Skill (canonical name) | Tier | Role & Trigger |
|---|---|---|
| `prompt-optimizer-gatekeeper` *(shared)* | `flash` / `inherit` | **Mandatory entry gate:** audits gaps/flaws, asks surgical questions, outputs compact prompt + target skill/tier/files. |
| `central-biomechanics-research-orchestrator` | `inherit` | Multi-step Macro/Micro planning, parallel subagent delegation (`Workspace: "inherit"`), `ask_question`, PDF policy (direct execution allowed for light tasks). |
| `biomechanics-co-advisor` | `inherit` / `pro` | Scope/deadline guardian (uniaxial knee ligaments), research direction, actionable 1–3 step plans. |
| `senior-applied-biomechanics-researcher-deep-research` | `flash` / `pro` | Web search (CAPES/PubMed/ScienceDirect) + CrossRef DOI validation; autonomous multi-hop funnel; writes directly to `base-literature/`. |
| `experimental-data-analyst` | `inherit` | Constitutive fitting logic, 4-scenario OAAT sensitivity, RMSE/$R^2$/AIC/BIC & error propagation, Dockerized Python plots. |
| `senior-backend-software-engineer-net` | `inherit` / `pro` | Incremental C#/.NET 10, PostgreSQL, FEBio/UDM, CI/CD & Docker from scratch, `dotnet build/test`, `CHANGELOG.md`. |
| `senior-frontend-mobile-software-engineer` | `inherit` | Accessible minimalist UI (Blazor/MAUI/Uno .NET synergy, WebGL/Canvas), NDJSON streaming, zero business rules on edge, Docker. |
| `academic-writer-biomechanics-specialist` | `pro` | Thesis chapters (PPEMM/UFCIRJ) & JOSS/journal manuscripts; 3 sig. figs. in prose, mandatory page-cited equations. |
| `high-standard-paper-reviewer` | `pro` | Blind critical review of chapters/papers (**milestones only**, clean context, complete prioritized actionable list). |
| `documentation-and-tracking-secretary` | `flash` | **End-of-phase** Ledger QA (`What / From where / Why`) for decisions, `future-works.md`, and `Opportunity/Gap` records. |

## 4. Absolute Prohibitions
- ❌ Fabricating DOIs, URLs, citations, or test results.
- ❌ Running custom research Python/API/DB/Frontend stacks directly on the host *(must run in Docker; exception: native host FEBio / C++ compiler documented as dependency)*.
- ❌ Rounding numbers outside Writer prose.
- ❌ Specialists calling `ask_question` directly.
- ❌ Modifying `AGENTS.md` architecture rules or deleting scientific data without explicit human approval.

## 5. Revision History
| Date | Version | Summary |
|---|---|---|
| 2026-10-02 – 2026-10-05 | v1.0 – v5.0 | Initial ecosystem, FR-01–FR-07 fixes, numerical precision & knee-ligament focus, thermodynamic context, English standardization. |
| 2026-10-07 | v6.0 | Pivot to isolated porcine uniaxial tension, open-source FEBio/C++ host exception, HGO/5th-model roadmap, UI vs. Python plotting split. |
| 2026-11-19 | v7.0 | Shared `prompt-optimizer-gatekeeper`, Model Tiering, Pass-by-Reference (replacing JSON-only/telemetry bloat), consolidated thesis & JOSS guides. |
