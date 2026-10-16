# Mapeamento e Seleção de Periódicos-Alvo para Submissão

- **Data**: 2026-10-16
- **Skills Aplicadas**: `senior-applied-biomechanics-researcher-deep-research` e `biomechanics-co-advisor`
- **Contexto**: Dissertação de Mestrado PPEMM/CEFET-RJ (Orientador: Prof. Paulo Pedro Kenedi) — Caracterização e modelagem constitutiva do comportamento mecânico de ligamentos do joelho (LCA, LCP, LCM, LCL).
- **Base Auditada**: 358 registros de referências (301 DOIs únicos) extraídos integralmente dos arquivos na pasta `.agents/soft-tissue-biomechanics/outputs/` (`task-1.1_result.json`, `task-1.2-results.json`, `task-1.3-payload.json`, `task-deep-research-funnel.json` e `task-deep-research-unified.json`), validados via APIs do Crossref e EuropePMC.
- **Escopo do Artigo a Submeter**:
  1. Comparação rigorosa entre o **Modelo QLV de Fung de Espectro Contínuo** ([`FungModelCalculator`](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs)) e o **Modelo QLV Discreto / Maxwell Generalizado com Série de Prony** ([`SimplifiedFungModelCalculator`](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/SimplifiedFung/SimplifiedFungModelCalculator.cs)), ambos integrando a resposta elástica exponencial $\sigma^e(\varepsilon) = A(e^{B\varepsilon}-1)$ sob as 3 condições de rampa (`Disregard`, `ConsiderWithoutViscoelasticEffect` e `ConsiderWithViscoelasticEffect`);
  2. Implementação integral da cadeia de cálculo (segmentação por janela deslizante, *warm-start* analítico, regressão não-linear restrita e análise de sensibilidade paramétrica) por meio de uma **suíte de software científica própria desenvolvida em C# / .NET 10** ([`MelloSilveiraTools`](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core)), com o objetivo deliberado de **eliminar a dependência de softwares comerciais pagos** (MATLAB Optimization/Curve Fitting Toolboxes, OriginLab Pro, Isight, Abaqus).

---

## 1. Metodologia Sistemática em 4 Passos

Para evitar viés de seleção e garantir rastreabilidade irrestrita com o material já investigado na pesquisa, a análise seguiu 4 etapas consecutivas sobre a base de dados:

1. **Passo 1:** Extração e contagem de todas as revistas científicas identificadas para os 301 artigos citados nos `outputs`;
2. **Passo 2:** Identificação das revistas que mais possuem artigos abordando o **Modelo de Fung**, a **Teoria Quase-Linear Viscoelástica (QLV)** ou o **Modelo de Maxwell Generalizado com Série de Prony**;
3. **Passo 3:** Identificação das revistas que mais possuem artigos que utilizaram **softwares abertos** (*open-source*, FEBio, OpenKnee) ou **códigos/softwares desenvolvidos pelo próprio grupo de pesquisa** (*in-house*, sub-rotinas customizadas UMAT/VUMAT/UDM, scripts próprios disponibilizados pelos autores);
4. **Passo 4:** Determinação da **interseção exata** entre os Passos 2 e 3, evidenciando os periódicos que possuem tradição e receptividade simultânea a modelos viscoelásticos e a soluções computacionais proprietárias/abertas.

---

## 2. Passo 1 — Todas as Revistas Encontradas nos Outputs

Dentre os 301 artigos únicos analisados, foram identificados **137 periódicos científicos distintos**. As revistas mais frequentes na base são:

| Posição | Periódico | Editora | Total nos Outputs |
| :---: | :--- | :--- | :---: |
| 1º | **Biomechanics and Modeling in Mechanobiology** | Springer | 15 |
| 2º | **Journal of Biomechanical Engineering** | ASME | 15 |
| 3º | **Scientific Reports** | Nature Portfolio | 14 |
| 4º | **Frontiers in Bioengineering and Biotechnology** | Frontiers | 13 |
| 5º | **Annals of Biomedical Engineering** | Springer / BMES | 13 |
| 6º | *Other / Thesis / Preprint* | Diversos | 12 |
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
| — | *Demais 115 periódicos (com 1 ou 2 artigos cada)* | Diversas | 120 |
| **Total** | **137 Revistas Únicas** | — | **301** |

---

## 3. Passo 2 — Revistas com Papers sobre Fung, QLV ou Maxwell com Série de Prony

Ao filtrar os artigos por menção e aplicação aos modelos estudados (**Fung QLV**, **espectro contínuo com integral exponencial**, **redes reológicas de Maxwell** e **Série de Prony com tempos de relaxação**), foram mapeados **27 artigos** distribuídos em **16 periódicos científicos** (excluindo teses/preprints):

