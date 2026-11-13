# Detalhamento Analítico & Metadados — Fung Quasi-Linear Viscoelastic (QLV) 1D & Integral Exponencial E1(x)

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
      "fitter_step_class": "MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter.FungRelaxationOnlyCurveFitterStep",
      "fitter_contract": "IMechanicalModelCurveFitterStep (IAsyncEnumerablePipelineStep<CurveSegment, MechanicalModelCurveFitOutput>)",
      "calculator_interface": "MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung.IFungModelCalculator",
      "numerical_solver": "AlglibCurveFitter (ALGLIB minbleic L-BFGS and mindf GDEMO)",
      "target_framework": "net10.0"
    }
  },
  "output_summary": "Validated and fully documented Fung QLV 1D model objective function logic using inline lambdas in FungRelaxationOnlyCurveFitterStep.cs, adhering to the project's existing architectural standards. CHANGELOG.md was updated to reflect this validation.",
  "justification_for_decisions": "To strictly adhere to the 'Do Not Reinvent the Wheel' directive and maintain architectural consistency across the MelloSilveiraTools ecosystem, I avoided creating redundant custom ObjectiveFunction classes (which would have diverged from how models like Schapery are implemented). Instead, I mapped the Fung QLV 1D logic directly using the existing lambda architecture within the CurveFitterStep logic. This ensures a unified approach to curve-fitting constraints and mathematical abstraction across all mechanical models.",
  "artifacts": [
    "D:\\Mello Silveira Serviços LTDA\\Projetos\\Tools\\src\\MelloSilveiraTools.MechanicsOfMaterials.Optimizations\\Pipelines\\ExperimentalData\\Steps\\CurveFitter\\FungRelaxationOnlyCurveFitterStep.cs",
    "D:\\Mello Silveira Serviços LTDA\\Projetos\\Tools\\CHANGELOG.md",
    "D:\\Mello Silveira Serviços LTDA\\Projetos\\Tools\\.agents\\soft-tissue-biomechanics\\outputs\\task-2.1-fung-analytical-detailing.md"
  ],
  "warnings": [],
  "token_usage": 0,
  "theoretical_precision": "high"
}
```

---

## 1. Visão Geral e Contexto Físico-Matemático

A teoria da Viscoelasticidade Quase-Linear (QLV) proposta por Y.C. Fung (1972, 1981, 1993) descreve o comportamento mecânico dependente do tempo de tecidos moles biológicos (ligamentos, tendões, cartilagem e fáscias), caracterizados por:
1. Resposta elástica instantânea não linear dependente exclusivamente da deformação atual $\epsilon(t)$;
2. Função de relaxação reduzida contínua dependente do histórico temporal $G(t)$, invariante com a amplitude de deformação no regime quase-linear;
3. Resposta tensil total expressa pela integral de convolução hereditária de Boltzmann.

Este documento formaliza as deduções analíticas, condições de contorno, singularidades assintóticas, formulações em frações contínuas e algoritmos numéricos implementados no ecossistema `MelloSilveiraTools`.

---

## 2. Função de Relaxação Reduzida de Fung G(t)

### 2.1 Espectro Contínuo de Tempos de Relaxação
Fung postulou um espectro contínuo de relaxação $S(\tau)$ inversamente proporcional a $\tau$ dentro de uma janela espectral limitada $[\tau_1, \tau_2]$:

$$
S(\tau) = \begin{cases}
\dfrac{C}{\tau}, & \tau_1 \le \tau \le \tau_2 \\
0, & \tau < \tau_1 \quad \text{ou} \quad \tau > \tau_2
\end{cases}
$$

onde:
- $C$: rigidez viscoelástica adimensional (viscous damping / relaxation amplitude);
- $\tau_1$: tempo de relaxação rápido inferior (fast relaxation time limit, com $\tau_1 > 0$);
- $\tau_2$: tempo de relaxação lento superior (slow relaxation time limit, com $\tau_2 > \tau_1$).

### 2.2 Dedução da Função Normalizada G(t)
A função de relaxação não normalizada $K(t)$ é dada por:

$$
K(t) = 1 + \int_0^\infty S(\tau) e^{-t/\tau} d\tau = 1 + C \int_{\tau_1}^{\tau_2} \frac{e^{-t/\tau}}{\tau} d\tau
$$

No instante $t = 0$, $K(0)$ assume o valor:

$$
K(0) = 1 + C \int_{\tau_1}^{\tau_2} \frac{1}{\tau} d\tau = 1 + C \left[ \ln(\tau) \right]_{\tau_1}^{\tau_2} = 1 + C \ln\left( \frac{\tau_2}{\tau_1} \right)
$$

A função de relaxação reduzida normalizada $G(t) = \dfrac{K(t)}{K(0)}$ é portanto:

$$
G(t) = \frac{1 + C \displaystyle\int_{\tau_1}^{\tau_2} \frac{e^{-t/\tau}}{\tau} d\tau}{1 + C \ln\left( \frac{\tau_2}{\tau_1} \right)}
$$

### 2.3 Mudança de Variável e Conexão com E1(x)
Aplicando a transformação de variável $s = \dfrac{t}{\tau}$:
- Quando $\tau = \tau_1 \implies s = \dfrac{t}{\tau_1}$
- Quando $\tau = \tau_2 \implies s = \dfrac{t}{\tau_2}$
- Diferencial: $\tau = \dfrac{t}{s} \implies d\tau = -\dfrac{t}{s^2} ds$
- Logo: $\dfrac{d\tau}{\tau} = \dfrac{-\frac{t}{s^2} ds}{\frac{t}{s}} = -\dfrac{ds}{s}$

Substituindo na integral:

$$
I(t) = \int_{\tau_1}^{\tau_2} \frac{e^{-t/\tau}}{\tau} d\tau = \int_{t/\tau_1}^{t/\tau_2} e^{-s} \left(-\frac{ds}{s}\right) = \int_{t/\tau_2}^{t/\tau_1} \frac{e^{-s}}{s} ds
$$

Pela definição da integral exponencial $E_1(x) = \displaystyle\int_x^\infty \frac{e^{-s}}{s} ds$:

$$
I(t) = \int_{t/\tau_2}^\infty \frac{e^{-s}}{s} ds - \int_{t/\tau_1}^\infty \frac{e^{-s}}{s} ds = E_1\left(\frac{t}{\tau_2}\right) - E_1\left(\frac{t}{\tau_1}\right)
$$

Portanto, a equação exata implementada para $G(t)$ em [`FungModelCalculator.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) é:

