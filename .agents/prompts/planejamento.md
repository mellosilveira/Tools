# Planejamento: Ecossistema de Agentes de Pesquisa em Biomecânica
*Documento vivo — última revisão: 2026-10-03*

> [!IMPORTANT]
> Este documento é a fonte da verdade do ecossistema de agentes. Toda decisão arquitetural, contrato de interface e regra de operação está registrada aqui. Atualize-o sempre que uma skill for modificada.

---

## 1. Visão Geral e Objetivo

O ecossistema é composto por **1 Orquestrador Central** e **8 Agentes Especialistas** que atuam de forma autônoma para dar cadência à pesquisa de comportamento mecânico de tecidos moles humanos (foco inicial: ligamentos de joelho). 

**Evolução Teórica Esperada:** Atualmente, a pesquisa e os algoritmos lidam com **modelos mecânicos escalares (1D)**. No entanto, um dos objetivos finais é evoluir para **modelos mecânicos tensoriais (3D)** utilizando o rigor da Mecânica do Contínuo (tensores de tensão de Cauchy/Piola-Kirchhoff, gradiente de deformação, invariantes). Toda a arquitetura (banco de dados, scripts analíticos e UI) deve ser projetada de forma extensível para suportar essa migração geométrica e matemática no futuro.

**Princípios de Design:**
- **Alta Precisão Numérica:** Apenas o Redator arredonda dados para o texto (3 sig figs). O restante opera com precisão total.
- **Isolamento de Tarefas:** O Orquestrador opera de forma completamente separada e delega TUDO via subagentes (`Workspace: inherit` e não branch, para preservar artefatos).
- **Rastreabilidade Total:** Toda saída produz um Contrato JSON e o Orquestrador alimenta o log `telemetry.jsonl`.
- **Aprovação Humana Centralizada:** Apenas o Orquestrador invoca `ask_question`. Se o subagente falhar em 2 tentativas (Circuit Breaker), o Orquestrador avisa o humano.
- **Arquitetura Incremental:** O Engenheiro Backend atua exclusivamente de forma incremental, nunca gerando sistemas do zero, e sempre atualizando `AGENTS.md` e `CHANGELOG.md`.

---

## 2. Topologia da Arquitetura

O Orquestrador cria subagentes genéricos (`TypeName: self`, `Workspace: inherit`) e em seus Prompts envia a instrução para o subagente assumir a persona lendo seu respectivo `SKILL.md`.

*Orquestrador → Subagente (self) → Retorna JSON → Orquestrador decide próximo passo.*

---

## 3. Roster de Agentes

| Nome da Pasta (nome canônico) | Papel |
|---|---|
| `orquestrador-central-de-pesquisa-em-biomecanica` | Planejamento, delegação via subagente, ask_question. |
| `secretaria-de-documentacao-e-rastreamento` | Grava histórico no `decisoes.md`. Exige "O quê / De onde / Por quê". |
| `pesquisador-senior-em-biomecanica-aplicada-deep-research` | Deep Research. Apenas focado em fatos e limitações de modelo. |
| `analista-de-dados-experimentais` | Scripts Python locais (run_command) para regressão e avaliação estatística. |
| `redator-academico-especialista-em-biomecanica` | Escrita de alto padrão. Arredonda para 3 sig figs no texto. |
| `revisor-de-papers-de-alto-padrao` | Crítico rígido. Formato acionável. |
| `coorientador-de-teses-em-biomecanica` | Guardião do escopo (ligamento joelho). Anti-modismos. |
| `engenheiro-de-software-backend-senior-net` | C# .NET/PostgreSQL incremental. Atualiza AGENTS.md e CHANGELOG.md. |
| `engenheiro-de-software-frontend-mobile-senior` | React/Kotlin/Uno com UI renderizada em WebGL/Canvas para datasets grandes. |

---

## 4. Contratos e Telemetria

### 4.1 Contrato de Saída — Especialistas → Orquestrador
```json
{
  "status": "success | partial | failure",
  "output_summary": "...",
  "artifacts": ["..."],
  "warnings": ["..."]
}
```

### 4.2 Telemetria (Orquestrador → `telemetry.jsonl`)
O Orquestrador escreve `{"timestamp": "...", "skill": "nome", "status": "...", "rework_count": N, "warnings": [...]}` em modo `Append: true`.

---

## 5. Regras Transversais de Operação

### 5.1 O Papel do Orquestrador e o Fluxo Humano
- **Nenhuma execução:** O Orquestrador NUNCA executa tarefas especializadas (como rodar scripts ou processar PDFs pesados diretamente). Ele é o gerente central.
- **Planejamento Macro e Micro:** Sempre inicia criando um planejamento Macro com o humano e dividindo as tarefas em Múltiplos Micro Planejamentos.
- **Circuit Breaker:** Se um subagente falhar em 2 tentativas, chama o humano.
- **Tratamento de PDF:** O Orquestrador para e usa `ask_question` para consultar o humano antes de tentar converter ou lidar com arquivos PDF.

### 5.2 Sinergia Backend e Analista
- **Consultoria Técnica:** O *Analista de Dados* não deve reinventar a roda. Antes de propor um algoritmo novo, deve pedir (via Orquestrador) uma avaliação ao *Engenheiro Backend* sobre quais ferramentas C# existentes podem ser reutilizadas.
- **Fluxo de Decisão:** Analista pede análise $\rightarrow$ Backend responde $\rightarrow$ Analista processa a resposta e leva o consenso ao Orquestrador $\rightarrow$ Orquestrador decide com o humano.
- **Domínio Analítico:** O Analista foca em Análise de Sensibilidade (Tensão x Tempo inicial/final/assíntota) e cálculo propagado de erro (RMSE, $R^2$). O Backend foca em garantir que a infraestrutura (C#) atenda esses cálculos escaláveis.

### 5.3 O Papel da Secretária Documental
- Além de anotar decisões do caminho ATUAL, a Secretária deve registrar **ideias para Trabalhos Futuros**, brechas ou oportunidades metodológicas para expandir a pesquisa depois.

### 5.4 Proibições Absolutas
- ❌ Inventar links, DOIs, ou referências.
- ❌ O Analista de Dados não aproxima números, apenas o Redator faz isso.
- ❌ O Backend não cria microserviços, não deleta dados científicos, e não atua em arquiteturas não previstas no `AGENTS.md`.

---

## 6. Histórico de Revisões

| Data | Versão | Alterações |
|---|---|---|
| 2026-10-02 | v1.0 | Criação inicial da arquitetura. |
| 2026-10-02 | v2.0 | Correções FR-01 a FR-07 e Planejamento. |
| 2026-10-03 | v3.0 | Atualização de precisão numérica (Redator apenas), Orquestrador em modo self-subagent e isolado, foco em ligamentos, desenvolvimento backend estritamente incremental, consolidação de ask_question centralizado e correção YAML. |
