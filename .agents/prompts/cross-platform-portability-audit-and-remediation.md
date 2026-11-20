# Cross-Platform & Multi-Architecture (.NET 10) Portability Audit & AI Remediation Guide

[Role: Principal .NET Systems Architect & Cross-Platform Runtime Specialist]
[Target Framework: .NET 10 (`net10.0`)]
[Target Infrastructure: Windows & Linux (glibc/musl) across x86, x64, Arm32, Arm64]
[Repository Context: c:\Pessoal\Projetos\Tools]

---

## 1. Executive Context & Platform Boundaries

### 1.1 Fundamental Platform Boundaries (.NET 10 Runtime Matrix)
An application or library targeting .NET 10 cannot alter vendor-defined platform lifecycle support. The host environment matrix is strictly defined as follows:

| Runtime Identifier (RID) | Supported by .NET 10 | Architectural Notes |
|---|---|---|
| `win-x64`, `win-arm64` | YES | Primary 64-bit Windows platforms. Full support. |
| `win-x86` | YES | Sole remaining 32-bit Windows platform. Useful for legacy COM / 32-bit processes. |
| `win-arm` (Arm32) | NO | Discontinued since .NET 6. Cannot run on Windows Arm32. |
| `linux-x64`, `linux-arm64` | YES | Tier-1 support across glibc (Ubuntu/Debian/RHEL) and musl (Alpine). |
| `linux-arm` (Arm32 / armv7) | YES | Sole remaining 32-bit Linux platform (Raspberry Pi OS, Debian 32-bit, Alpine). |
| `linux-x86` (i386) | NO | Never supported on .NET Core/.NET 5+. |

> **Operational Directive for AI Agents:** When evaluating "x32" compatibility, enforce constraints solely for `win-x86` and `linux-arm`. Do not attempt to produce builds for `linux-x86` or `win-arm`.

### 1.2 Solution Architecture & Existing Sound Decisions
- **Compilation Output:** All libraries build as AnyCPU IL-only (`Platform=AnyCPU`, no hardcoded `PlatformTarget` or single `RuntimeIdentifier` in package csproj files).
- **Native Interop Purity:** Zero custom `[DllImport]`, `LibraryImport`, `unsafe` pointer arithmetic, or CPU-specific intrinsics (`Avx2`, `AdvSimd`) in custom tool packages.
- **ALGLIB Isolation:** Preprocessor flag `ALGLIB_USE_SIMD` is not defined in project files, ensuring ALGLIB uses fallback scalar algorithms that run cleanly across x86, x64, Arm32, and Arm64 without AVX-only faults.
- **Invariant Ingestion:** [CsvStreamReader.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/CsvStreamReader.cs) implements zero-allocation parsing via `System.Buffers.Text.Utf8Parser`, operating culture-independently and supporting both LF and CRLF line endings.
- **Cryptographic Portability:** PBKDF2 and SHA-256 rely on BCL abstractions (`RandomNumberGenerator`, `SHA256`, `KeyDerivation`), automatically binding to CNG on Windows and OpenSSL on Linux.

---

## 2. Remediation Registry (Categorized by Severity)

### Priority 1: Critical (Must Fix Immediately)

