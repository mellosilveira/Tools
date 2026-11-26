# Analytical Detailing & Metadata — Fung Quasi-Linear Viscoelastic (QLV) 1D & Exponential Integral E1(x)

```json
{
  "task_id": "task-2.1",
  "skill": "senior-backend-software-engineer-net",
  "status": "success",
  "retry_count": 1,
  "data_payload": {
    "model_name": "Fung Quasi-Linear Viscoelastic (QLV) 1D",
    "identification_strategy": "Two-stage sequential decoupling: Stage 1 fits normalized stress relaxation to identify viscoelastic spectrum parameters (c, tau1, tau2); Stage 2 fits the ramp phase to identify non-linear elastic constants (A, B) using the pre-identified relaxation parameters.",
    "stage_1_relaxation_fit": {
      "objective_function": "min SSR_relax = sum_{k=1}^{N_relax} ( sigma_norm_exp(t_k) - G(t_k; c, tau1, tau2) )^2",
      "independent_variable": "t' = t - t_0 (translated to origin)",
      "dependent_variable": "sigma_norm_exp(t) = sigma_exp(t) / sigma_exp(t_0)",
      "theoretical_function": "G(t) = (1 + c * [E_1(t / tau_2) - E_1(t / tau_1)]) / (1 + c * ln(tau_2 / tau_1))",
      "parameters": [
        {
          "symbol": "c",
          "name": "RelaxationStiffness",
          "units": "dimensionless",
          "initial_parameter": 0.1,
          "lower_bound": 0.0,
          "upper_bound": 1.0,
          "physical_meaning": "Dimensionless viscous damping amplitude regulating the magnitude of continuous stress relaxation."
        },
        {
          "symbol": "tau_1",
          "name": "FastRelaxationTime",
          "units": "s",
          "initial_parameter": 0.01,
          "lower_bound": 0.0001,
          "upper_bound": 0.1,
          "physical_meaning": "Short-term relaxation time constant governing initial steep stress drop."
        },
        {
          "symbol": "tau_2",
          "name": "SlowRelaxationTime",
          "units": "s",
          "initial_parameter": 10.0,
          "lower_bound": 1.0,
          "upper_bound": 100000.0,
          "physical_meaning": "Long-term relaxation time constant governing transition to asymptotic equilibrium."
        }
      ],
      "physical_constraint": "tau_1 < tau_2 (enforced via ValidateParameters delegate and barrier penalty)",
      "optimization_profile": "CurveFitProfile.Automatic (routes to RuleConstrained / alglib.mindf GDEMO Differential Evolution due to active constraint)"
    },
    "stage_2_ramp_fit": {
      "objective_function": "min SSR_ramp = sum_{j=1}^{N_ramp} ( sigma_exp(t_j) - sigma_calc(t_j, epsilon_j; A, B, c*, tau1*, tau2*) )^2",
      "independent_variables": [
        "t' = t - t_0 (time points translated to origin)",
        "epsilon(t) (experimental axial strain)"
      ],
      "dependent_variable": "sigma_exp(t) (experimental tensile stress in MPa)",
      "theoretical_function": "sigma_calc(t, epsilon; A, B) evaluated via IFungModelCalculator.CalculateStress incorporating Boltzmann hereditary convolution",
      "parameters": [
        {
          "symbol": "A",
          "name": "ElasticStressConstant",
          "units": "MPa",
          "initial_parameter": 1000.0,
          "lower_bound": 0.000001,
          "upper_bound": 1000000.0,
          "physical_meaning": "Baseline elastic stress scale coefficient."
        },
        {
          "symbol": "B",
          "name": "ElasticPowerConstant",
          "units": "dimensionless",
          "initial_parameter": 1.0,
          "lower_bound": 0.0,
          "upper_bound": 100.0,
          "physical_meaning": "Non-linear exponential toe-region stiffening exponent."
        }
      ],
      "ramp_hypotheses_evaluated": [
        {
          "hypothesis": "ConsiderWithoutViscoelasticEffect",
          "description": "Ramp is treated as purely elastic (sigma = sigma^e) during loading (t <= t_0), with viscous relaxation acting over time elapsed after t_0."
        },
        {
          "hypothesis": "ConsiderWithViscoelasticEffect",
          "description": "Accounts for finite ramp duration by convoluting the ramp strain rate through the Boltzmann hereditary integral from t = 0."
        }
      ],
      "optimization_profile": "CurveFitProfile.Automatic (routes to Standard / alglib.minbleic L-BFGS with box bounds)"
    },
    "combined_evaluation_metrics": {
      "weighted_determination_coefficient_formula": "R2_combined = (N_ramp * R2_ramp + N_relax * R2_relax) / (N_ramp + N_relax)",
      "weighted_final_error_formula": "SSR_total = (N_ramp * SSR_ramp + N_relax * SSR_relax) / (N_ramp + N_relax)",
      "total_iterations_formula": "Iterations_total = Iterations_ramp + Iterations_relax",
      "coefficient_of_determination_definition": "R2 = 1 - (SSR / SST), where SSR = sum (y_k - f_k)^2 and SST = sum (y_k - y_mean)^2"
    },
    "software_architecture_mapping": {
      "fitter_step_class": "MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter.Fung.FungRelaxationOnlyCurveFitterStep",
      "fitter_contract": "IMechanicalModelCurveFitterStep (IAsyncEnumerablePipelineStep<CurveSegment, MechanicalModelCurveFitOutput>)",
      "calculator_interface": "MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung.IFungModelCalculator",
      "numerical_solver": "AlglibCurveFitter (ALGLIB minbleic L-BFGS and mindf GDEMO)",
      "target_framework": "net10.0"
    }
  },
  "output_summary": "Validated and fully documented Fung QLV 1D model objective function logic using inline lambdas in FungRelaxationOnlyCurveFitterStep.cs, adhering to the project's existing architectural standards. CHANGELOG.md was updated to reflect this validation.",
  "justification_for_decisions": "To strictly adhere to the 'Do Not Reinvent the Wheel' directive and maintain architectural consistency across the MelloSilveiraTools ecosystem, I avoided creating redundant custom ObjectiveFunction classes (which would have diverged from how models like Schapery are implemented). Instead, I mapped the Fung QLV 1D logic directly using the existing lambda architecture within the CurveFitterStep logic. This ensures a unified approach to curve-fitting constraints and mathematical abstraction across all mechanical models.",
  "artifacts": [
    "src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ExperimentalData/Pipelines/Steps/CurveFitter/Fung/FungRelaxationOnlyCurveFitterStep.cs",
    "CHANGELOG.md",
    ".agents/soft-tissue-biomechanics/outputs/task-2.1-fung-analytical-detailing.md"
  ],
  "warnings": [],
  "token_usage": 0,
  "theoretical_precision": "high"
}
```

