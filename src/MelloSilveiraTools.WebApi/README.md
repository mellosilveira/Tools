# MelloSilveiraTools.WebApi

Web API foundation library providing MVC controllers, Minimal APIs, NDJSON streaming, JWE bearer authentication, and Swagger configuration.

---

## 1. Key Capabilities

- **Dual HTTP Surfaces:**
  - **Minimal APIs:** Ultra-fast route mapping for CRUD entities (`app.MapCrud<TEntity, TFilter>(...)` with convention-based route/tag inference), authentication (`app.MapAuthEndpoints("/api/v1/auth")`), storage metadata & downloads (`app.MapStorageEntryMetadataEndpoints("/api/v1/storage-entry-metadata")`), aggregated health checks (`app.MapHealthEndpoint("/api/health")`), and plugins (`app.MapPluginEndpoints("/api/v1/plugins")`).
  - **MVC Controllers:** `CustomControllerBase`, `AuthController`, `HealthController`, `StorageEntryMetadataController`, and generic `CrudController<TEntity, TFilter>` providing automated pagination, filtering, creation, updates, and soft/hard deletion.
- **Aggregated Native Health Checks:** `AddWebApiHealthChecks(DatabaseSettings?)` aggregates `FileStorageHealthCheck` and conditional `PostgresHealthCheck`, evaluated by `CheckHealthCommand` returning `HealthCheckResultData` / `HealthCheckEntryResultData` (`Status`, `DurationMs`, `Description`, `Error`, `Data`) uniformly across all environments.
- **Real-Time NDJSON Streaming (`WriteNdjsonAsync`):**
  - High-throughput streaming of asynchronous sequences (`IAsyncEnumerable<T>`) using `application/x-ndjson` with automatic trailer status validation.
- **JWE Authentication, Rate Limiting & Security Pipeline:**
  - Secure token authentication using JWE (AES-256 key wrap + AES-256-CBC-HMAC-SHA-512 content encryption) via `AddJweAuthentication(jwtSettings)` with `AuthorizationPolicies.SuperuserOnly`, fixed-window `RateLimitPolicies` (`Auth`, `AnonymousCompute`, `PluginUpload`) via `AddDefaultRateLimiting()`, ordered middleware pipeline via `UseWebApiSecurityPipeline()`, and startup RLS verification via `EnsureDatabaseRlsSafetyAsync()`.
- **Global Error Handling Middleware:**
  - `ExceptionHandlingHttpMiddleware` catches unhandled exceptions, maps them to standard HTTP status codes, and preserves NDJSON stream integrity.
- **Turnkey Swagger / OpenAPI Documentation:**
  - Configures Swagger with Bearer authorization out-of-the-box (`AddSwaggerWithBearerSecurity()` and `UseSwaggerDocs()`).

---

## 2. Installation

```bash
dotnet add package MelloSilveiraTools.WebApi
```

---

## 3. Quick Start

### Registering Generic CRUD Minimal APIs

```csharp
using MelloSilveiraTools.WebApi.Application.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

// Map full CRUD endpoints for an entity with filtering in a single line
app.MapCrud<SpecimenEntity, SpecimenFilter>("/api/v1/specimens", "specimen");

app.Run();
```

---

## 4. Dependency Injection & Technical Invariants

- **DI Entry Points (`WebApiDependencyInjection`):** `AddWebApiServices(resiliencePipelineSettings)`, `AddWebApiHealthChecks(databaseSettings)`, `AddJweAuthentication(jwtSettings)`, `AddDefaultRateLimiting()`, `AddDefaultCors(allowedOrigins, allowAnyOriginWhenEmpty)`, `ConfigureToolsHttpJsonOptions()`, `ConfigureRequestBodyLimits(maxRequestBodySizeBytes)`, `AddSwaggerWithBearerSecurity()`, `UseSwaggerDocs()`, `UseCustomMiddlewares()`, `UseWebApiSecurityPipeline(useRateLimiter)`, `EnsureDatabaseRlsSafetyAsync()`.
  - **Singletons:** `ApiServiceAgentResiliencePipeline`, `JwtSettings`, `IAuthorizationHandler` (`SuperuserAuthorizationHandler`), Storage Entry Metadata commands & validators (`Add`, `Delete`, `Download`, `Read`, `Update`).
  - **Scoped:** `IAuthenticationTokenService`, `CheckHealthCommand`, auth commands (`Register`, `Login`, `Refresh`, `Validate`), open-generic CRUD commands (`AddEntity<>`, `ReadEntityById<>`, `ReadEntityPaged<,>`, `UpdateEntity<>`, `DeleteEntity<>`).
- **NDJSON Streaming:** `WriteNdjsonAsync` sets `application/x-ndjson`, `X-Content-Type-Options: nosniff`, and appends the `X-Stream-Status` trailer only when `context.Response.SupportsTrailers()` is true. `ExceptionHandlingHttpMiddleware` special-cases `NdjsonException` to avoid corrupting partial streams.
- **JWE Authentication:** `JsonWebTokenHandler` using `HmacSha256` signing + `Aes256KW` key wrap + `Aes256CbcHmacSha512` content encryption, paired with `UserContextMiddleware` verifying live user/superuser status against PostgreSQL.

---

## 5. Dependencies

- `MelloSilveiraTools.Core`
- `MelloSilveiraTools.Database`
- `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.15)
- `Microsoft.IdentityModel.JsonWebTokens` (8.23.0)
- `Swashbuckle.AspNetCore` (10.3.1)
- `Swashbuckle.AspNetCore.Newtonsoft` (10.3.1)
