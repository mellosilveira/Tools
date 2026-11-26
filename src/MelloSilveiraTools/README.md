# MelloSilveiraTools (Meta-Package)

The meta-package for the **MelloSilveiraTools** ecosystem. It transitively aggregates all contextual packages into a single reference and exposes a unified Dependency Injection entry point (`AddToolsServices`).

---

## 1. Overview

Installing `MelloSilveiraTools` provides access to the complete suite without managing separate NuGet dependencies:
- **Core** (`MelloSilveiraTools.Core`): Resilient pipelines, caching, file management (+ `FileStorageHealthCheck`), encryption, and logging.
- **Database** (`MelloSilveiraTools.Database`): Pure-Npgsql PostgreSQL relational data provider (`NpgsqlSlimDataSourceBuilder`, `NpgsqlBatch`), SQL generation, and `PostgresHealthCheck`.
- **WebApi** (`MelloSilveiraTools.WebApi`): Minimal APIs, MVC CRUD controllers, JWE authentication, rate limiting, CORS, aggregated health checks (`AddWebApiHealthChecks`), and NDJSON streaming.
- **Plugins** (`MelloSilveiraTools.Plugins`): Dynamic runtime plugin engine with collectible `PluginLoadContext`, Model 2 community submissions, and two-level cache.
- **Mathematics** (`MelloSilveiraTools.Mathematics`): Functions, expressions, numerical integration/differentiation, and ODE solvers.
- **Mechanics of Materials** (`MelloSilveiraTools.MechanicsOfMaterials`): Viscoelastic constitutive equations, fatigue, specimen analysis, and load sharing.
- **Optimizations** (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`): Experimental data segmentation, constrained curve fitting, parametric sweeps, and HTTP APIs.

---

## 2. Installation

Install via the .NET CLI:

```bash
dotnet add package MelloSilveiraTools
```

---

## 3. Usage & Dependency Injection

Register the entire suite in a single call inside `Program.cs`:

```csharp
using MelloSilveiraTools;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;
using Polly;

ServiceCollection services = new();

// Configure the default resilience pipeline (Polly v8)
ResiliencePipelineSettings resilienceSettings = new()
{
    BackoffType = DelayBackoffType.Exponential,
    DelayInMilliseconds = 500,
    MaxRetryAttempts = 3,
    UseJitter = true
};

// Register all suite services in one go
services.AddToolsServices(
    defaultResiliencePipelineSettings: resilienceSettings,
    databaseSettings: null,                    // Configure when using PostgreSQL
    encryptionSettings: null,                  // Configure when using encryption
    pluginSettings: null,                      // Configure for dynamic plugins
    emailSettings: null,                       // Configure when using SMTP email
    fileStorageSettings: null,                 // Configure for local/cloud file storage
    loggerSettings: null,                      // Configure for file/database Serilog sinks
    curveFitterOptions: null,                  // Configure for curve fitting defaults
    experimentalDataSettings: null,            // Configure for experimental data pipelines
    parametricSweepSettings: null,             // Configure for sensitivity analysis
    pipelineProgressNotifierSettings: null,    // Configure for bounded NDJSON progress channels
    addMechanicalModels: true);                // Registers all constitutive calculators
```

---

## 4. Dependencies & Module Reference

- [`MelloSilveiraTools.Core`](../MelloSilveiraTools.Core/README.md)
- [`MelloSilveiraTools.Database`](../MelloSilveiraTools.Database/README.md)
- [`MelloSilveiraTools.WebApi`](../MelloSilveiraTools.WebApi/README.md)
- [`MelloSilveiraTools.Plugins`](../MelloSilveiraTools.Plugins/README.md)
- [`MelloSilveiraTools.Mathematics`](../MelloSilveiraTools.Mathematics/README.md)
- [`MelloSilveiraTools.MechanicsOfMaterials`](../MelloSilveiraTools.MechanicsOfMaterials/README.md)
- [`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`](../MelloSilveiraTools.MechanicsOfMaterials.Optimizations/README.md)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (10.0.15)
