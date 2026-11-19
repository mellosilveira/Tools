# Plano Arquitetural e de Implementação — Módulo 08: Camada Frontend Interativa de Exploração Gráfica Dinâmica

## 1. Visão Geral e Objetivos

Especifica a arquitetura, o design de componentes e o plano de implementação da **Camada Frontend Interativa** (Blazor WebAssembly) para a pesquisa em biomecânica de tecidos moles (ligamentos de joelho sob tração uniaxial):

1. Permitir upload de ensaios mecânicos brutos em CSV e visualização da segmentação automática das fases de deformação (rampa, relaxação, descida e recuperação).
2. Disparar e acompanhar em tempo real o ajuste de curvas de múltiplos modelos constitutivos (Fung QLV, Schapery, Maxwell, HGO, etc.) via *streaming* NDJSON contínuo.
3. Sobrepor interativamente a **Tríade de Dados**: dados experimentais reais, simulação analítico-numérica 1D em C# (`MelloSilveiraTools`) e simulação tensorial 3D em FEBio.
4. Explorar visualmente a análise de sensibilidade paramétrica (*One-At-A-Time* — OAAT) e as superfícies *Iso-Stress* (relações entre variáveis via Newton-Raphson).

---

## 2. Justificativa Estratégica e Metodológica (The "Why")

* **Blazor WebAssembly (.NET 10):** Compartilha diretamente DTOs, registros (`*Output`), enum `SegmentType` e validações de `MelloSilveiraTools`, eliminando reescrita de interfaces em TypeScript e descompassos de contrato; executa código compilado no navegador processando arrays densos sem sobrecarregar a memória do servidor.
* **Renderização via WebGL / Canvas (Plotly.js ou Apache ECharts via JSInterop):** Diretriz **Anti-Crash Visual**. Ensaios de relaxação em alta frequência possuem entre $20.000$ e $50.000$ pontos (tempo, deformação, carga). Bibliotecas baseadas unicamente em SVG/DOM (Recharts, Chart.js padrão, D3 puro sem Canvas) criam dezenas de milhares de nós DOM e causam *browser freeze*; WebGL/Canvas usa aceleração de hardware a 60 FPS com pan, zoom e inspeção pontual.
* **Regra Inviolável de "Zero Regras de Negócio na Borda":** Nenhuma otimização, cálculo de resíduos, regressão não linear ou equação constitutiva reside no frontend. A UI atua exclusivamente como renderizador e validador de integridade de borda (ex.: bloquear módulos de rigidez negativos ou intervalos temporais inconsistentes).
* **Isolamento Completo em Contêiner Docker:** Em conformidade com a política contra execuções soltas no host, a aplicação é empacotada em imagem `nginx:alpine` via *multi-stage build*, leve e totalmente desacoplada.

---

## 3. Diretrizes de Engenharia e Padrões Locais

* **Tipagem Estrita C#:** Proibido o uso de `var`; tipos explícitos em todas as variáveis locais, campos e propriedades (ver [`AGENTS.md`](../../../AGENTS.md)).
* **Construtores Primários:** Uso prioritário em componentes, serviços e modelos.
* **Sincronismo de Contratos:** Consumo direto das abstrações e DTOs de `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
* **Comunicação por Streaming:** Consumo contínuo de NDJSON (`application/x-ndjson`) sem timeouts de socket via `IAsyncEnumerable<T>` e `System.IO.Pipelines`.

---

## 4. Arquitetura de Componentes

### 4.1 Estrutura de Pastas (`src/MelloSilveiraTools.WebUi/`)
```text
src/MelloSilveiraTools.WebUi/
├── App.razor
├── Program.cs
├── Dockerfile
├── nginx.conf
├── wwwroot/
│   ├── index.html
│   ├── css/app.css
│   └── js/biomechanics-charts.js         # Ponte JSInterop para Plotly.js / ECharts (Canvas/WebGL)
├── Components/
│   ├── Charts/
│   │   ├── BiomechanicsPlot.razor        # Gráfico reutilizável Canvas/WebGL de alta performance
│   │   └── ResidualPlot.razor            # Gráfico diferencial de resíduos
│   ├── Streaming/
│   │   └── PipelineProgressMonitor.razor # Monitor reativo de transições de estado do pipeline
│   └── Forms/
│       ├── FileUploadZone.razor          # Zona de drop com validação de extensão e tamanho
│       └── ParameterRangeInput.razor     # Input científico com validação de limites físicos
├── Pages/
│   ├── ExperimentalDataView.razor        # Upload, segmentação e disparo do pipeline de dados reais
│   ├── CurveFittingView.razor            # Ajuste multimodelo e sobreposição de curvas (Tríade)
│   ├── SensitivityView.razor             # Visualização dos 4 cenários OAAT particionados
│   └── IsoStressView.razor               # Superfícies e curvas de nível de parâmetros equivalentes
└── Services/
    ├── Http/
    │   ├── ExperimentalDataApiClient.cs
    │   ├── CurveFittingApiClient.cs
    │   ├── SensitivityApiClient.cs
    │   └── StoredFilesApiClient.cs
    └── Streaming/
        └── NdjsonStreamReaderService.cs
