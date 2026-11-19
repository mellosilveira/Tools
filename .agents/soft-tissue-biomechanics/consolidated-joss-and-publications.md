# Guia Consolidado de Publicações — JOSS & Artigo Científico Principal

> **Documento Mestre de Publicação** — Consolida a estratégia dual de publicação do projeto `MelloSilveiraTools` + Dissertação PPEMM/UFCIRJ: (1) submissão do artigo de software ao *Journal of Open Source Software* (**JOSS**) / *SoftwareX* e (2) seleção sistemática completa (Etapas 1–4) do periódico alvo para o artigo científico comparativo em biomecânica de ligamentos do joelho.
>
> *Última consolidação: 2026-11-19*

---

## 1. Estratégia Dual de Publicação

A profundidade arquitetural e científica construída sustenta **duas publicações complementares e sinergéticas**:

1. **Artigo de Software (Curto, 3–5 páginas) — *Journal of Open Source Software* (JOSS)** *(alternativa secundária: **SoftwareX — Elsevier**)*:
   * **Foco:** Arquitetura de engenharia de software da suíte `.NET 10 / C# 13` `MelloSilveiraTools`, modularidade em 8 pacotes NuGet, ergonomia de desenvolvimento, independência de licenças comerciais (MATLAB Optimization/Curve Fitting Toolboxes, OriginLab Pro, Isight, Abaqus), otimização híbrida restrita (Evolução Diferencial ALGLIB + L-BFGS), pipelines TPL Dataflow com *backpressure* real e rastreabilidade científica.
   * **Manuscrito:** [`paper/paper.md`](../../paper/paper.md) e [`paper/paper.bib`](../../paper/paper.bib) (compilação PDF automatizada via `.github/workflows/draft-pdf.yml`).
2. **Artigo Científico Principal — Periódico Internacional de Biomecânica (*Scientific Reports*, *JBME* ou *JMBBM*):**
   * **Foco:** Comparação rigorosa entre o **Modelo Fung QLV (Espectro Contínuo $E_1$)** (`FungModelCalculator`) e o **Modelo Fung Simplificado / Maxwell Generalizado (Série de Prony)** (`SimplifiedFungModelCalculator`), ambos com resposta elástica exponencial $\sigma^e(\varepsilon) = A(e^{B\varepsilon}-1)$ sob os três regimes de rampa (`Disregard`, `ConsiderWithoutViscoelasticEffect` e `ConsiderWithViscoelasticEffect`), executada ponta a ponta em ligamentos do joelho (ACL, PCL, MCL, LCL) sobre o `MelloSilveiraTools`.

---

## 2. Submissão JOSS — `MelloSilveiraTools`

### 2.1. Metadados Canônicos e Conformidade Legal
* **Título do Manuscrito (`paper/paper.md`):** *MelloSilveiraTools: A .NET 10 Computational Framework for Viscoelastic Constitutive Modeling, Numerical Analysis, and Experimental Data Optimization*
* **Autores:**
  * **Bruno Mello Silveira** (`ORCID: 0000-0001-6793-9387`) — Autor Correspondente (`affiliation: "1, 2"`)
  * **Prof. Dr. Paulo Pedro Kenedi** (`ORCID: 0000-0001-5563-843X`) — Coautor / Orientador (`affiliation: 1`)
* **Afiliações:**
  1. *Graduate Program in Mechanical Engineering and Materials Technology (PPEMM), Universidade Federal de Ciência e Inovação do Rio de Janeiro (UFCIRJ), Rio de Janeiro, Brazil*
  2. *Mello Silveira Serviços LTDA, Pinheiros, São Paulo, Brazil*