| Posição | Periódico | Artigos no Passo 2 | DOIs de Referência nos Outputs |
| :---: | :--- | :---: | :--- |
| **1º** | **Scientific Reports** | **6** | `10.1038/s41598-024-67603-8`<br>`10.1038/s41598-025-26576-y`<br>`10.1038/s41598-025-08471-8`<br>`10.1038/s41598-025-88767-x`<br>`10.1038/s41598-025-23918-8`<br>`10.1038/s41598-018-20739-w` |
| **2º** | **Journal of the Royal Society Interface** | **2** | `10.1098/rsif.2015.0486`<br>`10.1098/rsif.2026.0078` |
| **3º** | **Journal of the Mechanical Behavior of Biomedical Materials (JMBBM)** | **2** | `10.1016/j.jmbbm.2026.107452`<br>`10.1016/j.jmbbm.2023.105968` |
| **4º** | **Biomimetics** | **2** | `10.3390/biomimetics11010004`<br>`10.3390/biomimetics9120762` |
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

---

## 4. Passo 3 — Revistas com Papers que Usaram Softwares Abertos ou Desenvolvidos pelo Grupo

Ao filtrar os artigos que reportaram o uso de **softwares abertos** (FEBio, OpenKnee, repositórios públicos) ou **ferramentas/códigos desenvolvidos pelo próprio grupo de autores** (*in-house* code, sub-rotinas UMAT/VUMAT/UDM, scripts e algoritmos disponibilizados em repositórios abertos), foram mapeados **57 artigos** em **27 periódicos científicos**:

| Posição | Periódico | Artigos no Passo 3 | Foco Computacional / Ferramentas Identificadas |
| :---: | :--- | :---: | :--- |
| **1º** | **Annals of Biomedical Engineering** | **7** | Frameworks abertos de ligamentos (Open Knee), FEBio FINESSE e solvers abertos. |
| **2º** | **Journal of Biomechanical Engineering** | **5** | Redes reológicas *in-house* (`10.1115/1.4003620`), solvers abertos acoplados e integração do FEBio. |
| **3º** | **Biomechanics and Modeling in Mechanobiology** | **4** | Modelos reativos em FEBio, modelos baseados em voxels e códigos abertos. |
| **4º** | **Acta Biomaterialia** | **3** | Algoritmos e softwares desenvolvidos para orientação de fibras em elementos finitos. |
| **5º** | **PLOS ONE** | **3** | Pipelines abertos e scripts customizados de malha e processamento de dados. |
| **6º** | **Computer Methods and Programs in Biomedicine** | **3** | Programas computacionais e softwares biomédicos desenvolvidos *in-house*. |
| **7º** | **Medical Engineering & Physics** | **2** | Sub-rotina FORTRAN *in-house* para modelo viscoelástico no Abaqus (`10.1016/j.medengphy.2014.04.004`). |
| **8º** | **Engineering with Computers** | **2** | Frameworks de software *open-source* integrando FEBio e bibliotecas de elementos finitos. |
| **9º** | **The Knee** | **2** | Pipelines computacionais desenvolvidos para planejamento de osteotomia e análise de contato tíbio-femoral. |
| **10º** | **Journal of the Royal Society Interface** | **2** | Modelos constitutivos *in-house* com sub-rotinas de evolução e rede reológica *dual-branch*. |
| **11º** | **Scientific Reports** | **2** | Algoritmo genético *in-house* com código fornecido em material suplementar (`10.1038/s41598-024-67603-8`). |
| **12º** | **Frontiers in Bioengineering and Biotechnology** | **2** | Códigos abertos de validação de modelos de joelho e revisões de implementações. |
| **13º** | **Journal of Biomechanics** | **1** | Rotinas abertas do FEBio Studio para análise dinâmica musculoesquelética (`10.1016/j.jbiomech.2026.113308`). |
| **14º** | **Journal of the Mechanical Behavior of Biomedical Materials** | **1** | Modelo constitutivo visco-hiperelástico com dano descontínuo implementado em rotina customizada (`10.1016/j.jmbbm.2023.105968`). |
| **15º** | **Computer Methods in Biomechanics and Biomedical Engineering** | **1** | Algoritmos próprios de calibração paramétrica de ligamentos colaterais (`10.1080/10255842.2023.2253950`). |
| **16º** | **PLOS Computational Biology** | **1** | Framework de software aberto para simulação biofísica e biomecânica. |
| **17º** | **Proc. IMechE Part C: J. Mechanical Engineering Science** | **1** | Rotina computacional desenvolvida pelos autores para modelo de variáveis internas (`10.1177/0954406219875771`). |
| — | *Demais 10 revistas com 1 paper cada* | 10 | Diversos códigos próprios e sub-rotinas. |

---

## 5. Passo 4 — Interseção Exata (Revistas em Comum nos Passos 2 e 3)

O cruzamento exato entre as revistas que publicam sobre **Fung, QLV e Maxwell com Série de Prony** (Passo 2) e as revistas que publicam trabalhos usando **softwares abertos ou desenvolvidos pelo grupo** (Passo 3) revela exatamente **8 periódicos científicos**:

