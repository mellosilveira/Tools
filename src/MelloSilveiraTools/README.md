# MelloSilveiraTools (Meta-Package)

The meta-package for the **MelloSilveiraTools** ecosystem. It transitively aggregates all contextual packages into a single reference and exposes a unified Dependency Injection entry point (`AddToolsServices`).

---

## 1. Overview

Installing `MelloSilveiraTools` provides access to the complete suite without managing separate NuGet dependencies:
- **Core** (`MelloSilveiraTools.Core`): Resilient pipelines, caching, file management, encryption, and logging.
- **Database** (`MelloSilveiraTools.Database`): PostgreSQL relational data provider, Dapper helpers, and repositories.
- **WebApi** (`MelloSilveiraTools.WebApi`): Minimal APIs, MVC CRUD controllers, JWE authentication, and NDJSON streaming.
- **Plugins** (`MelloSilveiraTools.Plugins`): Dynamic runtime plugin engine with collectible load contexts.
- **Mathematics** (`MelloSilveiraTools.Mathematics`): Functions, expressions, numerical integration/differentiation, and ODE solvers.
- **Mechanics of Materials** (`MelloSilveiraTools.MechanicsOfMaterials`): Viscoelastic constitutive equations, fatigue, and load sharing.
- **Optimizations** (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`): Experimental data segmentation, constrained curve fitting, and parametric sweeps.

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
    databaseSettings: null,           // Configure when using PostgreSQL
    encryptionSettings: null,         // Configure when using encryption
    pluginSettings: null,             // Configure for dynamic plugins
    fileStorageSettings: null,        // Configure for local/cloud file storage
    curveFitterOptions: null,         // Configure for curve fitting defaults
    experimentalDataSettings: null,   // Configure for experimental data pipelines
    parametricSweepSettings: null,    // Configure for sensitivity analysis
    addMechanicalModels: true);       // Registers all constitutive calculators
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
- `Microsoft.Extensions.DependencyInjection.Abstractions`
