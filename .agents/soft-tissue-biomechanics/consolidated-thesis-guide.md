# Guia Consolidado da Dissertação de Mestrado — Biomecânica de Tecidos Moles (PPEMM/UFCIRJ)

> **Documento Mestre Consolidado** — Unifica as decisões científicas, o funil de evidências, o detalhamento analítico-numérico dos modelos constitutivos, o diagnóstico de maturidade (Pesquisa, Backend e Frontend), o roteiro de execução e os trabalhos futuros previamente dispersos em múltiplos artefatos de agentes.
>
> *Última consolidação: 2026-11-19*

---

## 1. Identificação, Escopo e Filosofia Científica

* **Título Provisório:** *Viscoelastic Characterization and Load-Sharing Modeling of Knee Ligaments: A Unified Computational Framework* (Foco da Dissertação: *Comparative Analysis of Non-Linear and Viscoelastic Constitutive Models Applied to Porcine Knee Ligaments under Uniaxial Tension*).
* **Programa / Instituição:** Programa de Pós-Graduação em Engenharia Mecânica e Tecnologia de Materiais (PPEMM), Universidade Federal de Ciência e Inovação do Rio de Janeiro (UFCIRJ, antigo CEFET/RJ).
* **Autor:** Bruno Mello Silveira (`ORCID: 0000-0001-6793-9387`).
* **Orientador:** Prof. Dr. Paulo Pedro Kenedi (`ORCID: 0000-0001-5563-843X`).
* **Base Experimental:** Ensaios uniaxiais de tração com rampa e relaxação de tensão em ligamentos isolados de joelho suíno (ACL, PCL, MCL, LCL — dados da tese do Dr. Rodrigo Rodarte).
* **Escopo Físico-Matemático Estrito:**
  * **Carregamento:** Tração uniaxial coaxial sob grandes deformações (*finite strain*) em regime quase-estático ($\rho \ddot{\mathbf{u}} \approx 0$), com eixos principais fixos (rotação nula $\mathbf{R} \equiv \mathbf{I}$, spin nulo $\mathbf{W} \equiv \mathbf{0}$) e incompressibilidade isocórica ($J \approx 1$, $A(t) = A_0 / \lambda(t)$).
  * **Fenômeno Viscoelástico:** Estritamente **relaxação de tensão** (*stress relaxation*). Formulações de fluência (*creep*) e o modelo Kelvin-Voigt foram removidos do escopo principal por não corresponderem ao protocolo experimental.
  * **Teoria de Pequenas Deformações:** Mantida apenas como linha de base assintótica comparativa (erro quadrático $\Delta = E_{11} - \varepsilon = \frac{1}{2}\varepsilon^2$: 5% em $\varepsilon = 10\%$ e 12,5% em $\varepsilon = 25\%$).
  * **Termodinâmica:** Restrita aos potenciais de energia livre necessários às leis constitutivas hiperelásticas e não-lineares (ex.: energia livre de Helmholtz no modelo de Schapery), assumindo ensaio isotérmico.
* **Tríade de Dados (Arquitetura Computacional):**
  1. **Dados Experimentais Reais** (curvas força/deslocamento convertidas em tensão-deformação verdadeira);
  2. **Simulação Numérica Escalar 1D** em C# / .NET 10 (`MelloSilveiraTools`, sem sobrecarga de malha FEA);
  3. **Simulação Tensorial 3D** em software open-source (**FEBio**), com recalibração 1D→3D e sub-rotinas C++ UDM apenas para modelos não nativos.
* **Filosofia Central ("The Why") e Precisão:** Toda decisão arquitetural ou metodológica deve registrar justificativa explícita (*What / From where / Why*). Números são mantidos em precisão total em dados/código e arredondados para **3 algarismos significativos** apenas no texto dissertativo final.

---

## 2. Matriz Unificada dos 6 Modelos Constitutivos

