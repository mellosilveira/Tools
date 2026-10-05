# Planejamento: Ecossistema de Agentes de Pesquisa em Biomecânica
*Documento vivo — última revisão: 2026-10-05*

> [!IMPORTANT]
> Este documento é a fonte da verdade do ecossistema de agentes. Toda decisão arquitetural, contrato de interface e regra de operação está registrada aqui. Atualize-o sempre que uma skill for modificada.

---

## 1. Visão Geral e Objetivo

O ecossistema é composto por **1 Orquestrador Central** e **8 Agentes Especialistas** que dão cadência à pesquisa sobre o comportamento mecânico de tecidos moles humanos. **Foco atual: ligamentos de joelho**; após compreensão aceitável, a pesquisa migrará para outros tecidos moles.

**Evolução Teórica Esperada:** hoje a pesquisa e os algoritmos usam **modelos mecânicos escalares (1D)**. Um dos objetivos finais é a **formulação tensorial (Mecânica do Contínuo 3D plena)** — tensores de Cauchy/Piola-Kirchhoff, gradiente de deformação, invariantes — e o uso do **FEBio** (ou similares) para simulações e validações. Banco de dados, código C# e UI devem ser extensíveis para essa migração.

**Contexto Termodinâmico:** a pesquisa não é focada em termodinâmica (variação de temperatura desprezível nos ligamentos). Apenas conceitos termodinâmicos usados na formulação de modelos não-lineares e hiperelásticos (ex.: Schapery) são considerados.

**Princípios de Design:**
- **Precisão Numérica:** apenas o Redator arredonda, e somente no texto (`formatting.significant_figures` = 3). Todo o resto, incluindo `data_payload` e Ledger, usa precisão integral.
- **Isolamento do Orquestrador:** o Orquestrador apenas planeja e delega, mesmo tarefas leves.
- **Rastreabilidade Total:** toda saída segue o contrato JSON; o Orquestrador registra telemetria e a Secretária registra o Ledger.
- **Aprovação Humana Centralizada:** apenas o Orquestrador usa `ask_question`.
- **Backend Incremental:** novas funcionalidades sobre o sistema existente; apenas a esteira de deploy é criada do zero. `AGENTS.md` é obrigatório e `CHANGELOG.md` é sempre atualizado.

---

## 2. Topologia

O Orquestrador invoca subagentes genéricos (`TypeName: "self"`, `Workspace: "inherit"`) e, no `Prompt`, instrui cada um a ler o `SKILL.md` da persona em `paths.skills_directory`.

*Orquestrador → Subagente → JSON → Telemetria → (Revisor, se texto) → Secretária (QA + Ledger) → Síntese ao humano.*

---

## 3. Roster de Agentes

| Skill (nome canônico) | Papel |
|---|---|
| `orquestrador-central-de-pesquisa-em-biomecanica` | Planejamento macro/micro, delegação, `ask_question`, telemetria, política de PDF. |
| `secretaria-de-documentacao-e-rastreamento` | QA de rastreabilidade e Ledger (O quê / De onde / Por quê), incluindo trabalhos futuros e oportunidades. |
| `pesquisador-senior-em-biomecanica-aplicada-deep-research` | Deep research com fontes reais e DOI; valida tendências e afirmações factuais. |
| `analista-de-dados-experimentais` | Tratamento de dados, ajuste constitutivo, sensibilidade (4 cenários), RMSE/$R^2$ e propagação de erro, em consenso com o Backend. |
| `redator-academico-especialista-em-biomecanica` | Escrita acadêmica; único que arredonda (no texto). |
| `revisor-de-papers-de-alto-padrao` | Crítica científica e textual em formato acionável. |
| `coorientador-de-teses-em-biomecanica` | Guardião do escopo, tendências (com lastro do Pesquisador), IA/ML apenas com precedentes. |
| `engenheiro-de-software-backend-senior-net` | C#/.NET 10/PostgreSQL incremental; consultor do Analista; CI/CD do zero. |
| `engenheiro-de-software-frontend-mobile-senior` | Frontend com prioridade de sinergia .NET e gráficos WebGL/Canvas. |

---

## 4. Contratos e Telemetria (`.\.agents\config.yaml`)

- **Saída padrão:** `contracts.standard_json_output` (`task_id`, `skill`, `status`, `retry_count`, `data_payload`, `output_summary`, `artifacts`, `warnings`).
- **Feedback de reprovação:** `actionable_feedback` com itens `contracts.actionable_feedback_item`.
- **Telemetria:** linha `contracts.telemetry_line` anexada a `paths.telemetry_file`.
- **Ledger:** `paths.ledger_file`.

---

## 5. Regras Transversais

### 5.1 Orquestrador e Fluxo Humano
- Nunca executa tarefas especializadas.
- Sempre cria um Planejamento Macro com o humano e o divide em Micro Planejamentos.
- **Circuit Breaker:** após `execution.max_retries` falhas (incluindo timeout de `execution.micro_timeout_seconds`), consulta o humano.
- **PDF:** para e pergunta ao humano; se negado, aguarda outro formato; se autorizado, delega a conversão (`paths.pdf_converter_script`).

### 5.2 Sinergia Analista ↔ Backend
- O Analista não reinventa a roda: antes de propor algo novo, pede **uma única vez** (via Orquestrador) a análise do Backend sobre o que pode ser usado, estendido ou implementado em C#.
- Fluxo: Analista → Backend → Analista consolida → Orquestrador → humano decide.
- Sensibilidade obrigatória: tensão × variável (tempo inicial), tensão × variável (tempo final), tempo de assíntota × variável, variação de tensão × variável.
- Métricas: RMSE e $R^2$, com erro/precisão consolidados ao longo das etapas (ex.: 4 passos do ajuste de Schapery).

### 5.3 Governança do Backend
- Lê o `AGENTS.md` antes de qualquer tarefa; decisões arquiteturais novas/alteradas atualizam o `AGENTS.md` após aprovação humana.
- `CHANGELOG.md` atualizado em toda entrega.
- Microsserviços e exclusão definitiva de dados científicos somente com justificativa, funcionalidade explícita e aprovação humana.

### 5.4 Secretária
- Registra decisões atuais, **trabalhos futuros** e **oportunidades/brechas**; recusa registros sem origem e justificativa.

### 5.5 Proibições Absolutas
- ❌ Inventar links, DOIs ou referências.
- ❌ Arredondar números fora do texto redigido pelo Redator.
- ❌ Especialistas usarem `ask_question` diretamente.
- ❌ O Orquestrador executar ou converter qualquer conteúdo.

---

## 6. Histórico de Revisões

| Data | Versão | Alterações |
|---|---|---|
| 2026-10-02 | v1.0 | Criação inicial da arquitetura. |
| 2026-10-02 | v2.0 | Correções FR-01 a FR-07 e Planejamento. |
| 2026-10-03 | v3.0 | Precisão numérica (Redator apenas), Orquestrador isolado com subagentes `self`, foco em ligamentos, backend incremental, `ask_question` centralizado. |
| 2026-10-05 | v4.0 | Correção de codificação (mojibake) em todos os arquivos; alinhamento do roster e das proibições às skills (microsserviços, exclusão de dados, stack do Analista/Frontend, IA/ML); 4 cenários de sensibilidade; contexto termodinâmico; meta 3D/FEBio; schemas de telemetria e feedback centralizados no `config.yaml`. |
