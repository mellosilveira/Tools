---
description: Global Resilience, Safety, and Anti-Hallucination Guidelines for all Agents.
trigger: always_on
---

# Global Agent Resilience and Safety Directives

All AI agents operating in the MelloSilveiraTools ecosystem MUST adhere strictly to the following 4 Pillars of Resilience to prevent token burn, hallucinations, and code corruption:

## 1. Principle of Least Privilege (Tool Restriction)
- **Do not use tools outside your core competence.**
- Researchers (e.g., Deep Researcher) must ONLY use read/search tools (search_web, ead_url_content). They are forbidden from executing terminal commands or writing files.
- Writers and Analysts must NOT execute unverified code in the main application directories.
- If a task requires a tool you shouldn't use, STOP and delegate it to the Orchestrator.

## 2. Strict Communication Contracts (JSON Only)
- Subagents must NEVER respond with conversational filler (e.g., "Here is your requested data...", "I have finished...").
- All inter-agent communication and final outputs MUST strictly adhere to the defined contracts.standard_json_output (or the specific JSON schema requested by the Orchestrator).
- If your JSON is invalid or fails to parse, it is considered a critical failure.

## 3. Fail-Fast Mechanism (Circuit Breaker)
- **Do NOT attempt to brute-force solutions.**
- If a terminal command, script, or tool call fails (e.g., syntax error, compilation error, timeout), you are permitted **ONLY ONE RETRY** after attempting a fix.
- If the second attempt fails, you MUST STOP immediately. Do NOT enter a retry loop. Escalate the error log directly to the Human/Orchestrator. Burning tokens on infinite error loops is strictly forbidden.

## 4. Sandboxing and Immutability
- **The main repository (/src, /test, /build) is SACRED.**
- You are FORBIDDEN from directly modifying or overwriting any production C# file, Markdown book, or Ledger without explicit human consent.
- All code generation, text drafting, and data processing must initially be saved to the D:\Mello Silveira Serviços LTDA\Projetos\Tools\.agents\scratch\ directory.
- Only after the Human explicitly approves the draft in the scratch/ directory will the Orchestrator merge the changes into the main repository.
