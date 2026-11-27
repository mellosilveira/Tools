# Mapped Future Works & Research Opportunities

This document centralizes improvement opportunities, architectural extensions, and post-scope research steps for Soft Tissue Biomechanics so no item is lost across development cycles.

## Future Works Backlog

- **1D Scalar Constants to 3D Tensor Conversion**
  - **Context:** Inverse Finite Element Analysis (FEA) Optimization.
  - **Description:** Adapt the constitutive models (currently operating with 1D scalar variables) into 3D tensorial space to enable realistic finite-element continuum simulations.

- **C++ User-Defined Material (UDM) Plugins for FEBio**
  - **Context:** Finite Element Analysis (FEA) Solver Integration.
  - **Description:** Develop *User-Defined Materials* (UDM) in FEBio using C++.
  - **Note:** This effort must remain strictly time-boxed (4 weeks / 4 cycles) so it does not derail the primary thesis timeline.

- **Include Strain Rate in Variable Sensitivity Analysis**
  - **Context:** Parametric Sensitivity Analysis (`ParametricSweep` / Grid Search).
  - **Description:** Evaluate how input strain rate ($\dot{\varepsilon}$) affects fitted constitutive parameters and simulation convergence across models.

- **Full Theoretical-Experimental Explainability (XAI) and Traceability in AI-Synthesized Outputs**
  - **Context:** Presentation Layer / AI Research Agents / Reports & Analytical Conclusions.
  - **Description:** Structure all generated reports, analyses, and final conclusions — including AI-synthesized summaries — so they never emit isolated numbers or black-box claims, instead explicitly exposing:
    1. Step-by-step analytical and computational derivation/reasoning;
    2. Strict Continuum Mechanics, Large Deformation Theory, and Linear/Non-Linear Viscoelasticity backing (kinematic hypotheses, tensors/scalars, constitutive equations);
    3. Contextualized experimental grounding (specimen ID, geometry/$L_0$, deformation phases — ramp, relaxation, recovery — fitted parameters, and $R^2$/RMSE metrics).
  - **Rationale:** Eliminates black-box outputs and guarantees the scientific auditability, methodological transparency, and full reproducibility required for the Master's defense (PPEMM/UFCIRJ) and international peer-reviewed journals.

- **Expansion of Communication Protocols Beyond HTTP/HTTPS REST (`v3+`)**
  - **Context:** Web API Layer, Real-Time Telemetry, Distributed Workers & HPC/FEBio Coupling (`MelloSilveiraTools.WebApi` / `Core.Pipelines`).
  - **Description:** Retain **HTTP/HTTPS + JSON + NDJSON Streaming** for the v3.0.0 Quickstart (JOSS) and progressively expand transport options in future releases:
    1. **WebSockets via SignalR (`Microsoft.AspNetCore.SignalR`):** Native ASP.NET Core bidirectional transport (zero third-party NuGets) for multiplexing multiple pipelines on one connection, auto-reconnection, and browser control commands (*pause/cancel*) with JWE authentication.
    2. **Server-Sent Events (SSE — `TypedResults.ServerSentEvents`):** Native `.NET 10` unidirectional streaming (`text/event-stream`) with named events (`topology`, `progress`, `error`) as a lightweight alternative to NDJSON.
    3. **gRPC / gRPC-Web (`Grpc.AspNetCore`):** High-performance binary transport over HTTP/2 + Protobuf (Native AOT compatible) for dense time series (20k–50k+ points) and Python/C++/FEBio solver coupling.
    4. **Message Queues / Brokers (`ProgressNotificationMode.MessageQueue`):** Concrete backend implementation for the existing `ProgressNotificationMode` enum (e.g., RabbitMQ, Redis Streams, Azure Service Bus) to offload heavy batch optimizations (Differential Evolution + L-BFGS) to horizontally scaled workers.
    5. **Local IPC (`Named Pipes` / `Unix Domain Sockets`):** Low-latency interprocess transport for local solvers or desktop clients without TCP/HTTP overhead.
  - **Rationale:** Keeps zero extra dependencies in the current JOSS Quickstart while establishing a clear evolution path for dense numerical streaming, distributed messaging, and multi-language solver integration.