* **Licença:** `GPL-3.0-or-later` (obrigatória para conformidade OSI/JOSS devido ao código-fonte C# embarcado do **ALGLIB Free Edition**, licenciado sob `GPL-2.0-or-later`).
* **Política de Uso de IA:** Seção `# AI usage disclosure` presente em `paper/paper.md` em conformidade com as normas editoriais atuais da JOSS.
* **Arquivos Comunitários Obrigatórios:** `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1) e `.github/ISSUE_TEMPLATE/` presentes na raiz.

### 2.2. Arquitetura Modular e Status Técnico
* **Alvo:** `.NET 10` (`net10.0`), C# 13, nullable habilitado, 100% documentação XML gerada no build (`GenerateDocumentationFile = true`).
* **Pacotes (`src/` — 9 projetos: 8 pacotes NuGet + `Database.Migrator`, compilando com 0 avisos e 0 erros):**
  * **Núcleo Científico (AOT-clean / *headless*):** `MelloSilveiraTools.Mathematics`, `MelloSilveiraTools.MechanicsOfMaterials`.
  * **Orquestração Científica & Otimização:** `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
  * **Infraestrutura Integrada & Meta-pacote:** `Core`, `Database`, `WebApi`, `Plugins`, `MelloSilveiraTools` (e `Database.Migrator`).
* **Testes e CI/CD:**
  * Testes unitários rápidos em memória (`test/UnitTests/UnitTests.csproj` — referenciados como 757 testes em `paper/paper.md`);
  * Testes de aceitação/integração ponta a ponta com dados reais de ligamentos (ACL, PCL, MCL, LCL) e Testcontainers PostgreSQL (`test/AcceptanceTests/AcceptanceTests.csproj`);
  * Workflows ativos em `.github/workflows/` (`publish_nuget_package.yml`, `publish_release_candidate_nuget_package.yml`, `draft-pdf.yml`).

### 2.3. Estratégia de Demonstração / Quickstart (Decisão Arquitetural)
> [!IMPORTANT]
> **Não será criado um projeto de console simples para o Quickstart.** A experiência interativa de demonstração para revisores da JOSS e usuários finais será provida por uma **Web API executável** + **Frontend Web leve**, integrando o ecossistema completo.

### 2.4. Checklist Objetivo de Prontidão para Submissão JOSS

#### Pré-Submissão
1. [ ] **Web API de Demonstração Executável (Quickstart):** Disponibilizar host executável (ex.: `src/MelloSilveiraTools.WebApi.Server/` ou `src/MelloSilveiraTools.WebApi/` executável) configurado com `AddToolsServices`, CORS, `GET /swagger` e dados amostrais (`data/samples/knee-ligaments/` com pares `strain.csv` e `stress.csv`), expondo:
   * `POST /api/v1/experimental-data/process` (`202 Accepted` com identificador de execução)
   * `POST /api/v1/experimental-data/process/batch` (concorrência via `MaxDegreeOfParallelism`)
   * `POST /api/v1/parametric-sweeps/process-from-experimental-data`
   * `GET /api/v1/pipelines/{pipelineId}/progress` (streaming NDJSON `application/x-ndjson`)
2. [ ] **Frontend Web Leve (Acessibilidade para Revisores sem perfil C#):** Upload CSV ou seleção com 1 clique de amostras pré-carregadas (ACL, PCL, etc.), gráfico de segmentação (`Ramp`, `Relaxation`, `Descent`, `Recovery`), sobreposição Experimental vs. Fung QLV / Simplified Fung / Schapery exibindo parâmetros identificados e $R^2 \ge 0.90$, e visualização OAAT.
3. [ ] **Workflow de CI para PRs e Pushes (`.github/workflows/ci.yml`):** Disparar em `push` e `pull_request` (`main` e `feature/*`), instalar `.NET 10` (`actions/setup-dotnet@v4`), executar `dotnet build MelloSilveiraTools.sln --configuration Release` e `dotnet test test/UnitTests/UnitTests.csproj --configuration Release --no-build`, e adicionar badge de status no `README.md`.
4. [ ] **Figura Ilustrativa em `paper/`:** Adicionar figura explicativa de alta qualidade em `paper/` (arquitetura TPL Dataflow e/ou comparativo experimental vs. Fung QLV vs. Simplified Fung vs. Schapery) e referenciá-la em `paper/paper.md` (`![...](figure.png)`).
5. [ ] **Sincronizar Contagem de Testes & Verificar PDF:** Reexecutar suítes completas (`UnitTests` + `AcceptanceTests` em `Release`), garantir consistência exata da contagem entre `paper/paper.md`, `README.md` e `JOSS_RECOMMENDATIONS.md`, e disparar `.github/workflows/draft-pdf.yml` para inspecionar o `paper.pdf` final.
6. [ ] **Submissão Formal:** Abrir submissão em [https://joss.theoj.org/papers/new](https://joss.theoj.org/papers/new).

#### Pós-Aceite da Revisão por Pares JOSS
* [ ] Criar tag de release final no GitHub (ex.: `v2.0.0`).
* [ ] Integrar repositório GitHub com o [Zenodo](https://zenodo.org/) (ou figshare) para emissão automática do **DOI** permanente.
* [ ] Informar versão e DOI na thread oficial de revisão da JOSS.

---

## 3. Seleção Sistemática de Periódicos Alvo (Artigo Científico Principal)

Auditoria bibliométrica completa em **4 etapas** (2026-10-16, `senior-applied-biomechanics-researcher-deep-research` + `biomechanics-co-advisor`) sobre **358 registros (301 DOIs únicos)** do Funil de Evidências ([`base-literature/evidence-funnel-bibliography.md`](./base-literature/evidence-funnel-bibliography.md)), validados via APIs Crossref e EuropePMC.

### 3.1. Etapa 1 — Todos os Periódicos Identificados no Corpus (137 periódicos / 301 artigos)

| Rank | Periódico | Editora | Total no Corpus |
| :---: | :--- | :--- | :---: |
| 1º | **Biomechanics and Modeling in Mechanobiology** | Springer | 15 |
| 2º | **Journal of Biomechanical Engineering** | ASME | 15 |
| 3º | **Scientific Reports** | Nature Portfolio | 14 |
| 4º | **Frontiers in Bioengineering and Biotechnology** | Frontiers | 13 |
| 5º | **Annals of Biomedical Engineering** | Springer / BMES | 13 |
| 6º | *Other / Thesis / Preprint* | Various | 12 |
| 7º | **Journal of Biomechanics** | Elsevier | 7 |
| 8º | **Journal of the Mechanical Behavior of Biomedical Materials** | Elsevier | 7 |
| 9º | **Computer Methods in Biomechanics and Biomedical Engineering** | Taylor & Francis | 6 |
| 10º | **Bioengineering** | MDPI | 6 |
| 11º | **PLOS ONE** | PLOS | 6 |
| 12º | **Advances in Bioengineering** | ASME | 6 |
| 13º | **Clinical Biomechanics** | Elsevier | 6 |
| 14º | **Acta Biomaterialia** | Elsevier | 4 |
| 15º | **International Journal for Numerical Methods in Biomedical Engineering** | Wiley | 4 |
| 16º | **Journal of the Royal Society Interface** | Royal Society | 4 |
| 17º | **Materials** | MDPI | 4 |
| 18º | **Frontiers in Physiology** | Frontiers | 4 |
| 19º | **Medical Engineering & Physics** | Elsevier | 3 |
| 20º | **Biomimetics** | MDPI | 3 |
| 21º | **Journal of the Mechanics and Physics of Solids** | Elsevier | 3 |
| 22º | **Computer Methods and Programs in Biomedicine** | Elsevier | 3 |
| — | *Demais 115 periódicos (1 ou 2 artigos cada)* | Various | 120 |
| **Total** | **137 Periódicos Únicos** | — | **301** |

### 3.2. Etapa 2 — Periódicos com Artigos em Fung, QLV ou Maxwell com Série de Prony (27 artigos / 16 periódicos)

| Rank | Periódico | Artigos | DOIs de Referência no Corpus |
| :---: | :--- | :---: | :--- |
| **1º** | **Scientific Reports** | **6** | `10.1038/s41598-024-67603-8`, `10.1038/s41598-025-26576-y`, `10.1038/s41598-025-08471-8`, `10.1038/s41598-025-88767-x`, `10.1038/s41598-025-23918-8`, `10.1038/s41598-018-20739-w` |
| **2º** | **Journal of the Royal Society Interface** | **2** | `10.1098/rsif.2015.0486`, `10.1098/rsif.2026.0078` |
| **3º** | **Journal of the Mechanical Behavior of Biomedical Materials (JMBBM)** | **2** | `10.1016/j.jmbbm.2026.107452`, `10.1016/j.jmbbm.2023.105968` |
| **4º** | **Biomimetics** | **2** | `10.3390/biomimetics11010004`, `10.3390/biomimetics9120762` |
| **5º** | **Journal of Biomechanical Engineering (JBME)** | **1** | `10.1115/1.4003620` |
| **6º** | **Journal of Biomechanics** | **1** | `10.1016/j.jbiomech.2008.02.007` |
| **7º** | **Frontiers in Bioengineering and Biotechnology** | **1** | `10.3389/fbioe.2026.1840657` |
| **8º** | **Medical Engineering & Physics** | **1** | `10.1016/j.medengphy.2014.04.004` |
| **9º** | **Journal of the Mechanics and Physics of Solids** | **1** | `10.1016/j.jmps.2025.106348` |
| **10º** | **International Journal for Numerical Methods in Biomedical Engineering (IJNMBE)** | **1** | `10.1002/cnm.70075` |
| **11º** | **Proc. IMechE Part C: J. Mechanical Engineering Science** | **1** | `10.1177/0954406219875771` |
| **12º** | **BioMedical Engineering OnLine** | **1** | `10.1186/s12938-015-0023-7` |
| **13º** | **Advances in Bioengineering** | **1** | `10.1115/imece1998-0121` |
| **14º** | **The Journal of the Acoustical Society of America** | **1** | `10.1121/1.4828831` |
| **15º** | **Forensic Science International** | **1** | `10.1016/j.forsciint.2026.113050` |
| **16º** | **Bioengineering & Translational Medicine** | **1** | `10.1002/btm2.70044` |

### 3.3. Etapa 3 — Periódicos com Artigos Utilizando Software Open-Source ou *In-House* (57 artigos / 27 periódicos)

| Rank | Periódico | Artigos | Foco Computacional / Ferramentas Identificadas |
| :---: | :--- | :---: | :--- |
| **1º** | **Annals of Biomedical Engineering** | **7** | Frameworks abertos de ligamento (Open Knee), FEBio FINESSE e solvers open-source. |
| **2º** | **Journal of Biomechanical Engineering** | **5** | Redes reológicas *in-house* (`10.1115/1.4003620`), solvers abertos acoplados e integração FEBio. |
| **3º** | **Biomechanics and Modeling in Mechanobiology** | **4** | Modelos reativos em FEBio, modelos baseados em voxel e código open-source. |
| **4º** | **Acta Biomaterialia** | **3** | Algoritmos e software desenvolvidos para orientação de fibras em elementos finitos. |
| **5º** | **PLOS ONE** | **3** | Pipelines open-source, malhagem customizada e scripts de processamento de dados. |
| **6º** | **Computer Methods and Programs in Biomedicine** | **3** | Programas e suítes computacionais biomédicas *in-house*. |
| **7º** | **Medical Engineering & Physics** | **2** | Sub-rotina FORTRAN *in-house* para modelagem viscoelástica em Abaqus (`10.1016/j.medengphy.2014.04.004`). |
| **8º** | **Engineering with Computers** | **2** | Frameworks open-source integrando FEBio e bibliotecas de elementos finitos. |
| **9º** | **The Knee** | **2** | Pipelines computacionais customizados para osteotomia e contato tibiofemoral. |
| **10º** | **Journal of the Royal Society Interface** | **2** | Modelos constitutivos *in-house* com sub-rotinas de evolução e redes reológicas de ramo duplo. |
| **11º** | **Scientific Reports** | **2** | Algoritmo genético *in-house* com código-fonte em material suplementar (`10.1038/s41598-024-67603-8`). |
| **12º** | **Frontiers in Bioengineering and Biotechnology** | **2** | Código open-source de validação para modelos de joelho e revisões de implementação computacional. |
| **13º** | **Journal of Biomechanics** | **1** | Rotinas abertas FEBio Studio para análise musculoesquelética dinâmica (`10.1016/j.jbiomech.2026.113308`). |
| **14º** | **Journal of the Mechanical Behavior of Biomedical Materials** | **1** | Modelo visco-hiperelástico com dano descontínuo via rotina customizada (`10.1016/j.jmbbm.2023.105968`). |
| **15º** | **Computer Methods in Biomechanics and Biomedical Engineering** | **1** | Algoritmos próprios para calibração de ligamentos colaterais (`10.1080/10255842.2023.2253950`). |
| **16º** | **PLOS Computational Biology** | **1** | Framework open-source para simulação biofísica e biomecânica. |
| **17º** | **Proc. IMechE Part C: J. Mechanical Engineering Science** | **1** | Rotina computacional própria para modelo de variáveis internas (`10.1177/0954406219875771`). |
| — | *Demais 10 periódicos (1 artigo cada)* | 10 | Códigos *in-house* e sub-rotinas customizadas diversas. |

### 3.4. Etapa 4 — Interseção Exata (Etapas 2 ∩ 3 — 8 periódicos)

| Rank | Periódico (Editora) | Total (Etapa 1) | Fung/QLV/Prony (Etapa 2) | Open-Source/*In-House* (Etapa 3) | Artigo Atendendo Ambos (DOI Referência) |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **1º** | **Scientific Reports** *(Nature Portfolio)* | 14 | **6** | 2 | **1** (`10.1038/s41598-024-67603-8`) |
| **2º** | **Journal of Biomechanical Engineering (JBME)** *(ASME)* | 15 | 1 | **5** | **1** (`10.1115/1.4003620`) |
| **3º** | **Journal of the Mechanical Behavior of Biomedical Materials (JMBBM)** *(Elsevier)* | 7 | 2 | 1 | **1** (`10.1016/j.jmbbm.2023.105968`) |
| **4º** | **Journal of the Royal Society Interface** *(Royal Society)* | 4 | 2 | 2 | **1** (`10.1098/rsif.2026.0078`) |
| **5º** | **Medical Engineering & Physics** *(Elsevier)* | 3 | 1 | 2 | **1** (`10.1016/j.medengphy.2014.04.004`) |
| **6º** | **Proc. IMechE Part C: J. Mechanical Engineering Science** *(SAGE)* | 1 | 1 | 1 | **1** (`10.1177/0954406219875771`) |
| **7º** | **Journal of Biomechanics** *(Elsevier)* | 7 | 1 | 1 | 0 |
| **8º** | **Frontiers in Bioengineering and Biotechnology** *(Frontiers)* | 13 | 1 | 2 | 0 |

### 3.5. Recomendações Top 3 (`biomechanics-co-advisor`)
1. **1ª Opção — *Scientific Reports* (JCR Q1, IF ~3.8):** Maior concentração de estudos de calibração viscoelástica no corpus (6 artigos) e forte política de reprodutibilidade e código próprio/aberto em substituição a toolboxes comerciais (benchmark direto: `10.1038/s41598-024-67603-8`).
2. **2ª Opção — *Journal of Biomechanical Engineering — JBME* (ASME, JCR Q2, IF ~1.7):** Casa histórica da metodologia Fung QLV com correção de rampa (Abramowitch & Woo, 2004) e calibrações de redes reológicas de Maxwell (`10.1115/1.4003620`), valorizando formulações 1D transparentes.
3. **3ª Opção — *Journal of the Mechanical Behavior of Biomedical Materials — JMBBM* (Elsevier, JCR Q1, IF ~3.4):** Ideal caso o artigo dê ênfase principal aos parâmetros calibrados dos ligamentos e física do tecido, posicionando a suíte `.NET 10` como subseção metodológica (*"Custom Reactive Pipeline Implementation in .NET 10"*).