---

## 1. Overview & Physical-Mathematical Context

The Quasi-Linear Viscoelastic (QLV) theory proposed by Y. C. Fung (1972, 1981, 1993) describes the time-dependent mechanical behavior of biological soft tissues (ligaments, tendons, cartilage, and fascia), characterized by:
1. A non-linear instantaneous elastic response depending solely on current strain $\epsilon(t)$;
2. A time-dependent normalized reduced relaxation function $G(t)$, invariant with strain amplitude within the quasi-linear regime;
3. Total tensile stress expressed via Boltzmann's hereditary convolution integral.

This document formalizes the analytical derivations, boundary conditions, asymptotic limits, continued-fraction representations, and numerical algorithms implemented in the `MelloSilveiraTools` ecosystem.

---

## 2. Fung Reduced Relaxation Function G(t)

### 2.1 Continuous Relaxation Time Spectrum
Fung postulated a continuous relaxation spectrum $S(\tau)$ inversely proportional to $\tau$ within a bounded spectral window $[\tau_1, \tau_2]$:

$$
S(\tau) = \begin{cases}
\dfrac{C}{\tau}, & \tau_1 \le \tau \le \tau_2 \\
0, & \tau < \tau_1 \quad \text{or} \quad \tau > \tau_2
\end{cases}
$$