#### [FINDING-01] Premature File Operation with Open File Handle
- **Target File:** [JsonFilePluginCachePersistence.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Plugins/Infrastructure/Persistences/JsonFilePluginCachePersistence.cs#L31-L45)
- **Severity:** CRITICAL (Platform Divergence / Definite Windows Runtime Failure)
- **Root Cause:** `FileStream stream = File.Create(tempPath)` is held open by `await using` scope while `File.Move(tempPath, FilePath, overwrite: true)` is executed. Windows kernel enforces strict mandatory file sharing locks (`ERROR_SHARING_VIOLATION`), causing 100% failure on Windows. Linux POSIX `rename(2)` allows renaming an unlinked open descriptor, masking the bug in Linux-only CI runs.
- **Remediation Specification:**
  Scope the stream disposal before calling `File.Move`, and enforce disk flush before atomic rename.
```csharp
// BEFORE (Buggy):
await using FileStream stream = File.Create(tempPath);
await using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });
// ... writing ...
await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
File.Move(tempPath, FilePath, overwrite: true); // Throws IOException on Windows!

// AFTER (Compliant):
string tempPath = FilePath + ".tmp";
await using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
{
    await using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
    {
        writer.WriteStartArray();
        await foreach (PluginCacheEntry entry in entries.WithCancellation(cancellationToken))
        {
            JsonSerializer.Serialize(writer, ToDto(entry), JsonOptions);
        }
        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
    stream.Flush(flushToDisk: true);
}
File.Move(tempPath, FilePath, overwrite: true);
```

---

#### [FINDING-02] Leaking ASP.NET Core Runtime Dependency into Base Computational Packages
- **Target Files:**
  - [MelloSilveiraTools.Core.csproj](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/MelloSilveiraTools.Core.csproj)
  - [MelloSilveiraTools.MechanicsOfMaterials.Optimizations.csproj](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/MelloSilveiraTools.MechanicsOfMaterials.Optimizations.csproj)
- **Severity:** CRITICAL (Host Environment Restriction)
- **Root Cause:** Both projects declare `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. Any non-web host (Console application, Windows Service, background worker daemon, WinForms/WPF, or lean Docker containers based on `mcr.microsoft.com/dotnet/runtime:10.0`) crashes on startup with:
  `Framework 'Microsoft.AspNetCore.App', version '10.0.0' was not found.`
- **Remediation Specification:**
  1. Remove `<FrameworkReference Include="Microsoft.AspNetCore.App" />` from `MelloSilveiraTools.Core.csproj`.
  2. Move HTTP/MVC Result extensions (`ToHttpResult`, `BuildHttpResponse`) from `MelloSilveiraTools.Core` into `MelloSilveiraTools.WebApi` or a dedicated bridge package (`MelloSilveiraTools.Core.AspNetCore`).
  3. Replace `Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2` in [EncryptionService.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Services/Encryption/EncryptionService.cs) with standard BCL `System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2`, eliminating the need for `Microsoft.AspNetCore.Cryptography.KeyDerivation`.
  4. In `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`, split database-dependent persistence steps into a distinct provider or reference `Microsoft.AspNetCore.App` only where Web hosting is actually required.

---

#### [FINDING-03] False AOT/Trimming Guarantees Suppressed by MSBuild Warnings
- **Target Files:**
  - [Directory.Build.props](file:///c:/Pessoal/Projetos/Tools/Directory.Build.props#L52)
  - All package `.csproj` files (`<IsAotCompatible>true</IsAotCompatible>`)
- **Severity:** CRITICAL (Runtime Crash on NativeAOT Hosts)
- **Root Cause:** `Directory.Build.props` globally suppresses trimming and AOT diagnostic codes (`IL2026`, `IL2046`, `IL2050`, `IL2067`, `IL2070`, `IL2072`, `IL2075`, `IL2077`, `IL2080`, `IL2087`, `IL2090`, `IL2091`, `IL2092`, `IL2093`, `IL2094`, `IL2095`, `IL2111`, `IL3000`, `IL3050`, `IL3051`, `IL3052`). Concurrently, packages claim `<IsAotCompatible>true`. Under NativeAOT (`dotnet publish -r <rid> -c Release /p:PublishAot=true`), critical features fail catastrophically:
  - `MelloSilveiraTools.Plugins`: `AssemblyLoadContext.LoadFromAssemblyPath` throws `PlatformNotSupportedException` at runtime under NativeAOT.
  - `MelloSilveiraTools.Database`: Dapper depends on dynamic IL emission via `System.Reflection.Emit`; `DbDataReaderExtensions` uses unbounded reflection.
  - `MelloSilveiraTools.MechanicsOfMaterials`: [MechanicalModelTypeCache.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Caching/MechanicalModelTypeCache.cs#L60-L108) relies on runtime `MakeGenericMethod` and `Expression.Compile()`.
  - JSON serialization: Dynamic polymorphic calls (`JsonSerializer.Serialize(value, value.GetType())`) fail without code-generated `JsonSerializerContext`.
- **Remediation Specification:**
  1. Remove all `IL*` suppressions from `Directory.Build.props` `<NoWarn>`.
  2. Set `<IsAotCompatible>false</IsAotCompatible>` on `MelloSilveiraTools.Plugins`, `MelloSilveiraTools.Database`, and `MelloSilveiraTools.WebApi`.
  3. Annotate unsafe dynamic methods with `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`.
  4. For numerical and calculation packages (`Mathematics`, `MechanicsOfMaterials`), replace dynamic runtime code generation (`Expression.Compile`) with compiled polymorphic factories or AOT source generators.

---

### Priority 2: High (Architecture & OS Divergence)

#### [FINDING-04] Dynamic Plugin Loading Lacks Architecture & Native Dependency Resolution
- **Target File:** [PluginAssemblyProcessor.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Plugins/Infrastructure/PluginAssemblyProcessor.cs#L28-L38)
- **Severity:** HIGH (Multi-Architecture / Multi-Platform Failure)
- **Root Cause:** A bare `AssemblyLoadContext` is instantiated without an `AssemblyDependencyResolver` and without overriding `Load` / `LoadUnmanagedDll`.
  - If a plugin has third-party managed dependencies in its directory, resolution fails with `FileNotFoundException`.
  - If a plugin includes native binaries targeting specific RIDs (`runtimes/linux-arm64/native/libfoo.so`, `runtimes/win-x64/native/foo.dll`), they will never be located.
  - If a user drops an x64-compiled native binary or platform-specific assembly into an Arm64 or x86 host, execution crashes with `BadImageFormatException`.
- **Remediation Specification:**
  Implement a custom `PluginLoadContext` backed by `AssemblyDependencyResolver` and validate PE headers using `System.Reflection.PortableExecutable`:
```csharp
internal sealed class PluginLoadContext(string pluginPath)
    : AssemblyLoadContext($"PluginContext_{Path.GetFileNameWithoutExtension(pluginPath)}", isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Prevent loading duplicate host contracts into the child context
        if (Default.Assemblies.Any(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName)))
        {
            return null;
        }

        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is not null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath is not null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
```

---

#### [FINDING-05] OS File Locking Discrepancy & Path Bug in Plugin Orchestrator
- **Target File:** [PluginFileProcessor.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Plugins/Infrastructure/PluginFileProcessor.cs#L53-L90)
- **Severity:** HIGH (Cross-OS Behavior Mismatch & Functional Bug)
- **Root Causes:**
  1. **Logical Bug in `MoveToMainFolder` (Line 55):**
     `string destination = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));`
     Moving back to main folder erroneously targets `_loadedDirectory` instead of `_pluginsDirectory`.
  2. **File Locking Differences:** On Windows, `LoadFromAssemblyPath` memory-maps the DLL with an active lock. Calling `ReplaceInLoadedFolder` -> `File.Delete` throws `IOException`/`UnauthorizedAccessException` because ALC unloading is cooperative and dependent on garbage collection. On Linux, unlinking an open file succeeds immediately, but the running process continues executing the old inode.
  3. **Torn Writes on Linux:** On Linux, if a deploy script copies a large assembly directly to the watched directory, the background orchestrator reads the file before writing finishes, resulting in `BadImageFormatException`.
- **Remediation Specification:**
  - Fix `MoveToMainFolder` to target `_pluginsDirectory`.
  - Implement *Shadow Copying* for plugin execution: load assemblies from a temporary scratch directory or via `MemoryStream`/byte arrays.
  - Enforce atomic deployment convention (write to `.tmp` then rename to `.dll`) or verify file size/mtime stability over consecutive timer ticks before attempting ingestion.

---

#### [FINDING-06] Working-Directory-Dependent Storage and Read-Only Root Collisions
- **Target Files:**
  - [FileStorageSettings.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/FileStorageSettings.cs#L11)
  - [LoggerSettings.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Logging/LoggerSettings.cs#L21)
- **Severity:** HIGH (Container & Service Host Failures)
- **Root Cause:**
  - `FileStorageSettings.BaseDirectory = "storage"` is relative to the current working directory (CWD). When running as a Windows Service, CWD is `C:\Windows\System32`. When running as a systemd service, CWD is `/`. Both result in unauthorized access or corrupted system roots.
  - `LoggerSettings.Directory = Path.Combine(AppContext.BaseDirectory, "logs")`. When deployed in `/usr/lib/<app>` on Linux, `C:\Program Files` on Windows, or in read-only Kubernetes containers (`readOnlyRootFilesystem: true`), initialization fails immediately.
- **Remediation Specification:**
  Canonicalize relative paths against a safe fallback location (`Environment.SpecialFolder.LocalApplicationData` or explicit environment variables) and document mandatory configuration for headless/container deployments.

---

#### [FINDING-07] Unsanitized File Names & Platform-Specific Path Separators
- **Target Files:**
  - [LocalFileStorageService.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/LocalFileStorageService.cs#L13-L21)
  - [MechanicalModelOutputFileWriterStep.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelOutputFileWriterStep.cs#L38)
- **Severity:** HIGH (Security / Cross-OS Path Traversal)
- **Root Cause:** External user identifiers are concatenated directly into paths:
  `Path.Combine(_settings.BaseDirectory, context, identifier, fileName)`
  - `Path.GetInvalidFileNameChars()` is platform-dependent: on Linux it only returns `\0` and `/`. If validation runs on Linux, characters like `:`, `*`, `?`, `"`, `<`, `>`, `|` or reserved device names (`CON`, `PRN`, `AUX`, `NUL`) pass validation, but crash when synchronized or run on Windows.
  - If `identifier` or `context` contains `../` or `..\`, path traversal occurs outside the storage root.
- **Remediation Specification:**
  Apply strict regex validation `^[a-zA-Z0-9_\-\.]+$` on all identifiers and context strings, and verify `Path.GetFullPath(resolvedPath).StartsWith(Path.GetFullPath(baseDirectory))` before any I/O operation.

---

#### [FINDING-08] Linux Case-Sensitive Filesystem Collisions
- **Target Files:**
  - [PluginFileProcessor.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Plugins/Infrastructure/PluginFileProcessor.cs#L34-L46)
  - [LocalFileStorageService.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/LocalFileStorageService.cs)
- **Severity:** HIGH (Functional Failure on Linux)
- **Root Cause:**
  - Windows file systems (NTFS) are case-insensitive. Linux file systems (ext4, XFS, btrfs) are case-sensitive by default.
  - `Directory.GetFiles(_pluginsDirectory, $"{pluginName}*.dll")` on Linux fails to match `Plugin.DLL` or casing variations.
  - In storage, `"ExperimentalData"` and `"experimentaldata"` create two distinct directories on Linux, but merge on Windows, causing file listing and discovery desynchronization.
- **Remediation Specification:**
  Pass `EnumerationOptions` with `MatchCasing = MatchCasing.CaseInsensitive` when scanning files:
```csharp
private static readonly EnumerationOptions CaseInsensitiveOptions = new()
{
    MatchCasing = MatchCasing.CaseInsensitive,
    RecurseSubdirectories = false,
    IgnoreInaccessible = true
};
```

---

#### [FINDING-09] Non-Deterministic Simulation Hashes Across Architectures and Runtimes
- **Target Files:**
  - [IdentifierBuilderStep.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/IdentifierBuilderStep.cs#L21-L23)
  - [MechanicalModelOutputPersistenceStep.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelOutputPersistenceStep.cs#L37-L38)
  - [AlglibCurveFitter.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/CurveFitting/Algorithms/AlglibCurveFitter.cs#L52-L59)
- **Severity:** HIGH (Scientific Integrity / Cache & Database Key Corruption)
- **Root Cause:**
  1. **Unseeded Random Number Generator in ALGLIB:** `alglib.mindfcreate` defaults to an unseeded system RNG. The ALGLIB manual explicitly states: *"By default, a random seed is used... the solver returns non-deterministic results."* GDEMO runs yield different parameters every run, producing distinct SHA-256 hashes for identical inputs.
  2. **Floating-Point Libm Discrepancies:** Transcendental functions (`Math.Exp`, `Math.Pow`) rely on platform standard C library implementations (`ucrtbase.dll` on Windows vs `glibc`/`musl` on Linux). Low-order bits (ULP) naturally diverge across x64 and Arm64 compilers.
  3. Formatting floats into JSON strings before hashing propagates minute floating-point variances into completely different SHA-256 hex digests.
- **Remediation Specification:**
  1. Add a configurable `Seed` property to `CurveFitterOptions` and explicitly call `alglib.mindfsetseed(state, options.Seed)`.
  2. Construct entity hashes from **canonical input parameters** (raw experimental dataset hash + model specification + optimization options), rather than calculated floating-point outputs.
  3. When serializing numeric outputs for deterministic comparison, round to strict decimal boundaries or use binary IEEE 754 representations with relative tolerances (`MathNet.Numerics.Precision`).

---

### Priority 3: Medium (Data Integrity & Edge Cases)

#### [FINDING-10] Culture-Sensitive Parsing in Vector3D
- **Target File:** [Vector3D.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Mathematics/Models/Vector3D.cs#L98-L126)
- **Severity:** MEDIUM (Localization Failure)
- **Root Cause:** `Vector3D.Parse` calls `double.Parse(vec[0])` without passing `CultureInfo.InvariantCulture`. In regions where comma is the decimal separator (e.g. `pt-BR`, `de-DE`), parsing `"1.5,2.0,3.0"` either throws `FormatException` or corrupts numbers. In contrast, `Point3D` correctly uses `CultureInfo.InvariantCulture`.
- **Remediation Specification:** Replace all `double.Parse(str)` with `double.Parse(str, CultureInfo.InvariantCulture)`. Enable .NET analyzers `CA1305` and `CA1304` as compilation warnings.

---

#### [FINDING-11] Absolute File System Paths Persisted to Shared Database
- **Target Files:**
  - [MechanicalModelOutputPersistenceStep.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelOutputPersistenceStep.cs#L65-L73)
  - `StoredFileEntity.Location`
- **Severity:** MEDIUM (Distributed / Mixed Fleet Breakdown)
- **Root Cause:** Physical paths like `C:\storage\ExperimentalData\sim.csv` are written into the PostgreSQL database. If a Linux worker or Docker container attempts to process that entity, the path is completely unresolvable. Furthermore, [FileMetadata.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/FileMetadata.cs#L53) parses directories using `LastIndexOfAny(['/', '\\'])`, which fails on Linux where `\` is a valid filename character.
- **Remediation Specification:** Store only normalized virtual storage keys (`{context}/{identifier}/{fileName}`) using forward slashes (`/`). Resolve physical paths dynamically at runtime via `IFileStorageService.ResolvePath`.

---

#### [FINDING-12] Platform-Dependent Newline Characters in Generated Data Artifacts
- **Target Files:**
  - [CsvFileWriter.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/CsvFileWriter.cs#L28-L49)
  - [SpanStringBuilder.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Models/SpanStringBuilder.cs#L150-L160)
- **Severity:** MEDIUM (Artifact Determinism & Checksums)
- **Root Cause:** Emits `Environment.NewLine` (`\r\n` on Windows, `\n` on Linux). Files generated from identical simulation inputs yield different byte sizes and divergent SHA-256 hashes depending on the host OS.
- **Remediation Specification:** Standardize on standard CRLF (`\r\n`) per RFC 4180 or LF (`\n`) across all environments, configurable via `FileWriterOptions.NewLine`.

---

#### [FINDING-13] Insecure Timestamp Filename Generation with Silent Overwrite
- **Target Files:**
  - [LocalFileStorageService.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/LocalFileStorageService.cs#L27-L44)
  - [MechanicalModelOutputFileWriterStep.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelOutputFileWriterStep.cs#L38)
- **Severity:** MEDIUM (Data Loss Risk)
- **Root Cause:** Files are named using `${prefix}_${DateTime.Now:yyyy-MM-dd_HH-mm-ss}${ext}` and opened with `FileMode.Create`.
  - `DateTime.Now` uses local server time: containers default to UTC, host machines often use local time.
  - During daylight saving fallback, timestamps repeat.
  - If two pipeline runs occur within the same second, `FileMode.Create` silently overwrites and destroys the previous run's data.
- **Remediation Specification:** Use `DateTime.UtcNow:yyyyMMddTHHmmssfffZ` combined with a unique monotonic sequence or short GUID, and use `FileMode.CreateNew` to prevent silent collisions.

---

#### [FINDING-14] Advisory-Only File Locking & Unreliable Creation Times on Linux
- **Target File:** [LocalFileStorageService.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/LocalFileStorageService.cs#L44-L98)
- **Severity:** MEDIUM (POSIX Semantic Variance)
- **Root Cause:**
  - `FileShare.None` maps to `flock(LOCK_EX)` on Linux, which is purely *advisory*. Non-.NET processes can still read/write the file concurrently.
  - `fileInfo.CreationTimeUtc` depends on filesystem birth time support via `statx(2)`. In older ext4, NFS mounts, or overlayfs inside Docker containers, birth time is unsupported and defaults to `0` or `mtime`.
- **Remediation Specification:** Use `LastWriteTimeUtc` as the primary cross-platform timestamp, or persist metadata inside headers/databases rather than relying on filesystem metadata.

---

#### [FINDING-15] Brittle Swagger XML Discovery & JIT Inlining Vulnerability
- **Target File:** [WebApi/DependencyInjection.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.WebApi/DependencyInjection.cs#L98-L131)
- **Severity:** MEDIUM (Startup Failure)
- **Root Cause:**
  - `Directory.GetFiles(assemblyLocation, "*.xml")` loads every XML file in the execution directory (e.g., config files, `.deps.json` companion XMLs). Passing a non-documentation XML to `options.IncludeXmlComments(xmlFile)` throws an unhandled `XmlException`.
  - `Assembly.GetCallingAssembly()` is sensitive to JIT method inlining differences between x64 and Arm64 and across tiered compilation levels.
- **Remediation Specification:** Filter XML discovery strictly to files matching loaded assembly names (`$"{assembly.GetName().Name}.xml"`). Replace `GetCallingAssembly()` with `Assembly.GetEntryAssembly()`.

---

#### [FINDING-16] 32-Bit Memory Constraints & Unbounded Dataflow Buffers
- **Target Files:**
  - [DataflowPipelineBuilder.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Pipelines/Dataflow/DataflowPipelineBuilder.cs)
  - `PipelineStepOptions.cs`
- **Severity:** MEDIUM (OOM on `win-x86` and `linux-arm`)
- **Root Cause:** 32-bit processes have a restricted 2 GB (or 3–4 GB with LAA) virtual address space. ALGLIB dense matrix allocations and unbounded TPL Dataflow blocks (`BoundedCapacity = DataflowBlockOptions.Unbounded`) can exhaust contiguous memory and throw `OutOfMemoryException` under moderate load.
- **Remediation Specification:** Enforce safe bounded capacity defaults in `PipelineStepOptions` and set `MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4)` on 32-bit runtimes (`IntPtr.Size == 4`).

---

### Priority 4: Low (Build Hygiene & Tooling)

#### [FINDING-17] CI/CD Pipeline Has Zero Test Execution and Single Architecture
- **Target File:** [.github/workflows/publish_nuget_package.yml](file:///c:/Pessoal/Projetos/Tools/.github/workflows/publish_nuget_package.yml)
- **Severity:** LOW (Quality Assurance Gap)
- **Root Cause:** Workflow runs exclusively on `ubuntu-latest` (x64) and executes `dotnet pack` without running unit or acceptance tests. Windows-specific lock errors (Finding 01) and architectural discrepancies pass completely undetected into published NuGet packages.
- **Remediation Specification:** Implement a matrix build testing on Windows, Linux, and Arm targets before packing (see Section 3).

---

#### [FINDING-18] MSBuild Environment Variable Assumptions & Missing Git Attributes
- **Target Files:**
  - [Directory.Build.props](file:///c:/Pessoal/Projetos/Tools/Directory.Build.props#L27-L61)
  - Repository root
- **Severity:** LOW (Build Environment Fragility)
- **Root Cause:**
  - `$(UserProfile)` is Windows-centric (Linux uses `$(HOME)`). While currently unused, it is an anti-pattern. Use `$(NuGetPackageRoot)`.
  - Line 61: `<DocumentationFile>..\$(MSBuildProjectName)\$(MSBuildProjectName).xml</DocumentationFile>` under `Debug|AnyCPU` writes generated files directly into source trees.
  - Absence of `.gitattributes` combined with CRLF `.editorconfig` rules causes line-ending flips when git checkouts occur on Linux build agents, corrupting test CSV comparisons.
- **Remediation Specification:** Add a root `.gitattributes` enforcing consistent LF/CRLF behavior and remove manual `DocumentationFile` overrides.

---

#### [FINDING-19] Deprecated SMTP Implementation
- **Target File:** [SmtpEmailService.cs](file:///c:/Pessoal/Projetos/Tools/src/MelloSilveiraTools.Core/Services/Email/SmtpEmailService.cs)
- **Severity:** LOW (Platform Flakiness)
- **Root Cause:** `System.Net.Mail.SmtpClient` is deprecated by Microsoft. On Linux, modern TLS negotiation depends heavily on system OpenSSL security levels (`SECLEVEL`), and NTLM requires external GSS-API libraries.
- **Remediation Specification:** Plan migration to `MailKit` once approved per repository dependency governance.

---

#### [FINDING-20] Undocumented Native Linux Prerequisites
- **Target File:** [README.md](file:///c:/Pessoal/Projetos/Tools/README.md)
- **Severity:** LOW (Deployment Friction)
- **Root Cause:** Minimal Linux container images (such as Alpine or Chiseled Ubuntu) lack `libicu` (globalization), `libssl3` (cryptography), and `tzdata` (time zones). Without documentation, consumers encounter obscure startup crashes.
- **Remediation Specification:** Add a dedicated "Linux Native Prerequisites" section to the root `README.md`.

---

## 3. Automated CI/CD Multi-Platform Verification Matrix

Incorporate the following matrix workflow into `.github/workflows/verify_portability.yml` to automatically validate cross-platform integrity:

```yaml
name: Multi-Platform & Architecture Test Matrix