$$
G(t) = \frac{1 + C \left[ E_1\left(\dfrac{t}{\tau_2}\right) - E_1\left(\dfrac{t}{\tau_1}\right) \right]}{1 + C \ln\left(\dfrac{\tau_2}{\tau_1}\right)}
$$

### 2.4 Condições de Contorno e Cancelamento de Singularidade
1. **Em $t = 0$:**
   Conforme $x \to 0^+$, $E_1(x) \sim -\gamma - \ln(x)$. Assim:
   
   $$
   \lim_{t \to 0^+} \left[ E_1\left(\frac{t}{\tau_2}\right) - E_1\left(\frac{t}{\tau_1}\right) \right] = \lim_{t \to 0^+} \left[ -\ln\left(\frac{t}{\tau_2}\right) + \ln\left(\frac{t}{\tau_1}\right) \right] = \ln\left(\frac{t/\tau_1}{t/\tau_2}\right) = \ln\left(\frac{\tau_2}{\tau_1}\right)
   $$
   
   Substituindo em $G(0)$:
   
   $$
   G(0) = \frac{1 + C \ln\left(\frac{\tau_2}{\tau_1}\right)}{1 + C \ln\left(\frac{\tau_2}{\tau_1}\right)} = 1.0
   $$

2. **Em $t \to \infty$:**
   Como $\lim_{x \to \infty} E_1(x) = 0$:
   
   $$
   G_\infty = \lim_{t \to \infty} G(t) = \frac{1}{1 + C \ln\left(\frac{\tau_2}{\tau_1}\right)}
   $$

