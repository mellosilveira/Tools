# Trabalhos Futuros Mapeados (Future Works)

Este documento centraliza as oportunidades de melhoria, expansões e próximos passos da pesquisa em Biomecânica de Tecidos Moles, garantindo que nenhum item se perca ao longo do desenvolvimento do projeto.

## Lista de Trabalhos Futuros

- **Conversão de constantes escalares 1D para tensores 3D**
  - **Contexto:** Otimização FEA Inversa.
  - **Descrição:** Adaptar os modelos mecânicos desenvolvidos (atualmente trabalhando com variáveis escalares 1D) para o espaço tensorial 3D, permitindo simulações de elementos finitos mais realistas.

- **Programação de plugins UDM em C++ para o FEBio**
  - **Contexto:** Integração com software FEA (Finite Element Analysis).
  - **Descrição:** Desenvolver *User-Defined Materials* (UDM) no FEBio utilizando C++.
  - **Nota:** Esta tarefa deve ser rigorosamente controlada com *time-box* para evitar que tome tempo excessivo do escopo principal da pesquisa.

- **Incluir taxa de deformação (strain rate) na análise de sensibilidade de variáveis**
  - **Contexto:** Análise de Sensibilidade (Parametric Sweep / Grid Search).
  - **Descrição:** Avaliar como a taxa de deformação de entrada afeta os parâmetros ajustados e a convergência da simulação para os modelos constitutivos.

- **Explicabilidade e rastreabilidade teórico-experimental integral na síntese da resposta final por IA**
  - **Contexto:** Camada de Apresentação / Agentes de Inteligência Artificial / Relatórios e Conclusões da Análise.
  - **Descrição:** Estruturar a geração de respostas, laudos e análises finais (inclusive quando sintetizados por agentes de IA) para que não apresentem apenas conclusões diretas ou números isolados, mas forneçam obrigatoriamente:
    1. O encadeamento analítico passo a passo (*step-by-step derivation/reasoning*) de todo o processo de cálculo e simulação;
    2. O embasamento teórico estrito da Mecânica dos Sólidos, Teoria de Grandes Deformações e Viscoelasticidade Linear/Não-Linear (hipóteses cinemáticas, tensores ou escalares adotados e formulações constitutivas empregadas);
    3. O embasamento experimental contextualizado (identificação do ensaio, geometria/L0 da amostra, fases de deformação — rampa, relaxação, recuperação —, parâmetros ajustados e métricas de aderência $R^2$/RMSE).
  - **Justificativa/Objetivo:** Eliminar saídas do tipo "caixa-preta" (*black-box*), garantindo auditabilidade científica, transparência metodológica e reprodutibilidade integral requeridas para a dissertação de mestrado (PPEMM/UFCIRJ) e para publicações em periódicos científicos de alto impacto.

- **Expansão de Protocolos de Comunicação Alternativos ao HTTP/HTTPS REST (`v2.1+`)**
  - **Contexto:** Camada de Web API, Telemetria em Tempo Real, Workers Distribuídos e Integração HPC/FEBio (`MelloSilveiraTools.WebApi` / `Core.Pipelines`).
  - **Descrição:** Manter **HTTP/HTTPS + JSON + NDJSON Streaming** para o Quickstart v2.0.0 (JOSS) e expandir progressivamente o ecossistema de transporte nas versões futuras:
    1. **WebSockets via SignalR (`Microsoft.AspNetCore.SignalR`):** Comunicação bidirecional nativa do ASP.NET Core (zero NuGets de terceiros) para multiplexação de múltiplos pipelines na mesma conexão, reconexão automática e envio de comandos de controle (*pause/cancel*) pelo navegador com autenticação JWE.
    2. **Server-Sent Events (SSE — `TypedResults.ServerSentEvents`):** Streaming unidirecional nativo do `.NET 10` (`text/event-stream`) com eventos nomeados (`topology`, `progress`, `error`) como alternativa leve ao NDJSON.
    3. **gRPC / gRPC-Web (`Grpc.AspNetCore`):** Transporte binário de alta performance sobre HTTP/2 + Protobuf (compatível com Native AOT) para tráfego denso de séries temporais (20k–50k+ pontos de tensão-deformação) e acoplamento com clientes Python/C++ e conector FEBio.
    4. **Message Queues / Brokers (`ProgressNotificationMode.MessageQueue`):** Implementação concreta do modo já previsto no enum `ProgressNotificationMode` (ex.: RabbitMQ, Redis Streams ou Azure Service Bus) para desacoplar otimizações em lote (Differential Evolution + L-BFGS) para *background workers* escaláveis horizontalmente.
    5. **IPC Local (`Named Pipes` / `Unix Domain Sockets`):** Comunicação interprocesso de latência mínima na mesma máquina para solvers locais ou aplicações desktop sem sobrecarga de pilha TCP/HTTP.
  - **Justificativa/Objetivo:** Garantir simplicidade e zero dependências extras no Quickstart JOSS atual, preservando uma trilha clara de evolução para alta densidade numérica, mensageria distribuída e acoplamento científico multi-linguagem.
