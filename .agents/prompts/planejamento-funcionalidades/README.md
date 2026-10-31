# Planos de Implementação de Funcionalidades do Projeto

Este diretório reúne os planejamentos técnicos e arquiteturais detalhados para as funcionalidades identificadas na avaliação técnica do ecossistema que se encontravam com maturidade inferior a *Atende parcialmente*.

Os planos foram desenhados com base em **fases lógicas e dependências técnicas**, **sem atrelamento a datas de calendário ou prazos fixos**, respeitando rigorosamente as diretrizes científicas e de software da pesquisa em biomecânica de tecidos moles.

---

## 📑 Índice dos Planos de Implementação

| Arquivo de Planejamento | Funcionalidade Corrigida | Foco Técnico Principal |
|---|---|---|
| [`camada-frontend-interativa-exploracao-grafica-dinamica.md`](./camada-frontend-interativa-exploracao-grafica-dinamica.md) | **Módulo 08: Camada Frontend Interativa** | Aplicação web científica em Blazor WebAssembly (.NET 10) conteinerizada em Docker, renderização gráfica via Canvas/WebGL (Plotly.js / ECharts) para prevenção de travamentos com séries temporais densas, consumo de streaming NDJSON em tempo real e formulários com validação física na borda sem regras de negócio locais. |
| [`integracao-febio-simulacao-tensorial-3d.md`](./integracao-febio-simulacao-tensorial-3d.md) | **Módulo 07: Integração com FEBio e Simulação Tensorial 3D** | Fechamento da Tríade de Dados (Experimental vs 1D C# vs 3D FEBio), dedução de Cheat Sheets matemáticos tensoriais, rotina de recalibração analítica de constantes escalares 1D em tensores 3D sob tração uniaxial, desenvolvimento de sub-rotina C++ UDM para o modelo de Schapery com gatilho de contingência de esforço e automação de execução headless do solver FEBio com ingestão no banco relacional. |
| [`automacao-graficos-publicacao-tabelas-comparacao-modelos.md`](./automacao-graficos-publicacao-tabelas-comparacao-modelos.md) | **Módulo 09: Automação de Gráficos de Publicação e Tabelas Comparativas** | Esteira automatizada e conteinerizada em Python (Matplotlib, Seaborn, Pandas) para geração programática de figuras estáticas em padrão de periódicos internacionais (vetoriais PDF e TIFF $\ge 300$ DPI) e compilação direta das Tabelas de Batalha de Modelos em código LaTeX (`booktabs`) e Markdown a partir dos dados do PostgreSQL e CSVs gerados. |

---

## 🛡️ Diretrizes Transversais Aplicadas aos Planos

1. **The "Why" como Fundamento:** Todo plano registra explicitamente a razão de cada escolha de tecnologia ou método, assegurando rastreabilidade epistemológica no *Scientific Ledger*.
2. **Execução Estritamente Conteinerizada:** Nenhuma rotina customizada do ecossistema é executada diretamente no host. Todas operam isoladas em contêineres Docker (Blazor/Nginx para o frontend, Python 3.12 para publicação, PostgreSQL para persistência). Apenas binários nativos de terceiros (solver FEBio) constituem dependência instalada no ambiente.
3. **Zero Regras de Negócio na Borda:** A camada frontend é exclusivamente de apresentação e validação de limites de interface. Toda a inteligência de otimização, cálculo de resíduos e equações constitutivas reside no backend `MelloSilveiraTools`.
4. **Precisão Numérica e Rastreabilidade Total:** Tolerância zero a transcrição manual de resultados em tabelas de publicação. As tabelas comparativas são computadas diretamente a partir das métricas registradas na base relacional e nos arquivos rastreados.
