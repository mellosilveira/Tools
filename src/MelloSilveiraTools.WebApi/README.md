# MelloSilveiraTools.WebApi

Web API foundation library providing MVC controllers, Minimal APIs, NDJSON streaming, JWE bearer authentication, and Swagger configuration.

---

## 1. Key Capabilities

- **Dual HTTP Surfaces:**
  - **Minimal APIs:** Ultra-fast route mapping for CRUD entities (`app.MapCrud<TEntity, TFilter>("/api/v1/entities", "entity")`) and plugins (`app.MapPluginEndpoints("/api/v1/plugins")`).
  - **MVC Controllers:** `CustomControllerBase` and generic `CrudController<TEntity, TFilter>` providing automated pagination, filtering, creation, updates, and soft/hard deletion.
- **Real-Time NDJSON Streaming (`WriteNdjsonAsync`):**
  - High-throughput streaming of asynchronous sequences (`IAsyncEnumerable<T>`) using `application/x-ndjson` with automatic trailer status validation.
- **JWE (JSON Web Encryption) Authentication:**
  - Secure token authentication using JWE (AES-256 key wrap + AES-256-CBC-HMAC-SHA-512 content encryption) via `AddJweAuthentication(jwtSettings)`.
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

- **DI Entry Points (`WebApiDependencyInjection`):** `AddWebApiServices(resiliencePipelineSettings)`, `AddJweAuthentication(jwtSettings)`, `AddSwaggerWithBearerSecurity()`, `UseSwaggerDocs()`, `UseCustomMiddlewares()`.
  - **Singletons:** `ApiServiceAgentResiliencePipeline`, `JwtSettings`.
  - **Scoped:** `IAuthenticationTokenService`, open-generic CRUD commands (`AddEntity<>`, `ReadEntityById<>`, `ReadEntityPaged<,>`, `UpdateEntity<>`, `DeleteEntity<>`).
- **NDJSON Streaming:** `WriteNdjsonAsync` sets `application/x-ndjson`, `X-Content-Type-Options: nosniff`, and appends the `X-Stream-Status` trailer only when `context.Response.SupportsTrailers()` is true. `ExceptionHandlingHttpMiddleware` special-cases `NdjsonException` to avoid corrupting partial streams.
- **JWE Authentication:** `JsonWebTokenHandler` using `HmacSha256` signing + `Aes256KW` key wrap + `Aes256CbcHmacSha512` content encryption.

---

## 5. Dependencies

- `MelloSilveiraTools.Core`
- `MelloSilveiraTools.Database`
- `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.11)
- `Microsoft.IdentityModel.JsonWebTokens` (8.22.0)
- `Swashbuckle.AspNetCore` (10.2.3)
