# Plano Arquitetural e de Implementação — Módulo 09: Automação de Gráficos de Publicação e Tabelas de Comparação de Modelos

## 1. Visão Geral e Objetivos

Especifica a arquitetura e as etapas de implementação do **Pipeline Automatizado de Pós-Processamento e Publicação Científica**, totalmente conteinerizado em Docker:

1. Consumir saídas brutas em CSV e metadados relacionais gerados pelo `MelloSilveiraTools` e pelo FEBio.
2. Gerar programaticamente **figuras estáticas em qualidade de publicação internacional** (vetoriais PDF/SVG e TIFF $\ge 300\text{ DPI}$), atendendo critérios tipográficos e dimensionais de periódicos de alto impacto (ex.: *Journal of Biomechanics*, *Acta Biomaterialia*).
3. Compilar automaticamente as **Tabelas de Batalha de Modelos** em LaTeX (`booktabs`) e Markdown, computando métricas estatísticas consolidadas ($RMSE$, $R^2$, erro assintótico, $p$, AIC/BIC, tempo de processamento e iterações) sem transcrição manual.
4. Garantir rastreabilidade e reprodutibilidade matemática absoluta antes da inserção na dissertação.

---

## 2. Justificativa Estratégica e Metodológica (The "Why")

* **Figuras estáticas separadas da camada frontend:** O frontend (Blazor/WebGL) foca exploração dinâmica em tempo real, enquanto gráficos de publicação exigem tipografia idêntica ao LaTeX (fonte *Computer Modern*, notação matemática TeX `$...$`, larguras rígidas de coluna de 85 mm ou 170 mm e controle estrito de espessura de traços), garantidos por Python/Matplotlib dedicado.
* **Geração 100% programática das tabelas comparativas:** O confronto entre múltiplos modelos (Fung QLV, Schapery, Maxwell, HGO, etc.) sobre dezenas de corpos de prova dos **4 ligamentos (LCA, LCP, LCM, LCL)** envolve centenas de parâmetros e métricas; a geração direta a partir do PostgreSQL e CSVs elimina erros humanos de planilhas manuais.
* **Python conteinerizado em Docker:** Cumpre a proibição de rotinas soltas no host, executando o padrão-ouro científico (Pandas, SciPy, Matplotlib, Seaborn) em contêiner oficial com volumes mapeados.

---

## 3. Arquitetura e Estrutura de Arquivos

**Fluxo da Esteira:**
Entradas (`{Model}_{Sample}_Simulation.csv`, `{VariableName}_SensitivityAnalysis.csv`, PostgreSQL `stored_files` e `parametric_sweeps`) → Volume Docker → Contêiner `publication-pipeline` (`python:3.12-slim`) → Artefatos finais (`outputs/figures/fig_relaxation_triad.pdf`, `outputs/figures/fig_sensitivity_oat.tiff`, `outputs/tables/table_model_battle.tex` e `.md`).

```text
scripts/publication_pipeline/
├── Dockerfile
├── requirements.txt
├── docker-compose.publication.yml
├── styles/
│   └── thesis_style.mplstyle            # DPI, fontes TeX, tamanhos e paleta colorblind-friendly
├── generators/
│   ├── figure_relaxation_triad.py       # Sobreposição Tríade: Experimental, 1D C#, 3D FEBio
│   ├── figure_residuals.py              # Painel de resíduos pontuais ao longo do tempo
│   ├── figure_sensitivity_oat.py        # Painel 2x2 com os 4 cenários de sensibilidade OAAT
│   └── table_model_battle.py            # Compilador da tabela LaTeX booktabs + Markdown
├── audit_outputs.py                     # Inspeção determinística de DPI, bounding boxes e valores
├── runner.py                            # Orquestrador CLI em lote
└── outputs/
    ├── figures/                         # PDFs vetoriais e TIFFs (>= 300 DPI)
    └── tables/                          # Arquivos .tex e .md finais
```

---

## 4. Fases de Implementação

### Fase 1: Ambiente Conteinerizado de Publicação e Estilização Tipográfica
1. Criar `scripts/publication_pipeline/` e `Dockerfile` baseado em `python:3.12-slim`.
2. Configurar `requirements.txt`: `matplotlib`, `seaborn`, `pandas`, `scipy`, `psycopg2-binary`.
3. Criar `styles/thesis_style.mplstyle`:
   * Tipografia TeX (`text.usetex: true` ou fontes matemáticas padrão TeX);
   * Fonte serifada (`font.serif: Computer Modern Roman`);
   * Tamanhos padronizados: Títulos (10 pt), Eixos (8 pt), Legendas (7 pt);
   * Paletas para daltônicos (*colorblind-friendly*, ex.: Okabe-Ito / Viridis);
   * Resolução padrão de exportação raster: 300 DPI.