### 2.5 Taxa de Relaxação (Derivada Temporal dG/dt)
Diferenciando $G(t)$ em relação a $t$ via regra de Leibniz e regra da cadeia:

$$
\frac{d}{dt} E_1\left(\frac{t}{\tau}\right) = -\frac{e^{-t/\tau}}{\frac{t}{\tau}} \cdot \frac{1}{\tau} = -\frac{e^{-t/\tau}}{t}
$$

Portanto:

$$
\frac{d}{dt} \left[ E_1\left(\frac{t}{\tau_2}\right) - E_1\left(\frac{t}{\tau_1}\right) \right] = -\frac{e^{-t/\tau_2}}{t} - \left( -\frac{e^{-t/\tau_1}}{t} \right) = \frac{e^{-t/\tau_1} - e^{-t/\tau_2}}{t}
$$

Resultando na derivada analítica implementada em `CalculateRelaxationFunctionDerivative`:

$$
\frac{dG}{dt}(t) = \frac{C}{1 + C \ln\left(\dfrac{\tau_2}{\tau_1}\right)} \left[ \frac{e^{-t/\tau_1} - e^{-t/\tau_2}}{t} \right]
$$

**Limite no instante inicial ($t \to 0^+$):**  
Aplicando expansão em série de Taylor de primeira ordem ($e^{-u} \approx 1 - u$):

$$
\lim_{t \to 0^+} \frac{e^{-t/\tau_1} - e^{-t/\tau_2}}{t} = \lim_{t \to 0^+} \frac{\left(1 - \frac{t}{\tau_1}\right) - \left(1 - \frac{t}{\tau_2}\right)}{t} = -\frac{1}{\tau_1} + \frac{1}{\tau_2}
$$

Portanto:

$$
\left. \frac{dG}{dt} \right|_{t=0} = \frac{C \left( -\dfrac{1}{\tau_1} + \dfrac{1}{\tau_2} \right)}{1 + C \ln\left(\dfrac{\tau_2}{\tau_1}\right)}
$$

---

## 3. Integral Exponencial E1(x) e Algoritmos Numéricos

A classe [`ExponentialIntegralFunction`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Mathematics/Functions/ExponentialIntegralFunction.cs) implementa a avaliação numérica de $E_1(x)$ com precisão de ponto flutuante duplo (IEEE 754):

$$
E_1(x) = \int_x^\infty \frac{e^{-t}}{t} dt \quad (x > 0)
$$

### 3.1 Regime para Valores Pequenos e Moderados ($x \le 1.0$): Expansão em Série de Taylor

Para $0 < x \le 1.0$, utiliza-se a série analítica alternada:

$$
E_1(x) = -\gamma - \ln(x) - \sum_{k=1}^\infty \frac{(-x)^k}{k \cdot k!}
$$

onde:
- $\gamma \approx 0.57721566490153286060651209$ é a constante de Euler-Mascheroni (`MathematicConstants.EulerMascheroniConstant`);
- Relação de recorrência para os termos alternados:
  
  $$
  f_0 = 1, \quad f_k = f_{k-1} \left( -\frac{x}{k} \right) = \frac{(-x)^k}{k!}
  $$
  
  $$
  t_k = \frac{f_k}{k} = \frac{(-x)^k}{k \cdot k!}
  $$
  
  $$
  S_k = S_{k-1} + t_k
  $$

- Critério de convergência absoluta/relativa:
  
  $$
  |t_k| < \text{tolerance} \cdot |S_k|
  $$
  
  com limite máximo de $100$ iterações.

### 3.2 Regime Assintótico ($x > 1.0$): Fração Contínua de Legendre/Gautschi

Para $x > 1.0$, a série de potências sofre cancelamento catastrófico. Utiliza-se a expansão de fração contínua de Gauss-Legendre/Gautschi:

$$
E_1(x) = \frac{e^{-x}}{x + \cfrac{1}{1 + \cfrac{1}{x + \cfrac{2}{1 + \cfrac{2}{x + \cfrac{3}{1 + \dots}}}}}}
$$

