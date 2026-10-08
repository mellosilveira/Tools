# Planejamento Macro da Pesquisa (Restrição de 1 Ano)

**Título Provisório:** Análise Comparativa de Modelos Constitutivos Não-Lineares e Viscoelásticos Aplicados a Ligamentos de Joelho sob Tensão Uniaxial.
**Restrição de Prazo:** 12 meses até a defesa.
**Diretriz Arquitetural:** Foco estrito em carregamento uniaxial, isolamento anatômico e comparação estatística profunda entre modelos teóricos contra dados experimentais empíricos. Abandona-se simulações conjuntas de múltiplos ligamentos com cinemática angular complexa (mitigação de risco computacional).

---

## FASE 1: Revisão e Fundamentação (A Base Teórica)
- **Objetivo:** Estabelecer o marco teórico sólido a partir de um "Funil de Evidências", mapeando do macro (tecidos moles gerais) para o micro (ligamentos do joelho e simulação).
- **Entregáveis:**
  - Levantamento dos principais modelos constitutivos utilizados para tecidos moles em geral.
  - Levantamento focado dos principais modelos constitutivos aplicados estritamente a ligamentos de joelho.
  - Mapeamento das principais tecnologias e softwares comerciais/open-source para análise de elementos finitos (FEA) na biomecânica.
  - Cheat Sheets teóricos matemáticos dos modelos selecionados (Fung QLV 3D, Schapery, Maxwell, etc.).

## FASE 2: Engenharia Computacional (O Motor C#)
- **Objetivo:** Expandir a API SoftTissue existente para absorver a nova complexidade matemática, eliminando dependência de processamento externo paralelo (ex: MATLAB).
- **Entregáveis:**
  - Inspeção e refatoração dos arquivos base (ExecuteSpecimenAnalysis e CustomMechanicalModel).
  - Implementação de algoritmos de *Curve Fitting* de forma nativa no C# (via MathNet/ALGLIB) para extração limpa das constantes visco-hiperelásticas a partir de dados brutos.
  - Testes unitários validando as equações de Fung e Schapery no C# contra saídas analíticas conhecidas.

## FASE 3: Aplicação Experimental e Curva Analítica
- **Objetivo:** Injetar os dados reais de laboratório no motor C# e processar as regressões isoladamente.
- **Entregáveis:**
  - Extração e formatação dos dados de carregamento uniaxial experimentais para cada um dos 4 ligamentos (LCA, LCP, LCM, LCL) individualmente.
  - Execução dos rotinas de otimização de forma plural (todos os modelos teóricos resolvidos contra todos os ligamentos isolados).

## FASE 4: Análise Comparativa e Redação (O Coração da Tese)
- **Objetivo:** O "clímax" do trabalho. Contrastar os modelos e definir limitações.
- **Entregáveis:**
  - Tabelas de Batalha de Modelos: comparação do ajuste matemático via métricas rígidas ($, ^2$).
  - Discussão (Prós e Contras): Ponderar estabilidade numérica (oscilação de constantes, assíntotas, custos computacionais) vs. precisão física. Exemplo clássico: o problema da assíntota que decai a zero em modelos empíricos versus modelos estruturais.
  - Geração de gráficos com barras de erro (mediana e quartis) usando *scripts* automatizados, prontos para a submissão de revista.
  - Defesa da tese.

---

## O Backlog Atômico (Micro-Tasks)

Este documento destrincha as 4 fases do Planejamento Macro em tarefas atômicas (micro-tasks). Cada tarefa é desenhada para ser executada de forma independente por um Agente de IA ou por um pesquisador humano, sem necessidade de contexto global prévio.

---

## FASE 1: Revisão e Fundamentação
*Status: Em andamento*

