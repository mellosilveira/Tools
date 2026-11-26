# MelloSilveiraTools.Database

High-performance, Native AOT-compatible relational database persistence library built specifically for **PostgreSQL**, powered exclusively by **Npgsql**.

> [!IMPORTANT]
> **Database Engine Support Notice:** This package exclusively targets **PostgreSQL**. All SQL generation routines, `NpgsqlDataSource` connection pools, transaction-scoped RLS helpers, and data type mappings are optimized specifically for PostgreSQL.

---

## 1. Key Capabilities

- **Repository Abstraction (`IRepository`):** Stateless, thread-safe repository provider (`PostgresRepository`) executing every operation via `NpgsqlDataSource.CreateBatch()` in a **single network round-trip** (`SELECT set_config('app.current_user_id', @UserId, true)` + target command inside PostgreSQL's implicit extended-query transaction block, with zero explicit `BEGIN`/`COMMIT` round-trips).
- **Native AOT & Pure Npgsql:** Zero `Dapper` / IL-emit dependency. Builds the singleton `NpgsqlDataSource` via `NpgsqlSlimDataSourceBuilder` (`EnableTransportSecurity()`, `EnableArrays()`, `UseLoggerFactory(...)`) and annotates entity/filter generic parameters with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]`.
- **Attribute-Driven SQL Generation:** Generates optimized parameterized SQL queries dynamically from entity attributes (`[Table]`, `[PrimaryKeyColumn]`, `[Column]`, `[UniqueColumn]`, `[ForeignKeyColumn]`).
- **Dynamic Filter System (`[Filter]`):** Automatically builds parameterized `WHERE` clauses and strongly-typed `NpgsqlParameter` collections from filter records decorating properties with `[FilterColumn]`.
- **Cached SQL Templates:** Static per-type generic caches eliminate SQL string concatenation and reflection overhead during runtime execution.
- **Dedicated Resilience Pipeline:** Integrates `PostgresResiliencePipeline` to handle transient network interruptions, deadlocks, and serialization retries via Polly v8 — building `NpgsqlParameter` collections once per call and cloning via `NpgsqlParameter.Clone()` only on retries (zero reflection on retries, zero extra allocations on the first attempt).
- **Batch Operations:** Efficient single-statement bulk inserts and upserts (`ON CONFLICT DO UPDATE`), plus `NULL`-safe conflict resolution (`IS NOT DISTINCT FROM`) in `TryInsertAsync`.

---

## 2. Installation

```bash
dotnet add package MelloSilveiraTools.Database
```

---

## 3. Quick Start

### Defining an Entity and Filter

```csharp
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

[Table("specimens")]
public record SpecimenEntity : EntityBase
{
    [Column("name")]
    public required string Name { get; init; }

    [Column("tissue_type")]
    public required string TissueType { get; init; }
}

[Filter(typeof(SpecimenEntity))]
public record SpecimenFilter : FilterBase
{
    [FilterColumn(FilterClause.Equal, "TissueType")]
    public string? TissueType { get; init; }
}
```

### Querying with `IRepository`

```csharp
using MelloSilveiraTools.Database.Repositories;

public class SpecimenService(IRepository repository)
{
    public async Task<List<SpecimenEntity>> GetLigamentSpecimensAsync(CancellationToken ct)
    {
        SpecimenFilter filter = new() { TissueType = "ACL" };
        return await repository
            .GetAsync<SpecimenEntity, SpecimenFilter>(filter, cancellationToken: ct)
            .ToListAsync(ct);
    }
}
```

---

## 4. Dependency Injection & Technical Invariants

- **DI Entry Point:** `DatabaseDependencyInjection.AddDatabaseServices(defaultResiliencePipelineSettings, databaseSettings)`.
  - **Singletons (thread-safe):** `DatabaseSettings`, `NpgsqlDataSource` (built via `NpgsqlSlimDataSourceBuilder` and disposed by the DI container on shutdown), `PostgresResiliencePipeline`, `ISqlProvider` (`PostgresSqlProvider`), `IRepository` (`PostgresRepository`).
- **Single-Round-Trip RLS & Pooling:** `PostgresRepository` uses `dataSource.CreateBatch()` to pipeline `SELECT set_config('app.current_user_id', @UserId, true)` with the target statement(s) in a single extended-query batch; PostgreSQL executes the batch atomically in an implicit transaction block and resets `app.current_user_id` automatically before the connection returns to the pool.
- **Two-Stage SQL Placeholders:** Static compile-time placeholders (`#TABLE_NAME`, `#COLUMNS`) remain strictly separated from runtime clauses (`#WHERE`, `#ORDERBY`, `#LIMIT`, `#OFFSET`). Batch parameter suffixes are **1-based** (`@Name_1`, `@Name_2`).
- **Key Operations:** `TryInsertAsync<TEntity>` (single-statement CTE with `IS NOT DISTINCT FROM` returning `Result<long>`), `GetByUniqueColumnAsync<TEntity>`, multi-row `ON CONFLICT (...) DO UPDATE` (`BuildUniqueUpdates`).

---

## 5. Dependencies

- `MelloSilveiraTools.Core`
- `Npgsql` (10.0.3)
- `Serilog.Sinks.Postgresql.Alternative` (4.3.0)
