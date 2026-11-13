# MelloSilveiraTools.Database.Migrator

Contextual, versioned DDL database schema migration utility powered by **DbUp**, specifically designed for the **MelloSilveiraTools** ecosystem.

> [!IMPORTANT]
> **Database Engine Support Notice:** This application and the **MelloSilveiraTools** suite exclusively support **PostgreSQL**. All DDL scripts, data types, journal tables, and generated SQL queries are designed and optimized strictly for the PostgreSQL relational database engine.

---

## 1. Overview and Architecture

The `MelloSilveiraTools.Database.Migrator` project was developed as an independent CLI tool and migration library to satisfy the following engineering requirements:

1. **Dependency Isolation:** The production library `MelloSilveiraTools.Database` remains lightweight and clean, with zero runtime dependency on `DbUp`.
2. **Domain-Contextual Migrations (`MigrationContext`):** Enables provisioning only the database tables required for a specific subsystem or operational scenario:
   * **`All`**: Applies all embedded migration scripts across the ecosystem (`Common`, `Optimizations`, and `Plugins`).
   * **`Common`**: Applies essential cross-cutting tables, such as `stored_files` (partitioned file metadata audit).
   * **`Optimizations`**: Applies tables for the biomechanical constitutive model fitting and sensitivity analysis pipelines (`mechanical_model_curve_fit_outputs`, `mechanical_model_simulations`, and `parametric_sweeps`), automatically including the `Common` prerequisites.
   * **`Plugins`**: Applies persistence tables for dynamic plugin runtime caches (`plugin_cache`).
3. **Declarative and Idiomatic SQL Scripts:**
   * Exclusively utilizes **pure `TEXT`** for textual columns and opaque JSON audit/traceability payloads, taking full advantage of PostgreSQL's `varlena` storage layout and native transparent TOAST compression (LZ4/PGLZ).
   * Idempotent DDL execution (`CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`, and named constraints).
4. **Version Control and Journaling:** DbUp manages the `public.schemaversions` journal table, ensuring that no migration script is executed twice and tracking the exact timestamp of each application.

---

## 2. Quick Start with Docker

The repository root includes a pre-configured `docker-compose.yml` file running the official **`postgres:17-alpine`** container image.

### Step 1: Start PostgreSQL via Docker Compose
```powershell
docker compose up -d postgres
```

### Step 2: Execute Contextual DDL Migrations
Run the migrator specifying the desired target context:

```powershell
# Provision tables for Biomechanical Optimizations (includes Common)
dotnet run --project src/MelloSilveiraTools.Database.Migrator -- --context Optimizations

# Provision the entire database schema for all packages
dotnet run --project src/MelloSilveiraTools.Database.Migrator -- --context All

# Provision tables exclusively for dynamic plugin cache persistence
dotnet run --project src/MelloSilveiraTools.Database.Migrator -- --context Plugins
```

---

## 3. Command-Line Interface (CLI) Reference

```
Usage: dotnet run --project src/MelloSilveiraTools.Database.Migrator -- [options]

Options:
  -c, --context <Context>            Target migration context: All (default), Common, Optimizations, Plugins.
  -s, --connection-string <String>   PostgreSQL connection string (defaults to CONNECTION_STRING env var or localhost).
  -t, --timeout <Seconds>            Per-script execution timeout in seconds (default: 60).
  -h, --help                         Display this help message.
```

---

## 4. Embedded Scripts Hierarchy

All migration scripts reside in `Scripts/` and are compiled into the assembly as `EmbeddedResource`:

```
Scripts/
├── Common/
│   └── 0001_create_stored_files.sql
├── Optimizations/
│   ├── 0001_create_mechanical_model_curve_fit_outputs.sql
│   ├── 0002_create_mechanical_model_simulations.sql
│   └── 0003_create_parametric_sweeps.sql
└── Plugins/
    └── 0001_create_plugin_cache.sql
```

---

## 5. Integration with `ISqlProvider` and `PostgresRepository`

The generated database schema strictly mirrors the domain entities in the codebase:
* `stored_files` $\leftrightarrow$ `StorageEntryMetadataEntity`
* `mechanical_model_curve_fit_outputs` $\leftrightarrow$ `MechanicalModelCurveFitOutputEntity`
* `mechanical_model_simulations` $\leftrightarrow$ `MechanicalModelSimulationEntity`
* `parametric_sweeps` $\leftrightarrow$ `ParametricSweepEntity`
* `plugin_cache` $\leftrightarrow$ `PluginCacheEntity`

The `PostgresSqlProvider` in the `MelloSilveiraTools.Database` package dynamically generates parameterized `INSERT`, `SELECT`, `UPDATE`, and `DELETE` SQL statements that are 100% compatible with the schema provisioned by this migrator.
