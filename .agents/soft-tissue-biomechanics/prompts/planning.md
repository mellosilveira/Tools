# Planning: Biomechanics Research Agents Ecosystem
*Living document - last revision: 2026-10-05*

> [!IMPORTANT]
> This document is the source of truth for the agent ecosystem. Every architectural decision, interface contract, and operation rule is registered here. Always update it when a skill is modified.

---

## 1. Overview and Objective

The ecosystem is composed of **1 Central Orchestrator** and **8 Specialist Agents** that give cadence to the research on the mechanical behavior of human soft tissues. **Current focus: knee ligaments**; after acceptable understanding, the research will migrate to other soft tissues.

**Expected Theoretical Evolution:** today the research and algorithms use **scalar mechanical models (1D)**. One of the final objectives is the **tensorial formulation (full 3D Continuum Mechanics)** - Cauchy/Piola-Kirchhoff stress tensors, deformation gradient, invariants - and the use of **FEBio** (or similar) for simulations and validations. Database, C# code, and UI must be extensible for this migration.

**Thermodynamic Context:** the research is not focused on thermodynamics (negligible temperature variation in ligaments). Only thermodynamic concepts used in the formulation of non-linear and hyperelastic models (e.g., Schapery) are considered.

**Design Principles:**
- **Numerical Precision:** only the Writer rounds numbers, and only in text (`formatting.significant_figures` = 3). Everything else, including `data_payload` and Ledger, uses full precision.
- **Orchestrator Isolation:** the Orchestrator only plans and delegates, even light tasks.
- **Total Traceability:** every output follows the JSON contract; the Orchestrator logs telemetry and the Secretary logs the Ledger.
- **Centralized Human Approval:** only the Orchestrator uses `ask_question`.
- **Incremental Backend:** new features over the existing system; only the deploy pipeline is created from scratch. `AGENTS.md` is mandatory and `CHANGELOG.md` is always updated.

---

## 2. Topology

The Orchestrator invokes generic subagents (`TypeName: "self"`, `Workspace: "inherit"`) and, in the `Prompt`, instructs each to read the persona's `SKILL.md` in `paths.skills_directory`.

*Orchestrator -> Subagent -> JSON -> Telemetry -> (Reviewer, if text) -> Secretary (QA + Ledger) -> Synthesis to human.*

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

### 5.2 Analyst â†” Backend Synergy
- The Analyst does not reinvent the wheel: before proposing something new, asks **once** (via Orchestrator) for the Backend's analysis on what can be used, extended, or implemented in C#.
- Flow: Analyst -> Backend -> Analyst consolidates -> Orchestrator -> human decides.
- Mandatory sensitivity: stress Ã— variable (initial time), stress Ã— variable (final time), asymptote time Ã— variable, stress variation Ã— variable.
- Metrics: RMSE and $R^2$, with consolidated error/precision across steps (e.g., 4 steps of Schapery fitting).

### 5.3 Backend Governance
- Reads `AGENTS.md` before any task; new/altered architectural decisions update `AGENTS.md` after human approval.
- `CHANGELOG.md` updated in every delivery.
- Microservices and definitive deletion of scientific data only with justification, explicit functionality, and human approval.

### 5.4 Secretary
- Registers current decisions, **future works** and **opportunities/breaches**; refuses records without origin and justification.

### 5.5 Absolute Prohibitions
- ❌ Executing research system codes (Backend, Frontend, Database, Python Scripts) directly on the Host operating system. EVERYTHING MUST run isolated in Docker containers.
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
| 2026-10-03 | v3.0 | Numerical precision (Writer only), Isolated Orchestrator with `self` subagents, knee ligament focus, incremental backend, centralized `ask_question`. |
| 2026-10-05 | v4.0 | Coding fix (mojibake) in all files; alignment of roster and prohibitions to skills (microservices, data deletion, Analyst/Frontend stack, AI/ML); 4 sensitivity scenarios; thermodynamic context; 3D/FEBio goal; centralized schemas. |
| 2026-10-05 | v5.0 | English standardization across all files. |