Resultado consolidado do **Funil de Evidências** (358 registros auditados → **301 DOIs únicos** em [`base-literature/evidence-funnel-bibliography.md`](./base-literature/evidence-funnel-bibliography.md)) cruzado com a avaliação de código (`MelloSilveiraTools`) e suporte no **FEBio**:

| Prioridade | Modelo Constitutivo | Uso na Literatura (Joelho) | Status 1D (`MelloSilveiraTools`) | Suporte 3D (FEBio) | Papel na Tese / Critério de Corte |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **1 (Núcleo)** | **Fung QLV (Espectro Contínuo $E_1$)** | Alto (Padrão-ouro histórico) | **Implementado** (`FungModelCalculator`, `FungRelaxationOnlyCurveFitterStep`, Sweep) | Nativo / via viscoelasticidade | Referência analítica histórica em ensaios uniaxiais *ex vivo*. |
| **2 (Núcleo)** | **Simplified Fung / Maxwell Generalizado (Série de Prony)** | Altíssimo (Padrão da indústria FEA) | **Parcial** (`SimplifiedFung` completo; `MaxwellModelCalculator` + `PronySeriesCurveFitter` existem, falta step/factory dedicado) | **Nativo** (variáveis internas) | Padrão de benchmark em FEA; base direta do artigo científico principal. |
| **3 (Núcleo)** | **Weiss Transversalmente Isotrópico** | Muito Alto (Padrão FEBio para LCA) | **Ausente** (implementar 1D uniaxial fibra-exponencial) | **Nativo** (`trans iso Mooney-Rivlin` / Weiss) | Linha de base hiperelástica pura ($\sigma_{\text{inicial}} = \sigma_{\text{final}}$, relaxação zero) para contraste na Discussão. |
| **4 (Estendido)** | **Schapery Viscoelástico Não-Linear** | Baixo / Emergente em 3D (Diferencial) | **Implementado** (`SchaperyModelCalculator`, `SchaperyRelaxationOnlyCurveFitterStep`, Sweep) | **Não nativo** (requer UDM C++) | Diferencial científico da tese. *Time-box* estrito de 4 semanas / 4 ciclos para UDM; fallback documentado para comparação estritamente 1D. |
| **5 (Estendido)** | **Holzapfel-Gasser-Ogden (HGO)** | Altíssimo (Dispersão de fibras $\kappa$) | **Ausente** (implementar 1D uniaxial com parâmetro $\kappa$) | **Nativo** | Modelo estrutural moderno com dispersão angular das fibras de colágeno. |
| **6 (Corte / Futuro)** | **Poroviscoelasticidade (Bifásico)** | Médio para ligamentos (Alto p/ cartilagem) | **Ausente** | **Nativo** (bifásico) | **Candidato prioritário a Trabalho Futuro** caso o cronograma aperte; redução 1D sem drenagem lateral tem baixa aderência em tração pura de ligamento. |

*Nota sobre Tecidos Moles vs. Polímeros:* O levantamento comparativo de 60 estudos verificados via CrossRef em [`base-literature/comparative-mechanics-soft-tissues-vs-polymers.md`](./base-literature/comparative-mechanics-soft-tissues-vs-polymers.md) embasa a fundamentação do porquê modelos oriundos da reologia de polímeros (especialmente **Schapery** e séries de Prony) podem ser adaptados a ligamentos biológicos, além de contrastar *toe-region* de colágeno, pré-condicionamento vs. efeito Mullins e tenacidade à fratura.

---

## 3. Base Matemática, Identificação de Parâmetros e Sensibilidade (Materiais e Métodos)

