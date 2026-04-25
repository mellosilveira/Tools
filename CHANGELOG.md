# MelloSilveiraTools

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.2] - 2026-04-25
### Fixed
- Postgres repository now honors the configured `CommandTimeout` on every command path (previously defaulted on some repositories).
- `unique_hash` computation for filter clauses is now stable across process restarts.
- Swagger generation picks up the correct calling assembly when multiple assemblies are loaded in the host.
- `JOIN` column resolution no longer leaks columns from unrelated tables when two entities share column names.
- `FilterClause` expansion correctly flattens nested groups and preserves operator precedence.
- `PluginCache.Stream` now correctly treats empty/`null` `name` as "match all" (previously empty string matched only entries with empty group key).
- `PluginService.Reload*` no longer clears unrelated cache entries on every iteration — `cache.Clear` now scopes to the plugin being reloaded.
- `Force` (Mechanics of Materials): magnitude (`AbsoluteValue`) is now always consistent with `X`/`Y`/`Z` after `Sum`/`Subtract`/`Round`/`Divide`/`Abs`/`Create`.
- `EnumerableExtensions.FirstWithoutValidate` exception message corrected ("No element matched the predicate.").
### Changed
- NuGet metadata populated on both `MelloSilveiraTools` and `MelloSilveiraTools.MechanicsOfMaterials` projects (PackageId, Description, Tags, README, license, symbols).
- Both packages now target `net10.0` (`MechanicsOfMaterials` was previously on `net9.0`).
- XML documentation is now generated and shipped inside the `.nupkg` so consumers get IntelliSense.
- `IPluginCachePersistence` implementations are registered as keyed services; the `{target}` route segment on cache endpoints selects the implementation at runtime.
- Documented contracts that were previously implicit: `ISqlProvider` template placeholders (`#TABLE_NAME`, `#COLUMNS`, `#WHERE`, `#ORDERBY`, `#LIMIT`, `#OFFSET`, `#JOIN`, etc.); `PostgresSqlProvider` static cache lifetime and thread-safety; `PostgresRepository` resilience pipeline / timeout behavior; `ExceptionHandlingMiddleware` exception→status mapping; `AuthenticationJweTokenService` 32-byte UTF-8 key requirement; Newmark / Newmark-β stability characteristics in `IDifferentialEquationMethod`.
- Added `<example>` snippets to `OperationBase.ProcessAsync`, `IRepository.GetAsync`, `IPluginService.LoadPluginsOnStartup` and `IFatigueCalculator.CalculateFatigueResult`.
- Added `<exception>` documentation across public surfaces that throw (`IAuthenticationTokenService.RefreshAsync`, `PluginFileProcessor.*`, `EnumerableExtensions.FirstWithoutValidate`, `TypeExtensions.GetDbTypeFromPropertyType`, `IRepository`).
### Deprecated
- `Infrastructure.Logger.LocalFileLogger` marked `[Obsolete]` — it is a no-op placeholder. Consumers must register their own `ILogger` implementation before relying on it in production.
### Breaking
- `MelloSilveiraTools.MechanicsOfMaterials.Models.Force.AbsolutValue` renamed to `AbsoluteValue` (typo fix).
- `Force` `X`/`Y`/`Z`/`AbsoluteValue` setters are now `private`; `Force` instances are immutable after construction. Use `Sum`/`Subtract`/`Round`/`Divide`/`Abs`/`Create` to derive new instances.
- `IDynamicServiceProvider.GetService(Type)` now returns `object?` (was `object`); `IDynamicServiceProvider.GetKeyed<T>(object)` now returns `T?` (was `T`). Aligns the declared signature with the actual behavior — implementations already returned null for missing services.
- `PluginRegistrationContext.Services` and `PluginRegistrationContext.DynamicProvider` are now nullable (`IServiceCollection?` / `IDynamicServiceProvider?`). Exactly one is non-null at any time, determined by `IsStartup`.

## [1.2.1] - 2026-01-06
### Fixed
- `OperationBase` now uses a generic method instead of casting `OperationResponse` when handling unexpected errors.

## [1.2.0] - 2026-01-06
### Added
- `DifferentialEquationMethodFactory` and `CurveType` enum.
- `CreateSuccessOk` overload on `OperationResponseBase` that carries response data.
- `CreateConflict` and `CreateUnprocessableEntity` helpers on `OperationResponse`.
- `MelloSilveiraTools.MechanicsOfMaterials` companion package.
### Renamed
- `Fatigue` → `FatigueCalculator`.
- `GeometricProperty` → `GeometricPropertyCalculator`.
- `MechanicsOfMaterials` → `ConstitutiveEquationsCalculator`.

## [1.1.0] - 2025-11-29
### Added
- File-based `ILogger` implementation.
- `GetDeleteByPrimaryKeyQuery`, `GetSelectByPrimaryKeyQuery` and `GetUpdateByPrimaryKeyQuery` on `ISqlProvider`.
- Extension methods for Npgsql types, `IFormFile`, `Type` and `string`.
- JWE authentication (`AddJweAuthentication`, `AuthenticationJweTokenService`).
- Swagger bootstrap helpers (`AddSwaggerDocsWithJwtAuthentication`, `UseSwaggerDocs`).
- Encryption service.
- Pooled/stack-allocating string builder.
- Default resilience pipeline and PostgreSQL-specific pipeline (Polly).
- Base `IRepository` contract and `PostgresRepository` implementation.
- `OperationBase` and use-case operation scaffolding.
- Base HTTP service agent.
- Initial Mechanics of Materials primitives.
### Removed
- `ReferencedPropertyName` from `ForeignKeyColumnAttribute`.

## [1.0.3] - 2025-06-11
### Added
- `TableAttribute` constructor accepting name and alias.

## [1.0.2] - 2025-06-10
### Fixed
- Project metadata.

## [1.0.1] - 2025-06-10
### Removed
- Unnecessary build files.

## [1.0.0] - 2025-06-10
### Added
- First public release.
