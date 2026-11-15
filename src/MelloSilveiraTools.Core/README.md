# MelloSilveiraTools.Core

The foundational infrastructure library of the **MelloSilveiraTools** suite. It provides resilient streaming pipelines, caching mechanisms, cloud/local file management, Result monads, Polly v8 resilience, and core utility patterns.

---

## 1. Key Capabilities

- **Streaming Pipelines (TPL Dataflow):** Build concurrent, backpressure-aware processing graphs with fork/join branching, broadcast observers, dead-letter queues (DLQ), and DAG progress telemetry (`PipelineFactory.StartDataflow<T>`).
- **Sequential Fluent Pipelines:** Build lightweight, pull-based request/response pipelines with type-safe transformations (`PipelineFactory.StartFluent<T>`).
- **Result Monad (`Result<T>`):** Functional error handling eliminating exceptions for business validation failures (`Result.CreateSuccessOk()`, `Result.CreateBadRequest()`, `Result.CreateNotFound()`), with HTTP projection extensions.
- **Storage-Agnostic File Management (`IFileStorageService`):** Uniform file operations across `Local`, `AWS S3`, `Azure Blob Storage`, and `Distributed File System (DFS)` providers.
- **High-Throughput CSV Streaming (`CsvFileWriter`):** High-performance CSV writer utilizing 128 KB buffering, batch flushing (1,000 rows), and culture-invariant numeric serialization (`CultureInfo.InvariantCulture`).
- **Resilience Pipelines (Polly v8):** Zero-allocation retry and rate-limiting pipelines configured via `ResiliencePipelineSettings`.
- **In-Memory Caching:** Single-level (`ISingleLevelCache`) and two-level (`ITwoLevelCache`) thread-safe in-memory caches.
- **Security & Encryption:** Standard PBKDF2 key derivation (`Rfc2898DeriveBytes`) with cryptographically secure salt generation.

---

## 2. Installation

```bash
dotnet add package MelloSilveiraTools.Core
```

---

## 3. Quick Start

### Result Monad Example

```csharp
using MelloSilveiraTools.Core.Models;

public Result<double> CalculateCompliance(double strain, double stress)
{
    if (stress <= 0)
        return Result.CreateBadRequest("Stress must be positive.");

    double compliance = strain / stress;
    return Result.CreateSuccessOk(compliance);
}
```

### TPL Dataflow Pipeline Example

```csharp
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using Microsoft.Extensions.Logging.Abstractions;

PipelineFactory factory = new(NullPipelineProgressNotifier.Instance, TimeProvider.System);

IDataflowPipeline<int> pipeline = factory
    .StartDataflow<int>(NullLogger.Instance, NullPipelineProgressNotifier.Instance)
    .AddStep("MultiplyByTwo", (int x) => x * 2)
    .AddStep("FilterEven", (int x) => x % 4 == 0)
    .BuildTerminal();

await pipeline.SendAsync(10);
await pipeline.Complete();
```

---

## 4. Dependencies

- `Polly` (8.7.0)
- `Polly.RateLimiting` (8.7.0)
- `Microsoft.Extensions.Logging` (10.0.11)
- `Serilog.Extensions.Logging` (10.0.0)
- `Serilog.Sinks.File` (7.0.0)