### 3.1. Documento Canônico de Formulações
* **Arquivo oficial:** [`thesis/Consolidated_Biomechanical_Formulations.docx`](./thesis/Consolidated_Biomechanical_Formulations.docx) (gerado via [`scripts/build-consolidated-thesis.py`](./scripts/build-consolidated-thesis.py) e validado por [`scripts/verify-consolidated-thesis.py`](./scripts/verify-consolidated-thesis.py)).
* **Estrutura:** **246 equações numeradas sequencialmente** em 7 capítulos (padrão ABNT/PPEMM, OMML nativo, 100% preto automático):
  1. **Cap. 1:** Mecânica do Contínuo (formulações duais Lagrangiana e Euleriana, síntese experimental de tensores $\mathbf{F}, \mathbf{C}, \mathbf{E}, \mathbf{e}$ a partir de LVDT/célula de carga, operadores tensoriais e as 4 vertentes 3D: hiperelasticidade, viscoelasticidade, viscohiperelasticidade e viscoporoelasticidade);
  2. **Cap. 2:** Transformações cinemáticas e viscoelasticidade linear (foco em relaxação);
  3. **Cap. 3:** Viscoelasticidade Quase-Linear (progressão didática: integral hereditária geral → relaxação em degrau → resposta elástica exponencial $\sigma^e(\varepsilon) = A(e^{B\varepsilon}-1)$ → vertentes Fung Contínuo $E_1(z)$ vs. Fung Simplificado com Prony);
  4. **Cap. 4:** Viscoelasticidade Não-Linear (Schapery em relaxação com $h_e, h_2$ e Método da Superposição Modificada);
  5. **Cap. 5:** Gradientes analíticos gerais de sensibilidade $\nabla_{\mathbf{p}}\sigma$, Levenberg-Marquardt, continuação de isoclina e condição de zero-crossing para tempo de assíntota;
  6. **Cap. 6:** Compartilhamento de carga 3D anatômico entre os 4 ligamentos do joelho;
  7. **Cap. 7:** Registro bibliográfico auditado via CrossRef/ISBN.

### 3.2. Estratégias de Ajuste de Curvas Implementadas em `MelloSilveiraTools`

1. **Fung QLV 1D (Desacoplamento em 2 Estágios — `FungRelaxationOnlyCurveFitterStep`):**
   * *Estágio 1 (Relaxação Normalizada):* Minimiza $\text{SSR}_{\text{relax}}$ ajustando $(c, \tau_1, \tau_2)$ em $G(t) = \frac{1 + c[E_1(t/\tau_2) - E_1(t/\tau_1)]}{1 + c\ln(\tau_2/\tau_1)}$, sujeito à restrição física $\tau_1 < \tau_2$, com limites $c \in [0, 1]$, $\tau_1 \in [10^{-4}, 0{,}1]\,\text{s}$, $\tau_2 \in [1, 10^5]\,\text{s}$ (via Evolução Diferencial ALGLIB + polimento L-BFGS).
   * *Estágio 2 (Rampa):* Com $(c^*, \tau_1^*, \tau_2^*)$ fixos, ajusta constantes elásticas $A \in [10^{-6}, 10^6]\,\text{MPa}$ e $B \in [0, 100]$ sobre a convolução hereditária (`Disregard` e `ConsiderWithViscoelasticEffect` validados; `ConsiderWithoutViscoelasticEffect` em evolução).
2. **Schapery Não-Linear 1D (Desacoplamento Hierárquico em 3 Estágios — `SchaperyRelaxationOnlyCurveFitterStep`):**
   * *Estágio 1 (Segmento Âncora Linear, menor deformação $\varepsilon_1$):* Fixa $h_e = h_1 = h_2 = 1$ e ajusta $(G_e, g_1, n)$ em $\sigma(t) = (G_e + g_1 t^{-n})\varepsilon_1$ via L-BFGS limitado (`minbleic`).
   * *Estágio 2 (Segmentos Não-Lineares Subsequentes):* Com $(G_e^*, g_1^*, n^*)$ fixos, identifica os multiplicadores pontuais de Helmholtz $h_e(\varepsilon_j), h_2(\varepsilon_j) \in [0, 10]$ por nível de deformação.
   * *Estágio 3 (Ajuste Contínuo dos Multiplicadores):* Compete funções candidatas (`Logarithmic`, `Exponential`, `Polynomial`) selecionando pelo menor SSR para formar expressões fechadas $h_e(\varepsilon)$ e $h_2(\varepsilon)$ (fallback `ConstantFunction` quando há apenas 1 segmento).
   * *Métrica Combinada:* $\text{Precision} = \frac{1}{3}\left(\frac{\sum w_j R_j^2}{\sum w_j} + R_{h_e}^2 + R_{h_2}^2\right)$.
