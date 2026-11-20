# Fung's Quasi-Linear Viscoelasticity (QLV) Model

## 1. The Pseudo-Strain Energy Function ($W$)
- **Formulation:** 
  $$ W = \frac{C}{2} \left( \exp(Q) - 1 \right) $$
- **Reference:** [Fung, 2nd Ed., pp. 306-308]
- **$C$:** Material parameter scaling the overall magnitude of the instantaneous elastic stress response (units of stress or energy density).
- **$Q$:** Dimensionless scalar function of the Green-Lagrange strain tensor $E_{ij}$, typically a quadratic form $Q = a_{ijkl} E_{ij} E_{kl}$ for anisotropic materials.
- **$a_{ijkl}$:** Dimensionless constants defining nonlinearity and anisotropy.
- **Mechanical Testing for Elastic Parameters ($C$, $a_{ijkl}$):**
  - **Reference:** [Fung, 2nd Ed., PDF Pages: 287, 308, 310, 311, 318, 345, 382, 453, 454, 529, 538, 556, 579, 582, 583]
  - **Test Type:** Uniaxial or biaxial tension tests.
  - **Procedure & Rationale:** These tests must be performed at **very high strain rates** (effectively a step strain). The goal is to isolate the *instantaneous* elastic response ($S_{ij}^e$) before any significant viscous relaxation can occur.
  - **Mechanics Context:** In terms of spring-dashpot models, a high strain rate "freezes" the viscous dashpots, forcing the entire deformation to be accommodated immediately by the nonlinear elastic springs. This allows the direct fitting of $C$ and $a_{ijkl}$ to the peak stresses achieved during the rapid ramp phase, independently of the time-dependent viscous decay.

## 2. Generalized Viscoelastic Relation for Second Piola-Kirchhoff Stress ($S_{ij}$)
- **Formulation:**
  $$ S_{ij}(t) = \int_{-\infty}^{t} G(t - \tau) \frac{\partial S_{ij}^e(\tau)}{\partial \tau} d\tau $$
- **Reference:** [Fung, 2nd Ed., pp. 306-308]
- **$S_{ij}^e$:** Instantaneous elastic stress response, derived as $S_{ij}^e = \frac{\partial W}{\partial E_{ij}}$.
- **$G(t)$:** Normalized reduced relaxation function governing the time-dependent stress history via convolution.

## 3. Continuous Relaxation Spectrum and $G(t)$
- **Spectrum Formulation:** 
  $$ S(\tau) = \begin{cases} \frac{c}{\tau} & \text{for } \tau_1 \le \tau \le \tau_2 \\ 0 & \text{otherwise} \end{cases} $$
- **Reduced Relaxation Function Formulation:**
  $$ G(t) = \frac{1 + c \left[ E_1(t/\tau_2) - E_1(t/\tau_1) \right]}{1 + c \ln(\tau_2/\tau_1)} $$
- **Reference:** [Fung, 2nd Ed., p. 286, Eq. (34); spectrum on p. 284, Eqs. (30)-(31); hereditary integral on p. 279, Eq. (7)]
- **$E_1(z)$:** Exponential integral function, $E_1(z) = \int_z^\infty \frac{e^{-y}}{y} dy$.
- **$c$:** Dimensionless viscous amplitude regulating total stress relaxation.
- **$\tau_1$:** Short-time constant defining initial rapid relaxation.
- **$\tau_2$:** Long-time constant characterizing slow asymptotic relaxation.
- **Mechanical Testing for Viscoelastic Parameters ($c$, $\tau_1$, $\tau_2$):**
  - **Reference:** [Fung, 2nd Ed., PDF Pages: 58, 61, 268, 277, 279, 309, 347, 348, 496, 497, 511, 537, 538, 541, 542, 584]
  - **Test Type:** Stress relaxation tests.
  - **Procedure:** The specimen is rapidly stretched to a predetermined target strain and held perfectly constant while the resulting decaying stress is continuously recorded over time.
  - **Extracting $\tau_1$:** This constant governs the steep, immediate stress drop at the very beginning of the holding phase. Accurate extraction requires **high-frequency data acquisition** during the first milliseconds to capture the short-term relaxation dynamics without aliasing.
  - **Extracting $c$:** When the stress response is plotted on a logarithmic time scale ($\log(t)$), the middle region of the decay curve becomes linear. The parameter $c$ is directly proportional to the **slope of this linear-logarithmic region**. It dictates the magnitude of the viscous effect relative to the elastic response.
  - **Extracting $\tau_2$:** This constant governs the behavior at large times as the material approaches its steady-state equilibrium (the horizontal asymptote). It is extracted from the "tail" of the relaxation curve, which strictly necessitates **long-duration testing** (often lasting several hours) to ensure the tissue has fully relaxed.