**Algoritmo de Recorrência de Euler-Wallis:**
A fração contínua é avaliada pelas matrizes de convergentes sucessivos:

$$
A_{-1} = 1, \quad A_0 = 0, \qquad B_{-1} = 0, \quad B_0 = 1
$$

A cada passo $k \ge 1$:
- Para $k$ ímpar ($k = 1, 3, 5, \dots$):
  $$a_k = \left\lfloor \frac{k}{2} \right\rfloor, \quad b_k = 1$$
- Para $k$ par ($k = 2, 4, 6, \dots$):
  $$a_k = \frac{k}{2}, \quad b_k = x$$

As relações de avanço de Euler-Wallis são:

$$
A_k = b_k A_{k-1} + a_k A_{k-2}
$$

$$
B_k = b_k B_{k-1} + a_k B_{k-2}
$$

O convergente atual é:

$$
r_k = \frac{A_k}{B_k}
$$

Critério de parada:

$$
\left| \frac{r_k - r_{k-1}}{r_k} \right| < \text{tolerance}
$$

**Prevenção de Overflow (Reescalonamento de Gautschi):**  
Se $|A_k| > 2^{57} \approx 1.4411518807585587 \times 10^{17}$, reescalona-se por $2^{-57}$:

$$
A_k \leftarrow A_k \cdot 2^{-57}, \quad A_{k-1} \leftarrow A_{k-1} \cdot 2^{-57}, \quad B_k \leftarrow B_k \cdot 2^{-57}, \quad B_{k-1} \leftarrow B_{k-1} \cdot 2^{-57}
$$

### 3.3 Limite de Truncamento Superior (Cutoff Threshold)
Quando $x > x_{\max}$, a magnitude $e^{-x}$ decai abaixo da precisão da máquina. O limiar analítico é derivado de $e^{-x_{\max}} \approx \text{tolerance}$:

$$
x_{\max} = -\ln(\text{tolerance})
$$

Para $\text{tolerance} = 10^{-16}$, $x_{\max} = -\ln(10^{-16}) \approx 36.841361487904734$. Para $x > x_{\max}$, $E_1(x) = 0.0$.

### 3.4 Derivada de Primeira Ordem
Pelo Teorema Fundamental do Cálculo:

$$
\frac{d}{dx} E_1(x) = \frac{d}{dx} \int_x^\infty \frac{e^{-t}}{t} dt = -\frac{e^{-x}}{x}
$$

---

## 4. Tensão Total e Resposta Não Linear (QLV 1D)

### 4.1 Resposta Elástica Instantânea $\sigma^e(\epsilon)$
A resposta elástica não linear de Fung adota uma relação de tensão exponencial para tecidos moles:

$$
\sigma^e(\epsilon) = A \left( e^{B \epsilon} - 1 \right)
$$

onde:
- $A > 0$: coeficiente elástico de escala de tensão (MPa);
- $B > 0$: expoente não linear adimensional que rege a curvatura na região do *toe* (*toe-region stiffening*).

O módulo tangente instantâneo é:

$$
\frac{d\sigma^e}{d\epsilon} = A B e^{B \epsilon}
$$

### 4.2 Convolução Hereditária de Boltzmann
Para uma história de deformação arbitrária $\epsilon(t)$ com início em $t = 0$:

$$
\sigma(t) = \sigma^e(0) G(t) + \int_0^t G(t - s) \frac{\partial \sigma^e(\epsilon(s))}{\partial s} ds = \int_0^t G(t - s) \left( \frac{d\sigma^e}{d\epsilon} \right) \dot{\epsilon}(s) ds
$$

Substituindo a expressão analítica da derivada elástica:

$$
\sigma(t) = \int_0^t G(t - s) \left[ A B e^{B \epsilon(s)} \right] \dot{\epsilon}(s) ds
$$

---

## 5. Estratégia de Ajuste Sequencial em Dois Estágios (Task 2.1)