3. **Engine de Análise de Sensibilidade 1D OAAT (`ParametricSweepPipeline`):**
   * Avalia os **4 cenários obrigatórios** via grafo TPL Dataflow concorrente para Fung QLV, Simplified Fung e Schapery:
     1. Tensão inicial $\sigma(t_0; \theta_i)$;
     2. Tensão final $\sigma(t_f; \theta_i)$;
     3. Tempo de assíntota $t_{\text{asymptote}}$ (critério de estabilização assintótica / busca de raiz);
     4. Variação absoluta e percentual de tensão $\Delta\sigma = \sigma(t_f) - \sigma(t_0)$ e $\%\Delta\sigma$.
   * Compila dinamicamente adaptadores de entrada em `MechanicalModelTypeCache` (zero *reflection* no *hot path*) e persiste CSVs particionados por variável.

---

## 4. Diagnóstico Unificado de Maturidade e Backlog Priorizado

Consolidação das auditorias independentes de Pesquisa, Backend e Frontend (escala: **1** atende completamente · **5** não atende):

| Eixo | Pontos Fortes (Nível 1–2) | Lacunas Críticas (Nível 4–5) | Próximas Ações Prioritárias |
| :--- | :--- | :--- | :--- |
| **Pesquisa & Teoria** | Funil de 301 DOIs auditados; `Consolidated_Biomechanical_Formulations.docx` (246 eqs); Cheat Sheets de Fung QLV 3D e Sensibilidade; revisão tecidos vs. polímeros. | Faltam Cheat Sheets 3D isolados (Schapery, Maxwell/Prony, Weiss, HGO — Tasks 1.5–1.9); falta método documentado de conversão 1D→3D (Task 3.3); capítulos dissertativos ainda não redigidos (Tasks 4.3–4.7). | 1) Fechar lista oficial de modelos com ordem de corte;<br>2) Derivar mapeamento analítico 1D→3D (uniaxial isocórico) + plano B de FEA inversa;<br>3) Iniciar redação incremental dos Capítulos 1–2. |
| **Backend (`MelloSilveiraTools` + `SoftTissue`)** | Infraestrutura de otimização (ALGLIB/MathNet, intervalos de confiança), pipelines TPL Dataflow, streaming NDJSON, persistência PostgreSQL/migrations, suítes automatizadas de testes unitários e aceitação. | Falta step/factory dedicado para Maxwell 1D (Task 2.3); faltam modelos Weiss e HGO 1D (Tasks 2.4, 2.6); **RMSE, $R^2$ persistido, AIC/BIC e metadados de execução** (tempo, memória, iterações) ausentes na entidade do banco; ausência de perfil de importação Dr. Rodarte com metadados anatômicos; integração FEBio ausente. | 1) Adicionar `RMSE`, `RSquared`, `AIC/BIC` e `ExecutionMetadata` (tempo/memória) na persistência;<br>2) Implementar `MaxwellRelaxationOnlyCurveFitterStep`, `Weiss` 1D e `HGO` 1D;<br>3) Criar projeto executável host da Web API com CORS/Swagger e perfil de dados Rodarte;<br>4) Implementar conector FEBio headless (`.feb` → solver → ingestão). |
| **Frontend & Visualização** | Contratos REST e streaming NDJSON já disponíveis no backend (`ExperimentalDataController`, `ParametricSweepController`, `PipelineProgressController`, `StorageEntryMetadataController`). | Nenhuma camada UI implementada (Task 2.10); falta endpoint de listagem de `stored_files` por `externalIdentifier`; faltam scripts Python de gráficos estáticos de alta resolução. | 1) Construir MVP Blazor WebAssembly + Canvas/WebGL (ECharts/Plotly.js) em Docker (ver plano em `prompts/planejamento-funcionalidades/`);<br>2) Construir esteira Python (Matplotlib/Seaborn) para gráficos $\ge 300$ DPI e tabelas LaTeX (`booktabs`) direto do PostgreSQL. |

