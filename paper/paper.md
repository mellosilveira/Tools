---
title: 'MelloSilveiraTools: A .NET 10 Computational Framework for Viscoelastic Constitutive Modeling, Numerical Analysis, and Experimental Data Optimization'
tags:
  - C#
  - .NET
  - mechanics of materials
  - viscoelasticity
  - biomechanics
  - soft tissues
  - constitutive modeling
  - curve fitting
authors:
  - name: Bruno Mello Silveira
    orcid: 0000-0001-6793-9387
    corresponding: true
    affiliation: "1, 2"
  - name: Paulo Pedro Kenedi
    orcid: 0000-0001-5563-843X
    affiliation: 1
affiliations:
  - name: Graduate Program in Mechanical Engineering and Materials Technology (PPEMM), Universidade Federal de Ciência e Inovação do Rio de Janeiro (UFCIRJ), Rio de Janeiro, Brazil
    index: 1
  - name: Mello Silveira Serviços LTDA, Pinheiros, São Paulo, Brazil
    index: 2
date: 12 November 2026
bibliography: paper.bib
---

# Summary

`MelloSilveiraTools` is an open-source, modular .NET 10 software suite designed for computational mechanics of materials, viscoelastic constitutive modeling, and automated experimental data characterization. While providing foundational infrastructure for scientific data persistence and high-throughput streaming pipelines, its core research engine is structured across three specialized scientific packages: `MelloSilveiraTools.Mathematics`, `MelloSilveiraTools.MechanicsOfMaterials`, and `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`. Together, these libraries enable researchers and engineers to process noisy experimental time-series data, prepare and ingest raw `stress.csv` and `strain.csv` file pairs, segment cyclic loading phases, fit linear, quasi-linear, and non-linear viscoelastic models under physical constraints, and execute large-scale one-at-a-time (OAAT) parametric sensitivity sweeps in sequential or parallel execution modes without relying on proprietary commercial software.

# Statement of need

Characterizing the time- and history-dependent mechanical behavior of polymers and biological soft tissues — such as knee ligaments (ACL, PCL, MCL, and LCL) and tendons — requires evaluating hereditary integrals and calibrating multi-parameter non-linear constitutive equations against experimental ramp-and-hold stress relaxation or creep datasets [@Fung1993; @Provenzano2001]. In practice, accounting for finite strain-ramp histories [@Abramowitch2004], enforcing thermodynamic and physical admissibility bounds on relaxation spectra, and quantifying parameter sensitivity across multiple loading regimes remain computationally demanding and error-prone.

Researchers in experimental mechanics and biomechanics frequently rely on ad-hoc scripts coupled to commercial toolboxes (e.g., MATLAB Optimization Toolbox, OriginLab Pro) or heavy 3D finite element environments [@Maas2012] even when performing scalar (1D) uniaxial constitutive identification. This creates reproducibility barriers, licensing bottlenecks, and performance limitations when batch-processing large experimental campaigns composed of multiple specimen datasets. `MelloSilveiraTools` addresses this gap by providing a strongly typed, native-AOT-compatible C#/.NET 10 scientific library that unifies analytical and numerical calculus, viscoelastic constitutive formulations, constrained global-to-local curve fitting, and concurrent dataflow pipelines in an open-source ecosystem.

# State of the field

The scientific computing landscape offers mature general-purpose optimization and numerical libraries in Python, such as `SciPy` [@Virtanen2020], as well as specialized 3D biomechanical finite element solvers such as `FEBio` [@Maas2012]. In the .NET ecosystem, `MathNet.Numerics` provides foundational linear algebra, probability distributions, and unconstrained local minimizers (e.g., BFGS and Nelder-Mead), while `ALGLIB` provides low-level numerical optimization routines.

However, existing .NET numerical libraries do not provide domain-specific abstractions for continuum mechanics or viscoelasticity, nor do they implement automated experimental phase segmentation or multi-stage constitutive identification workflows. Rather than reinventing established mathematical primitives, `MelloSilveiraTools` strategically builds upon and extends `MathNet.Numerics` and embedded `ALGLIB` routines to deliver a domain-tailored biomechanical architecture motivated by four key requirements:

1. **Domain-Specific Constitutive Formulations:** Direct implementation of hereditary viscoelastic models — including continuous-spectrum Fung Quasi-Linear Viscoelasticity (QLV) [@Fung1993], discrete Prony-series Generalized Maxwell / Simplified Fung QLV [@Abramowitch2004], Schapery non-linear viscoelasticity [@Schapery1969], and the Modified Superposition Method — under explicit ramp-time regimes (`Disregard`, `ConsiderWithoutViscoelasticEffect`, and `ConsiderWithViscoelasticEffect`).
2. **Hybrid Global-Local Constrained Optimization:** Constitutive models such as continuous-spectrum Fung QLV and multi-branch Prony series exhibit strong parameter collinearity and non-linear admissibility constraints. `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` orchestrates derivative-free adaptive Differential Evolution [@Storn1997] with penalty barriers for physical rules followed by bounded L-BFGS local gradient polishing and Student-$t$ parameter confidence interval estimation.
3. **High-Throughput Streaming & Batch Execution:** Integration with .NET Task Parallel Library (TPL) Dataflow (`MelloSilveiraTools.Core.Pipelines`) enables backpressure-aware processing of experimental data streams, throttled batch execution across multiple specimen folders, real-time progress telemetry (NDJSON/OpenTelemetry), and deterministic SHA-256 artifact tracking.
4. **Architectural Modularity and Local Testability:** Pure domain mechanics and numerical routines are encapsulated in standalone packages with zero web or external database dependencies, ensuring complete local testability via standard CLI commands (`dotnet test`) and easy embeddability into desktop, console, or high-performance computing environments.

# Software design

`MelloSilveiraTools` is architected around explicit separation of concerns, immutability, and compile-time type safety across seven contextual NuGet packages:

- **`MelloSilveiraTools.Mathematics`:** Implements symbolic-like function hierarchies (`Function`, `PolynomialFunction`, `ExponencialFunction`, `PowerLaw`, `SineFunction`, `CosineFunction`) with lazy analytical derivatives and integrals, mathematical expressions (`PronySeries`), Simpson's rule numerical integration, finite-difference differentiation, root-finding algorithms (`BisectionMethod`, `BrentMethod` [@Brent1971], `StepByStepMethod`), and Newmark / Newmark-$\beta$ differential equation solvers [@Newmark1959].
- **`MelloSilveiraTools.MechanicsOfMaterials`:** Built on `Mathematics`, this package provides classic fatigue criteria (Goodman and Marin), cross-sectional geometric property calculators, 3D force/vector load-sharing models, and a unified viscoelastic calculator hierarchy (`IMechanicalModelCalculator<T>`). Concrete calculators cover `ElasticModelCalculator`, `MaxwellModelCalculator`, `FungModelCalculator`, `SimplifiedFungModelCalculator`, `SchaperyModelCalculator`, and `ModifiedSuperpositionMethodCalculator`. Pre-compiled expression trees in `IMechanicalModelCalculatorFacade` eliminate reflection overhead during time-history simulations.
- **`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`:** Implements end-to-end TPL Dataflow pipelines and application commands for experimental mechanics and sensitivity analysis:
  1. *Single-Group Experimental Processing (`ExecuteExperimentalDataPipeline`):* Ingests synchronized `strain.csv` and `stress.csv` file pairs, streams data points through `ExperimentalDataSegmenterStep` to classify deformation phases (`Ramp`, `Relaxation`, `Descent`, `Recovery`) via sliding-window numerical differentiation, applies temporal downsampling (`ExperimentalDataDownsamplerStep`), accumulates phase curves (`CurveSegmentAccumulatorStep`), and executes multi-stage constitutive parameter identification (`IMechanicalModelCurveFitterStep`).
  2. *Automated Batch Preparation & Execution (`ExecuteBatchExperimentalDataPipeline`):* Scans experimental directories to prepare and organize multiple `strain.csv` and `stress.csv` pairs into batch requests, executing the processing pipeline across all specimen groups either sequentially or in parallel with configurable `MaxDegreeOfParallelism` enforced via non-blocking `SemaphoreSlim` throttling and per-group fault isolation.
  3. *Baseline Parametric Sweep (`ExecuteParametricSweepPipeline`):* Conducts concurrent One-At-A-Time (OAAT) sensitivity analyses for a single baseline constitutive parameter set across varying strain amplitudes, ramp times, and rate regimes, evaluating initial stress, relaxation decay, and steady-state relaxation asymptotes via non-throwing root-finding.
  4. *Multi-Model Sweep from Experimental Pipeline Runs (`ExecuteParametricSweepFromExperimentalDataCommand`):* Retrieves all calibrated constitutive parameter entities produced by an experimental pipeline run sharing a common `Identifier`, deserializes polymorphic parameter payloads, and dispatches parametric sweeps across all identified models in sequential or parallel modes.