```

### 4.2 Componentes Chave
* **`BiomechanicsPlot.razor`:** Componente agnóstico conectado à biblioteca WebGL via JSInterop.
  * `IReadOnlyList<PlotSeriesData> Series`: Séries Experimental, Modelo 1D e Simulação 3D.
  * `string XAxisLabel`: Ex.: `"Tempo (s)"` ou `"Deformação Green-Lagrange"`.
  * `string YAxisLabel`: Ex.: `"Tensão Cauchy (MPa)"`.
  * `bool ShowResiduals`: Exibe painel inferior síncrono com o resíduo pontual ($\sigma_{\text{exp}} - \sigma_{\text{sim}}$).
  * `bool EnableDecimation`: Ativa decimação gráfica inteligente quando o zoom está aberto em escala global.
* **`PipelineProgressMonitor.razor`:** Consome a stream NDJSON de `PipelineProgressEvent` exposta em `GET api/v1/pipelines/{pipelineId}/progress` (`HttpClient.GetStreamAsync` / `PostAsync`).
  * Atualiza progresso visual conforme `PipelineStepState`: `Pending`, `Started`, `Running`, `Completed`, `Faulted`.
  * Exibe detalhes da etapa corrente (`StepName`, `Description`, `ReceivedItems`, `ProcessedItems` e percentual derivado, além de `UpstreamSteps`) e oferece cancelamento sob demanda via `CancellationTokenSource`.

---

## 5. Fases de Implementação

### Fase 1: Fundação do Projeto Web e Contêiner Docker
1. Criar projeto Blazor WebAssembly (`net10.0`) em `src/MelloSilveiraTools.WebUi`.
2. Referenciar `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` para compartilhamento de contratos e DTOs.
3. Criar `Dockerfile` *multi-stage*: Stage 1 (build/publish via SDK `.NET 10`) → Stage 2 (`nginx:alpine` servindo `wwwroot` publicado).
4. Configurar `nginx.conf` com **headers de segurança** e proxy reverso para os endpoints da Web API (além de CORS na Web API host, quando necessário).
5. Adicionar o serviço ao `docker-compose.yml` na raiz do repositório.

### Fase 2: Cliente HTTP Reativo e Streaming NDJSON
1. Implementar `NdjsonStreamReaderService.cs` usando `System.Text.Json` para desserializar linhas UTF-8 assincronamente conforme os bytes chegam ao buffer da resposta.
2. Implementar clientes tipados (`ExperimentalDataApiClient`, `CurveFittingApiClient`, `SensitivityApiClient`, `StoredFilesApiClient`) consumindo os endpoints do backend.
3. Desenvolver `PipelineProgressMonitor.razor` garantindo que requisições longas (ajustes não lineares e sweeps paramétricos) reportem progresso contínuo sem risco de timeout.

### Fase 3: Motor Gráfico Anti-Crash (Canvas / WebGL)
1. Integrar Plotly.js ou Apache ECharts (via CDN ou pacote local) em `wwwroot/js/biomechanics-charts.js`.
2. Implementar ponte JSInterop para:
   * Inicializar o canvas WebGL;
   * Adicionar/remover traços gráficos dinamicamente;
   * Executar zoom e pan contínuos a 60 FPS com 50.000 pontos;
   * Exportar a imagem em formato vetorial SVG ou PNG de alta resolução.
3. Empacotar no componente `BiomechanicsPlot.razor`.

### Fase 4: Telas de Exploração Científica e Validações de Borda
1. **`ExperimentalDataView.razor`:** Upload de CSV bruto de ensaio + visualização da segmentação (`SegmentType`) com marcadores de cor por fase: **Rampa em azul, Relaxação em verde, Descida em laranja, Recuperação em cinza**.
2. **`CurveFittingView.razor`:**
   * Seleção do modelo constitutivo (Fung, Schapery, Maxwell, HGO);
   * Formulário de parâmetros iniciais e limites com validação de borda em `EditForm` (bloqueio de valores $\le 0$);
   * Disparo dos comandos de ajuste/processamento (`FitCurveCommand` / `ProcessExperimentalDataCommand` — *comandos de aplicação a implementar/alinhar aos pipelines de `Optimizations`, como `ExecuteParametricSweepFromExperimentalDataCommand`*);
   * Sobreposição das curvas (Experimental × 1D Ajustada × 3D FEBio) e tabela com métricas ($R^2$, RMSE e parâmetros finais).
3. **`SensitivityView.razor` e `IsoStressView.razor`:** Interface para configuração de variação paramétrica, visualização dos 4 cenários OAAT e mapa de isolinhas de parâmetros equivalentes.

### Fase 5: Validação, Testes de Carga Visual e Empacotamento
1. Executar testes de estresse com arquivos reais de ensaios contendo $> 50.000$ amostras temporais.
2. Validar isolamento e execução dentro do contêiner Docker oficial.
3. Verificar ausência de *memory leaks* em re-renderizações sucessivas do canvas WebGL.

---

## 6. Critérios de Aceite e Validação

* [ ] Aplicação sobe com 1 comando via `docker-compose up` e se comunica corretamente com a Web API.
* [ ] Nenhuma regra de negócio ou cálculo de otimização física reside no frontend.
* [ ] Arquivos de ensaios com 50.000 pontos são plotados em $< 500\text{ ms}$ via Canvas/WebGL, sem congelamento da interface.
* [ ] O monitor de streaming NDJSON atualiza o progresso continuamente durante a execução do pipeline.
* [ ] Entradas fisicamente inválidas nos formulários são interceptadas na borda antes de qualquer chamada à API.
