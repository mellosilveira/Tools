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
| **`MelloSilveiraTools.Core`** | TPL Dataflow & fluent pipeline builders with real-time telemetry, Polly v8 resilience pipelines, in-memory caches, cryptography, file storage providers (`Local`, `AWS S3`, `Azure Blob`, `DFS`), and extension methods. | BCL, `Microsoft.Extensions.*`, `Polly` |
| **`MelloSilveiraTools.Database`** | High-performance relational database provider for PostgreSQL, Dapper extensions, attribute-driven SQL generation, dynamic filtering clauses, and resilient transaction handling. | `Core`, `Npgsql`, `Dapper` |
| **`MelloSilveiraTools.WebApi`** | Base controllers (`CustomControllerBase`, `CrudController`), Minimal API mappings (`MapCrud`), NDJSON real-time streaming, JWE bearer authentication, and Swagger configuration. | `Core`, `Database`, `Microsoft.AspNetCore.App` |
| **`MelloSilveiraTools.Plugins`** | Dynamic plugin loader with collectible `AssemblyLoadContext`, two-level caching, storage-agnostic binary inspection, dynamic DI registration, and background file orchestrator. | `WebApi`, `Core`, `Database` |
| **`MelloSilveiraTools.Mathematics`** | Symbolic-like function hierarchy (`FunctionFactory`), numerical integration (Simpson's rule), differentiation, root finding (Brent, Bisection), ODE solvers (Newmark, Newmark-$\beta$), 3D vectors, and statistics. | `Core` (zero external dependencies) |
| **`MelloSilveiraTools.MechanicsOfMaterials`** | Linear and non-linear viscoelastic constitutive models (Fung QLV, Simplified Fung with Prony series, Schapery, Maxwell), classical fatigue criteria (Goodman/Marin), geometric profiles, and 3D ligament load sharing. | `Mathematics` |
| **`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`** | TPL Dataflow experimental data pipelines: CSV phase segmentation (`Ramp`, `Relaxation`, `Descent`, `Recovery`), downsampling, constrained curve fitting (embedded ALGLIB L-BFGS & MathNet.Numerics), and parametric sensitivity sweeps. | `MechanicsOfMaterials`, `WebApi`, `MathNet.Numerics` |
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

## 5. Web API Usage (In Development)

A turnkey, standalone REST and streaming Web API application is currently under development to expose the computational engine as a headless microservice. 

Planned API endpoints and capabilities:
- **Asynchronous Processing (`POST /api/v1/experimental-data/process`):** Accepts `stress.csv` and `strain.csv` uploads, returning `202 Accepted` with a deterministic execution identifier.
- **Batch Processing (`POST /api/v1/experimental-data/process/batch`):** Ingests batches of experimental datasets with configurable sequential or parallel execution.
- **Parametric Sweeps (`POST /api/v1/parametric-sweeps/process-from-experimental-data`):** Triggers sensitivity sweeps across all calibrated constitutive models for an experimental identifier.
- **Real-Time Progress Streaming (`GET /api/v1/pipeline-runs/{id}/events`):** Emits Server-Sent Events (SSE) and NDJSON streams providing item-level progress counters and DAG step topology updates.

---

## 6. Frontend Usage (In Development)

An accessible, interactive web frontend is currently under active development. Designed with high synergy for the .NET backend, it will provide:
- **Experimental Curve Visualization:** Interactive plotting of raw experimental stress-strain time histories.
- **Automated Phase Inspection:** Visual validation of the sliding-window phase segmentation (`Ramp`, `Relaxation`, `Descent`, `Recovery`).
- **Interactive Model Fitting:** Real-time visual comparison between experimental data and calibrated constitutive models (Fung QLV, Simplified Fung, and Schapery) with parameter confidence interval displays.
- **Parametric Sensitivity Graphs:** Multi-dimensional visualization of One-At-A-Time (OAAT) parametric sweeps.

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

Executes **449 unit tests** covering all analytical solvers, constitutive equations, and pipeline components in ~4 seconds without requiring any database or Docker container:

```bash
dotnet test test/UnitTests/UnitTests.csproj --configuration Release --no-build
```

### 8.2. Acceptance Tests (Requires Docker)

Executes **23 end-to-end integration tests** validating real knee ligament tensile test data (ACL, PCL, MCL, LCL) and database persistence using [Testcontainers](https://dotnet.testcontainers.org/) to spin up a containerized PostgreSQL instance.

> [!IMPORTANT]
> The **Docker daemon must be running** on the host machine before executing acceptance tests.

```bash
dotnet test test/AcceptanceTests/AcceptanceTests.csproj --configuration Release
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