- **Supporting Infrastructure (`Core`, `Database`, `WebApi`, `Plugins`):** Provides the underlying TPL Dataflow and fluent pipeline builders, Polly v8 resilience pipelines, Dapper/PostgreSQL persistence for fitted constitutive parameters, and dynamic plugin loading. While the core scientific packages are entirely headless, `MelloSilveiraTools.WebApi` adheres to strict ASP.NET Core MVC patterns (`CrudController`, `ExperimentalDataController`, `ParametricSweepController`) and Minimal APIs, exposing streaming NDJSON telemetry without compromising the standalone testability of the scientific engine.

All packages target `.NET 10.0` with strict nullable reference types and include an automated unit test suite of 449 tests verifying analytical solutions, numerical convergence, and pipeline topology.

# Research impact statement

`MelloSilveiraTools` serves as the primary computational engine for ongoing Master's thesis research (*Viscoelastic Characterization and Load-Sharing Modeling of Knee Ligaments: A Unified Computational Framework*) at the Graduate Program in Mechanical Engineering and Materials Technology (PPEMM) at Universidade Federal de Ciência e Inovação do Rio de Janeiro (UFCIRJ), Rio de Janeiro, Brazil. In this research, the library is used to process batches of experimental ramp-and-hold tensile test files from human and animal knee ligaments (anterior cruciate ligament [ACL], posterior cruciate ligament [PCL], medial collateral ligament [MCL], and lateral collateral ligament [LCL]), systematically benchmarking continuous-spectrum Fung QLV against discrete Prony-series QLV and non-linear Schapery formulations across different strain-ramp boundary assumptions. By completely replacing proprietary commercial software (MATLAB Optimization Toolbox and OriginLab Pro) with open-source NuGet packages under the GNU GPLv3 license, `MelloSilveiraTools` provides biomechanics and materials engineering laboratories with a transparent, fully reproducible, and extensible platform for experimental characterization.

# AI usage disclosure

Generative AI tools (specifically large language models via Google Antigravity, powered by Anthropic Claude 5.5 Sonnet, Anthropic Claude 5.5 Opus and Google Gemini 3.8 Flash) were used during the development of this software and the preparation of this manuscript. In software development, AI assistance was employed for boilerplate code refactoring, XML API documentation generation, and expanding unit test scaffolding across edge cases. During manuscript preparation, AI assistance was used to structure and copy-edit the Markdown text and format bibliographic citations according to JOSS guidelines. All scientific conceptualizations, mathematical formulations, constitutive equations, numerical solver implementations, optimization constraints, and experimental validation results were conceived, verified, and audited directly by the authors, who made all core architectural decisions and assume full intellectual responsibility for the work.

# Acknowledgements

The authors acknowledge the institutional and academic support of the Graduate Program in Mechanical Engineering and Materials Technology (PPEMM) at the Universidade Federal de Ciência e Inovação do Rio de Janeiro (UFCIRJ), Rio de Janeiro, Brazil.

# References