on:
  pull_request:
    branches: [main]
  push:
    branches: [main]

jobs:
  test-native:
    name: Test ${{ matrix.name }}
    strategy:
      fail-fast: false
      matrix:
        include:
          - { os: windows-latest,   arch: x64,   name: "Windows x64" }
          - { os: windows-latest,   arch: x86,   name: "Windows x86" }
          - { os: ubuntu-latest,    arch: x64,   name: "Linux x64 (glibc)" }
          - { os: ubuntu-24.04-arm, arch: arm64, name: "Linux Arm64 (glibc)" }
    runs-on: ${{ matrix.os }}
    steps:
      - name: Checkout Code
        uses: actions/checkout@v4

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          architecture: ${{ matrix.arch }}

      - name: Restore Dependencies
        run: dotnet restore MelloSilveiraTools.sln

      - name: Build Solution
        run: dotnet build MelloSilveiraTools.sln -c Release --no-restore

      - name: Execute Test Suite
        run: dotnet test test/UnitTests/UnitTests.csproj -c Release --no-build --logger "trx;LogFileName=test-results.trx"

  test-containerized:
    name: Test ${{ matrix.name }}
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        include:
          - name: "Linux Arm32 (QEMU)"
            platform: "linux/arm/v7"
            image: "mcr.microsoft.com/dotnet/sdk:10.0"
          - name: "Linux Alpine x64 (musl)"
            platform: "linux/amd64"
            image: "mcr.microsoft.com/dotnet/sdk:10.0-alpine"
    steps:
      - name: Checkout Code
        uses: actions/checkout@v4

      - name: Setup QEMU for Multi-Arch Emulation
        uses: docker/setup-qemu-action@v3

      - name: Run Tests in Target Container
        run: >
          docker run --rm
          --platform ${{ matrix.platform }}
          -v ${{ github.workspace }}:/workspace
          -w /workspace
          ${{ matrix.image }}
          sh -c "dotnet test test/UnitTests/UnitTests.csproj -c Release"