- **[ ] TASK 1.1:** [FUNIL - PASSO 1] Levantar, listar e justificar os principais modelos constitutivos (elásticos, hiperelásticos, viscoelásticos) utilizados para tecidos moles em geral na literatura atual.
- **[ ] TASK 1.2:** [FUNIL - PASSO 2] Afunilar a pesquisa para listar e comparar os principais modelos constitutivos utilizados *especificamente* para modelar ligamentos de joelho.
- **[ ] TASK 1.3:** [FUNIL - PASSO 3] Levantar e catalogar as principais tecnologias e pacotes de software utilizados para Análise de Elementos Finitos (FEA) na biomecânica de tecidos (ex: FEBio, Abaqus, ANSYS, PolyFEM).
- **[x] TASK 1.4:** Escrever o Cheat Sheet matemático do modelo QLV 3D de Fung com referências de ensaios. (CONCLUÍDO)
- **[ ] TASK 1.5:** Escrever o Cheat Sheet matemático do **Modelo Não-Linear de Schapery em formulação Tensorial 3D** focado em tração uniaxial.
- **[ ] TASK 1.6:** Escrever o Cheat Sheet matemático de **Modelos baseados em Molas-Amortecedores** (Maxwell Generalizado/Standard Linear Solid).
- **[ ] TASK 1.7:** Escrever o Cheat Sheet matemático do **Modelo Hiperelástico Transversalmente Isotrópico de Weiss** (padrão em ligamentos).
- **[ ] TASK 1.8:** Escrever o Cheat Sheet teórico-matemático de **Poroelasticidade (Modelos Bifásicos)** aplicados a tecidos articulares.

---

## FASE 2: Engenharia Computacional (Integração C# e MelloSilveiraTools)
*Status: A Fazer (Integração de Sistemas)*

- **[ ] TASK 2.1:** Inspecionar o código legado em D:\Pessoal\Bruno\GitHub\SoftTissue\api\src (especificamente ExecuteSpecimenAnalysis e DataContracts) e mapear as propriedades que recebem dados brutos de laboratório.
- **[ ] TASK 2.2:** Codificar a "Ponte" (Wire-up) conectando os dados extraídos do projeto SoftTissue às interfaces ICurveFitter do MelloSilveiraTools.MechanicsOfMaterials.Optimizations.
- **[ ] TASK 2.3:** Implementar/Garantir a função objetivo de **Fung QLV** dentro do motor do ALGLIB/MathNet em MelloSilveiraTools, para fittar as constantes , a, c, \tau_1, \tau_2$.
- **[ ] TASK 2.4:** Implementar/Garantir a função objetivo de **Schapery** dentro do motor do ALGLIB/MathNet em MelloSilveiraTools.
- **[ ] TASK 2.5:** Escrever **Testes Unitários (xUnit)** no repositório MelloSilveiraTools validando o output das constantes numéricas fittadas pelo C# contra valores "mockados" sabidamente corretos gerados anteriormente no MATLAB.

---

## FASE 3: Processamento Experimental (Data Crunching)
*Status: A Fazer*

- **[ ] TASK 3.1:** Ingerir os CSVs/Planilhas de ensaio real de tração do **LCA (Ligamento Cruzado Anterior)** na API C# e salvar no banco/json as constantes ótimas retornadas por *todos* os modelos implementados.
- **[ ] TASK 3.2:** Repetir a ingestão e Curve Fitting (via C#) para o **LCP (Ligamento Cruzado Posterior)**.
- **[ ] TASK 3.3:** Repetir a ingestão e Curve Fitting (via C#) para o **LCM (Ligamento Colateral Medial)**.
- **[ ] TASK 3.4:** Repetir a ingestão e Curve Fitting (via C#) para o **LCL (Ligamento Colateral Lateral)**.
- **[ ] TASK 3.5:** Executar simulação forward (Simulação Numérica Step-by-Step) usando as constantes otimizadas para gerar vetores de "Tensão vs Tempo" projetados pelos modelos. Exportar CSVs com essas curvas sintéticas.

---

## FASE 4: Análise Comparativa e Redação (O Documento da Tese)
*Status: A Fazer*

- **[ ] TASK 4.1:** Calcular métricas globais de erro estatístico ($ e ^2$) comparando os vetores numéricos de cada modelo contra os vetores de dados brutos reais.
- **[ ] TASK 4.2:** Plotar Gráficos Comparativos (via script Python do *Data Analyst*) sobrepondo os dados experimentais (com barras de erro/mediana) e as curvas teóricas de Fung, Schapery e Maxwell.
- **[ ] TASK 4.3:** Redigir o "Capítulo de Resultados": focado na tabela comparativa dos erros e no custo computacional de convergência de cada modelo no ALGLIB.
- **[ ] TASK 4.4:** Redigir o "Capítulo de Discussão/Conclusão": ponderar fisicamente os prós e contras. Focar em estabilidade das constantes (oscilações) e precisão de longo prazo (assíntotas físicas vs colapso para zero).
- **[ ] TASK 4.5:** Passar os capítulos gerados pelo crivo cego do *High-Standard Paper Reviewer* para auditoria rigorosa antes de compilar o PDF final para a banca.






