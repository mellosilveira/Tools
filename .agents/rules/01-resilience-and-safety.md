---
description: Global Resilience, Safety, Model Tiering, and Low-Token Guardrails for all Agents.
trigger: always_on
---

# Global Agent Guardrails

1. **Least Privilege:** Stay within the active skill's tool scope — researchers never edit production code or run builds/terminal commands; writers/analysts never execute unverified code in main app directories (`src/`, `test/`). If a task requires out-of-scope tools, STOP and delegate/escalate to the Orchestrator/User.
2. **Pass-by-Reference & Zero Filler (Low Token Output):** Never wrap large reports or code inside JSON envelopes, and omit conversational filler in subagent replies. Write deliverables directly to target files and return a compact summary (≤ 150 tokens: status, files changed, key points, open risks).
3. **Fail-Fast Circuit Breaker:** Never brute-force solutions. After a failed command, build, or tool call, allow **at most 1 targeted retry** after a fix. If the second attempt fails, STOP immediately and escalate the exact error log to the Orchestrator/User.
4. **Model Tiering:** Route subagents by cognitive load — `flash_lite`/`flash` for lookups, formatting, and ledger audits; `inherit` for standard code/data tasks; `pro` strictly for 3D continuum derivations, UDM C++, academic writing, and blind reviews.
5. **Protected Repository & Zero Hallucination:** Never modify or overwrite production code (`src/`, `test/`), `build/`, thesis/TCC/manuscript files (`paper/`), `ledger/`, or `AGENTS.md` architecture rules — and never perform destructive deletes — without explicit user consent. Never invent DOIs, URLs, citations, or test outcomes.
