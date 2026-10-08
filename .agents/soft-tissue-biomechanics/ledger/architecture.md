# Planning: Biomechanics Research Agents Ecosystem
*Living document - last revision: 2026-10-07*

> [!IMPORTANT]
> This document is the source of truth for the agent ecosystem. Every architectural decision, interface contract, and operation rule is registered here. Always update it when a skill is modified.

---

## 1. Overview and Objective

The ecosystem is composed of **1 Central Orchestrator** and **8 Specialist Agents** that give cadence to the research on the mechanical behavior of porcine soft tissues. **Strict Current Scope:** Porcine knee ligaments (Master's thesis). **Long-term Vision:** Expanding the architecture to other soft tissues (e.g., spine) is a future goal for the research group, post-thesis.

**System Architecture & Strategy:** The ecosystem is designed to accelerate research rather than compete with established FEA software. It is divided into: 1) **Frontend** (dynamic plotting, interactive visual comparison); 2) **Backend** (business logic strictly separated into reusable logic in `MelloSilveiraTools` and research-specific orchestration in `SoftTissue`); and 3) **Database** (persistence).
**Theoretical Evolution:** The internal C# computational engine will remain **scalar (1D)** to completely avoid the overhead of implementing complex finite element logic (e.g., mesh generation). The definitive output for the frontend visualizer will always contrast 3 data sources: (1) Real experimental data, (2) Scalar numerical simulation (C# Engine), and (3) 3D Tensorial numerical simulation (Open-source software, preferably FEBio).

**Thermodynamic Context:** the research is not focused on thermodynamics (negligible temperature variation in ligaments). Only thermodynamic concepts used in the formulation of non-linear and hyperelastic models (e.g., Schapery) are considered.

**Design Principles:**
- **Numerical Precision:** only the Writer rounds numbers, and only in text (`formatting.significant_figures` = 3). Everything else, including `data_payload` and Ledger, uses full precision.
- **Orchestrator Isolation:** the Orchestrator only plans and delegates, even light tasks.
- **Total Traceability:** every output follows the JSON contract; the Orchestrator logs telemetry and the Secretary logs the Ledger.
- **Centralized Human Approval:** only the Orchestrator uses `ask_question`.
- **Incremental Backend:** new features over the existing system; only the deploy pipeline is created from scratch. `AGENTS.md` is mandatory and `CHANGELOG.md` is always updated.

---

## 2. Topology

The ecosystem strictly enforces a **Pre-Execution Justification and QA Gate**:
1. **Strategy Proposal:** Before executing any actual work (e.g., writing code, processing data), the assigned subagent MUST present a justification and strategy of what will be done.
2. **Evaluation:** The Secretary reviews this strategy. Then, the Orchestrator evaluates it alongside the Human User.
3. **Execution:** ONLY after explicit approval from the Human User is the subagent allowed to proceed with the execution.

The Orchestrator invokes generic subagents (`TypeName: "self"`, `Workspace: "inherit"`) and, in the `Prompt`, instructs each to read the persona's `SKILL.md` in `paths.skills_directory`.

*Orchestrator -> Subagent (Strategy Proposal) -> Secretary (Strategy QA) -> Human (Approval) -> Subagent (Execution) -> JSON -> Telemetry -> Save JSON to `outputs/` folder -> (Reviewer, if text) -> Secretary (QA + Ledger) -> Synthesis to human.*

---

## 3. Agents Roster

| Skill (canonical name) | Role |
|---|---|
| `central-biomechanics-research-orchestrator` | Macro/micro planning, delegation, `ask_question`, telemetry, PDF policy. |
| `documentation-and-tracking-secretary` | Traceability QA and Ledger (What / From where / Why), including future works and opportunities. |
| `senior-applied-biomechanics-researcher-deep-research` | Deep research with real sources and DOI; validates trends and factual statements. |
| `experimental-data-analyst` | Data processing, constitutive fitting, sensitivity (4 scenarios), RMSE/$R^2$ and error propagation, in consensus with the Backend. |
| `academic-writer-biomechanics-specialist` | Academic writing; the only one who rounds numbers (in text). |
| `high-standard-paper-reviewer` | Scientific and textual criticism in an actionable format. |
| `biomechanics-co-advisor` | Scope guardian, trends (with Researcher backing), AI/ML only with precedents. |
| `senior-backend-software-engineer-net` | C#/.NET 10/PostgreSQL incremental; Analyst's consultant; CI/CD from scratch. |
| `senior-frontend-mobile-software-engineer` | Frontend prioritizing .NET synergy and WebGL/Canvas graphics. |