### Planos Detalhados Ativos & Backlog Vivo (Fonte Única de Verdade)
* **Backlog atômico das Fases 1–4:** mantido exclusivamente em [`ledger/thesis-roadmap.md`](./ledger/thesis-roadmap.md) (evitar duplicação).
* **Planos técnicos de próximas funcionalidades:** mantidos em [`.agents/prompts/planejamento-funcionalidades/`](../prompts/planejamento-funcionalidades/README.md) (Módulo 07 FEBio 3D, Módulo 08 Frontend Blazor WebGL, Módulo 09 Gráficos Python/LaTeX).

---

## 5. Trabalhos Futuros & Diretriz de Explicabilidade (XAI)

* **Fonte única de verdade:** mantida exclusivamente em [`ledger/future-works.md`](./ledger/future-works.md) (cobre conversão 1D→3D via FEA inversa, plugins UDM C++ para FEBio com *time-box*, *strain rate* na sensibilidade e a diretriz obrigatória de **explicabilidade passo a passo teórico-experimental por IA**).

---

## 6. Mapa Rápido dos Documentos de Referência em `.agents/soft-tissue-biomechanics/`

| Caminho | Conteúdo |
| :--- | :--- |
| [`consolidated-joss-and-publications.md`](./consolidated-joss-and-publications.md) | Estratégia dual de publicação: submissão **JOSS** (`MelloSilveiraTools`) + seleção de periódicos alvo (*Scientific Reports*, *JBME*, *JMBBM*). |
| [`thesis/Consolidated_Biomechanical_Formulations.docx`](./thesis/Consolidated_Biomechanical_Formulations.docx) | Documento oficial de formulações matemáticas da dissertação (246 equações em OMML nativo). |
| [`base-literature/evidence-funnel-bibliography.md`](./base-literature/evidence-funnel-bibliography.md) | Catálogo deduplicado com todos os **301 DOIs verificados** do funil de evidências e queries de busca. |
| [`base-literature/comparative-mechanics-soft-tissues-vs-polymers.md`](./base-literature/comparative-mechanics-soft-tissues-vs-polymers.md) | Revisão comparativa tecidos moles vs. polímeros (60 estudos CrossRef verificados). |
| [`base-literature/theory-cheat-sheets/fung-qlv-3d.md`](./base-literature/theory-cheat-sheets/fung-qlv-3d.md) | Cheat sheet matemático Fung QLV 3D/1D. |
| [`base-literature/theory-cheat-sheets/sensitivity-analysis-and-continuation.md`](./base-literature/theory-cheat-sheets/sensitivity-analysis-and-continuation.md) | Cheat sheet de gradientes de sensibilidade, continuação de isoclina e referências CrossRef. |
| [`base-literature/markdown-books/fung-biomechanics-base.md`](./base-literature/markdown-books/fung-biomechanics-base.md) | Livro-base de Y. C. Fung convertido para Markdown. |
| [`ledger/decisions.md`](./ledger/decisions.md) | Ledger enxuto de rastreabilidade científica (*What / From where / Why*). |
| [`ledger/architecture.md`](./ledger/architecture.md) | Regras de governança e contratos do ecossistema multi-agente. |
| [`ledger/thesis-roadmap.md`](./ledger/thesis-roadmap.md) | Backlog atômico das Fases 1–4. |
| [`ledger/future-works.md`](./ledger/future-works.md) | Registro sintético de trabalhos futuros. |
