```json
{
  "task_id": "eval-research-vs-direction",
  "skill": "senior-applied-biomechanics-researcher-deep-research + biomechanics-co-advisor",
  "status": "success",
  "retry_count": 0,
  "data_payload": {
    "levels": {"1":2,"2":8,"3":1,"4":3,"5":2},
    "items": [
      {"item":"Funil de evidências 1.1–1.2 (modelos de tecidos moles → ligamentos)","nivel":2},
      {"item":"Justificativa FEBio (1.3)","nivel":2},
      {"item":"Escolha dos 5(+HGO) modelos","nivel":2},
      {"item":"Cheat sheet Fung QLV 3D (1.4)","nivel":1},
      {"item":"Cheat sheets Schapery/Maxwell/Weiss/Poroviscoelástico/HGO (1.5–1.9)","nivel":5},
      {"item":"Conversão de constantes 1D→3D (3.3)","nivel":4},
      {"item":"Risco UDM Schapery 4 semanas (3.4)","nivel":2},
      {"item":"Setup FEBio / geometria / solver (3.5–3.6)","nivel":3},
      {"item":"Cobertura de metodologia e métricas (4.1–4.2, 4.4)","nivel":4},
      {"item":"Rastreabilidade do 'Why'","nivel":2},
      {"item":"Comparação tecidos moles vs polímeros","nivel":2},
      {"item":"Capítulos de tese (4.3–4.7)","nivel":5},
      {"item":"Adequação ao prazo de 12 meses / escopo uniaxial de 4 ligamentos","nivel":4},
      {"item":"Weiss: relaxação zero (4.6)","nivel":2},
      {"item":"Coerência entre decisões","nivel":2}
    ]
  },
  "output_summary": "Avaliação de 15 itens do conteúdo entregue vs. thesis_roadmap Phases 1–4. Base teórica Fung e funil de literatura sólidos; faltam cheat sheets 1.5–1.9, plano de conversão 1D→3D e capítulos da tese; escopo/prazo em risco.",
  "justification_for_decisions": "Classificação baseada exclusivamente em leitura de ledger/, base_literature/, outputs/ e thesis/ (PDFs não lidos, conforme política). Nenhum DOI/URL novo foi citado; não houve web search. Itens 'não verificados' (conteúdo de JSONs grandes lido apenas por grep/listagem) foram penalizados para nível 2/3, não 1.",
  "artifacts": ["outputs/eval-research-vs-direction.md"],
  "warnings": ["task-1.1/1.2/1.3/deep-research JSONs (50–225 KB) não foram lidos integralmente; claims de volume (358 refs/301 DOIs) vêm de target_journals.md","Outputs do decisions.md têm ~106 KB: leitura por amostragem","Itens fora de ordem na 1ª linha de decisions (5 modelos) vs roadmap 4.2 (6 modelos)"],
  "token_usage": 0,
  "theoretical_precision": "medium"
}
```

# Avaliação: conteúdo entregue × rumo da pesquisa (Phases 1–4)

Escala: 1 atende completamente · 2 atende parcialmente · 3 indefinido · 4 não atende parcialmente · 5 não atende completamente.