| Posição | Periódico | Total Outputs (Passo 1) | Artigos Passo 2 (Fung/QLV/Prony) | Artigos Passo 3 (Software Aberto/In-House) | Artigos que Atendem a Ambos Simultaneamente |
| :---: | :--- | :---: | :---: | :---: | :---: |
| **1º** | **Scientific Reports** *(Nature Portfolio)* | **14** | **6** | **2** | **1** (`10.1038/s41598-024-67603-8`) |
| **2º** | **Journal of Biomechanical Engineering (JBME)** *(ASME)* | **15** | **1** | **5** | **1** (`10.1115/1.4003620`) |
| **3º** | **Journal of the Mechanical Behavior of Biomedical Materials (JMBBM)** *(Elsevier)* | **7** | **2** | **1** | **1** (`10.1016/j.jmbbm.2023.105968`) |
| **4º** | **Journal of the Royal Society Interface** *(Royal Society)* | **4** | **2** | **2** | **1** (`10.1098/rsif.2026.0078`) |
| **5º** | **Medical Engineering & Physics** *(Elsevier)* | **3** | **1** | **2** | **1** (`10.1016/j.medengphy.2014.04.004`) |
| **6º** | **Proc. IMechE Part C: J. Mechanical Engineering Science** *(SAGE)* | **1** | **1** | **1** | **1** (`10.1177/0954406219875771`) |
| **7º** | **Journal of Biomechanics** *(Elsevier)* | **7** | **1** | **1** | **0** |
| **8º** | **Frontiers in Bioengineering and Biotechnology** *(Frontiers)* | **13** | **1** | **2** | **0** |

---

## 6. Parecer Refinado do Co-Orientador (`biomechanics-co-advisor`) para a Escolha da Revista

Considerando que o manuscrito já foi redigido e possui dois pilares fortes — (1) a comparação constitutiva de **Fung QLV** vs. **Maxwell Generalizado com Série de Prony** e (2) a execução através de um **sistema próprio de software em C#/.NET 10** para não depender de softwares comerciais fechados:

### 6.1. Análise dos 3 Melhores Alvos da Interseção

1. **Top 1 Recomendado (Para Máxima Valorização do Software Aberto e Metodologia): *Scientific Reports (Nature Portfolio)***
   - **Fator de Impacto / Qualis:** JCR Q1, IF ~3.8.
   - **Por que é o alvo ideal:** Foi o periódico que mais congregou trabalhos de calibração viscoelástica nos outputs (6 artigos) e possui política explícita de valorização da reprodutibilidade científica. O artigo de referência nos outputs (`10.1038/s41598-024-67603-8`) foi publicado exatamente nos mesmos moldes: calibração de modelos visco-hiperelásticos com código próprio disponibilizado, evitando ferramentas proprietárias.
2. **Top 2 Recomendado (Para Tradicionalismo em Mecânica Constitutiva de Tecidos Moles): *Journal of Biomechanical Engineering (JBME — ASME)***
   - **Fator de Impacto / Qualis:** JCR Q2, IF ~1.7.
   - **Por que é o alvo ideal:** É a revista de maior tradição histórica para o método QLV de Fung com correção de tempo de rampa (Abramowitch & Woo, 2004) e para a identificação de redes reológicas de Maxwell desenvolvidas pelos autores (`10.1115/1.4003620`). Os editores apreciam implementações analítico-numéricas 1D detalhadas.
3. **Top 3 Recomendado (Para Foco nos Parâmetros Mecânicos do Ligamento): *Journal of the Mechanical Behavior of Biomedical Materials (JMBBM — Elsevier)***
   - **Fator de Impacto / Qualis:** JCR Q1, IF ~3.4.
   - **Por que é o alvo ideal:** Excelente se o manuscrito der ênfase primordial aos valores ajustados dos ligamentos do joelho, às curvas de relaxação de tensão e à física do tecido, mantendo o software em .NET 10 como um subitem de destaque na seção de Métodos (*"Custom Reactive Pipeline Implementation in .NET 10"*).

---

## 7. Estratégia de Dupla Publicação (Recomendação Estratégica)

Como o sistema de software foi inteiramente arquitetado e implementado em .NET 10 (com padrões modernos de Dependency Injection, pipelines TPL Dataflow e algoritmos numéricos de alto desempenho), você dispõe de material para **duas publicações complementares**:

* **Artigo Científico Principal:** Submeter o artigo comparativo dos ligamentos do joelho (Fung vs. Maxwell Generalizado com Série de Prony) a um dos periódicos da interseção (**Scientific Reports**, **JBME** ou **JMBBM**).
* **Software Article Secundário (Curto, 3 a 5 páginas):** Submeter uma descrição formal do software `MelloSilveiraTools` ao **SoftwareX (Elsevier)** ou **Journal of Open Source Software (JOSS)**, documentando a arquitetura do software, facilidade de uso e independência de licenças comerciais, citando o Artigo Principal como estudo de caso prático de aplicação.
