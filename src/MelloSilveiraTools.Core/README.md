# MelloSilveiraTools.Core

The foundational infrastructure library of the **MelloSilveiraTools** suite. It provides resilient streaming pipelines, caching mechanisms, cloud/local file management, Result monads, Polly v8 resilience, and core utility patterns.

---

## 1. Key Capabilities

- **Streaming Pipelines (TPL Dataflow):** Build concurrent, backpressure-aware processing graphs with fork/join branching, lossless fan-out (`DataflowFanOut`), broadcast observers, dead-letter queues (DLQ), and DAG progress telemetry (`PipelineFactory.CreateDataflow<T>`).
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
    .CreateDataflow<int>(NullLogger.Instance, "sample-pipeline", initialBufferSize: 16)
    .AddDataMapping(x => x * 2)
    .AddFilterStep(x => x % 4 == 0)
    .BuildTerminal();

await pipeline.SendAsync(10);
pipeline.Complete();
await pipeline.Completion;
```

---

## 4. Dependency Injection & Technical Invariants

- **DI Entry Points:** `CoreDependencyInjection.AddCoreServices(defaultResiliencePipelineSettings, encryptionSettings, emailSettings, fileStorageSettings, loggerSettings, pipelineProgressNotifierSettings)`, `AddCoreLogging(loggerSettings)`, and `AddFileStorageHealthCheck(name, tags)` (both on `IHealthChecksBuilder` and `IServiceCollection`).
  - **Singletons (thread-safe):** `TimeProvider.System`, `ICurrentUserProvider`, `PipelineFactory`, `IFileStorageService` (default `LocalFileStorageService` at `%TEMP%/storage` + keyed `StorageProvider.*`, path topology `{BaseDirectory}/{context}/{identifier}/{fileName}`, fail-closed `IsHealthAsync` probes + `FileStorageHealthCheck`), `ISingleLevelCache`, `ITwoLevelCache`, `ServiceLocator`, `IDynamicServiceProvider` (`InMemoryDynamicServiceProvider` using `Lazy<object>`), resilience pipelines (`Smtp`, `BlobStorage`, `AwsS3`), `IPipelineProgressNotifier` (`ChannelPipelineProgressNotifier` or `NullPipelineProgressNotifier.Instance`).
  - **Singletons / Scoped Services:** `IEncryptionService` (`Singleton`), `IEmailService` (`Scoped`).
- **Result Monad & Command Pattern:** `ResultBase` → `Result` → `Result<T>` → `ListedResult<T>` → `PagedResult<T>` with factories (`Result.CreateSuccessOk`, `CreateBadRequest`, `CreateUnauthorized`, `CreateForbidden`, `CreateNotFound`, `CreateServiceUnavailable`), fluent validation (`.AddErrorIf()`, `.AddErrorIfNull()`, `.AddErrorIfNegativeOrZero()`), branching (`.OnSuccess()`, `.OnError()`, `.Match()`), and HTTP projections (`.ToHttpResult()`, `.BuildHttpResponse()`). `CommandBase<TRequest, TResult>` / `CommandBaseWithoutRequest<TResult>` with async `IValidator<TRequest>` (`ValueTask<Result> ValidateAsync(...)`); `*Request` DTOs never carry `CancellationToken`.
- **Pipelines (`IDataflowPipeline` & `IFluentPipeline`):**
  - *TPL Dataflow (`DataflowFanOut`):* Forks and broadcasts intentionally avoid native `BroadcastBlock<T>` (lossy last-value-wins) and dispatch sequentially via `ActionBlock` + `SendAsync` to bounded `BufferBlock` targets to guarantee 100% delivery, end-to-end backpressure, selective cloning (`i > 0`), and deterministic completion/fault propagation (`DataflowExtensions.LinkTo` defaults to `PropagateCompletion = true`).
  - *Fluent (`IFluentPipeline<TIn, TOut>`):* Pull-based sequential chains via `AddStep` (sync, async, `IAsyncEnumerable`) and `AddDataMapping`.
- **Low-Allocation Primitives & CSV Composition:** `SpanStringBuilder` (`ref struct` + `ArrayPool<char>.Shared`, stack-only across non-`await` scopes), `CsvStreamReader` (`PipeReader` + `MemoryPool<byte>.Shared` + `stackalloc` ≤ 512 B), `CsvFileWriter` (composition over inheritance, UTF-8 without BOM, 128 KB `StreamWriter` buffer, 16 KB row `StringBuilder`, 1,000-row batch flush, `CultureInfo.InvariantCulture`, `IAsyncDisposable`, `CsvFileWriter.DisposeAllAsync`), `DefaultResiliencePipeline` (`ResilienceContextPool.Shared` + `[CallerMemberName]`), `EnumerableExtensions.ForeachAsync` (`SemaphoreSlim` bounded parallelism).

---

## 5. Dependencies

- `Microsoft.AspNetCore.App` (FrameworkReference)
- `AWSSDK.S3` (4.1.0)
- `Azure.Storage.Blobs` (12.30.1)
- `Polly` (8.9.1)
- `Polly.RateLimiting` (8.9.1)
- `Serilog.Enrichers.Environment` (3.0.1)
- `Serilog.Extensions.Logging` (10.0.0)
- `Serilog.Sinks.File` (7.0.0)
- `Serilog.Sinks.Postgresql.Alternative` (4.3.0)
