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
src/MelloSilveiraTools/                                — Meta-package (aggregates all packages; meta DI entry)
src/MelloSilveiraTools.Core/                           — Result monad, caches, Polly v8, crypto, email, IFileStorageService, TPL Dataflow & fluent pipelines
src/MelloSilveiraTools.Database/                       — IRepository, PostgresRepository, PostgresSqlProvider, attributes, filters (Npgsql/Dapper)
src/MelloSilveiraTools.Database.Migrator/              — PostgreSQL schema migrator / migration runner for stored_files and optimization tables
src/MelloSilveiraTools.WebApi/                         — Controllers, minimal endpoints, NDJSON streaming, Swagger, JWE auth, ApiServiceAgent, CRUD commands
src/MelloSilveiraTools.Plugins/                        — Storage-agnostic collectible plugin runtime, two-level cache, dynamic DI, background orchestrator
src/MelloSilveiraTools.Mathematics/                    — Function hierarchy, PronySeries, Simpson integration, differentiation, Brent/Bisection, Newmark-β
src/MelloSilveiraTools.MechanicsOfMaterials/           — Fatigue, profiles, 3D forces, constitutive calculators (Elastic, Maxwell, Fung, SimplifiedFung, Schapery, MSM)
src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ — Experimental segmentation/fitting pipelines (ALGLIB/MathNet), OAAT parametric sweep, API controllers
test/UnitTests/                                        — Fast in-memory xUnit test suite (no external dependencies)
test/AcceptanceTests/                                  — End-to-end ligament tests against Testcontainers PostgreSQL (requires Docker)
build/                                                 — Centralized bin/obj output via Directory.Build.props
paper/                                                 — JOSS manuscript (paper.md, paper.bib), compiled by .github/workflows/draft-pdf.yml
```

```powershell
dotnet build MelloSilveiraTools.sln                    # Builds all projects + .nupkg/.snupkg in build/bin/Debug/
dotnet test test/UnitTests/UnitTests.csproj            # Fast in-memory unit tests
dotnet test test/AcceptanceTests/AcceptanceTests.csproj # Integration tests (requires Docker)
```

---

## Licensing, Dependency Graph & Package Policy

- **License:** `GPL-3.0-or-later` (mandatory for OSI/JOSS compliance because `Optimizations` bundles ALGLIB Free Edition C# source under `GPL-2.0-or-later`).
- **Common Build Props (`Directory.Build.props`):** `net10.0`, `ImplicitUsings=enable`, `Nullable=enable`, `GeneratePackageOnBuild=true`, `GenerateDocumentationFile=true`, `VersionPrefix=2.0.0-thesis-softtissue-rc01`.
- **Dependency Graph:**
  ```text
  Core ←─── Mathematics ←─── MechanicsOfMaterials ←─── MechanicsOfMaterials.Optimizations
    ↑
    └── Database ←── WebApi ←── Plugins
  MelloSilveiraTools (meta) → all packages above
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
8. **Settings & DI:** Settings are `record`/class types with `required` mandatory properties and nullable (`?`) optional properties — **never** assume silent default values (`= default;`). Validate platform/critical prerequisites eagerly at DI registration.
9. **XML Documentation:** Mandatory on all `public` types and members (`<summary>`, `<param>`, `<returns>`, plus `<exception>`/`<remarks>` where applicable).

---

## Strict Architectural Rules & Constraints

### Contracts & Naming
- **Result vs. Output:** Calculators emit raw `*Output` models (`MechanicalModelOutput`, `FatigueOutput`) — never `*Result` monads.
- **Constitutive Parameters:** Inherit `ConstitutiveParameters` with `*ConstitutiveParameters` suffix.
- **Request DTOs:** `*Request` classes must **never** carry a `CancellationToken` property; pass cancellation explicitly to `command.ExecuteAsync(request, cancellationToken)`.
- **Database Entities & SQL:** PascalCase `record` inheriting `EntityBase`; `[Table("snake_case")]`; properties map via `ToSnakeCase()`. Keep static compile-time placeholders (`#TABLE_NAME`) separate from runtime clauses (`#WHERE`, `#LIMIT`). Batch SQL parameter suffixes are **1-based** (`@Name_1`, `@Name_2`).
- **Breaking Changes:** Published NuGet suite — do **not** rename, remove, or alter signatures of existing `public` members or change DI lifetimes without explicit human approval.

### Concurrency, Safety & AOT
- **Thread-Safety:** Every **Singleton** service (calculators, caches, `PostgresRepository`, `PostgresSqlProvider`, resilience pipelines, storage services) MUST be thread-safe. `PostgresRepository` opens per-call `NpgsqlConnection` via `await using` (no mutable instance state); static metadata/SQL caches must remain `ConcurrentDictionary`.
- **Lossless Fan-Out:** Never use native `BroadcastBlock<T>` in TPL Dataflow forks/broadcasts; use `DataflowFanOut` (`ActionBlock` + bounded `BufferBlock`s) for lossless delivery and end-to-end backpressure.
- **AOT & Trimming:** Projects set `<IsAotCompatible>true</IsAotCompatible>` (trim warnings suppressed in `Directory.Build.props`). Do **not** pollute public APIs with `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`; use `[DynamicallyAccessedMembers]` only when functionally required.

### Testing & Versioning
- **Tests:** xUnit 2.9.3 + Moq 4.20.72, AAA pattern (`[Fact]`, `[Theory]`), `FakeDataReader` for DB-free reader tests, exact string + `Assert.Same` cache checks for SQL generation tests.
- **Changelog:** SemVer following Keep a Changelog — record unreleased changes in `CHANGELOG.md` under `## [Unreleased]` (`Added`, `Changed`, `Fixed`, `Breaking`, `Removed`).
