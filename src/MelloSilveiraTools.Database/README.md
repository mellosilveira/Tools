# MelloSilveiraTools.Database

High-performance relational database persistence library built specifically for **PostgreSQL**, powered by **Npgsql** and **Dapper**.

> [!IMPORTANT]
> **Database Engine Support Notice:** This package exclusively targets **PostgreSQL**. All SQL generation routines, connection lifecycles, and data type mappings are optimized specifically for PostgreSQL.

---

## 1. Key Capabilities

- **Repository Abstraction (`IRepository`):** Stateless, thread-safe repository provider (`PostgresRepository`) managing per-operation connection lifecycles via asynchronous disposables (`await using`).
- **Attribute-Driven SQL Generation:** Generates optimized parameterized SQL queries dynamically from entity attributes (`[Table]`, `[PrimaryKeyColumn]`, `[Column]`, `[UniqueColumn]`, `[ForeignKeyColumn]`).
- **Dynamic Filter System (`[Filter]`):** Automatically builds parameterized `WHERE` clauses from strongly typed filter records decorating properties with `[FilterColumn]`.
- **Cached SQL Templates:** Static thread-safe caches (`ConcurrentDictionary`) eliminate SQL string concatenation overhead during runtime execution.
- **Dedicated Resilience Pipeline:** Integrates `PostgresResiliencePipeline` to handle transient network interruptions and connection retries via Polly v8.
- **Batch Operations:** Efficient single-statement bulk inserts and upserts (`ON CONFLICT DO UPDATE`).

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
  - **Singletons (thread-safe):** `DatabaseSettings`, `PostgresResiliencePipeline`, `ISqlProvider` (`PostgresSqlProvider`), `IRepository` (`PostgresRepository`).
- **Stateless Connections & Caching:** `PostgresRepository` opens a fresh `NpgsqlConnection` per call via `await using`. `PostgresSqlProvider` caches entity metadata and generated SQL in static `ConcurrentDictionary`s (`_metadataCache`, `_sqlCache`).
- **Two-Stage SQL Placeholders:** Static compile-time placeholders (`#TABLE_NAME`, `#COLUMNS`) remain strictly separated from runtime clauses (`#WHERE`, `#ORDERBY`, `#LIMIT`, `#OFFSET`). Batch parameter suffixes are **1-based** (`@Name_1`, `@Name_2`).
- **Key Operations:** `TryInsertAsync<TEntity>` (single-statement CTE returning `Result<long>`), `GetByUniqueColumnAsync<TEntity>`, multi-row `ON CONFLICT (...) DO UPDATE` (`BuildUniqueUpdates`).

---

## 5. Dependencies

- `MelloSilveiraTools.Core`
- `Npgsql` (10.0.3)
- `Dapper` (2.1.79)
- `Serilog.Sinks.Postgresql.Alternative` (4.3.0)