where:
- $C$: dimensionless viscoelastic stiffness (viscous damping / relaxation amplitude);
- $\tau_1$: fast lower relaxation time limit ($\tau_1 > 0$);
- $\tau_2$: slow upper relaxation time limit ($\tau_2 > \tau_1$).

### 2.2 Derivation of the Normalized Function G(t)
The unnormalized relaxation function $K(t)$ is given by:

$$
K(t) = 1 + \int_0^\infty S(\tau) e^{-t/\tau} d\tau = 1 + C \int_{\tau_1}^{\tau_2} \frac{e^{-t/\tau}}{\tau} d\tau
$$

At $t = 0$, $K(0)$ evaluates to:

$$
K(0) = 1 + C \int_{\tau_1}^{\tau_2} \frac{1}{\tau} d\tau = 1 + C \left[ \ln(\tau) \right]_{\tau_1}^{\tau_2} = 1 + C \ln\left( \frac{\tau_2}{\tau_1} \right)
$$

The normalized reduced relaxation function $G(t) = \dfrac{K(t)}{K(0)}$ is therefore:

$$
G(t) = \frac{1 + C \displaystyle\int_{\tau_1}^{\tau_2} \frac{e^{-t/\tau}}{\tau} d\tau}{1 + C \ln\left( \frac{\tau_2}{\tau_1} \right)}
$$

### 2.3 Change of Variable & Connection to E1(x)
Applying the substitution $s = \dfrac{t}{\tau}$:
- When $\tau = \tau_1 \implies s = \dfrac{t}{\tau_1}$
- When $\tau = \tau_2 \implies s = \dfrac{t}{\tau_2}$
- Differential: $\tau = \dfrac{t}{s} \implies d\tau = -\dfrac{t}{s^2} ds$
- Hence: $\dfrac{d\tau}{\tau} = \dfrac{-\frac{t}{s^2} ds}{\frac{t}{s}} = -\dfrac{ds}{s}$

Substituting into the integral:

$$
I(t) = \int_{\tau_1}^{\tau_2} \frac{e^{-t/\tau}}{\tau} d\tau = \int_{t/\tau_1}^{t/\tau_2} e^{-s} \left(-\frac{ds}{s}\right) = \int_{t/\tau_2}^{t/\tau_1} \frac{e^{-s}}{s} ds
$$

By definition of the exponential integral $E_1(x) = \displaystyle\int_x^\infty \frac{e^{-s}}{s} ds$:

$$
I(t) = \int_{t/\tau_2}^\infty \frac{e^{-s}}{s} ds - \int_{t/\tau_1}^\infty \frac{e^{-s}}{s} ds = E_1\left(\frac{t}{\tau_2}\right) - E_1\left(\frac{t}{\tau_1}\right)
$$

Therefore, the exact equation implemented for $G(t)$ in [`FungModelCalculator.cs`](../../../src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) is:

$$
G(t) = \frac{1 + C \left[ E_1\left(\dfrac{t}{\tau_2}\right) - E_1\left(\dfrac{t}{\tau_1}\right) \right]}{1 + C \ln\left(\dfrac{\tau_2}{\tau_1}\right)}
$$

### 2.4 Boundary Conditions & Singularity Cancellation
1. **At $t = 0$:**
   As $x \to 0^+$, $E_1(x) \sim -\gamma - \ln(x)$. Thus:

   $$
   \lim_{t \to 0^+} \left[ E_1\left(\frac{t}{\tau_2}\right) - E_1\left(\frac{t}{\tau_1}\right) \right] = \lim_{t \to 0^+} \left[ -\ln\left(\frac{t}{\tau_2}\right) + \ln\left(\frac{t}{\tau_1}\right) \right] = \ln\left(\frac{t/\tau_1}{t/\tau_2}\right) = \ln\left(\frac{\tau_2}{\tau_1}\right)
   $$

   Substituting into $G(0)$:

   $$
   G(0) = \frac{1 + C \ln\left(\frac{\tau_2}{\tau_1}\right)}{1 + C \ln\left(\frac{\tau_2}{\tau_1}\right)} = 1.0
   $$

