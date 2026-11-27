# AGENTS.md

Developer and AI agent guidelines for **MelloSilveiraTools** — a modular `.NET 10` / `C# 13` suite of NuGet packages for scientific computing, viscoelastic constitutive modeling, experimental data optimization, relational persistence, Web APIs, and dynamic plugins.

> **On-Demand Documentation (Single Source of Truth — load only when working on those subsystems):**
> - Suite overview & architecture: [`README.md`](README.md)
> - Module-specific DI lifetimes, capabilities & technical invariants: each package's `README.md` (`src/<Package>/README.md`, e.g., [`Core`](src/MelloSilveiraTools.Core/README.md), [`Database`](src/MelloSilveiraTools.Database/README.md), [`WebApi`](src/MelloSilveiraTools.WebApi/README.md), [`Plugins`](src/MelloSilveiraTools.Plugins/README.md), [`Mathematics`](src/MelloSilveiraTools.Mathematics/README.md), [`MechanicsOfMaterials`](src/MelloSilveiraTools.MechanicsOfMaterials/README.md), [`Optimizations`](src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/README.md), [`Migrator`](src/MelloSilveiraTools.Database.Migrator/README.md))
> - Biomechanics Master's Thesis Guide (PPEMM/UFCIRJ): [`.agents/soft-tissue-biomechanics/consolidated-thesis-guide.md`](.agents/soft-tissue-biomechanics/consolidated-thesis-guide.md)
> - JOSS Submission & Target Journals Guide: [`.agents/soft-tissue-biomechanics/consolidated-joss-and-publications.md`](.agents/soft-tissue-biomechanics/consolidated-joss-and-publications.md)

---

## Solution Layout & Commands

```text
src/MelloSilveiraTools/                                — Meta-package (aggregates all NuGet packages; meta DI entry)
src/MelloSilveiraTools.Core/                           — Result monad, caches, Polly v8, crypto, email, IFileStorageService (+ FileStorageHealthCheck), TPL Dataflow & fluent pipelines
src/MelloSilveiraTools.Database/                       — IRepository, PostgresRepository (pure Npgsql/NpgsqlBatch), PostgresSqlProvider, PostgresHealthCheck, attributes, filters
src/MelloSilveiraTools.Database.Migrator/              — PostgreSQL DbUp schema migrator / CLI runner (exempt from broad coverage)
src/MelloSilveiraTools.WebApi/                         — Controllers, Minimal APIs, NDJSON streaming, Swagger, JWE auth, rate limiting, CORS, aggregated health checks, CRUD commands
src/MelloSilveiraTools.Plugins/                        — Storage-agnostic collectible plugin runtime, two-level cache, Model 2 community submissions, dynamic DI, background orchestrator
src/MelloSilveiraTools.Mathematics/                    — Function hierarchy, PronySeries, Simpson integration, differentiation, Brent/Bisection, Newmark-β
src/MelloSilveiraTools.MechanicsOfMaterials/           — Fatigue, profiles, 3D forces, specimen analysis commands/endpoints, constitutive calculators (Elastic, Maxwell, Fung, SimplifiedFung, Schapery, MSM)
src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ — Experimental segmentation/fitting pipelines (ALGLIB/MathNet), OAAT parametric sweep, API controllers & Minimal API endpoints
src/MelloSilveiraTools.WebApi.Host/                    — Turnkey executable ASP.NET Core host serving REST/NDJSON endpoints, Swagger, and WebUi (exempt from broad coverage)
src/MelloSilveiraTools.WebUi/                          — Interactive Blazor WASM + Apache ECharts workbench (exempt from broad coverage)
src/MelloSilveiraTools.Launcher/                       — Cross-platform CLI infrastructure bootstrapper & orchestrator (exempt from broad coverage)
test/UnitTests/                                        — Fast in-memory xUnit test suite (no external dependencies)
test/AcceptanceTests/                                  — End-to-end ligament, plugin & endpoint tests; PostgreSQL integration tests via Testcontainers (Docker)
build/                                                 — Centralized bin/obj output via Directory.Build.props
paper/                                                 — JOSS manuscript (paper.md, paper.bib), compiled by .github/workflows/draft-pdf.yml
Directory.Build.targets / coverlet.runsettings         — SDK NuGet package validation (`EnablePackageValidation=true`) and Coverlet coverage exclusions (ALGLIB, `*.g.cs`, non-core hosts/UI/CLI)
```

