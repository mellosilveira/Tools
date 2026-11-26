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

- **Adequação Completa da Aplicação para NativeAOT (`PublishAot=true`)**
  - **Contexto:** Camada de Hospedagem Web, Persistência e Sistema de Plugins (`MelloSilveiraTools.WebApi.Host`, `WebApi`, `Database`, `Database.Migrator`, `Plugins`).
  - **Descrição:** Refatorar a aplicação backend para suportar compilação estrita com NativeAOT (`PublishAot=true`):
    1. Migrar controllers ASP.NET Core MVC (`AddControllers()`, `AddApplicationPart`) para **Minimal APIs** sem reflexão;
    2. Substituir `Swashbuckle` (`AddSwaggerWithBearerSecurity`) por `Microsoft.AspNetCore.OpenApi` + UI estática;
    3. Registrar todos os DTOs de request/response em `JsonSerializerContext` (source generation);
    4. ~~Substituir `Dapper` por `Dapper.AOT` ou mapeamento estático com `NpgsqlDataReader`~~ *(Concluído: `Dapper` removido, `MelloSilveiraTools.Database` migrado para Npgsql puro + `NpgsqlSlimDataSourceBuilder` + `DictionaryExtensions` com fallback AOT e 0 avisos `IL2xxx`/`IL3xxx`)*; resta revisar `Expression.Compile()` em `MechanicalModelTypeCache`;
    5. Desacoplar ou isolar `MelloSilveiraTools.Plugins` do host NativeAOT, pois o carregamento dinâmico de assemblies externos em runtime (`AssemblyLoadContext`) não é suportado em binários NativeAOT fechados;
    6. Remover a supressão de avisos `IL2xxx`/`IL3xxx` do `Directory.Build.props`.
  - **Principais Benefícios do NativeAOT:**
    - Redução expressiva do tamanho da imagem de container e do binário final (dispensa runtime .NET completo);
    - Inicialização (*cold-start*) praticamente instantânea e menor consumo base de memória RAM;
    - Eliminação do overhead de compilação JIT em runtime, entregando binário nativo único.
  - **Principais Malefícios e Custos do NativeAOT:**
    - Complexidade arquitetural **alta** (refatoração ampla de MVC, Swagger e serialização);
    - **Incompatibilidade estrutural com carregamento dinâmico de plugins** em runtime (`MelloSilveiraTools.Plugins`);
    - `Expression.Compile()` passa a executar em modo interpretado (potencial regressão de desempenho em avaliações repetidas se não convertido para source generation);
    - Exigência de **toolchain/linker C++ instalado na máquina** para compilação local (Visual Studio *Desktop Development with C++* no Windows, `clang`/headers no Linux, Xcode CLT no macOS), aumentando a fricção para revisores da JOSS.
  - **Justificativa da Tomada de Decisão:** Para a entrega `v2.0.0` (submissão JOSS), optou-se por **`--self-contained` + `PublishReadyToRun=true` (AOT gerenciado / ReadyToRun)** nos containers Docker (`runtime-deps:10.0`) e no modo `Local` do `Launcher`, mantendo **NativeAOT (`PublishAot=true`) como trabalho futuro** para o host completo (embora a camada `MelloSilveiraTools.Database` já seja 100% compatível com NativeAOT). Essa decisão entrega pré-compilação AOT dos assemblies da aplicação e containers totalmente autocontidos sem exigir linker C++ na máquina do usuário/revisor e sem quebrar MVC, Swagger ou o sistema de plugins dinâmicos.

- **Análise e Adaptação da Camada de Persistência para Uso de PgBouncer sob Alta Carga**
  - **Contexto:** Camada de Persistência PostgreSQL (`MelloSilveiraTools.Database` / `PostgresRepository` / `PostgresResiliencePipeline`) em cenários futuros de escala horizontal elevada ou *serverless*.
  - **Gatilho / Critério de Adoção:** Avaliar somente quando a soma total de conexões entre todas as réplicas/serviços (`réplicas × MaxPoolSize`) ultrapassar ~200–300 conexões simultâneas, ou em ambientes com *autoscaling* agressivo / funções *serverless* com alta rotatividade de conexões curtas.
  - **Descrição das Adaptações a Analisar:**
    1. **Fronteira explícita de transação em `pool_mode = transaction`:** Avaliar a substituição da transação implícita de `NpgsqlBatch` por transação explícita combinada com batch (`BeginTransactionAsync` + `NpgsqlBatch([set_config, query])` + `CommitAsync` em 2 round-trips), mitigando erros de dessincronização de protocolo (`08P01: server conn crashed?` e `Exception while reading from stream`) observados sob alta carga em *transaction pooling*;
    2. **Parâmetros obrigatórios de connection string e pooler:** Validar `No Reset On Close=true`, `Connection Idle Lifetime` inferior aos timeouts do PgBouncer/firewall, `Keepalive`, proibição de `Multiplexing=true` e compatibilidade com PgBouncer 1.21+ (`max_prepared_statements` / `Max Auto Prepare`);
    3. **Resiliência no início de streaming e idempotência de escritas:** Avaliar retry via Polly antes do primeiro `yield return` em `GetAsync`/`GetDistinctAsync` (para conexões ociosas derrubadas pelo proxy) e auditar idempotência de operações `InsertAsync` sem chave única em caso de queda durante a leitura da resposta após `COMMIT`;
    4. **Benchmark e testes de estresse:** Executar testes de carga concorrente contra PgBouncer real em `pool_mode = transaction` validando ausência de vazamento de contexto RLS (`app.current_user_id`) e recuperação limpa em cenários de falha transiente.
  - **Justificativa da Decisão Atual:** No estágio atual do projeto (poucas instâncias da API e baixo volume de conexões), o pool interno do `NpgsqlDataSource` Singleton conectado diretamente ao PostgreSQL via `NpgsqlBatch` (1 round-trip, sem `BEGIN`/`COMMIT` explícitos) oferece menor latência, menor complexidade operacional e elimina um ponto intermediário de falha.