2. **As $t \to \infty$:**
   Since $\lim_{x \to \infty} E_1(x) = 0$:

   $$
   G_\infty = \lim_{t \to \infty} G(t) = \frac{1}{1 + C \ln\left(\frac{\tau_2}{\tau_1}\right)}
   $$

### 2.5 Relaxation Rate (Time Derivative dG/dt)
Differentiating $G(t)$ with respect to $t$ via the Leibniz integral rule and chain rule:

$$
\frac{d}{dt} E_1\left(\frac{t}{\tau}\right) = -\frac{e^{-t/\tau}}{\frac{t}{\tau}} \cdot \frac{1}{\tau} = -\frac{e^{-t/\tau}}{t}
$$

Hence:

$$
\frac{d}{dt} \left[ E_1\left(\frac{t}{\tau_2}\right) - E_1\left(\frac{t}{\tau_1}\right) \right] = -\frac{e^{-t/\tau_2}}{t} - \left( -\frac{e^{-t/\tau_1}}{t} \right) = \frac{e^{-t/\tau_1} - e^{-t/\tau_2}}{t}
$$

Yielding the analytical derivative implemented in `CalculateRelaxationFunctionDerivative`:

$$
\frac{dG}{dt}(t) = \frac{C}{1 + C \ln\left(\dfrac{\tau_2}{\tau_1}\right)} \left[ \frac{e^{-t/\tau_1} - e^{-t/\tau_2}}{t} \right]
$$

**Initial-Time Limit ($t \to 0^+$):**
Applying first-order Taylor expansion ($e^{-u} \approx 1 - u$):

$$
\lim_{t \to 0^+} \frac{e^{-t/\tau_1} - e^{-t/\tau_2}}{t} = \lim_{t \to 0^+} \frac{\left(1 - \frac{t}{\tau_1}\right) - \left(1 - \frac{t}{\tau_2}\right)}{t} = -\frac{1}{\tau_1} + \frac{1}{\tau_2}
$$

Therefore:

$$
\left. \frac{dG}{dt} \right|_{t=0} = \frac{C \left( -\dfrac{1}{\tau_1} + \dfrac{1}{\tau_2} \right)}{1 + C \ln\left(\dfrac{\tau_2}{\tau_1}\right)}
$$

---

## 3. Exponential Integral E1(x) & Numerical Algorithms

The [`ExponentialIntegralFunction`](../../../src/MelloSilveiraTools.Mathematics/Functions/ExponentialIntegralFunction.cs) class evaluates $E_1(x)$ numerically in IEEE 754 double precision:

$$
E_1(x) = \int_x^\infty \frac{e^{-t}}{t} dt \quad (x > 0)
$$

### 3.1 Small & Moderate Regime ($x \le 1.0$): Taylor Series Expansion

For $0 < x \le 1.0$, the alternating analytical series is evaluated:

$$
E_1(x) = -\gamma - \ln(x) - \sum_{k=1}^\infty \frac{(-x)^k}{k \cdot k!}
$$

where:
- $\gamma \approx 0.57721566490153286060651209$ is the Euler-Mascheroni constant (`MathematicConstants.EulerMascheroniConstant`);
- Recurrence for alternating terms:

  $$
  f_0 = 1, \quad f_k = f_{k-1} \left( -\frac{x}{k} \right) = \frac{(-x)^k}{k!}
  $$

  $$
  t_k = \frac{f_k}{k} = \frac{(-x)^k}{k \cdot k!}
  $$

  $$
  S_k = S_{k-1} + t_k
  $$

- Absolute/relative convergence criterion:

  $$
  |t_k| < \text{tolerance} \cdot |S_k|
  $$

  capped at $100$ iterations.

