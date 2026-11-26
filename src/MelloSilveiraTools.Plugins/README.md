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
    - `pending/`: Isolated staging partition for authenticated community plugin submissions awaiting superuser review (never auto-loaded by the background orchestrator).
    - `rejected/`: Community submissions rejected during moderation.
    - `incoming/`: Approved plugin binaries awaiting runtime discovery and loading.
    - `loaded/`: Active loaded plugin assemblies.
    - `archive/`: Deprecated versions evicted per retention policies.
    - `quarantine/`: Invalid binaries (failed naming pattern, mixed-mode/non-IL-only, or incompatible PE headers).
    - `cache/`: JSON cache persistence.
- **Model 2 Community Security & Governance:**
  - **Public consumption (`GET /api/v1/plugins`):** Open to all users (anonymous and authenticated); reads exclusively from in-memory `PluginCache` and emits sanitized `PublicPluginInfo` DTOs without internal server paths (`FullPath`).
  - **Authenticated community submissions (`POST /submissions`, `GET /submissions`, `DELETE /submissions/{id}`):** Regular authenticated users submit into `pending/` via `SubmitPlugin` with SHA-256 hashing, per-plugin author ownership enforcement (anti-version-hijacking), `MaxPluginSizeBytes`, `MaxPendingSubmissionsPerUser`, and dedicated upload rate limiting; authors may cancel only their own `Pending` submissions.
  - **Superuser direct publish & moderation (`POST /upload`, `POST /submissions/{id}/approve`, `/reject`):** Superusers publish directly into `incoming/` via `UploadPlugin` (auto-approved, `SuperuserOnly`) or review community submissions with constant-time SHA-256 anti-TOCTOU verification, atomic promotion `pending/` → `incoming/`, and immediate runtime loading.
  - **Strict PE & Unmanaged Isolation:** Enforces `PEReader.GetMetadataReader().IsAssembly`, `CorFlags.ILOnly` (unless `AllowUnmanagedDlls = true`), and disables native library resolution in `PluginLoadContext` by default.
  - **PostgreSQL RLS & Normalized File Persistence:** `plugin_cache` (public read, superuser write) and `plugin_submissions` referencing `storage_entry_metadata(id)` via `storage_entry_metadata_id` (`ON DELETE RESTRICT`, zero duplicated file metadata): `Pending` binaries remain private to the author in `storage_entry_metadata`, while approved/superuser binaries promote `storage_entry_metadata.user_id = NULL` (public read/download for all users, edit/delete restricted strictly to superusers, preventing post-approval author tampering).
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