### Fase 2: Gerador de Figuras da Tríade, Resíduos e Sensibilidade
1. **Curvas de Relaxação da Tríade (`figure_relaxation_triad.py`):**
   * Ler CSV experimental real, curva 1D em C# e curva 3D do FEBio.
   * Renderizar dados experimentais como **faixa sombreada contínua** (mediana central $\pm$ intervalo interquartil $Q_1\text{–}Q_3$).
   * Sobrepor linhas teóricas com **espessuras e traços diferenciados (sólido, tracejado, traço-ponto)**.
   * Formatar para largura de **coluna única (85 mm)** e **coluna dupla (170 mm)**.
2. **Painel de Resíduos Pontuais (`figure_residuals.py`):**
   * Calcular diferença instantânea $\Delta\sigma(t) = \sigma_{\text{exp}}(t) - \sigma_{\text{sim}}(t)$.
   * Plotar **linha de referência horizontal em zero** e **limites de tolerância de erro**.
3. **Painel 2×2 de Análise de Sensibilidade (`figure_sensitivity_oat.py`):**
   * Ler CSVs particionados `{VariableName}_SensitivityAnalysis.csv`.
   * Gerar painel integrado com 4 quadrantes obrigatórios:
     * **Quadrante A:** Tensão inicial $\sigma(t_0)$ vs. Parâmetro;
     * **Quadrante B:** Tensão final $\sigma(t_f)$ vs. Parâmetro;
     * **Quadrante C:** Tempo de assíntota $t_{\text{asymptote}}$ vs. Parâmetro;
     * **Quadrante D:** Variação relativa $\Delta\sigma$ vs. Parâmetro.

### Fase 3: Compilador da Tabela de Batalha de Modelos em LaTeX
1. Implementar `table_model_battle.py` consultando tabelas de resultados no PostgreSQL e CSVs de simulação para computar deterministicamente:
   * Número de parâmetros livres ($p$);
   * Raiz do Erro Quadrático Médio global: $RMSE = \sqrt{\frac{1}{N}\sum (\sigma_{\text{exp}} - \sigma_{\text{sim}})^2}$;
   * $RMSE$ na fase de rampa vs. $RMSE$ na fase de relaxação;
   * Coeficiente de determinação ($R^2$) e critérios de informação parcimoniosa (AIC/BIC);
   * Erro percentual relativo no patamar assintótico: $\Delta\sigma_{\%} = \frac{|\sigma_{\text{exp}}(t_f) - \sigma_{\text{sim}}(t_f)|}{\sigma_{\text{exp}}(t_f)} \times 100$;
   * Tempo médio de processamento computacional (ms) e iterações até convergência.
2. Exportar `outputs/tables/table_model_battle.tex` usando `booktabs` (`\toprule`, `\midrule`, `\bottomrule`) com alinhamento numérico por vírgula decimal via `siunitx`.
3. Exportar simultaneamente versão correspondente em Markdown (`outputs/tables/table_model_battle.md`) para inspeção rápida no repositório.

### Fase 4: Esteira de Auditoria Editorial e Rastreabilidade
1. Implementar `audit_outputs.py`:
   * Verificar resolução de imagem ($\ge 300\text{ DPI}$ em arquivos TIFF/PNG);
   * Verificar cortes de legendas ou eixos nas caixas delimitadoras (*bounding boxes*);
   * Validar consistência numérica (tolerância zero a desvios) entre a tabela LaTeX e os dados brutos do banco de dados.
2. Submeter formalmente os artefatos aos agentes `high-standard-paper-reviewer` e `documentation-and-tracking-secretary` (*Secretária de Rastreabilidade*) para homologação e inclusão no capítulo de Resultados da dissertação.

---

## 5. Critérios de Aceite e Validação

* [ ] Execução integral em contêiner Docker isolado via `docker compose -f docker-compose.publication.yml run --rm publication-runner`.
* [ ] Figuras geradas em versões vetoriais (PDF) e matriciais (TIFF $\ge 300\text{ DPI}$), com fontes compatíveis com o documento da dissertação.
* [ ] Todas as curvas experimentais reais incluem representação explícita de dispersão estatística (intervalo interquartil).
* [ ] Tabela de Batalha de Modelos em LaTeX compilável diretamente sem ajustes manuais de sintaxe TeX.
* [ ] `audit_outputs.py` atesta que 100% dos valores numéricos da tabela LaTeX correspondem exatamente aos dados processados pelo backend.