---

## 4. Contracts and Telemetry (`.\.agents\soft-tissue-biomechanics\config.yaml`)

- **Standard output:** `contracts.standard_json_output` (`task_id`, `skill`, `status`, `retry_count`, `data_payload`, `output_summary`, `artifacts`, `warnings`).
- **Rejection feedback:** `actionable_feedback` with `contracts.actionable_feedback_item` items.
- **Telemetry:** `contracts.telemetry_line` appended to `paths.telemetry_file`.
- **Ledger:** `paths.ledger_file`.

---

## 5. Transversal Rules

### 5.1 Orchestrator and Human Flow
- Never executes specialized tasks.
- Always creates a Macro Planning with the human and divides it into Micro Plannings.
- **Circuit Breaker:** after `execution.max_retries` failures (including timeout of `execution.micro_timeout_seconds`), consults the human.
- **PDF:** stops and asks the human; if denied, waits for another format; if authorized, delegates conversion (`paths.pdf_converter_script`).

### 5.2 Analyst -> Backend Synergy
- The Analyst does not reinvent the wheel: before proposing something new, asks **once** (via Orchestrator) for the Backend's analysis on what can be used, extended, or implemented in C#.
- Flow: Analyst -> Backend -> Analyst consolidates -> Orchestrator -> human decides.
- Mandatory sensitivity: stress x variable (initial time), stress x variable (final time), asymptote time x variable, stress variation x variable (Strictly mandated for the scalar 1D formulation).
- Metrics: RMSE and $R^2$, with consolidated error/precision across steps (e.g., 4 steps of Schapery fitting).

### 5.3 Backend Governance
- Reads `AGENTS.md` before any task; new/altered architectural decisions update `AGENTS.md` after human approval.
- `CHANGELOG.md` updated in every delivery.
- Microservices and definitive deletion of scientific data only with justification, explicit functionality, and human approval.

### 5.4 Secretary
- Registers current decisions, **future works** and **opportunities/breaches**.
- **CRITICAL DIRECTIVE:** The Secretary MUST enforce the documentation of the "Why" at every single step. No architectural, methodological, or technical decision is accepted without explicit, documented reasoning (e.g., explicitly noting that C# was chosen due to the need for automation and the researcher's existing expertise). The justification is strictly more important than the action itself.

### 5.5 Absolute Prohibitions
- ❌ Executing custom research system codes (Backend, Frontend, Database, Python Scripts) directly on the Host operating system. These MUST run isolated in Docker containers. *(Exception: Third-party native technologies like FEBio and C++ compilers must be installed on the host and documented as dependencies).*
- ❌ Inventing links, DOIs, or references.
- ❌ Rounding numbers outside the text written by the Writer.
- ❌ Specialists using `ask_question` directly.
- ❌ Orchestrator executing or converting any content.

---

## 6. Revision History

| Date | Version | Changes |
|---|---|---|
| 2026-10-02 | v1.0 | Initial creation of the architecture. |
| 2026-10-02 | v2.0 | Fixes FR-01 to FR-07 and Planning. |
| 2026-10-03 | v3.0 | Numerical precision, Isolated Orchestrator, knee ligament focus, incremental backend. |
| 2026-10-05 | v4.0 | Alignment of roster and prohibitions to skills; thermodynamic context; centralized schemas. |
| 2026-10-05 | v5.0 | English standardization across all files. |
| 2026-10-07 | v6.0 | Pivot to strictly porcine uniaxial tension. Open-source FEBio/C++ host exception. 5th model included. UI/Python graphing division defined. |