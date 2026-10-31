# Plano Arquitetural e de Implementação: Automação de Gráficos de Publicação e Tabelas de Comparação de Modelos

## 1. Visão Geral e Objetivos

Este documento especifica a arquitetura e as etapas de implementação para o **Pipeline Automatizado de Pós-Processamento e Publicação Científica** da pesquisa em biomecânica de tecidos moles.

O objetivo central é fornecer um ferramental autônomo, totalmente conteinerizado em Docker, capaz de:
1. Consumir as saídas brutas em CSV e os metadados relacionais gerados pelo `MelloSilveiraTools` e pelo FEBio.
2. Gerar programaticamente **figuras estáticas em qualidade de publicação internacional** (vetoriais PDF/SVG e TIFF com resolução mínima de 300 DPI), atendendo rigorosamente aos critérios tipográficos e dimensionais de periódicos de alto impacto da área (e.g., *Journal of Biomechanics*, *Acta Biomaterialia*).
3. Compilar automaticamente as **Tabelas de Batalha de Modelos** em código LaTeX (`booktabs`) e Markdown, computando métricas estatísticas consolidadas ($RMSE, R^2$, erro na assíntota, número de parâmetros, tempo de processamento e iterações) sem qualquer transcrição manual.
4. Assegurar rastreabilidade e reprodutibilidade matemática absoluta antes da inserção dos resultados no corpo da dissertação.

---

## 2. Justificativa Estratégica e Metodológica (The "Why")

* **Por que Figuras Estáticas Separadas da Camada Frontend:**
  * O frontend dinâmico (Blazor / Canvas-WebGL) é projetado para exploração em tempo real pelo pesquisador. No entanto, gráficos de publicação exigem renderização com precisão tipográfica idêntica à do documento LaTeX (fonte *Computer Modern*, notação matemática via TeX `$...$`, dimensões rígidas de coluna de 85 mm ou 170 mm e controle estrito de espessura de traços). O pós-processamento dedicado em Python/Matplotlib garante aderência estrita a essas normas editoriais.
* **Por que Geração 100% Programática das Tabelas Comparativas:**
  * O confronto entre múltiplos modelos constitutivos (Fung QLV, Schapery, Maxwell, HGO, etc.) avaliados sobre dezenas de corpos de prova dos 4 ligamentos (LCA, LCP, LCM, LCL) envolve centenas de parâmetros e métricas. A compilação manual em planilhas eletrônicas é lenta, propensa a erros de digitação e compromete a integridade dos dados. A geração direta via script a partir dos dados do PostgreSQL elimina o erro humano.
* **Por que Python Conteinerizado (Isolamento em Docker):**
  * O ecossistema proíbe terminantemente a execução de rotinas no host sem isolamento. Python com Pandas, SciPy, Matplotlib e Seaborn constitui o padrão-ouro em computação científica para pós-processamento de dados. Ao rodar essa esteira dentro de um contêiner Docker oficial com volumes mapeados, cumpre-se a diretriz de conteinerização sem abrir mão do ecossistema científico da linguagem.

---

## 3. Arquitetura da Esteira de Publicação

```
┌────────────────────────────────────────────────────────┐
│  Saídas Computacionais (MelloSilveiraTools + FEBio)    │
│  - CSVs Particionados ({Model}_{Sample}_Simulation.csv)│
│  - CSVs de Sensibilidade ({Var}_SensitivityAnalysis.csv│
│  - PostgreSQL (stored_files, parametric_sweeps)       │
└──────────────────────────┬─────────────────────────────┘
                           │ Montagem de Volume Docker
                           ▼
┌────────────────────────────────────────────────────────┐
│  Contêiner Docker: publication-pipeline (Python 3.12)  │
│  ├── thesis_style.mplstyle (Estilo TeX / 300+ DPI)     │
│  ├── generate_figures.py   (Tríade, Resíduos, OAAT)    │
│  ├── generate_tables.py    (LaTeX booktabs / Markdown) │
│  └── audit_outputs.py      (Inspeção de Conformidade)  │
└──────────────────────────┬─────────────────────────────┘
                           │ Artefatos Finais
                           ▼
┌────────────────────────────────────────────────────────┐
│  Saídas Finais para a Dissertação de Mestrado          │
│  - Figuras: figures/fig_relaxation_triad.pdf (Vetor)  │
│  - Figuras: figures/fig_sensitivity_oat.tiff (300 DPI) │
│  - Tabelas: tables/table_model_battle.tex (booktabs)   │
└────────────────────────────────────────────────────────┘
```

---

## 4. Estrutura de Arquivos da Esteira

```
scripts/publication_pipeline/
├── Dockerfile
├── requirements.txt
├── docker-compose.publication.yml
├── styles/
│   └── thesis_style.mplstyle           (Configurações de DPI, fontes TeX, tamanhos de fonte)
├── generators/
│   ├── figure_relaxation_triad.py      (Sobreposição Tríade: Experimental, 1D C#, 3D FEBio)
│   ├── figure_residuals.py             (Painel de resíduos pontuais ao longo do tempo)
│   ├── figure_sensitivity_oat.py       (Painel 2x2 com os 4 cenários de sensibilidade)
│   └── table_model_battle.py           (Compilador da tabela LaTeX booktabs)
├── runner.py                           (Orquestrador CLI que executa toda a esteira em lote)
└── outputs/
    ├── figures/                        (PDFs e TIFFs finais)
    └── tables/                         (Arquivos .tex e .md finais)
```