## 4. Push-forward to Cauchy Stress ($\sigma_{ij}$)
- **Formulation:**
  $$ \sigma_{ij} = \frac{1}{J} F_{iK} F_{jL} S_{KL} $$
- **Reference:** [Fung, 2nd Ed., pp. 306-308]
- **$\mathbf{F}$ ($F_{iK}$):** Deformation gradient tensor mapping the reference configuration to the current spatial configuration.
- **$J$:** Volume ratio, $J = \det(\mathbf{F})$. Approximated as $J \approx 1$ for nearly incompressible biological soft tissues.
- **$\sigma_{ij}$:** True stress (Cauchy stress) experienced by the material in the current configuration.

## 5. Uniaxial 1D Reduction & Implementation in MelloSilveiraTools
- **Kinematic Hypothesis:** Uniaxial tension along the primary collagen fiber axis ($\mathbf{e}_1$) under isochoric / nearly incompressible deformation ($J \approx 1$):
  $$ \mathbf{F} = \operatorname{diag}\left(\lambda,\, \lambda^{-1/2},\, \lambda^{-1/2}\right) $$
  where $\lambda = 1 + \varepsilon$ is the axial stretch ratio and $\varepsilon$ is nominal strain.
- **Green-Lagrange Strain Tensor ($\mathbf{E}$):**
  $$ E_{11} = \frac{1}{2}(\lambda^2 - 1) = \varepsilon + \frac{1}{2}\varepsilon^2 \approx \varepsilon $$
- **Boundary Conditions:** Uniaxial stress state with traction-free lateral faces:
  $$ \sigma_{11}(t) = \sigma(t), \quad \sigma_{22}(t) = \sigma_{33}(t) = 0, \quad \sigma_{ij} = 0 \; (i \neq j) $$
- **1D Instantaneous Elastic Response ($\sigma^e$):**
  $$ \sigma^e(\varepsilon) = A \left( e^{B \varepsilon} - 1 \right) $$
  - **Reference:** [Fung, 2nd Ed., pp. 270-276; Rodarte Thesis, Eq. 37]
  - **$A$ (`ElasticStressConstant`):** Elastic stress coefficient (MPa) scaling baseline stiffness.
  - **$B$ (`ElasticPowerConstant`):** Dimensionless non-linear stiffening exponent governing toe-to-linear region curvature.
  - **Time Derivative:**
    $$ \frac{d\sigma^e(t)}{dt} = A B e^{B \varepsilon(t)} \dot{\varepsilon}(t) $$
- **1D Hereditary Convolution Law:**
  $$ \sigma(t) = \int_0^t G(t - \tau) \frac{d\sigma^e(\tau)}{d\tau} d\tau $$
  - **Reference:** [Fung, 2nd Ed., p. 279, Eq. (7)]
  - **Integration by Parts Representation:**
    $$ \sigma(t) = \sigma^e(t) G(0) + \int_0^t \sigma^e(t - \tau) \frac{dG(\tau)}{d\tau} d\tau $$
    where $G(0) = 1$ and the reduced relaxation rate is:
    $$ \frac{dG(t)}{dt} = \frac{c \left( e^{-t/\tau_1} - e^{-t/\tau_2} \right)}{t \left[ 1 + c \ln(\tau_2/\tau_1) \right]} $$
- **Ramp Time Considerations in Software (`MelloSilveiraTools`):**
  1. **`Disregard` (Ideal Step Strain $\varepsilon(t) = \varepsilon_0 H(t)$):**
     $$ \sigma(t) = G(t) \sigma^e(\varepsilon_0) = G(t) A \left( e^{B \varepsilon_0} - 1 \right) \implies \frac{\sigma(t)}{\sigma(0)} = G(t) $$
     Enables analytical decoupling where relaxation parameters ($c, \tau_1, \tau_2$) are identified independently from elastic parameters ($A, B$).
  2. **`ConsiderWithoutViscoelasticEffect`:**
     Assumes the ramp phase ($t \le t_0$) is dominated by elastic deformation ($\sigma(t) = \sigma^e(t)$ for $t \le t_0$), with viscous relaxation acting over history after $t_0$.
  3. **`ConsiderWithViscoelasticEffect`:**
     Accounts for finite ramp duration ($t_0$) by integrating the continuous strain rate $\dot{\varepsilon} = \varepsilon_0 / t_0$ through the Boltzmann hereditary convolution from $t = 0$.