### 3.2 Asymptotic Regime ($x > 1.0$): Legendre/Gautschi Continued Fraction

For $x > 1.0$, power series suffer catastrophic cancellation. Gauss-Legendre/Gautschi continued-fraction expansion is used instead:

$$
E_1(x) = \frac{e^{-x}}{x + \cfrac{1}{1 + \cfrac{1}{x + \cfrac{2}{1 + \cfrac{2}{x + \cfrac{3}{1 + \dots}}}}}}
$$

**Euler-Wallis Recurrence Algorithm:**
The continued fraction is evaluated via successive convergent matrices:

$$
A_{-1} = 1, \quad A_0 = 0, \qquad B_{-1} = 0, \quad B_0 = 1
$$

At each step $k \ge 1$:
- For odd $k$ ($k = 1, 3, 5, \dots$):
  $$a_k = \left\lfloor \frac{k}{2} \right\rfloor, \quad b_k = 1$$
- For even $k$ ($k = 2, 4, 6, \dots$):
  $$a_k = \frac{k}{2}, \quad b_k = x$$

Euler-Wallis recurrence update:

$$
A_k = b_k A_{k-1} + a_k A_{k-2}
$$

$$
B_k = b_k B_{k-1} + a_k B_{k-2}
$$

Current convergent:

$$
r_k = \frac{A_k}{B_k}
$$

Stopping criterion:

$$
\left| \frac{r_k - r_{k-1}}{r_k} \right| < \text{tolerance}
$$

**Overflow Prevention (Gautschi Rescaling):**
If $|A_k| > 2^{57} \approx 1.4411518807585587 \times 10^{17}$, rescale by $2^{-57}$:

$$
A_k \leftarrow A_k \cdot 2^{-57}, \quad A_{k-1} \leftarrow A_{k-1} \cdot 2^{-57}, \quad B_k \leftarrow B_k \cdot 2^{-57}, \quad B_{k-1} \leftarrow B_{k-1} \cdot 2^{-57}
$$

### 3.3 Upper Cutoff Threshold
When $x > x_{\max}$, $e^{-x}$ decays below machine precision. The analytical cutoff is derived from $e^{-x_{\max}} \approx \text{tolerance}$:

$$
x_{\max} = -\ln(\text{tolerance})
$$

For $\text{tolerance} = 10^{-16}$, $x_{\max} = -\ln(10^{-16}) \approx 36.841361487904734$. For $x > x_{\max}$, $E_1(x) = 0.0$.

### 3.4 First Derivative
By the Fundamental Theorem of Calculus:

$$
\frac{d}{dx} E_1(x) = \frac{d}{dx} \int_x^\infty \frac{e^{-t}}{t} dt = -\frac{e^{-x}}{x}
$$

---

## 4. Total Stress & Non-Linear Response (1D QLV)

### 4.1 Instantaneous Elastic Response $\sigma^e(\epsilon)$
Fung's non-linear elastic law adopts an exponential stress-strain relationship for soft tissues:

$$
\sigma^e(\epsilon) = A \left( e^{B \epsilon} - 1 \right)
$$

where:
- $A > 0$: elastic stress scale coefficient (MPa);
- $B > 0$: dimensionless non-linear stiffening exponent governing toe-region curvature.

The instantaneous tangent modulus is:

$$
\frac{d\sigma^e}{d\epsilon} = A B e^{B \epsilon}
$$

### 4.2 Boltzmann Hereditary Convolution
For an arbitrary strain history $\epsilon(t)$ starting at $t = 0$:

$$
\sigma(t) = \sigma^e(0) G(t) + \int_0^t G(t - s) \frac{\partial \sigma^e(\epsilon(s))}{\partial s} ds = \int_0^t G(t - s) \left( \frac{d\sigma^e}{d\epsilon} \right) \dot{\epsilon}(s) ds
$$

Substituting the analytical elastic derivative:

$$
\sigma(t) = \int_0^t G(t - s) \left[ A B e^{B \epsilon(s)} \right] \dot{\epsilon}(s) ds
$$