```

---

## 4. Phased Implementation Roadmap for AI Agents

When executing automated code refactoring, strictly adhere to the following sequence:

```
[Phase 1: Zero-Risk Hotfixes]
├── Step 1.1: Fix stream scoping in JsonFilePluginCachePersistence.cs (Finding 01)
├── Step 1.2: Correct target directory in PluginFileProcessor.MoveToMainFolder (Finding 05)
└── Step 1.3: Add CultureInfo.InvariantCulture to Vector3D.cs (Finding 10)

[Phase 2: Host Decoupling & Security]
├── Step 2.1: Remove AspNetCore.App from MelloSilveiraTools.Core.csproj (Finding 02)
├── Step 2.2: Migrate EncryptionService to Rfc2898DeriveBytes.Pbkdf2 (Finding 02)
├── Step 2.3: Sanitize and canonicalize storage paths and identifiers (Findings 06 & 07)
└── Step 2.4: Introduce CaseInsensitive EnumerationOptions for Linux scanning (Finding 08)

[Phase 3: Plugin Isolation & Dynamic Loading]
├── Step 3.1: Implement custom PluginLoadContext with AssemblyDependencyResolver (Finding 04)
├── Step 3.2: Validate PE architecture flags before loading assemblies (Finding 04)
└── Step 3.3: Implement shadow copying to avoid Windows file locks (Finding 05)

[Phase 4: Numerical & Scientific Determinism]
├── Step 4.1: Expose and bind explicit RNG seed in AlglibCurveFitter (Finding 09)
├── Step 4.2: Replace computed-float hashing with canonical-input hashing (Finding 09)
└── Step 4.3: Enforce standardized newline characters in CsvFileWriter (Finding 12)

[Phase 5: AOT Honesty & Build Hygiene]
├── Step 5.1: Remove IL* diagnostic suppressions from Directory.Build.props (Finding 03)
├── Step 5.2: Set IsAotCompatible=false on reflection-heavy packages (Finding 03)
├── Step 5.3: Add root .gitattributes file (Finding 18)
└── Step 5.4: Activate Multi-Platform CI workflow (Finding 17)
```
