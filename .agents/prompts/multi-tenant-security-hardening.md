# Multi-Tenant Security Hardening (`userId` Isolation)

> [!NOTE]
> **Current Status (2026-11-26 — Historical Reference):** Blocks 1 and 2 have been largely implemented and evolved in key architectural areas: `stored_files` was renamed to `storage_entry_metadata`; `Dapper` was removed in favor of pure `Npgsql` (`NpgsqlBatch` with `set_config(..., true)`); `PARTITION BY LIST (user_id)` / `ensure_user_partitions` was replaced by non-partitioned Row-Level Security with nullable `user_id` (`NULL` for public records); and `AddJweAuthentication` / `AddDefaultRateLimiting` / `AddDefaultCors` / `EnsureDatabaseRlsSafetyAsync` were centralized in `WebApiDependencyInjection`. Consult `CHANGELOG.md` and `src/MelloSilveiraTools.WebApi/README.md` before taking action.

## Original Context (Prior Snapshot)
- **PostgreSQL RLS + Partitioning:** Originally `stored_files`, `mechanical_model_curve_fit_outputs`, `mechanical_model_simulations`, and `parametric_sweeps` used `PARTITION BY LIST (user_id)` + `_default` partitions + `ENABLE`/`FORCE ROW LEVEL SECURITY` (`user_id = public.current_app_user_id()`).
- **Non-Superuser Role:** `tools_app` (`NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS`) provisioned in `src/MelloSilveiraTools.Database.Migrator/Scripts/Common/0001_create_users.sql`.
- **User Propagation:** Single singleton implementation `CurrentUserProvider : ICurrentUserProvider` using instance `AsyncLocal<long?>` (no static state), populated by `UserContextMiddleware` (`src/MelloSilveiraTools.WebApi/Application/Middlewares/UserContextMiddleware.cs`) and enforced by `PostgresRepository`.

---

## Objective
Close multi-tenant isolation gaps identified outside the database (file storage, in-memory channels, JWE authentication, rate limiting, and defense in depth), maintaining **0 warnings / 0 errors** on build and passing all unit tests.

---

## Block 1 — Storage & In-Memory Pipeline Isolation (Highest Priority)

### 1. [CRITICAL] File storage without `user_id` isolation + raw `Location` access/deletion
- **Affected files:**
  - `src/MelloSilveiraTools.Core/Files/Providers/FileStorageServiceBase.cs`
  - `src/MelloSilveiraTools.Core/Files/Providers/Local/LocalFileStorageService.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Commands/StorageEntryMetadata/Create/AddStorageEntryCommand.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Commands/StorageEntryMetadata/Delete/DeleteStorageEntryCommand.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Commands/StorageEntryMetadata/Read/ReadStorageEntriesCommand.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Commands/StorageEntryMetadata/Upload/UpdateStorageEntryFileCommand.cs`
  - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ExperimentalData/Commands/Process/Single/ExecuteExperimentalDataPipeline.cs`
  - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ExperimentalData/Commands/Process/Single/ExecuteExperimentalDataPipelineValidator.cs`
  - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ExperimentalData/Pipelines/Steps/ExperimentalDataSegmenterStep.cs`
- **Vulnerabilities:**
  1. `LocalFileStorageService` only validated path traversal in `ResolveLocation`, while location-based methods accepted raw paths before calling `File.Delete`/`Directory.Delete`.
  2. `DeleteStorageEntryCommand` accepted raw `request.Location` when `FileId` was omitted/not found.
  3. `ExecuteExperimentalDataPipeline` / `ExperimentalDataSegmenterStep` opened `StrainFileLocation` and `StressFileLocation` directly from JSON payloads without verifying RLS ownership.
  4. Shared `storage/{identifier}/{context}/{fileName}` lacked `user_id` prefixing, risking cross-user file overwrites.
- **Required Measures:**
  1. Validate in **all** location-based methods of `LocalFileStorageService` (and sibling providers where applicable) that `Path.GetFullPath(location)` stays strictly inside `BaseDirectory` and the authenticated user's scope, blocking path traversal (`..`, escaping absolute paths, symlinks).
  2. Physically partition user storage paths (e.g., `u_{userId}/{identifier}/{context}/{fileName}`) to prevent collisions across disk and object storage.
  3. Disallow arbitrary path deletion outside user scope in `DeleteStorageEntryCommand`; prefer `FileId` resolution via RLS-protected metadata or constrain fallback paths strictly to the user directory.
  4. Verify ownership of `StrainFileLocation`/`StressFileLocation` via RLS metadata before opening streams.

### 2. [HIGH] In-memory progress channels without `user_id` isolation
- **Affected files:**
  - `src/MelloSilveiraTools.Core/Pipelines/Notifiers/ChannelPipelineProgressNotifier.cs`
  - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Controllers/PipelineProgressController.cs`
