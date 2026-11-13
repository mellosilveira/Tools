# Contributing to MelloSilveiraTools

Thank you for your interest in contributing to **MelloSilveiraTools**! This document provides guidelines for reporting issues, seeking support, and submitting code or documentation improvements.

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

---

## 1. Seeking Support & Asking Questions

If you have questions about installing the packages, configuring dependency injection, formulating constitutive viscoelastic models, or running experimental data optimization pipelines:

1. Check the [README.md](README.md) and XML API documentation included with each package.
2. Search existing [GitHub Issues](https://github.com/mellosilveira/Tools/issues) to see if your question has already been answered.
3. Open a new issue using the **Question / Support** label or start a thread in the repository issue tracker describing your use case, target .NET runtime, and a minimal code snippet.

---

## 2. Reporting Bugs & Issues

If you encounter a numerical discrepancy, bug, or unexpected behavior, please open an issue on the [GitHub Issue Tracker](https://github.com/mellosilveira/Tools/issues/new/choose) using the **Bug Report** template and include:

- The affected package(s) and version (e.g., `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` v2.0.0).
- Your operating system and `.NET SDK` version (`dotnet --version`).
- A minimal reproducible example (input parameters, constitutive model, or sample CSV segment).
- Expected numerical/functional behavior vs. actual output (including exception stack traces if applicable).

---

## 3. Suggesting Enhancements

Feature requests — such as new hyperelastic/viscoelastic constitutive equations, numerical integration routines, root-finding solvers, or curve-fitting algorithms — are welcome! Please open an issue using the **Feature Request** template and include:

- The mathematical formulation and primary literature reference (with DOI).
- The target package (`Mathematics`, `MechanicsOfMaterials`, `Optimizations`, etc.).
- Proposed input/output data contracts.

---

## 4. Development Setup & Running Tests

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later.
- Git.
- *(Optional)* Docker (only required if running external integration/acceptance tests against PostgreSQL).

### Building the Solution
Clone the repository and build the solution from the root directory:

```bash
git clone https://github.com/mellosilveira/Tools.git
cd Tools
dotnet restore MelloSilveiraTools.sln
dotnet build MelloSilveiraTools.sln --configuration Release
```

### Running the Automated Test Suite
All unit tests are located in `test/UnitTests/UnitTests.csproj` and run without external infrastructure dependencies:

```bash
dotnet test test/UnitTests/UnitTests.csproj --configuration Release
```

---

## 5. Coding Standards & Pull Request Process

To maintain consistency across the codebase, all contributions must adhere to the repository's `.editorconfig` rules:

1. **Explicit Type Declarations (No `var`):** Always declare explicit types (e.g., `double stress = 0.0;`, `List<CurveSegment> segments = [];`). Target-typed `new()` is encouraged when the left-hand type is explicit.
2. **Primary Constructors & Expression Bodies:** Prefer primary constructors for dependency injection and expression bodies (`=>`) for single-statement members.
3. **Deterministic Time & Seeding:** Use `TimeProvider` instead of `DateTimeOffset.UtcNow` in production code, and ensure stochastic optimizers support deterministic seeds.
4. **XML Documentation:** All public classes, records, interfaces, methods, and properties must include comprehensive XML documentation (`<summary>`, `<param>`, `<returns>`).
5. **Unit Tests:** Every new mathematical function, constitutive model calculator, or pipeline step must be accompanied by unit tests in `test/UnitTests/` verifying analytical solutions, edge cases, and invalid inputs.
6. **Changelog:** Document notable changes under the `[Unreleased]` section of [CHANGELOG.md](CHANGELOG.md) following Keep-a-Changelog conventions.

### Submitting a Pull Request
1. Fork the repository and create a feature branch from `main` (e.g., `feat/holzapfel-model` or `fix/prony-series-bounds`).
2. Ensure `dotnet build` and `dotnet test test/UnitTests/UnitTests.csproj` pass with zero errors.
3. Open a Pull Request against `main` referencing the related issue.

---

## 6. License

By contributing to **MelloSilveiraTools**, you agree that your contributions will be licensed under the [GNU Affero General Public License v3.0 or later (AGPL-3.0-or-later)](LICENSE).