| # | Item | Nível | Evidência (arquivo) | Lacuna | Recomendação | TASK |
|---|---|---|---|---|---|---|
| 1 | Funil de evidências (tecidos moles → ligamentos) | 2 | `decisions.md` ("Evidence Funnel"); `target_journals.md` (358 registros/301 DOIs, validados Crossref/EuropePMC); `outputs/ranked_models_analysis.md` | O ranking de modelos é qualitativo ("Altíssimo", "Médio"), sem contagens nem citações inline. Não há protocolo PRISMA-like (bases, strings, critérios de inclusão). O ranking está em PT, o restante em EN. | Adicionar tabela string de busca → n → critérios → n final. Citar DOIs já verificados no ranking. | 1.1, 1.2 |
| 2 | Justificativa FEBio vs Abaqus/Ansys | 2 | `decisions.md` (2026-10-07, 10-08) | O "Why" (open-source, biphasic nativo, plugins) é válido, mas não compara critérios: custo, curva de aprendizado, UDM API, malha, pós-processamento. Não registra o que o FEBio tem de nativo para cada um dos 5 modelos. A afirmação de que o FEBio é "tailored for biomechanics" não é citada (ex.: artigo FEBio, Maas 2012). | Tabela modelo × suporte nativo FEBio × necessidade de UDM, com versão do FEBio. Citar o paper FEBio (verificar DOI por busca). | 1.3, 3.4 |
| 3 | Escolha dos 5 (+HGO) modelos | 2 | `decisions.md` (2026-10-07 "5 Guiding Models"; 2026-10-08 HGO); roadmap 4.2 | O "Why" da escolha original é só "análises prévias do grupo". Inconsistência: a decisão cita 5, o roadmap 4.1 diz "5 models" e 4.2 lista 6 (com HGO). Maxwell/Prony e Fung ficam redundantes na curva 1D (relaxação). Falta o critério de parada se o HGO ou a poroviscoelasticidade forem cortados. | Formalizar em decisions.md "6 modelos", com matriz de priorização (Fung, Maxwell, Weiss = núcleo; Schapery, HGO, Poro = estendidos) e ordem de corte. | 1.1, 4.1, 4.2 |
| 4 | Cheat sheet Fung QLV 3D | 1 | `theory_cheat_sheets/fung_qlv_3d.md` | Pequenas: o arquivo não traz DOIs inline (o README diz "100% CrossRef-verified") e não mapeia explicitamente parâmetros 1D (A, B, c, τ1, τ2) → 3D (C, a_ijkl). | Adicionar seção de correspondência de parâmetros (insumo da 3.3). | 1.4 |
| 5 | Cheat sheets Schapery 3D, Maxwell/Prony 3D, Weiss, Poroviscoelástico, HGO | 5 | Existem apenas `fung_qlv_3d.md` e `sensitivity_analysis_and_continuation.md` em `theory_cheat_sheets/`. Há conteúdo parcial no `thesis/Consolidated_Biomechanical_Formulations.docx` (decisions.md cita as seções 1.12–1.15: hiperelasticidade, HGO, Weiss, Boltzmann, poroviscoelasticidade). | Roadmap marca 1.5–1.9 como `[ ]`, sem entrega no formato cheat sheet. O docx não foi auditado quanto a Schapery tensorial. A cheat sheet de sensibilidade não é uma das tarefas 1.5–1.9. | Priorizar 1.6 (Prony, nativo FEBio) e 1.7 (Weiss), depois 1.5, 1.9 e 1.8. Reaproveitar o docx e converter para o formato cheat sheet. | 1.5–1.9 |
| 6 | Conversão de constantes 1D→3D | 4 | `future_works.md` (menciona só como trabalho futuro); roadmap 3.3; `fung_qlv_3d.md` §5 (redução uniaxial) | Não existe levantamento de literatura nem método. Risco científico central: constantes 1D (nominais, fibra) não são diretamente transferíveis (A,B → C e a_ijkl; Prony 1D → G(t) do FEBio; Weiss exige c3-c6, matriz e fibra separadas). Incompletude: o modelo 1D ignora a matriz, e a 3D exige recalibração (inverse FE). | Adotar estratégia de 2 etapas: (a) mapeamento analítico para uniaxial isocórico (λ, λ^-1/2); (b) recalibração inversa no FEBio (ex.: otimização local) como plano B. Fazer a revisão da 3.3 antes de montar o FEBio. | 3.3 |
| 7 | Risco do UDM Schapery (4 semanas) | 2 | roadmap 3.4 (time-box + fallback para 1D); `ranked_models_analysis.md` (Jacobiano assimétrico); `future_works.md` | A mitigação existe, mas não há gatilho mensurável (critérios de "convergência falhou"), nem pré-requisitos (ambiente C++/SDK FEBio, docker, versão). Não há alternativa nativa para a comparação 3D do Schapery (ex.: aproximar por Prony+hiperelástico). Desconsiderar Schapery do 3D enfraquece a "novidade" apontada. | Definir spike de 3 dias (UDM "hello world" no FEBio) antes das 4 semanas, critérios de saída objetivos (ex.: erro de convergência ao patch test) e fallback documentado. | 3.4 |
| 8 | Setup FEBio, contornos e solver | 3 | Roadmap 3.5–3.6; `evaluate-3d-transition.json` (plano genérico) | Sem entregas. Indefinido: geometria (cilindro idealizado vs. segmento real), BCs de garra, malha, convergência, nº de ligamentos simulados no 3D (4?). O pipeline de volta para o banco SoftTissue não está especificado. | Fechar escopo: geometria idealizada única (uniaxial, cilindro/barra), estudo de malha, 1 ligamento-piloto antes dos 4. | 3.5, 3.6 |
| 9 | Cobertura de metodologia e métricas (RMSE, R², tempo, recursos) | 4 | roadmap 4.1, 4.2, 4.4; outputs de pipeline (`task-biomechanics-phase2/3-*.json`, `task-2.1`, `task-2.2`, `task-2.7`) | O C# 1D está bem documentado (Fung, Schapery, ajuste, sensibilidade), mas Maxwell, Weiss, Poro e HGO 1D não têm entrega (2.3–2.6 abertos). Faltam: estatística comparativa (AIC/BIC, validação cruzada, número de parâmetros), intervalos de confiança, tratamento da variabilidade entre espécimes, e protocolos de pré-condicionamento. R² e RMSE sozinhos favorecem modelos com mais parâmetros. | Incluir AIC/BIC, resíduos, identificabilidade (já há base na cheat sheet de sensibilidade) e normalização por nº de parâmetros. | 4.1, 4.2, 4.4 |
| 10 | Rastreabilidade do "Why" | 2 | `decisions.md` (~106 KB; muitos "Why:" por entrada); `thesis_roadmap.md` (Core Philosophy) | Boa cobertura nas decisões de engenharia (storage API, pipelines), mas desproporcional: muito do ledger trata de infraestrutura C#. O "Why" científico (por que 6 modelos, por que uniaxial, por que Dr. Rodarte como fonte) é curto. O ledger não tem índice, nem vínculo ID-decisão → TASK. | Criar índice por TASK e backfill de "Why" científico para as decisões 1–3 acima. | 1.1–1.9, 4.x |
| 11 | Comparação tecidos moles vs polímeros | 2 | `base_literature/comparative_mechanics_soft_tissues_vs_polymers.md` (60 estudos CrossRef); `task-deep-research-soft-tissue-vs-polymers-*.json`; `task-review-comparative-literature-base.json` | Bem desenvolvida, mas fora do roadmap (Phases 1–4 não têm TASK para isso). Risco de scope creep: o `biomechanics-co-advisor` limita o foco aos ligamentos do joelho. A conexão com as 5 escolhas de modelos (por que Schapery, de origem polimérica, serve ao ligamento) está implícita. | Usar como um ou dois parágrafos de justificativa do Schapery no Cap. 2 (4.3). Não expandir. | 1.1, 4.3 |
| 12 | Capítulos de tese | 5 | Roadmap 4.3–4.7 `[ ]`; `thesis/` contém apenas `Consolidated_Biomechanical_Formulations.docx` | Nenhum capítulo (Intro, Métodos, Resultados, Discussão) existe. O docx é apêndice de formulações, não um capítulo. 4.7 (revisão) não pode iniciar. | Escrever o rascunho de Intro/Fundamentação (4.3) assim que 1.5–1.9 estiverem prontos; revisar por capítulo, não só no final. | 4.3–4.7 |
| 13 | Prazo de 12 meses e escopo uniaxial dos 4 ligamentos suínos | 4 | `decisions.md` (2026-10-07, reestruturação do prazo); roadmap Phases 2–4 | A decisão de excluir joelho completo é coerente. Porém: 6 modelos × 4 ligamentos × 3 fontes (experimental, 1D, 3D) = 72 combinações, mais UDM e escrita, em 12 meses. Phase 2 (C#/frontend/API) consumiu grande parte do esforço registrado, enquanto Phase 1 (1.5–1.9) e 3.x estão atrasadas. Dados: o roadmap não declara o n de espécimes, os protocolos e as taxas de deformação da tese do Dr. Rodarte, nem se os 4 ligamentos têm relaxação. | Cronograma com marcos mensais. Congelar o frontend. Reduzir o 3D ao núcleo (Fung/Prony, Weiss, HGO) em 1–2 ligamentos, mantendo os 4 no 1D. Confirmar a disponibilidade dos dados antes da Phase 3. | Todos |
| 14 | Weiss: relaxação zero (hiperelástico) | 2 | roadmap 4.6; `ranked_models_analysis.md` (Weiss como baseline) | O ponto está listado como pontos de discussão, mas sem formalização: um modelo hiperelástico puro prevê σ_inicial = σ_final, então não ajusta os dados de relaxação. Falta definir como Weiss entrará nas métricas (apenas na rampa? acoplado a Prony?). O FEBio permite combinar viscoelasticidade com materiais base, e isso muda a comparação "justa". | Decidir e registrar: Weiss isolado (baseline elástico, avaliado só na rampa/quase-estática) vs. Weiss+Prony. Incluir nas notas da 1.7 e na 4.1. | 1.7, 4.1, 4.6 |
| 15 | Coerência entre decisões e lacunas científicas | 2 | `decisions.md`, roadmap, `target_journals.md`, `ranked_models_analysis.md` | (a) 5 vs 6 modelos. (b) O manuscrito de `target_journals.md` foca em Fung contínuo vs Fung simplificado (Prony) 1D, enquanto a tese enfatiza 3D/FEBio: são duas narrativas, sem relação declarada. (c) Poroviscoelasticidade em tração uniaxial de ligamento: baixa evidência para ligamentos (ranking: "Médio"), alto custo, sem justificativa de por que o fluxo de fluido importa em tração. (d) Não há plano para pré-condicionamento, taxa de deformação (fica em future_works) nem validação do FEBio (benchmark analítico). (e) `target_journals.md` e outros arquivos usam caminhos `c:/Pessoal/...`, o que quebra portabilidade. | Registrar o relacionamento artigo-tese. Reavaliar a poroviscoelasticidade (candidata ao corte/future work). Incluir benchmark analítico FEBio (patch uniaxial) antes das comparações. | 1.8, 3.5, 4.1 |

## Resumo

- **Atende:** apenas o cheat sheet Fung (1.4) é nível 1.
- **Parcial:** o funil de literatura e as decisões sobre FEBio e modelos têm base real (358 registros, 301 DOIs), mas faltam a transparência do protocolo e um "Why" científico detalhado. A comparação polímero × tecido é boa, porém tangencial.
- **Críticos (4–5):** cheat sheets 1.5–1.9 e capítulos da tese não foram entregues. A conversão 1D→3D (3.3) não tem método, e o planejamento do prazo para 6 modelos × 4 ligamentos × 3 fontes é otimista.
- **Prioridades (próximas 4 semanas):**
  1. Fechar a lista de modelos (6) com ordem de corte e decidir sobre poroviscoelasticidade.
  2. Entregar as cheat sheets 1.6 e 1.7 e depois 1.5 e 1.9.
  3. Fazer o levantamento da 3.3 e um spike de UDM/FEBio com benchmark uniaxial.
  4. Confirmar os dados do Dr. Rodarte (n, taxas, relaxação nos 4 ligamentos).
  5. Congelar o escopo do frontend.
- **Limitações desta avaliação:** os JSONs grandes (task-1.1, 1.2, 1.3, deep-research) e o docx não foram lidos integralmente, e PDFs não foram lidos. Nenhum DOI novo foi citado.