- **Full Application NativeAOT Compatibility (`PublishAot=true`)**
  - **Context:** Web Host, Persistence, and Plugin Runtime (`MelloSilveiraTools.WebApi.Host`, `WebApi`, `Database`, `Database.Migrator`, `Plugins`).
  - **Description:** Refactor the host application to support strict NativeAOT compilation (`PublishAot=true`):
    1. Migrate ASP.NET Core MVC controllers (`AddControllers()`, `AddApplicationPart`) to reflection-free **Minimal APIs**;
    2. Replace `Swashbuckle` (`AddSwaggerWithBearerSecurity`) with `Microsoft.AspNetCore.OpenApi` + static UI;
    3. Register all request/response DTOs in `JsonSerializerContext` (source generation);
    4. ~~Replace `Dapper` with `Dapper.AOT` or static `NpgsqlDataReader` mapping~~ *(Completed: `Dapper` removed, `MelloSilveiraTools.Database` migrated to pure Npgsql + `NpgsqlSlimDataSourceBuilder` + `DictionaryExtensions` AOT fallback with 0 `IL2xxx`/`IL3xxx` warnings)*; review `Expression.Compile()` in `MechanicalModelTypeCache`;
    5. Isolate or decouple `MelloSilveiraTools.Plugins` from any NativeAOT host binary, as runtime `AssemblyLoadContext` loading is unsupported in closed NativeAOT images;
    6. Remove `IL2xxx`/`IL3xxx` warning suppressions from `Directory.Build.props`.
  - **Benefits of NativeAOT:** Smaller containers/binaries without full `.NET` runtime, near-instant cold start, lower baseline RAM, zero runtime JIT overhead.
  - **Costs & Trade-offs:** High architectural refactoring cost; structural incompatibility with runtime dynamic plugin loading; `Expression.Compile()` falls back to interpreted mode unless source-generated; requires a host **C++ linker/toolchain** (Visual Studio C++ tools on Windows, `clang`/headers on Linux, Xcode CLT on macOS), raising setup friction for JOSS reviewers.
  - **Current Decision Rationale:** For `v3.0.0` (JOSS submission), **`--self-contained` + `PublishReadyToRun=true` (managed ReadyToRun AOT)** is used in Docker containers (`runtime-deps:10.0`) and `Launcher` Local mode, keeping full `PublishAot=true` as future work for the composite host (while `MelloSilveiraTools.Database` and core libraries remain AOT-compatible).

- **PgBouncer Persistence Layer Adaptation for High Horizontal Concurrency**
  - **Context:** PostgreSQL Persistence Layer (`MelloSilveiraTools.Database` / `PostgresRepository` / `PostgresResiliencePipeline`) under future high-concurrency or serverless scaling.
  - **Adoption Trigger:** Evaluate only when total aggregate connections across replicas/services (`replicas × MaxPoolSize`) exceed ~200–300 simultaneous connections, or under aggressive serverless autoscaling with rapid short-lived connection churn.
  - **Adaptations to Evaluate:**
    1. **Explicit transaction boundaries under `pool_mode = transaction`:** Evaluate replacing `NpgsqlBatch` implicit transactions with explicit `BeginTransactionAsync` + `NpgsqlBatch([set_config, query])` + `CommitAsync` (2 round-trips) to prevent protocol desynchronization (`08P01` / stream errors) under heavy transaction pooling;
    2. **Required connection string & pooler settings:** Validate `No Reset On Close=true`, `Connection Idle Lifetime` below PgBouncer/firewall timeouts, `Keepalive`, disallow `Multiplexing=true`, and verify PgBouncer 1.21+ settings (`max_prepared_statements` / `Max Auto Prepare`);
    3. **Stream startup resilience & write idempotency:** Evaluate Polly retry before the first `yield return` in `GetAsync`/`GetDistinctAsync` and audit `InsertAsync` idempotency on post-`COMMIT` response drops;
    4. **Load benchmarks:** Execute concurrent stress tests against real PgBouncer in `pool_mode = transaction`, verifying zero RLS context (`app.current_user_id`) leakage and clean transient failure recovery.
  - **Current Decision Rationale:** At the current project scale (few API instances and low connection concurrency), `NpgsqlDataSource`'s built-in pool connected directly to PostgreSQL via single-round-trip `NpgsqlBatch` yields lower latency, simpler operations, and fewer failure points.

- **Removal of `Creep` Modeling Code Paths (`MechanicsOfMaterials`, `Optimizations`, `WebUi`)**
  - **Context:** Viscoelastic Constitutive Modeling (`ViscoelasticEffect.Creep`, creep compliance methods, and UI selectors).
  - **Description:** Remove all residual `Creep` branches, stub/unimplemented methods throwing `NotImplementedException` (e.g., `CalculateCreepCompliance`, `Creep` branches in `LinearModelCalculator`, `QuasiLinearModelCalculator`, `SchaperyModelCalculator`, `ModifiedSuperpositionMethodCalculator`, `MechanicalModelCalculatorFacade`), and `Creep` UI options in future major versions once backward compatibility allows.
  - **Rationale:** There is no research or product forecast to use creep modeling (all thesis and optimization pipelines target ramp-and-hold stress relaxation exclusively); keeping dead or partially unimplemented `Creep` paths adds unnecessary maintenance burden, surface area, and risk of incomplete/silent error paths.