```powershell
dotnet build MelloSilveiraTools.sln                                      # Builds all projects + validates & packs .nupkg/.snupkg in build/bin/Debug/
dotnet test test/UnitTests/UnitTests.csproj --settings coverlet.runsettings
dotnet test test/AcceptanceTests/AcceptanceTests.csproj --filter "Category!=Slow" --settings coverlet.runsettings
```

---

## Licensing, Dependency Graph & Package Policy

- **License:** `GPL-3.0-or-later` (mandatory for OSI/JOSS compliance because `Optimizations` bundles ALGLIB Free Edition C# source under `GPL-2.0-or-later`).
- **Common Build Props (`Directory.Build.props` / `Directory.Build.targets`):** `net10.0`, `ImplicitUsings=enable`, `Nullable=enable`, `GeneratePackageOnBuild=true`, `GenerateDocumentationFile=true`, `EnablePackageValidation=true` on packable packages, `VersionPrefix=2.0.0-thesis-softtissue-rc01`.
- **Dependency Graph:**
  ```text
  Core ←─── Mathematics ──────────────────┐
    │                                     ↓
    ├─── Database ←──────────────── MechanicsOfMaterials ───┐
    │       ↑                                               ↓
    └───► WebApi ←── Plugins                    MechanicsOfMaterials.Optimizations (also → Database, WebApi)

  MelloSilveiraTools (meta) → all 7 library packages above
  Database.Migrator → Core, Database
  WebApi.Host → MelloSilveiraTools, WebUi  |  Launcher → Database.Migrator, WebApi.Host
  ```
- **Namespaces:** Mirror each package ID (`MelloSilveiraTools.<Package>.*`).
- **Dependency Policy:** Never add overlapping NuGet dependencies; keep `Core`, `Mathematics`, and `MechanicsOfMaterials` AOT-clean (avoid reflection-heavy dependencies).

---

## Coding Conventions & Style (`.editorconfig` — Mandatory)

1. **No `var`:** Explicit types are mandatory for every local variable (`int count = 0;`, `List<T> items = [];`). Target-typed `new()` is encouraged when the left-hand type is explicit.
2. **Constructors & Bodies:** Prefer **primary constructors**; use expression bodies (`=>`) for properties, indexers, accessors, lambdas, and single-expression methods; use block bodies `{ ... }` for multi-statement methods/constructors.
3. **Layout:** 4 spaces indentation, **CRLF** line endings (`end_of_line = crlf`), `using` directives outside namespaces, private fields prefixed with `_`, single-line `if` without braces allowed/preferred, multi-line blocks require braces.
4. **Parameter Ordering:** (1) `ILogger`/`ILogger<T>`/`ILoggerFactory`, (2) interfaces (with `IPipelineProgressNotifier` first), (3) concrete classes and primitives (with `TimeProvider` preceding other instances), ..., (last) `CancellationToken`.
5. **Temporal Operations:** Mandatory non-optional, non-nullable `TimeProvider`. Ambient `DateTime.UtcNow` / `DateTimeOffset.UtcNow` is **forbidden** in production code.
6. **Modern C# 13:** Use `extension(Type)` blocks, collection expressions `[...]`, `System.Threading.Lock`, switch/property pattern matching, spans/slices, `readonly record struct` for immutable value DTOs, `ref struct` for stack-only builders.
7. **Logging:** Inject standard `Microsoft.Extensions.Logging.ILogger<T>` (backed by Serilog file/PostgreSQL sinks).
8. **Settings & DI:** Settings are `record`/class types with `required` mandatory properties and nullable (`?`) optional properties — **never** assume silent default values (`= default;`), except for optional `bool` flags whose safe default is `false` (which do not need to be `bool?`, e.g., opt-out/feature-enable flags like `AllowUnmanagedDlls` or `AllowSuperuserDatabaseConnection`). Validate platform/critical prerequisites eagerly at DI registration.
9. **XML Documentation:** Mandatory on all `public` types and members (`<summary>`, `<param>`, `<returns>`, plus `<exception>`/`<remarks>` where applicable).
10. **Assign Before Applying Logic to Method Returns:** Never apply inline operators or ternary/compound logic directly to a method call's return value (e.g., `return await CountAsync(...) > 0;`). First assign the return value to an explicitly typed local variable, then evaluate the logic (`long count = await CountAsync(...); return count > 0;`). This rule does **not** apply to boolean method returns evaluated directly inside `if` conditions (e.g., `if (!await IsReadyAsync(...))`) or passed directly without transformation.

---

## Strict Architectural Rules & Constraints

### Contracts & Naming
- **Result vs. Output:** Calculators emit raw `*Output` models (`MechanicalModelOutput`, `FatigueOutput`) — never `*Result` monads.
- **Constitutive Parameters:** Inherit `ConstitutiveParameters` with `*ConstitutiveParameters` suffix.
- **Request DTOs:** `*Request` classes must **never** carry a `CancellationToken` property; pass cancellation explicitly to `command.ExecuteAsync(request, cancellationToken)`.
- **Database Entities & SQL:** PascalCase `record` inheriting `EntityBase`; `[Table("snake_case")]`; properties map via `ToSnakeCase()`. Keep static compile-time placeholders (`#TABLE_NAME`) separate from runtime clauses (`#WHERE`, `#LIMIT`). Batch SQL parameter suffixes are **1-based** (`@Name_1`, `@Name_2`).
- **DI Over Static Helpers with Dependencies:** Never create `static` classes that receive service or infrastructure dependencies (e.g., `IRepository`, `IFileStorageService`, `ILogger`, `TimeProvider`, processors, or domain services) as method parameters. Every class that requires dependencies MUST be an instantiable type using constructor injection and registered in the DI container. Static classes are strictly reserved for stateless pure utilities, C# 13 `extension(...)` blocks, and `*DependencyInjection` registration/bootstrap modules.
- **Breaking Changes:** Published NuGet suite — do **not** rename, remove, or alter signatures of existing `public` members or change DI lifetimes without explicit human approval.

### Concurrency, Safety & AOT
- **Thread-Safety:** Every **Singleton** service (calculators, caches, `PostgresRepository`, `PostgresSqlProvider`, resilience pipelines, storage services) MUST be thread-safe. `PostgresRepository` opens per-call `NpgsqlConnection` via `await using` (no mutable instance state); static metadata/SQL caches must remain `ConcurrentDictionary`.
- **Lossless Fan-Out:** Never use native `BroadcastBlock<T>` in TPL Dataflow forks/broadcasts; use `DataflowFanOut` (`ActionBlock` + bounded `BufferBlock`s) for lossless delivery and end-to-end backpressure.
- **AOT & Trimming:** The 8 core NuGet library projects set `<IsAotCompatible>true</IsAotCompatible>` (`Database.Migrator` sets `false`; `WebApi.Host`, `WebUi`, `Launcher` set `IsPackable=false`; trim warnings are suppressed in `Directory.Build.props`). Do **not** pollute public APIs with `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`; use `[DynamicallyAccessedMembers]` only when functionally required.

### Testing & Versioning
- **Tests:** xUnit 2.9.3 + Moq 4.21.0 + coverlet.collector 10.1.0 + Testcontainers.PostgreSql 4.16.0, AAA pattern (`[Fact]`, `[Theory]`), `FakeDataReader` for DB-free reader tests, exact string + `Assert.Same` cache checks for SQL generation tests. Use `[PostgresIntegrationFact]` for Docker/PostgreSQL tests (skips cleanly locally without Docker, fails strictly in CI) and `[Trait("Category", "Slow")]` for multi-minute optimization runs. Broad coverage targets focus on the **8 publishable NuGet packages** (`coverlet.runsettings` excludes vendored ALGLIB, `*.g.cs`, and the 4 exempted projects `Database.Migrator`, `WebApi.Host`, `WebUi`, `Launcher`, which only need lightweight smoke/contract tests).
- **Changelog:** SemVer following Keep a Changelog — record unreleased changes in `CHANGELOG.md` under `## [Unreleased]` (`Added`, `Changed`, `Fixed`, `Breaking`, `Removed`).
