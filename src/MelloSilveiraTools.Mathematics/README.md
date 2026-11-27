# MelloSilveiraTools.Mathematics

High-performance numerical analysis and mathematical calculation library designed for scientific computing and mechanics of materials, with **zero external NuGet dependencies** and full **Native AOT compatibility**.

---

## 1. Key Capabilities

- **Symbolic-Like Function Hierarchy:** Base `Function` class with lazy analytical `Derivative` and `Integral` properties. Concrete implementations include:
  - `PolynomialFunction`, `ExponentialFunction`, `PowerLaw`, `SineFunction`, `CosineFunction`, and `ConstantFunction`.
  - Instantiated dynamically via `FunctionFactory`.
- **Mathematical Expressions:**
  - `MathExpression`: Linear combinations of heterogeneous functions.
  - `PronySeries`: Exponential decay series ($c + \sum a_n e^{b_n x}$) essential for viscoelastic relaxation spectrum modeling.
- **Differential Equation Solvers:**
  - Second-order dynamic ODE solvers: `NewmarkMethod` and `NewmarkBetaMethod` (keyed `IDifferentialEquationMethod`).
- **Numerical Calculus:**
  - Numerical Integration: `SimpsonRuleIntegration` (`IIntegration`).
  - Finite Difference Differentiation: `Differentiation` (`IDifferentiation`).
- **Root-Finding Algorithms (`IRootFinding`):**
  - High-precision, non-throwing bracketed root-finders: `BrentMethod`, `BisectionMethod`, and `StepByStepMethod`.
- **Statistics & 3D Geometry:**
  - `StatisticsCalculator`: Mean, variance, standard deviation, and covariance.
  - `Vector3D`, `Point3D`: Invariant-culture 3D vector algebra (`CultureInfo.InvariantCulture`).

---

## 2. Installation

```bash
dotnet add package MelloSilveiraTools.Mathematics
```

---

## 3. Quick Start

### Evaluating Analytical Derivatives and Integrals

```csharp
using MelloSilveiraTools.Mathematics.Functions;

// Define a polynomial: f(x) = 2 + 3x + 4x^2
PolynomialFunction poly = new([2.0, 3.0, 4.0]);

double valueAt2 = poly.Evaluate(2.0); // 2 + 6 + 16 = 24.0

// Obtain the exact analytical derivative: f'(x) = 3 + 8x
Function derivative = poly.Derivative;
double slopeAt2 = derivative.Evaluate(2.0); // 3 + 16 = 19.0
```

### Root-Finding with Brent's Method

```csharp
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

BrentMethod brent = new();
RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 5.0, Tolerance: 1e-7, MaxIterations: 100);
// Find root of f(x) = x^2 - 4 in interval [0, 5]
(double root, double error) = brent.FindRoot(input, x => x * x - 4.0);
// root ≈ 2.0
```

---

## 4. Dependency Injection & Technical Invariants

- **DI Entry Point:** `MathematicsDependencyInjection.AddMathematicsServices()`.
  - **Singletons (thread-safe):** `IDifferentiation`, `IIntegration`, `FunctionFactory`, `IStatisticsCalculator`; keyed: `IDifferentialEquationMethod` (`Newmark`, `NewmarkBeta`), `IRootFinding` (`BisectionMethod`, `BrentMethod`, `StepByStepMethod`).
- **Serialization:** `PronySeries` primary constructor is annotated with `[method: JsonConstructor]` for deterministic `System.Text.Json` round-trips.
- **AOT & Culture Invariance:** Zero third-party NuGet dependencies; `IsAotCompatible = true`; invariant-culture numerical parsing/formatting throughout.

---

## 5. Dependencies

- `MelloSilveiraTools.Core`
- *Zero third-party NuGet dependencies*
