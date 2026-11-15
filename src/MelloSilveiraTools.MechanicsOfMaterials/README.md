# MelloSilveiraTools.MechanicsOfMaterials

Domain library for computational mechanics of materials, viscoelastic constitutive modeling, fatigue evaluation, and 3D soft-tissue load sharing.

---

## 1. Key Capabilities

- **Viscoelastic Constitutive Models (`IMechanicalModelCalculator<T>`):**
  - **Quasi-Linear Viscoelasticity (QLV):**
    - Continuous-spectrum Fung QLV (`FungModelCalculator`) integrating exponential integrals ($E_1$) over frequency spectra.
    - Discrete Prony-series QLV (`SimplifiedFungModelCalculator`) for multi-branch exponential relaxation.
  - **Non-Linear Viscoelasticity:**
    - Schapery's thermodynamic formulation (`SchaperyModelCalculator`) with non-linear transient functions.
    - Modified Superposition Method (`ModifiedSuperpositionMethodCalculator`).
  - **Linear Models:**
    - Classic Elasticity (`ElasticModelCalculator`) and Generalized Maxwell (`MaxwellModelCalculator`).
- **Strain-Ramp Regimes (`RampTimeConsideration`):**
  - Supports analytical solutions under finite strain rates: `Disregard`, `ConsiderWithoutViscoelasticEffect`, and `ConsiderWithViscoelasticEffect`.
- **Pre-Compiled Simulation Facade (`IMechanicalModelCalculatorFacade`):**
  - Compiles expression trees to eliminate reflection overhead during large-scale time-history simulations.
- **Fatigue Criteria (`IFatigueCalculator`):**
  - Uniaxial fatigue life and safety factors under Goodman and Marin failure criteria.
- **Geometric Properties:**
  - `IGeometricPropertyCalculator<CircularProfile>` and `IGeometricPropertyCalculator<RectangularProfile>`.
- **3D Soft-Tissue Load Sharing (`ILoadSharingCalculator`):**
  - Models load projection, displacement, and spatial kinematics of ligament structures in 3D coordinate space.

---

## 2. Installation

```bash
dotnet add package MelloSilveiraTools.MechanicsOfMaterials
```

---

## 3. Quick Start

### Evaluating a Quasi-Linear Viscoelastic (Prony Series) Model

```csharp
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

// Define a 2-branch Prony series: G(t) = c + a1*exp(-t/tau1) + a2*exp(-t/tau2)
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

double predictedStress = calculator.CalculateStress(input);
```

---

## 4. Dependencies

- `MelloSilveiraTools.Mathematics`
- `Microsoft.Extensions.DependencyInjection.Abstractions` (10.0.11)