- **Vulnerability:** `_channels` keyed solely by `pipelineId` allows another authenticated caller who knows or guesses `pipelineId` to consume progress/error events.
- **Required Measure:** Scope progress channels by user (composite key `(userId, pipelineIdentifier)`), returning `404 NotFound` when accessed by another user.

---

## Block 2 — Authentication, Rate Limiting, and Defense in Depth

### 3. [CRITICAL] Hardcoded JWE keys in `appsettings.json`
- **Affected files:**
  - `src/MelloSilveiraTools.WebApi.Host/appsettings.json`
  - `src/MelloSilveiraTools.WebApi.Host/Program.cs`
- **Vulnerability:** Plaintext versioned `Jwt:EncryptionKey` and `Jwt:SigningKey` allow anyone reading the repository to forge JWE tokens with arbitrary `sub` (`user_id`), bypassing RLS.
- **Required Measures:**
  1. Move development secrets to `dotnet user-secrets` / runtime injection (Docker/Launcher/CI) and remove real keys from `appsettings.json`.
  2. Enforce startup validation rejecting empty, short (< 32 UTF-8 bytes for AES-256), or placeholder keys outside Development.

### 4. [HIGH] `/api/v1/auth/refresh` did not verify `users.is_active`
- **Affected files:**
  - `src/MelloSilveiraTools.WebApi/Authentication/Commands/` (`RefreshAuthenticationTokenCommand`)
  - `src/MelloSilveiraTools.WebApi/Authentication/Services/AuthenticationJweTokenService.cs`
- **Vulnerability:** A deactivated user (`is_active = false`) holding an unexpired token could refresh it indefinitely.
- **Required Measure:** Query `UserEntity` via `IRepository` inside `RefreshAuthenticationTokenCommand` and return `401 Unauthorized` when missing or inactive.

### 5. [HIGH] Rate Limiting on authentication endpoints
- **Affected files:**
  - `src/MelloSilveiraTools.WebApi.Host/Program.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Controllers/AuthController.cs`
- **Vulnerability:** Anonymous `/register`, `/login`, `/refresh`, and `/validate` endpoints lack IP rate limiting against abuse/brute force.
- **Required Measure:** Configure native ASP.NET Core `AddRateLimiter` / `UseRateLimiter` with restrictive fixed-window IP policies.

### 6. [MEDIUM] Startup guard against PostgreSQL `SUPERUSER` / `BYPASSRLS`
- **Affected files:**
  - `src/MelloSilveiraTools.WebApi.Host/Program.cs`
- **Vulnerability:** In PostgreSQL, `SUPERUSER` or `BYPASSRLS` roles silently bypass `FORCE ROW LEVEL SECURITY`.
- **Required Measure:** Execute `SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname = current_user;` at startup and abort initialization unless explicitly opted out via configuration.

### 7. [MEDIUM] Enforce server-side `UserId` isolation in payloads and repository writes
- **Affected files:**
  - `src/MelloSilveiraTools.Database/RelationalDatabase/Models/Entities/UserScopedEntityBase.cs`
  - `src/MelloSilveiraTools.Database/RelationalDatabase/Repositories/PostgresRepository.cs`
- **Required Measures:** Prevent HTTP JSON payloads from spoofing `userId` (`[JsonIgnore]` or middleware/RLS enforcement) and enforce `ICurrentUserProvider` parameters in `PostgresRepository` alongside PostgreSQL RLS `WITH CHECK` policies.

### 8. [MEDIUM] Restrict CORS in production and document in-memory cache isolation
- **Affected files:**
  - `src/MelloSilveiraTools.WebApi.Host/Program.cs`
- **Required Measure:** Configure explicit allowed origins (`AllowedCorsOrigins`) instead of `AllowAnyOrigin()` in production.

---

## Mandatory Guidelines (`AGENTS.md`)
- C# 13 / `.NET 10`: **no `var`** (explicit types mandatory), **CRLF** line endings, 4-space indentation, primary constructors, XML docs on all `public` types and members, mandatory `TimeProvider` (never ambient `DateTime.UtcNow` / `DateTimeOffset.UtcNow`).
- Controllers/Endpoints must contain zero business logic (delegate to `Command` + `IValidator`).
- Add deterministic unit tests covering every mitigated gap and verify with `dotnet build` and `dotnet test`.