---

## 5. Fases de Implementação

### Fase 1: Ambiente Conteinerizado de Publicação e Estilização Tipográfica
1. Criar o diretório `scripts/publication_pipeline/` e o arquivo `Dockerfile` baseado em `python:3.12-slim`.
2. Configurar `requirements.txt` com as bibliotecas: `matplotlib`, `seaborn`, `pandas`, `scipy`, `psycopg2-binary`.
3. Desenvolver o arquivo de estilo `thesis_style.mplstyle`:
   * Habilitação de tipografia TeX (`text.usetex: true` ou fontes matemáticas padrão TeX).
   * Fonte: Serifada (`font.serif: Computer Modern Roman`).
   * Tamanhos padronizados: Títulos (10 pt), Eixos (8 pt), Legendas (7 pt).
   * Paletas de cores para daltônicos (*colorblind-friendly*, e.g., Okabe-Ito / Viridis).
   * Resolução padrão de exportação raster: 300 DPI.

### Fase 2: Gerador de Figuras da Tríade e Sensibilidade
1. **Curvas de Relaxação da Tríade (`figure_relaxation_triad.py`):**
   * Leitura do CSV experimental real, da curva 1D em C# e da curva 3D do FEBio.
   * Renderização dos dados experimentais como faixa sombreada contínua (mediana central $\pm$ intervalo interquartil $Q_1-Q_3$).
   * Sobreposição das linhas teóricas dos modelos constitutivos com espessuras e traços diferenciados (sólido, tracejado, traço-ponto).
   * Formatação em largura de coluna única (85 mm) e dupla (170 mm).
2. **Painel de Resíduos Pontuais (`figure_residuals.py`):**
   * Cálculo da diferença instantânea $\Delta\sigma(t) = \sigma_{\text{exp}}(t) - \sigma_{\text{sim}}(t)$.
   * Linha de referência horizontal em zero e limites de tolerância de erro.
3. **Painel 2x2 de Análise de Sensibilidade (`figure_sensitivity_oat.py`):**
   * Leitura dos CSVs particionados `{VariableName}_SensitivityAnalysis.csv`.
   * Geração de um único painel integrado com 4 quadrantes para os cenários obrigatórios:
     * Quadrante A: Tensão inicial $\sigma(t_0)$ vs Parâmetro;
     * Quadrante B: Tensão final $\sigma(t_f)$ vs Parâmetro;
     * Quadrante C: Tempo de assíntota $t_{\text{asymptote}}$ vs Parâmetro;
     * Quadrante D: Variação relativa $\Delta\sigma$ vs Parâmetro.

### Fase 3: Compilador da Tabela de Batalha de Modelos em LaTeX
1. Implementar `table_model_battle.py`:
   * Consulta automatizada às tabelas de resultados e leitura dos CSVs de simulação.
   * Cálculo determinístico das métricas para cada modelo:
     * Número de parâmetros livres ($p$);
     * Raiz do Erro Quadrático Médio global ($RMSE = \sqrt{\frac{1}{N}\sum (\sigma_{\text{exp}} - \sigma_{\text{sim}})^2}$);
     * $RMSE$ na fase de rampa vs $RMSE$ na fase de relaxação;
     * Coeficiente de determinação $R^2$;
     * Erro percentual relativo no patamar assintótico ($\Delta\sigma_{\%} = \frac{|\sigma_{\text{exp}}(t_f) - \sigma_{\text{sim}}(t_f)|}{\sigma_{\text{exp}}(t_f)} \times 100$);
     * Tempo de processamento computacional médio (ms) e iterações até convergência.
2. Exportar o arquivo `table_battle_models.tex` utilizando o pacote LaTeX `booktabs` (`\toprule`, `\midrule`, `\bottomrule`), com alinhamento numérico por vírgula decimal via `siunitx`.
3. Exportar simultaneamente uma versão correspondente em Markdown para inspeção imediata no repositório.

### Fase 4: Esteira de Auditoria Editorial e Rastreabilidade
1. Implementar o script de auditoria `audit_outputs.py`:
   * Verificação automatizada de resolução de imagem (garantindo $\ge 300$ DPI em arquivos TIFF/PNG).
   * Verificação de corte de legendas ou eixos nas caixas delimitadoras (*bounding boxes*).
   * Validação de consistência entre os números da tabela LaTeX e os arquivos brutos do banco de dados (tolerância zero a desvios numéricos).
2. Submissão formal dos artefatos ao agente *High-Standard Paper Reviewer* e à *Secretária de Rastreabilidade* para homologação e inclusão no capítulo de Resultados da dissertação.

---

## 6. Critérios de Aceite e Validação

* [ ] O pipeline roda integralmente via contêiner Docker isolado com o comando `docker compose -f docker-compose.publication.yml run --rm publication-runner`.
* [ ] As figuras geradas possuem versões vetoriais (PDF) e matriciais (TIFF $\ge 300$ DPI), com fontes compatíveis com o documento da dissertação.
* [ ] Todas as curvas experimentais reais incluem representação explícita de dispersão estatística (intervalo interquartil).
* [ ] A Tabela de Batalha de Modelos em LaTeX é compilável diretamente sem necessidade de ajustes manuais de sintaxe TeX.
* [ ] O auditor determinístico atesta que 100% dos valores numéricos contidos na tabela LaTeX correspondem exatamente aos dados processados pelo backend.