O pipeline de otimização em [`FungRelaxationOnlyCurveFitterStep.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/CurveFitter/FungRelaxationOnlyCurveFitterStep.cs) desacopla a não linearidade elástica dos parâmetros viscoelásticos:

```
[Dados Experimentais Brutos]
         │
         ├──► Estágio 1: Relaxação Normalizada (epsilon = const, t >= t0)
         │       Minimizar SSR_relax(c, tau1, tau2)
         │       Algoritmo: GDEMO (Differential Evolution) com restrição tau1 < tau2
         │       Obtém: c*, tau1*, tau2*
         │
         └──► Estágio 2: Fase de Rampa (t <= t0)
                 Minimizar SSR_ramp(A, B | c*, tau1*, tau2*)
                 Algoritmo: L-BFGS com box bounds (alglib.minbleic)
                 Obtém: A*, B*
```

### 5.1 Parâmetros e Limites de Otimização

#### Estágio 1 — Espectro de Relaxação Normalizado
- **Variável Independente:** $t' = t - t_0$ (tempo transladado para o início da relaxação).
- **Variável Dependente Normalizada:** $y_k = \dfrac{\sigma_{\exp}(t_k)}{\sigma_{\exp}(t_0)}$.
- **Função Teórica:** $G(t'; c, \tau_1, \tau_2)$.
- **Função Objetivo:**
  
  $$
  \min_{c, \tau_1, \tau_2} \text{SSR}_{\text{relax}} = \sum_{k=1}^{N_{\text{relax}}} \left[ y_k - G(t'_k; c, \tau_1, \tau_2) \right]^2
  $$

| Parâmetro | Símbolo | Unidade | Valor Inicial | Limite Inferior | Limite Superior | Significado Físico |
|---|---|---|---|---|---|---|
| Rigidez de Relaxação | $c$ | adm. | 0.1 | 0.0 | 1.0 | Amplitude do amortecimento viscoso contínuo. |
| Tempo Rápido | $\tau_1$ | s | 0.01 | 0.0001 | 0.1 | Constante de relaxação rápida (decaimento inicial). |
| Tempo Lento | $\tau_2$ | s | 10.0 | 1.0 | 100000.0 | Constante de relaxação lenta (equilíbrio assintótico). |

- **Restrição Física Ativa:** $\tau_1 < \tau_2$ (penalidade de barreira $10^{12}$ aplicada quando $\tau_1 \ge \tau_2$).
- **Perfil de Otimização:** `CurveFitProfile.Automatic` $\to$ `RuleConstrained` / GDEMO Differential Evolution (`alglib.mindf`).

#### Estágio 2 — Fase de Rampa Elástica
- **Variáveis Independentes:** $t'$ e $\epsilon(t')$.
- **Variável Dependente:** $\sigma_{\exp}(t')$ (tensão experimental em MPa).
- **Função Teórica:** $\sigma_{\text{calc}}(t', \epsilon; A, B)$ avaliada via `IFungModelCalculator.CalculateStress`.
- **Função Objetivo:**
  
  $$
  \min_{A, B} \text{SSR}_{\text{ramp}} = \sum_{j=1}^{N_{\text{ramp}}} \left[ \sigma_{\exp}(t_j) - \sigma_{\text{calc}}(t_j, \epsilon_j; A, B, c^*, \tau_1^*, \tau_2^*) \right]^2
  $$

| Parâmetro | Símbolo | Unidade | Valor Inicial | Limite Inferior | Limite Superior | Significado Físico |
|---|---|---|---|---|---|---|
| Constante de Tensão | $A$ | MPa | 1000.0 | 0.000001 | 1000000.0 | Coeficiente elástico de escala de tensão. |
| Constante de Potência | $B$ | adm. | 1.0 | 0.0 | 100.0 | Expoente não linear de enrijecimento da região *toe*. |

- **Hipóteses de Rampa Avaliadas:**
  1. `ConsiderWithoutViscoelasticEffect`: Rampa tratada como puramente elástica ($\sigma = \sigma^e$) durante o carregamento ($t \le t_0$).
  2. `ConsiderWithViscoelasticEffect`: Convolução hereditária completa durante a rampa finita com taxa $\dot{\epsilon}(t)$.
- **Perfil de Otimização:** `CurveFitProfile.Automatic` $\to$ `Standard` / L-BFGS com box bounds (`alglib.minbleic`).

### 5.2 Métricas Agregadas Ponderadas
- **Coeficiente de Determinação Combinado:**
  
  $$
  R^2_{\text{combined}} = \frac{N_{\text{ramp}} R^2_{\text{ramp}} + N_{\text{relax}} R^2_{\text{relax}}}{N_{\text{ramp}} + N_{\text{relax}}}
  $$

- **Soma dos Quadrados dos Resíduos Total:**
  
  $$
  \text{SSR}_{\text{total}} = \frac{N_{\text{ramp}} \text{SSR}_{\text{ramp}} + N_{\text{relax}} \text{SSR}_{\text{relax}}}{N_{\text{ramp}} + N_{\text{relax}}}
  $$

- **Total de Iterações:**
  
  $$
  \text{Iterações}_{\text{total}} = \text{Iterações}_{\text{ramp}} + \text{Iterações}_{\text{relax}}
  $$

- **Definição de $R^2$:**
  
  $$
  R^2 = 1 - \frac{\text{SSR}}{\text{SST}}, \quad \text{onde } \text{SSR} = \sum (y_k - f_k)^2 \quad \text{e} \quad \text{SST} = \sum (y_k - \bar{y})^2
  $$

---

## 6. Mapeamento de Código-Fonte e Classes

| Componente | Classe / Interface | Método / Propriedade | Descrição |
|---|---|---|---|
| Avaliação $E_1(x)$ | [`ExponentialIntegralFunction`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Mathematics/Functions/ExponentialIntegralFunction.cs) | `EvaluateE1(double)` | Avalia $E_1(x)$ bifurcando entre Taylor ($x \le 1.0$) e Fração Contínua ($x > 1.0$). |
| Série de Taylor | [`ExponentialIntegralFunction`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Mathematics/Functions/ExponentialIntegralFunction.cs) | `EvaluateSeriesSum(double)` | Série de potências com parada $|t_k| < \text{tol} \cdot |S_k|$. |
| Fração Contínua | [`ExponentialIntegralFunction`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Mathematics/Functions/ExponentialIntegralFunction.cs) | `EvaluateContinuedFraction(double)` | Algoritmo de Euler-Wallis com reescalonamento $2^{-57}$. |
| Constante $\gamma$ | [`MathematicConstants`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Mathematics/Models/MathematicConstants.cs) | `EulerMascheroniConstant` | $\gamma \approx 0.57721566490153286060651209$. |
| Calculadora Fung | [`FungModelCalculator`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateRelaxationFunction` | $G(t) = \frac{1 + C [E_1(t/\tau_2) - E_1(t/\tau_1)]}{1 + C \ln(\tau_2/\tau_1)}$. |
| Derivada $\dot{G}(t)$ | [`FungModelCalculator`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateRelaxationFunctionDerivative` | $\frac{dG}{dt}(t)$ com tratamento singular em $t = 0$. |
| Resposta Elástica | [`FungModelCalculator`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateElasticStress` | $\sigma^e(\epsilon) = A (e^{B \epsilon} - 1)$. |
| Módulo Tangente | [`FungModelCalculator`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateElasticTangentModulus` | $\frac{d\sigma^e}{d\epsilon} = A B e^{B \epsilon}$. |
| Convolução Hereditária | [`FungModelCalculator`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/Viscoelasticity/QuasiLinear/Fung/FungModelCalculator.cs) | `CalculateStress` | Convolução de Boltzmann com quadratura numérica discreta. |
| Pipeline de Ajuste | [`FungRelaxationOnlyCurveFitterStep`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/CurveFitter/FungRelaxationOnlyCurveFitterStep.cs) | `FitModelAsync` | Ajuste em dois estágios com ALGLIB e restrições paramétricas. |
