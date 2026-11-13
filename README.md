# MelloSilveiraTools

[![Build and Publish NuGet](https://github.com/mellosilveira/Tools/actions/workflows/publish_nuget_package.yml/badge.svg)](https://github.com/mellosilveira/Tools/actions/workflows/publish_nuget_package.yml)
[![JOSS Draft PDF](https://github.com/mellosilveira/Tools/actions/workflows/draft-pdf.yml/badge.svg)](https://github.com/mellosilveira/Tools/actions/workflows/draft-pdf.yml)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)

A modular .NET 10 suite of open-source NuGet packages for **computational mechanics of materials, viscoelastic constitutive modeling, numerical analysis, and experimental data optimization**, backed by high-throughput TPL Dataflow pipelines, relational persistence, web-API helpers, and a dynamic plugin runtime.

---

## Packages

Pick the contextual packages your research or engineering application needs, or install the meta-package for the entire suite:

| Package | What it ships | Depends on |
|---|---|---|
| `MelloSilveiraTools.Mathematics` | Differential equation solvers (Newmark, Newmark-$\beta$), symbolic-like function hierarchy (constant, polynomial, exponential, sine, cosine, power-law + factory) with lazy analytical derivatives/integrals, expressions (`PronySeries`), numerical integration (Simpson's rule), finite-difference differentiation, root-finding (Bisection, Brent, Step-by-Step), statistics, 3D geometry (`Point3D`, `Vector3D`), unit converter | Core |
| `MelloSilveiraTools.MechanicsOfMaterials` | Fatigue (Goodman/Marin), constitutive equations, geometric properties (circular/rectangular profiles), 3D vector/force models, mechanical models (elastic, linear Maxwell, non-linear Schapery & Modified Superposition Method, quasi-linear Fung & Simplified Fung viscoelasticity), 3D tissue load-sharing calculator | Mathematics |
| `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` | TPL Dataflow pipelines for experimental mechanics, automated CSV phase segmentation (`Ramp`, `Relaxation`, `Descent`, `Recovery`), constrained curve fitting (`AlglibCurveFitter` L-BFGS / GDEMO Differential Evolution and `MathNetCurveFitter` BFGS / Nelder-Mead), Prony-series & constitutive parameter identification, and One-At-A-Time (OAAT) parametric sensitivity sweeps | MechanicsOfMaterials, WebApi |
| `MelloSilveiraTools.Core` | TPL Dataflow & fluent pipeline builders with real-time progress telemetry (`IPipelineProgressNotifier`), extension methods, `ILogger` + structured file logger, in-memory single/two-level caches, Polly v8 resilience pipelines, encryption (PBKDF2), file storage (`Local`/`DFS`), SMTP email | — |
| `MelloSilveiraTools.Database` | `IRepository`, `PostgresRepository`, `ISqlProvider`, attribute-driven SQL generation, filter clauses, Npgsql/Dapper extensions, dedicated Postgres resilience pipeline | Core |
| `MelloSilveiraTools.WebApi` | `CustomControllerBase`, `CrudController<TEntity, TFilter>`, minimal API `MapCrud`, `ExceptionHandlingMiddleware`, NDJSON streaming, JWE bearer authentication, Swagger bootstrap, `ApiServiceAgent`, `Result<T>` HTTP projections | Core, Database |
| `MelloSilveiraTools.Plugins` | File-based plugin runtime, two-level cache, dynamic DI, persistence (file/database), HTTP-friendly operations, background orchestrator | Core, Database, WebApi |
| `MelloSilveiraTools` | Meta-package: aggregates every package above; install once and get everything | all of the above |

---

## Installation

Requires the [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later.

Install the specific scientific packages via the .NET CLI:

```bash
dotnet add package MelloSilveiraTools.Mathematics
dotnet add package MelloSilveiraTools.MechanicsOfMaterials
dotnet add package MelloSilveiraTools.MechanicsOfMaterials.Optimizations
```

Or install the full meta-package:

```bash
dotnet add package MelloSilveiraTools
```

---

## Scientific Quick Start (Viscoelastic Modeling & Curve Fitting)

### 1. Evaluating a Quasi-Linear Viscoelastic (Simplified Fung / Prony Series) Model

```csharp
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

// Define a 2-branch Prony series reduced relaxation function: G(t) = c + a1*exp(-t/tau1) + a2*exp(-t/tau2)
PronySeries relaxationFunction = new(
    independentParameter: 0.60,
    iteratorCoefficients: [0.25, -0.10, 0.15, -0.01],
    initialVariableValue: 0.0,
    finalVariableValue: 100.0);

SimplifiedFungConstitutiveParameters parameters = new()
{
    ElasticStressConstant = 12.5,
    ElasticPowerConstant = 18.0,
    ReducedRelaxationFunction = relaxationFunction
};

SimplifiedFungModelCalculator calculator = new();
MechanicalModelInput<SimplifiedFungConstitutiveParameters> input = new()
{
    ConstitutiveParameters = parameters,
    Strain = 0.04,
    Time = 30.0,
    RampTime = 1.0,
    RampTimeConsideration = RampTimeConsideration.ConsiderWithViscoelasticEffect
};

double stress = calculator.CalculateStress(input);
```

### 2. Constrained Non-Linear Curve Fitting (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`)

```csharp
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging.Abstractions;

// Fit an exponential elastic response sigma(epsilon) = A * (exp(B * epsilon) - 1)
double[] strains = [0.00, 0.01, 0.02, 0.03, 0.04, 0.05];
double[] measuredStresses = [0.00, 2.46, 5.42, 8.98, 13.25, 18.39];

AlglibCurveFitter fitter = new(NullLogger<AlglibCurveFitter>.Instance, new CurveFitterSettings { Seed = 42 });

CurveFitInput fitInput = new(
    IndependentVariable: strains,
    DependentVariable: measuredStresses,
    ModelFunction: (eps, p) => p[0] * (Math.Exp(p[1] * eps) - 1.0),
    InitialParameters: [5.0, 10.0],
    LowerBounds: [0.001, 0.001],
    UpperBounds: [500.0, 200.0],
    Profile: CurveFitProfile.Standard);

CurveFitOutput fitOutput = fitter.Fit(fitInput);
// fitOutput.OptimizedParameters -> [A ≈ 12.5, B ≈ 18.0], fitOutput.RSquared -> > 0.9999
```

---

## Dependency Injection Wire-Up

### Everything in one go

```csharp
services.AddToolsServices(
    defaultResiliencePipelineSettings,
    databaseSettings,
    encryptionSettings,
    pluginSettings,
    emailSettings,
    fileStorageSettings,
    loggerSettings,
    curveFitterOptions,
    experimentalDataSettings,
    parametricSweepSettings,
    pipelineProgressNotifierSettings,
    addMechanicalModels: true);
```

`AddToolsServices` (from the meta-package) chains `AddCoreServices`, `AddDatabaseServices`, `AddMathematicsServices`, `AddMechanicsOfMaterialsServices`, `AddOptimizationsServices`, `AddPluginServices` and `AddWebApiServices`. JWE auth and Swagger remain opt-in.

### Granular wire-up

```csharp
services
    .AddCoreServices(defaultResiliencePipelineSettings, encryptionSettings, emailSettings, fileStorageSettings, loggerSettings, pipelineProgressNotifierSettings)
    .AddDatabaseServices(defaultResiliencePipelineSettings, databaseSettings)
    .AddWebApiServices(defaultResiliencePipelineSettings)
    .AddJweAuthentication(jwtSettings)
    .AddSwaggerWithBearerSecurity()
    .AddMathematicsServices()
    .AddMechanicsOfMaterialsServices(addMechanicalModels: true)
    .AddOptimizationsServices(curveFitterOptions, experimentalDataSettings, parametricSweepSettings)
    .AddPluginServices(pluginSettings);
```

---

## Mathematics (`MelloSilveiraTools.Mathematics`)

- **Differential equation solvers** — `NewmarkMethod`, `NewmarkBetaMethod` (registered via DI as keyed `IDifferentialEquationMethod`).
- **Function system** — `Function` abstract class with lazy analytical `Derivative` / `Integral` properties. Concrete types: `ConstantFunction`, `PolynomialFunction`, `ExponencialFunction`, `SineFunction`, `CosineFunction`, `PowerLaw`, `GenericFunction`. Create by enum via `FunctionFactory`.
- **Expressions** — `MathExpression` (linear combination of `Function` instances) and `PronySeries` ($c + \sum a_n e^{b_n x}$).
- **Numerical methods** — `SimpsonRuleIntegration` (`IIntegration`), `FiniteDifferenceDifferentiation` (`IDifferentiation`), and `BisectionMethod` / `BrentMethod` / `StepByStepMethod` (keyed `IRootFinding`).
- **Statistics & 3D Geometry** — `IStatisticsCalculator` / `StatisticsCalculator`, `Point3D`, `Vector3D`, `UnitConverter`.

---

## Mechanics of Materials (`MelloSilveiraTools.MechanicsOfMaterials`)

- **Fatigue** — `IFatigueCalculator` / `FatigueCalculator` (Goodman and Marin criteria).
- **Constitutive equations** — `IConstitutiveEquationsCalculator` / `ConstitutiveEquationsCalculator`.
- **Geometric properties** — `IGeometricPropertyCalculator<CircularProfile>` / `IGeometricPropertyCalculator<RectangularProfile>`.
- **Viscoelastic & Mechanical models** — `IMechanicalModelCalculator<TConstitutiveParameters>` hierarchy:
  - Linear elastic: `ElasticModelCalculator`
  - Linear viscoelastic: `MaxwellModelCalculator`
  - Quasi-linear viscoelastic (QLV): `FungModelCalculator` (continuous spectrum with exponential integral $E_1$) and `SimplifiedFungModelCalculator` (discrete Prony-series relaxation)
  - Non-linear viscoelastic: `SchaperyModelCalculator` and `ModifiedSuperpositionMethodCalculator`
- **Load sharing** — `ILoadSharingCalculator` / `LoadShare1DTissueThreeDimensionalSpaceCalculator` (specimen displacement, angle, force projection and derivatives in 3D space).

---

## Experimental Data & Optimizations (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`)

Provides streaming TPL Dataflow pipelines and numerical optimizers for experimental characterization:

- **Experimental Phase Segmentation (`ExperimentalDataSegmenterStep`)** — Streams raw CSV time-series and classifies points into `Ramp`, `Relaxation`, `Descent`, and `Recovery` phases using sliding-window numerical differentiation.
- **Downsampling & Accumulation (`ExperimentalDataDownsamplerStep`, `CurveSegmentAccumulatorStep`)** — Applies temporal filtering and groups contiguous deformation phases into `CurveSegment` batches.
- **Constitutive Curve Fitting (`IMechanicalModelCurveFitterStep`, `AlglibCurveFitter`, `MathNetCurveFitter`)** — Calibrates viscoelastic constitutive models using bounded L-BFGS (`minbleic`), derivative-free adaptive Differential Evolution (`mindf` GDEMO) with non-linear parameter admissibility rules, and BFGS + Nelder-Mead simplex polishing, outputting $R^2$, RMSE, and Student-$t$ confidence intervals.
- **Parametric Sensitivity Analysis (`ParametricSweepPipeline`)** — Executes One-At-A-Time (OAAT) sweeps across scalar and array-indexed constitutive parameters, computing initial/final stress responses and steady-state relaxation asymptotes.

---

## HTTP Endpoints & Plugin System

Both MVC controllers and minimal-API endpoint extensions ship side-by-side (`app.MapCrud<TEntity, TFilter>("/api/v1/things", "thing")`, `app.MapPluginEndpoints("/api/v1/plugins")`), with built-in support for `application/x-ndjson` real-time streaming (`WriteNdjsonAsync`).

`MelloSilveiraTools.Plugins` enables file-based discovery (`{name}.v{major}.{minor}.{patch}.dll`), two-level caching, startup/runtime dynamic DI registration (`IDynamicServiceProvider`), and background hot-swapping (`PluginOrchestratorBackgroundService`).

---

## Building & Running Automated Tests

Clone the repository, build the solution, and execute the automated unit test suite (439 unit tests in `test/UnitTests`):

```bash
git clone https://github.com/mellosilveira/Tools.git
cd Tools
dotnet restore MelloSilveiraTools.sln
dotnet build MelloSilveiraTools.sln --configuration Release --no-restore
dotnet test test/UnitTests/UnitTests.csproj --configuration Release --no-build
```

---

## Contributing & Community Guidelines

Contributions, bug reports, and scientific model proposals are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) and [Code of Conduct](CODE_OF_CONDUCT.md) before opening an issue or submitting a Pull Request.

---

## License

This project is licensed under the **GNU Affero General Public License v3.0 or later (`AGPL-3.0-or-later`)** — see the [LICENSE](LICENSE) file for full details.
