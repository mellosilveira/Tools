# Plano Arquitetural e de Implementação: Camada Frontend Interativa de Exploração Gráfica Dinâmica

## 1. Visão Geral e Objetivos

Este documento especifica a arquitetura, o design de componentes e o plano de implementação da **Camada Frontend Interativa** para a pesquisa de biomecânica de tecidos moles (foco em ligamentos de joelho sob tração uniaxial).

O objetivo principal é fornecer uma aplicação web científica minimalista, de alta responsividade e ergonomia, capaz de:
1. Permitir que o pesquisador realize upload de ensaios mecânicos brutos em CSV e visualize a segmentação automática das fases de deformação (rampa, relaxação, descida e recuperação).
2. Disparar e acompanhar em tempo real o ajuste de curvas de múltiplos modelos constitutivos (Fung QLV, Schapery, Maxwell, HGO, etc.) via *streaming* NDJSON contínuo.
3. Sobrepor interativamente as três fontes de dados da pesquisa (**Tríade de Dados**): dados experimentais reais, simulação analítico-numérica 1D em C# (`MelloSilveiraTools`) e simulação tensorial 3D em FEBio.
4. Explorar visualmente os resultados do motor de análise de sensibilidade paramétrica (*One-At-A-Time* - OAAT) e das superfícies *Iso-Stress* (relações entre variáveis via Newton-Raphson).

---

## 2. Justificativa Estratégica e Metodológica (The "Why")

* **Por que Blazor WebAssembly (.NET 10):**
  * *Sinergia Total com o Backend:* Compartilha diretamente os DTOs, registros de dados (`*Output`), enums de segmentação (`SegmentType`) e regras de validação estruturadas na solução `MelloSilveiraTools`, eliminando a necessidade de reescrever interfaces em TypeScript e impedindo descompassos de contrato.
  * *Performance na Borda:* O runtime WebAssembly executa código compilado no navegador do cliente, processando arrays numéricos densos sem sobrecarregar a memória do servidor.
* **Por que Renderização via WebGL / Canvas (Plotly.js ou Apache ECharts via JSInterop):**
  * *Diretriz Anti-Crash Visual:* Um ensaio típico de relaxação de tensão amostrado em alta frequência contém entre $20.000$ e $50.000$ pontos de tempo, deformação e carga. Bibliotecas gráficas baseadas unicamente em SVG/DOM (como Recharts, Chart.js padrão ou D3 puro sem Canvas) instanciam dezenas de milhares de nós no DOM, provocando congelamento da aba (*browser freeze*) e travamentos visuais. WebGL e Canvas operam com aceleração de hardware e renderizam essas séries temporais a 60 FPS com suporte a pan, zoom e inspeção pontual.
* **Por que Regra Inviolável de "Zero Regras de Negócio na Borda":**
  * Nenhuma rotina de otimização, cálculo de resíduos, regressão não linear ou resolução de equações constitutivas reside no frontend. O frontend atua unicamente como renderizador visual de dados processados pelo backend e como validador de integridade de borda (por exemplo, impedindo o envio de formulários com módulos de rigidez negativos ou intervalos temporais inconsistentes).
* **Por que Isolamento Completo em Contêiner Docker:**
  * Em conformidade com a política do ecossistema que proíbe execuções não isoladas no host, a aplicação frontend será empacotada em um contêiner Nginx com *multi-stage build*, servida de forma leve e totalmente desacoplada.

---

## 3. Diretrizes de Engenharia e Padrões Locais

* **Tipagem Estrita C#:** Sem uso da palavra-chave `var`. Todas as variáveis locais, campos e propriedades devem declarar explicitamente seus tipos.
* **Construtores Primários:** Utilização prioritária de construtores primários em componentes, serviços e modelos.
* **Sincronismo de Contratos:** Consumo direto das bibliotecas de abstração e DTOs de `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`.
* **Comunicação por Streaming:** Consumo contínuo de NDJSON (`application/x-ndjson`) sem timeouts de socket via `IAsyncEnumerable<T>` e `System.IO.Pipelines`.

---

## 4. Arquitetura de Componentes

