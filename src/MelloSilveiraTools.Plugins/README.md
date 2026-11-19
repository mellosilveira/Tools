# MelloSilveiraTools.Plugins

Dynamic, storage-agnostic plugin engine for .NET 10, featuring collectible assembly load contexts, two-level caching, and background orchestrators.

> [!NOTE]
> **Domain Specialization:** In the current release, dynamic plugin loading is functional and specialized for mechanical constitutive models (`IMechanicalModelCalculator` and `ConstitutiveParameters`), allowing researchers to hot-load new material models into running systems at runtime without recompiling the host.

---

## 1. Key Capabilities

- **Isolated & Collectible Assembly Loading:**
  - Loads assemblies inside collectible `PluginLoadContext` instances, enabling memory to be reclaimed when plugins are unloaded or upgraded.
- **Shadow-Copy Loading (No File Locks):**
  - Copies plugin binaries to isolated shadow directories before loading via `PluginLoadContext.LoadFromAssemblyPath`, eliminating operating system file locks and supporting cloud object storage (AWS S3, Azure Blob, DFS).
- **Storage Partition Topology (`PluginStoragePartitions`):**
  - Decoupled from physical disk paths via `IFileStorageService`:
    - `incoming/`: Stage for uploaded or placed plugin binaries awaiting inspection.
    - `loaded/`: Active loaded plugin assemblies.
    - `archive/`: Deprecated versions evicted per retention policies.
    - `quarantine/`: Invalid binaries (failed naming pattern or non-.NET PE headers).
    - `cache/`: JSON cache persistence.
- **Strict PE Header & Metadata Inspection:**
  - Validates managed .NET PE headers via `PEReader.HasMetadata`.
  - Enforces semantic version naming: `{name}.v{major}.{minor}.{patch}.dll`.
- **Dynamic Dependency Injection:**
  - Supports both startup DI registration (`ForStartup`) and runtime hot-swap registration (`ForRuntime` via `IDynamicServiceProvider`).
- **Two-Level Caching & Persistence:**
  - Fast in-memory cache with persistence backing to JSON files or PostgreSQL tables.
- **Background Orchestrator (`PluginOrchestratorBackgroundService`):**
  - Polling loop driven by `PeriodicTimer` that automatically discovers, inspects, loads, promotes higher versions, and archives older plugins.

---

## 2. Installation

```bash
dotnet add package MelloSilveiraTools.Plugins
```

---

## 3. Quick Start

### Registering Plugin Services in Dependency Injection

```csharp
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

PluginSettings pluginSettings = new()
{
    RootPath = "plugins",
    ScanIntervalSeconds = 30,
    PreviousVersionRetentionHours = 24
};

services.AddPluginServices(pluginSettings);
```

---

## 4. Dependency Injection & Technical Invariants

- **DI Entry Points:** `PluginsDependencyInjection.AddPluginServices(pluginSettings)` and `AddPluginServicesAsync(pluginSettings, cancellationToken)`.
  - **Singletons:** `PluginSettings`, `PluginFileProcessor`, `PluginAssemblyProcessor`, `PluginCache`, `PluginValidator` variants; keyed: `JsonFilePluginCachePersistence` (`"file"`), `DatabasePluginCachePersistence` (`"database"`).
  - **Scoped:** `IPluginService`, `IPluginCachePersistence`, plugin commands (`UploadPlugin` / `POST /api/v1/plugins/upload`).
  - **Hosted:** `PluginOrchestratorBackgroundService` (`PeriodicTimer` polling loop, 24h default retention in `archive/`).
- **Storage Partitions & Validation:** Storage-agnostic partitions (`incoming/`, `loaded/`, `archive/`, `quarantine/`, `cache/` with `plugin-cache.json`) via `IFileStorageService`; validates managed PE metadata via `PEReader.HasMetadata` and filename regex `^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.dll$` (invalid binaries moved to `quarantine/`).
- **Lifecycle & Registration:** `DiscoveredPlugin` → `LoadedPlugin` → `RegisteredPlugin` (`IsFullyLoaded`, `FullyLoadedAt`) in isolated collectible `PluginLoadContext`; `PluginRegistrationContext.ForStartup(IServiceCollection)` mutates static DI while `ForRuntime(IDynamicServiceProvider)` enables hot-swap. Fundamentally non-AOT due to dynamic `AssemblyLoadContext` loading.

---

## 5. Dependencies

- `MelloSilveiraTools.WebApi`
- `MelloSilveiraTools.Core`
- `MelloSilveiraTools.Database`