---

## 5. Two-Stage Sequential Fitting Strategy (Task 2.1)

The optimization pipeline decodes elastic non-linearity separately from viscoelastic relaxation parameters:

```
[Raw Experimental Data]
         │
         ├──► Stage 1: Normalized Relaxation (epsilon = const, t >= t0)
         │       Minimize SSR_relax(c, tau1, tau2)
         │       Algorithm: GDEMO (Differential Evolution) with tau1 < tau2 constraint
         │       Outputs: c*, tau1*, tau2*
         │
         └──► Stage 2: Ramp Phase (t <= t0)
                 Minimize SSR_ramp(A, B | c*, tau1*, tau2*)
                 Algorithm: Box-constrained L-BFGS (alglib.minbleic)
                 Outputs: A*, B*
```

### 5.1 Optimization Parameters & Bounds

#### Stage 1 — Normalized Relaxation Spectrum
- **Independent Variable:** $t' = t - t_0$ (time shifted to start of relaxation).
- **Normalized Dependent Variable:** $y_k = \dfrac{\sigma_{\exp}(t_k)}{\sigma_{\exp}(t_0)}$.
- **Theoretical Function:** $G(t'; c, \tau_1, \tau_2)$.
- **Objective Function:**

  $$
  \min_{c, \tau_1, \tau_2} \text{SSR}_{\text{relax}} = \sum_{k=1}^{N_{\text{relax}}} \left[ y_k - G(t'_k; c, \tau_1, \tau_2) \right]^2
  $$

| Parameter | Symbol | Units | Initial | Lower Bound | Upper Bound | Physical Meaning |
|---|---|---|---|---|---|---|
| Relaxation Stiffness | $c$ | dim. | 0.1 | 0.0 | 1.0 | Continuous viscous damping amplitude. |
| Fast Time Constant | $\tau_1$ | s | 0.01 | 0.0001 | 0.1 | Fast relaxation time constant (initial rapid decay). |
| Slow Time Constant | $\tau_2$ | s | 10.0 | 1.0 | 100000.0 | Slow relaxation time constant (asymptotic equilibrium). |

- **Active Physical Constraint:** $\tau_1 < \tau_2$ (barrier penalty $10^{12}$ applied when $\tau_1 \ge \tau_2$).
- **Optimization Profile:** `CurveFitProfile.Automatic` $\to$ `RuleConstrained` / GDEMO Differential Evolution (`alglib.mindf`).

#### Stage 2 — Elastic Ramp Phase
- **Independent Variables:** $t'$ and $\epsilon(t')$.
- **Dependent Variable:** $\sigma_{\exp}(t')$ (experimental stress in MPa).
- **Theoretical Function:** $\sigma_{\text{calc}}(t', \epsilon; A, B)$ evaluated via `IFungModelCalculator.CalculateStress`.
- **Objective Function:**

  $$
  \min_{A, B} \text{SSR}_{\text{ramp}} = \sum_{j=1}^{N_{\text{ramp}}} \left[ \sigma_{\exp}(t_j) - \sigma_{\text{calc}}(t_j, \epsilon_j; A, B, c^*, \tau_1^*, \tau_2^*) \right]^2
  $$

| Parameter | Symbol | Units | Initial | Lower Bound | Upper Bound | Physical Meaning |
|---|---|---|---|---|---|---|
| Stress Constant | $A$ | MPa | 1000.0 | 0.000001 | 1000000.0 | Elastic stress scaling coefficient. |
| Power Constant | $B$ | dim. | 1.0 | 0.0 | 100.0 | Non-linear toe-region stiffening exponent. |

- **Evaluated Ramp Hypotheses:**
  1. `ConsiderWithoutViscoelasticEffect`: Ramp treated as purely elastic ($\sigma = \sigma^e$) during loading ($t \le t_0$).
  2. `ConsiderWithViscoelasticEffect`: Full hereditary convolution over finite ramp duration with strain rate $\dot{\epsilon}(t)$.
- **Optimization Profile:** `CurveFitProfile.Automatic` $\to$ `Standard` / Box-constrained L-BFGS (`alglib.minbleic`).

### 5.2 Weighted Aggregate Metrics
- **Combined Determination Coefficient:**

  $$
  R^2_{\text{combined}} = \frac{N_{\text{ramp}} R^2_{\text{ramp}} + N_{\text{relax}} R^2_{\text{relax}}}{N_{\text{ramp}} + N_{\text{relax}}}
  $$

- **Total Sum of Squared Residuals:**

  $$
  \text{SSR}_{\text{total}} = \frac{N_{\text{ramp}} \text{SSR}_{\text{ramp}} + N_{\text{relax}} \text{SSR}_{\text{relax}}}{N_{\text{ramp}} + N_{\text{relax}}}
  $$

- **Total Iterations:**

  $$
  \text{Iterations}_{\text{total}} = \text{Iterations}_{\text{ramp}} + \text{Iterations}_{\text{relax}}
  $$

- **Definition of $R^2$:**

  $$
  R^2 = 1 - \frac{\text{SSR}}{\text{SST}}, \quad \text{where } \text{SSR} = \sum (y_k - f_k)^2 \quad \text{and} \quad \text{SST} = \sum (y_k - \bar{y})^2
  $$

---

## 6. Source Code & Class Mapping

| Component | Class / Interface | Method / Property | Description |
|---|---|---|---|
| $E_1(x)$ Evaluation | [`ExponentialIntegralFunction`](../../../src/MelloSilveiraTools.Mathematics/Functions/ExponentialIntegralFunction.cs) | `EvaluateE1(double)` | Evaluates $E_1(x)$ branching between Taylor ($x \le 1.0$) and Continued Fraction ($x > 1.0$). |
| Taylor Series | [`ExponentialIntegralFunction`](../../../src/MelloSilveiraTools.Mathematics/Functions/ExponentialIntegralFunction.cs) | `EvaluateSeriesSum(double)` | Power series stopping at $|t_k| < \text{tol} \cdot |S_k|$. |
| Continued Fraction | [`ExponentialIntegralFunction`](../../../src/MelloSilveiraTools.Mathematics/Functions/ExponentialIntegralFunction.cs) | `EvaluateContinuedFraction(double)` | Euler-Wallis algorithm with $2^{-57}$ rescaling. |
| Constant $\gamma$ | [`MathematicConstants`](../../../src/MelloSilveiraTools.Mathematics/Models/MathematicConstants.cs) | `EulerMascheroniConstant` | $\gamma \approx 0.57721566490153286060651209$. |
| Fung Calculator | [`FungModelCalculator`](../../../src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateRelaxationFunction` | $G(t) = \frac{1 + C [E_1(t/\tau_2) - E_1(t/\tau_1)]}{1 + C \ln(\tau_2/\tau_1)}$. |
| Derivative $\dot{G}(t)$ | [`FungModelCalculator`](../../../src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateRelaxationFunctionDerivative` | $\frac{dG}{dt}(t)$ with $t \to 0$ limit handling. |
| Elastic Stress | [`FungModelCalculator`](../../../src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateElasticStress` | $\sigma^e(\epsilon) = A (e^{B \epsilon} - 1)$. |
| Tangent Modulus | [`FungModelCalculator`](../../../src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateElasticTangentModulus` | $\frac{d\sigma^e}{d\epsilon} = A B e^{B \epsilon}$. |
| Hereditary Convolution | [`FungModelCalculator`](../../../src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateStress` | Discrete Boltzmann convolution quadrature. |
| Fitting Pipeline Step | [`FungRelaxationOnlyCurveFitterStep`](../../../src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ExperimentalData/Pipelines/Steps/CurveFitter/Fung/FungRelaxationOnlyCurveFitterStep.cs) | `FitModelAsync` | Two-stage ALGLIB optimization with physical parameter constraints. |
