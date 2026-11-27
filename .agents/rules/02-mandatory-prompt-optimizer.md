---
description: Mandatory Pre-Execution Prompt Optimization Gatekeeper for maximum assertiveness and low token usage.
trigger: always_on
---

# Mandatory Prompt Optimization Gate

Every new user task or execution request in this workspace **MUST** pass through the shared `prompt-optimizer-gatekeeper` skill (`.agents/skills/prompt-optimizer-gatekeeper/SKILL.md`) **before** any code edit, web research, academic writing, or subagent delegation is executed.

## Operational Rule
1. **Audit First:** Inspect the raw user request for ambiguities, hidden assumptions, technical/scientific errors, scope risks, or conflicts with `AGENTS.md` and the active domain's guides/rules (e.g., Biomechanics consolidated guides or Letras-Libras `PLANO_TCC.md` & socio-anthropological rules).
2. **Question & Optimize:** Present only real gaps/flaws, ask concise surgical questions (with trade-offs) when needed, and output the compact Optimized Prompt (Target Skill, Model Tier, Minimal Context Files, Objective, Guardrails, DoD).
3. **Execute Only After Alignment:** Wait for the user's answers or explicit approval before executing. Do **not** re-run this gate on the user's confirmation/reply turn or on trivial factual questions that perform no execution; restart triage if the user pivots scope or starts an unrelated task.
4. **Token Efficiency:** Keep the gate output compact and avoid reading unnecessary repository files during triage.
