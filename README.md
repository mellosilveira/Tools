# MelloSilveiraTools

[![Build and Test](https://github.com/mellosilveira/Tools/actions/workflows/publish_release_candidate_nuget_package.yml/badge.svg)](https://github.com/mellosilveira/Tools/actions/workflows/publish_release_candidate_nuget_package.yml)
[![JOSS Draft PDF](https://github.com/mellosilveira/Tools/actions/workflows/draft-pdf.yml/badge.svg)](https://github.com/mellosilveira/Tools/actions/workflows/draft-pdf.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Target: .NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

A modular .NET 10 suite of high-performance packages designed for computational mechanics of materials, viscoelastic constitutive modeling, experimental data optimization, dynamic plugin execution, and enterprise-grade system foundations.

---

## 1. About the Project

`MelloSilveiraTools` originated from a practical, real-world initiative: centralizing and standardizing battle-tested, recurring code patterns across multiple mission-critical software systems and research initiatives. Rather than an abstract theoretical framework, every capability in this suite was born out of genuine, demanding requirements encountered in real-world projects.

The project provides a comprehensive, unified foundation for engineering high-complexity, high-performance .NET applications. It combines advanced mathematical modeling, biomechanical constitutive characterization, and high-throughput streaming pipelines with production-grade architectural utilities (resilience pipelines, relational database providers, dynamic plugin loading, and robust file management).

---

## 2. Technical Decisions & Architectural Patterns

The suite is architected around strict engineering principles to guarantee high throughput, maintainability, and clean separation of concerns:

- **Domain-Driven Design (DDD):** Pure domain logic is encapsulated inside dedicated core packages (`Mathematics`, `MechanicsOfMaterials`), free from infrastructure leakage. Models, constitutive parameters, and geometric profiles are represented as immutable, strongly typed records enforcing thermodynamic and physical validity bounds.
- **Clean Architecture:** Strict unidirectional dependency flow. Calculation engines and mathematical abstractions have zero knowledge of databases, web frameworks, or operating system storage abstractions. Infrastructure layers (`Database`, `WebApi`, `Plugins`) act purely as external adapters.
- **Plugin Architecture (Extensibility):** Features a dynamic, file-based and in-memory plugin runtime utilizing collectible .NET `AssemblyLoadContext` instances and two-level caching. Currently functional and specialized for mechanical constitutive models (`IMechanicalModelCalculator`, `ConstitutiveParameters`), allowing researchers and engineers to hot-load new material models into running systems without modifying or redeploying the core application.
- **Native AOT & High Performance:** Core libraries target Ahead-of-Time (AOT) compilation and trimming compatibility (`<IsAotCompatible>true</IsAotCompatible>`). Reflection is eliminated in critical simulation paths through compiled expression trees, and performance is optimized using modern C# 13 features (`ref struct`, `Span<T>`, `ArrayPool<T>`), along with non-blocking concurrency primitives (`SemaphoreSlim`, `System.Threading.Channels`, and concurrent collections).
- **Minimal APIs, Controllers & Swagger:** The web layer provides a versatile, dual-surface HTTP exposure: ultra-fast ASP.NET Core Minimal APIs alongside structured MVC controllers (`CrudController<TEntity, TFilter>` with automated generic CRUD, filtering, and pagination). Complete OpenAPI/Swagger documentation with Bearer security is configured out-of-the-box.
- **Strict Dependency Discipline (Minimal Third-Party NuGets):** To prevent supply-chain vulnerabilities, breaking transitive dependencies, and framework bloat, the suite strictly restricts external dependencies. It relies almost exclusively on official, first-party Microsoft/C# runtime libraries (`Microsoft.Extensions.*`, `System.Threading.Channels`, TPL Dataflow) and the official `Npgsql` driver for PostgreSQL.

---

## 3. Project Modules & Dependencies

The solution is partitioned into contextual, decoupled NuGet packages:

```
Core ←─── Mathematics ←─── MechanicsOfMaterials ←─── MechanicsOfMaterials.Optimizations
  ↑
  └── Database ←── WebApi ←── Plugins

MelloSilveiraTools (meta) → Aggregates all above packages
```

| Package | Key Features & Responsibilities | Dependencies |
|---|---|---|
| **`MelloSilveiraTools.Core`** | TPL Dataflow & fluent pipeline builders with real-time telemetry, Polly v8 resilience pipelines, in-memory caches, cryptography, file storage providers (`Local`, `AWS S3`, `Azure Blob`, `DFS`) with native health checks, and extension methods. | BCL, `Microsoft.AspNetCore.App`, `Polly`, `AWSSDK.S3`, `Azure.Storage.Blobs`, `Serilog.*` |
| **`MelloSilveiraTools.Database`** | High-performance pure-Npgsql relational database provider for PostgreSQL (`NpgsqlSlimDataSourceBuilder`, `NpgsqlBatch` single-round-trip RLS scoping), attribute-driven SQL generation, dynamic filtering clauses, native `PostgresHealthCheck`, and Polly v8 resilience. | `Core`, `Npgsql`, `Serilog.Sinks.Postgresql.Alternative` |
| **`MelloSilveiraTools.WebApi`** | Base controllers (`CustomControllerBase`, `CrudController`), Minimal API mappings (`MapCrud`, `Auth`, `StorageEntryMetadata`, `Health`), host security/rate-limiting/CORS extensions, aggregated native health checks, NDJSON real-time streaming, JWE bearer authentication, and Swagger configuration. | `Core`, `Database`, `Microsoft.AspNetCore.App`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.IdentityModel.JsonWebTokens`, `Swashbuckle.AspNetCore` |
| **`MelloSilveiraTools.Plugins`** | Dynamic plugin loader with collectible `PluginLoadContext`, two-level caching, storage-agnostic binary inspection, Model 2 community submissions & superuser moderation, dynamic DI registration, and background file orchestrator. | `WebApi`, `Core`, `Database` |
| **`MelloSilveiraTools.Mathematics`** | Symbolic-like function hierarchy (`FunctionFactory`), numerical integration (Simpson's rule), differentiation, root finding (Brent, Bisection, StepByStep), ODE solvers (Newmark, Newmark-$\beta$), 3D vectors, and statistics. | `Core` (zero external third-party dependencies) |
| **`MelloSilveiraTools.MechanicsOfMaterials`** | Linear and non-linear viscoelastic constitutive models (Fung QLV, Simplified Fung with Prony series, Schapery, Maxwell, Modified Superposition Method), classical fatigue criteria (Goodman/Marin), geometric profiles, specimen analysis endpoints/commands, and 3D ligament load sharing. | `Mathematics`, `Database`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| **`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`** | TPL Dataflow experimental data pipelines: CSV phase segmentation (`Ramp`, `Relaxation`, `Descent`, `Recovery`), downsampling, constrained curve fitting (embedded ALGLIB L-BFGS & MathNet.Numerics), OAAT parametric sensitivity sweeps, and HTTP APIs. | `MechanicsOfMaterials`, `Database`, `WebApi`, `MathNet.Numerics` |
| **`MelloSilveiraTools`** | Meta-package aggregating all packages above into a single reference with unified dependency injection (`AddToolsServices`). | All suite packages |

---

## 4. How to Install NuGet Packages

The suite is available on [NuGet.org](https://www.nuget.org/). Install the full meta-package via the .NET CLI:

```bash
dotnet add package MelloSilveiraTools
```

Or install individual packages according to your system's specific requirements:

```bash
# Core utilities and data pipelines
dotnet add package MelloSilveiraTools.Core

# Mathematics and numerical methods
dotnet add package MelloSilveiraTools.Mathematics

# Biomechanics and constitutive modeling
dotnet add package MelloSilveiraTools.MechanicsOfMaterials

# Experimental data optimization and curve fitting
dotnet add package MelloSilveiraTools.MechanicsOfMaterials.Optimizations

# PostgreSQL database provider
dotnet add package MelloSilveiraTools.Database
```

---

## 5. Web API Usage (`MelloSilveiraTools.WebApi.Host`)

A turnkey ASP.NET Core Web API host (`src/MelloSilveiraTools.WebApi.Host`) exposes the computational engine via JWE-secured REST endpoints, Swagger UI (`/swagger`), and real-time NDJSON progress streaming.

### Quickstart via Cross-Platform Launcher
```bash
dotnet run --project src/MelloSilveiraTools.Launcher/
```
The launcher validates Docker / PostgreSQL availability, runs idempotent DbUp migrations (`MelloSilveiraTools.Database.Migrator`), starts `WebApi.Host` at `http://localhost:5080`, and opens the browser automatically.

### Quickstart via Docker Compose
```bash
docker compose up --build
```

### Core Endpoints
- **Aggregated Health Check (`GET /api/health`):** Executes registered `FileStorageHealthCheck` and `PostgresHealthCheck` probes, returning `HealthCheckResultData` (`200 OK` or `503 Service Unavailable`).
- **JWE Authentication (`POST /api/v1/auth/register`, `/login`, `/refresh`, `/validate`, `GET /api/v1/auth/me`):** Issues, refreshes, and validates signed and encrypted JWE bearer tokens and inspects active user/superuser sessions.
- **Bundled Samples (`GET /api/v1/samples`):** Lists pre-loaded knee ligament datasets (`LCA_02_07_21_L38_D11`).
- **Storage Entries & Artifacts (`POST/GET/PUT/DELETE /api/v1/storage-entry-metadata`, `GET /api/v1/storage-entry-metadata/{id}/download`):** Multipart storage, metadata queries, updates/deletes, and file streaming.
- **Experimental Data Pipelines (`POST /api/v1/experimental-data/upload`, `/process`, `/process/batch`):** Uploads CSV pairs and executes single or batch experimental segmentation, curve fitting, and simulation, returning `202 Accepted` with the execution identifier.
- **Parametric Sweeps (`POST /api/v1/parametric-sweeps/process`, `/process/experimental-data`):** Triggers OAAT sensitivity sweeps across baseline parameters or calibrated experimental models *(MVC controller also exposes `/process-from-experimental-data`)*.
- **Mechanics of Materials (`GET /api/v1/mechanics/models`, `POST /api/v1/mechanics/specimen-analysis/execute`, `/fatigue/calculate`, `/fatigue/calculate/endurance-limit`):** Discovers constitutive models and executes deterministic specimen analysis and Goodman/Marin fatigue evaluations.
- **Dynamic Plugins (`GET /api/v1/plugins`, `/inspect`, `POST/GET/DELETE /api/v1/plugins/submissions`, `POST /upload`):** Public plugin catalog, superuser assembly partition inspection, community submission moderation, and runtime cache/load management.
- **Real-Time Progress Streaming (`GET /api/v1/pipelines/{pipelineId}/progress`):** Emits `application/x-ndjson` streams providing item-level progress counters and DAG step topology updates.
- **Persisted Result Queries (`GET /api/v1/curve-fit-outputs`, `/api/v1/mechanical-model-simulations`, `/api/v1/parametric-sweeps`):** Paginated queries and NDJSON streaming for stored computational outputs.

---

## 6. Frontend Usage (`MelloSilveiraTools.WebUi`)

An interactive **Blazor WebAssembly (.NET 10)** workbench (`src/MelloSilveiraTools.WebUi`, served directly by `MelloSilveiraTools.WebApi.Host` at `http://localhost:5080` locally or `http://localhost:24263` via Docker Compose) using Canvas-accelerated **Apache ECharts** provides:
- **JWE Session Management:** User registration, login, JWE token refresh, and live session/role inspection.
- **Sample & CSV Ingestion:** Instant selection of bundled knee ligament samples or browser CSV pair upload.
- **Live NDJSON Telemetry:** Step-by-step progress table (`Pending`, `Started`, `Completed`) during pipeline execution.
- **Experimental Segmentation & Model Fit Overlay:** Interactive zoomable plotting of `Ramp`, `Relaxation`, `Descent`, and `Recovery` phases overlaid with calibrated constitutive model simulations (`Fung`, `SimplifiedFung`, `Schapery`).
- **Parametric Sensitivity & Workbench Tabs:** Dedicated tabs for Experimental Pipelines, Mechanics of Materials (Specimen Analysis & Fatigue), Storage Entry Metadata management, and Plugin catalog/moderation.

---

## 7. Project-Specific Documentation

For in-depth architectural notes, class references, and specialized usage guides, please refer to the dedicated `README.md` file located inside each project directory:

- [**`src/MelloSilveiraTools/README.md`**](src/MelloSilveiraTools/README.md) — Meta-package overview and unified DI setup.
- [**`src/MelloSilveiraTools.Core/README.md`**](src/MelloSilveiraTools.Core/README.md) — Pipelines (TPL Dataflow/Fluent), resilience, caching, and storage.
- [**`src/MelloSilveiraTools.Database/README.md`**](src/MelloSilveiraTools.Database/README.md) — PostgreSQL repositories, SQL generators, and filtering.
- [**`src/MelloSilveiraTools.Database.Migrator/README.md`**](src/MelloSilveiraTools.Database.Migrator/README.md) — Contextual DDL schema migrations powered by DbUp.
- [**`src/MelloSilveiraTools.Mathematics/README.md`**](src/MelloSilveiraTools.Mathematics/README.md) — Functions, expressions, numerical integration, differentiation, and ODE solvers.
- [**`src/MelloSilveiraTools.MechanicsOfMaterials/README.md`**](src/MelloSilveiraTools.MechanicsOfMaterials/README.md) — Viscoelastic constitutive equations, fatigue, and load-sharing models.
- [**`src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/README.md`**](src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/README.md) — Curve fitting, phase segmentation, and parametric sweeps.
- [**`src/MelloSilveiraTools.Plugins/README.md`**](src/MelloSilveiraTools.Plugins/README.md) — Dynamic plugin loader, collectible contexts, and caching.
- [**`src/MelloSilveiraTools.WebApi/README.md`**](src/MelloSilveiraTools.WebApi/README.md) — Minimal APIs, generic CRUD controllers, NDJSON streaming, and authentication.

---

## 8. Running Automated Tests & Dependencies

The solution is thoroughly validated with automated test suites verifying mathematical accuracy, numerical convergence, and pipeline resilience.

Clone and build the solution:

```bash
git clone https://github.com/mellosilveira/Tools.git
cd Tools
dotnet restore MelloSilveiraTools.sln
dotnet build MelloSilveiraTools.sln --configuration Release --no-restore
```

### 8.1. Unit Tests (Fast, In-Memory — No External Dependencies)

Executes the automated in-memory unit test suite covering analytical solvers, constitutive equations, and pipeline components in seconds without requiring any database or Docker container:

```bash
dotnet test test/UnitTests/UnitTests.csproj --configuration Release --no-build --settings coverlet.runsettings
```

### 8.2. Acceptance Tests (End-to-End & PostgreSQL Testcontainers)

Executes the end-to-end acceptance suite validating real knee ligament tensile test data (ACL, PCL, MCL, LCL), HTTP/NDJSON streaming, dynamic plugin lifecycles, and PostgreSQL persistence using [Testcontainers](https://dotnet.testcontainers.org/) to spin up a containerized database instance. PostgreSQL container integration tests marked with `[PostgresIntegrationFact]` automatically report `SKIP` locally when no Docker daemon or `POSTGRES_CONNECTION_STRING` is available, and fail strictly in CI (`CI=true` or `REQUIRE_POSTGRES_TESTS=true`).

> [!IMPORTANT]
> Keep the **Docker daemon running** on the host machine when executing the full PostgreSQL integration suite, or add `--filter "Category!=Slow"` to skip multi-minute curve-fitting benchmarks.

```bash
dotnet test test/AcceptanceTests/AcceptanceTests.csproj --configuration Release --no-build --filter "Category!=Slow" --settings coverlet.runsettings
```

---

## 9. JOSS Publication

`MelloSilveiraTools` is prepared for submission to the *Journal of Open Source Software* (JOSS). 

- **Manuscript:** The full paper draft is available in [`paper/paper.md`](paper/paper.md).
- **Bibliography:** The complete BibTeX references are available in [`paper/paper.bib`](paper/paper.bib).
- **Automated Draft Compilation:** A GitHub Actions workflow ([`.github/workflows/draft-pdf.yml`](.github/workflows/draft-pdf.yml)) compiles the paper into a draft PDF using `openjournals/openjournals-draft-action`.

---

## 10. Contributing & Community Guidelines

Contributions, bug reports, and scientific feedback are warmly encouraged!

- **Guidelines:** Review our [Contributing Guidelines](CONTRIBUTING.md) for pull request workflows, coding style, and testing requirements.
- **Code of Conduct:** We adhere to the [Contributor Covenant v2.1](CODE_OF_CONDUCT.md).
- **Issue Templates:** Use the GitHub issue templates for bug reports and feature requests.

---

## 11. License

This project is licensed under the **GNU General Public License v3.0 or later (`GPL-3.0-or-later`)** — see the [LICENSE](LICENSE) file for details.

*Notice:* `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` embeds C# source files from ALGLIB Free Edition (`GPL-2.0-or-later`). Licensing the suite under GPLv3 complies with ALGLIB, the Open Source Initiative (OSI), and JOSS criteria.