### 4.1 Estrutura de Pastas Sugerida
```
src/MelloSilveiraTools.WebUi/
├── App.razor
├── Program.cs
├── Dockerfile
├── nginx.conf
├── wwwroot/
│   ├── index.html
│   ├── css/app.css
│   └── js/biomechanics-charts.js       (Módulo JSInterop para Plotly.js / ECharts Canvas-WebGL)
├── Components/
│   ├── Charts/
│   │   ├── BiomechanicsPlot.razor      (Componente reutilizável de alta performance Canvas/WebGL)
│   │   └── ResidualPlot.razor          (Gráfico diferencial de resíduos)
│   ├── Streaming/
│   │   └── PipelineProgressMonitor.razor (Monitor reativo de transições de estado do pipeline)
│   └── Forms/
│       ├── FileUploadZone.razor        (Zona de drop com validação de extensão e tamanho)
│       └── ParameterRangeInput.razor   (Input numérico científico com validação de limites físicos)
├── Pages/
│   ├── ExperimentalDataView.razor      (Upload, segmentação e disparo do pipeline de dados reais)
│   ├── CurveFittingView.razor          (Ajuste multimodelo e sobreposição de curvas)
│   ├── SensitivityView.razor           (Visualização dos 4 cenários OAAT particionados)
│   └── IsoStressView.razor             (Superfícies e curvas de nível de parâmetros equivalentes)
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

#### `BiomechanicsPlot.razor`
Componente agnóstico responsável por chamar a biblioteca WebGL no cliente através de JSInterop.
* **Parâmetros:**
  * `IReadOnlyList<PlotSeriesData> Series`: Conjunto de séries (Experimental, Modelo 1D, Simulação 3D).
  * `string XAxisLabel`: Ex: "Tempo (s)" ou "Deformação Green-Lagrange".
  * `string YAxisLabel`: Ex: "Tensão Cauchy (MPa)".
  * `bool ShowResiduals`: Exibe painel inferior síncrono com o resíduo pontual ($\sigma_{\text{exp}} - \sigma_{\text{sim}}$).
  * `bool EnableDecimation`: Ativa decimação gráfica inteligente quando o zoom estiver aberto em escala global.

#### `PipelineProgressMonitor.razor`
Componente reativo que consome a stream de `PipelineProgressEvent` da API.
* **Comportamento:**
  * Ouve o canal NDJSON via `HttpClient.GetStreamAsync` / `PostAsync`.
  * Atualiza a barra de progresso visual baseando-se nos estados: `Pending`, `Started`, `Running`, `Completed`, `Faulted`.
  * Exibe detalhes da etapa corrente (`StepName`, `ProcessedItems`, `ProgressPercentage`) e cancelamento sob demanda via `CancellationTokenSource`.

---

## 5. Fases de Implementação

### Fase 1: Fundação do Projeto Web e Contêiner Docker
1. Criar o projeto Blazor WebAssembly (`net10.0`) em `src/MelloSilveiraTools.WebUi`.
2. Adicionar referência de projeto para `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` para compartilhamento dos contratos e DTOs.
3. Criar `Dockerfile` multi-stage:
   * Stage 1: Build e publicação via SDK .NET 10.
   * Stage 2: Imagem enxuta baseada em `nginx:alpine` copiando a pasta `wwwroot` publicada.
4. Configurar `nginx.conf` incluindo headers de segurança e proxy reverso para os endpoints da Web API.
5. Adicionar o serviço ao `docker-compose.yml` da raiz do repositório.

### Fase 2: Cliente HTTP Reativo e Streaming NDJSON
1. Implementar `NdjsonStreamReaderService.cs` utilizando `System.Text.Json` para desserializar linhas UTF-8 de forma assíncrona conforme os bytes chegam ao buffer da resposta.
2. Implementar os clientes tipados de API (`ExperimentalDataApiClient`, `CurveFittingApiClient`, `SensitivityApiClient`) consumindo os endpoints do backend.
3. Desenvolver o componente `PipelineProgressMonitor.razor`, garantindo que requisições longas (como ajustes não lineares e sweeps paramétricos) mostrem o progresso sem risco de timeout.

### Fase 3: Motor Gráfico Anti-Crash (Canvas / WebGL)
1. Integrar a biblioteca gráfica (Plotly.js ou Apache ECharts) via CDN ou pacote local em `wwwroot/js/biomechanics-charts.js`.
2. Escrever a ponte JSInterop para:
   * Inicializar o canvas WebGL.
   * Adicionar/remover traços gráficos dinamicamente.
   * Executar zoom e pan com renderização contínua a 60 FPS com 50.000 pontos.
   * Exportar a imagem gerada em formato vetorial SVG ou imagem de alta resolução PNG.
3. Empacotar a funcionalidade no componente Razor `BiomechanicsPlot.razor`.

### Fase 4: Telas de Exploração Científica e Validações de Borda
1. **Página de Ingestão (`ExperimentalDataView.razor`):**
   * Upload de arquivo CSV bruto de ensaio.
   * Visualização da segmentação com marcadores de cores nas fases (Rampa em azul, Relaxação em verde, Descida em laranja, Recuperação em cinza).
2. **Página de Ajuste e Comparação (`CurveFittingView.razor`):**
   * Seleção do modelo constitutivo desejado (Fung, Schapery, Maxwell, HGO).
   * Formulário de parâmetros iniciais e limites com validação de borda em `EditForm` (bloqueio de valores $\le 0$).
   * Disparo do comando `FitCurveCommand` ou `ProcessExperimentalDataCommand`.
   * Exibição das curvas sobrepostas (Experimental vs Ajustada) e tabela com métricas ($R^2, RMSE$, parâmetros finais).
3. **Página de Sensibilidade e IsoStress (`SensitivityView.razor` e `IsoStressView.razor`):**
   * Interface para configuração de variação paramétrica e visualização direta dos 4 cenários OAAT.
   * Mapa de isolinhas para exploração de relações entre parâmetros do modelo.

### Fase 5: Validação, Testes de Carga Visual e Empacotamento
1. Realizar testes de estresse com arquivos reais de ensaios contendo mais de 50.000 amostras temporais.
2. Validar o isolamento e a execução dentro do contêiner Docker oficial.
3. Verificar a inexistência de memory leaks em re-renderizações sucessivas do canvas WebGL.

---

## 6. Critérios de Aceite e Validação

* [ ] A aplicação sobe com 1 comando via `docker-compose up` e se comunica perfeitamente com a Web API.
* [ ] Nenhuma regra de negócio ou cálculo de otimização física reside no frontend.
* [ ] Arquivos de ensaios com 50.000 pontos são plotados em menos de 500 ms via Canvas/WebGL, sem congelamento da interface.
* [ ] O monitor de streaming NDJSON atualiza a barra de progresso continuamente durante o processamento do pipeline.
* [ ] Entradas antinegociais nos formulários são interceptadas na borda antes de qualquer chamada à API.
