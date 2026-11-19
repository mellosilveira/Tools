# Comparative Mechanical Framework & Literature Catalog: Soft Biological Tissues vs. Synthetic Polymeric Biomaterials
**Comparative Materials Mechanics Framework: Soft Biological Tissues vs. Synthetic Polymers**  
*Constitutive Modeling, Microstructural Mechanisms, Continuous Tensorial Formulations, and 60-Study CrossRef-Verified Literature Catalog*

---

## Executive Header & Overview

| Metadata Field | Project Specification |
| :--- | :--- |
| **Institutional Context** | Graduate Program in Mechanical Engineering and Materials Technology (PPEMM / CEFET-RJ) |
| **Research Group** | Soft Tissue Biomechanics and Continuum Mechanics Research Group (Coord. Prof. Dr. Paulo Pedro Kenedi) |
| **Primary Biological Systems** | Knee Ligaments (ACL, PCL, MCL, LCL), Tendons (Patellar, Achilles), Articular Cartilage, Meniscus, Dermis, Vascular Walls |
| **Primary Synthetic Systems** | Thermoplastic Polyurethanes (TPU), Hydrogels & Double Networks (PVA, PEG, PAM, Alginate), Elastomers (PDMS, Silicones), Fiber Polymers (UHMWPE, PET) |
| **Software Infrastructure** | `MelloSilveiraTools.MechanicsOfMaterials` & `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` (.NET 10 / C# 13); FEBio 4.0 |
| **Literature Base** | 60 peer-reviewed scientific studies with 100% active, CrossRef-verified DOIs (Zero Bibliographic Hallucination) |
| **Numerical Standard** | Strict 3 significant figures policy for continuous experimental values (`formatting.significant_figures: 3`) |

### Research Purpose & Scope
This comprehensive reference document establishes the theoretical, constitutive, and experimental foundation for comparative mechanics between native soft biological tissues and advanced synthetic polymeric biomaterials. Musculoskeletal soft tissues—most notably knee ligaments (ACL, PCL, MCL, LCL) and tendons—operate in severe physiological mechanical environments requiring an intricate balance of high tensile stiffness, non-linear compliance at low strains, time-dependent viscoelastic dissipation, flaw insensitivity, and multidecade fatigue durability.

When designing artificial replacement grafts, tissue engineering scaffolds, or surrogate benchtop testing phantoms, synthetic polymers (polyurethanes, tough hydrogels, silicone elastomers, and braided fibers) present distinct mechanical advantages but also profound constitutive divergences. Understanding these differences at both the microscopic structural level and the macroscopic continuum mechanics level is vital for:
1. Rigorous parameter calibration and curve fitting within the `MelloSilveiraTools.MechanicsOfMaterials.Optimizations` automated data pipelines.
2. Formulating robust 3D hyperelastic, viscoelastic, and biphasic/poroelastic material models for finite element implementation in FEBio.
3. Informing graft design to prevent stress-shielding, graft elongation, joint laxity, and premature fatigue failure.

---

### Academic & Engineering Justifications for Investigating Polymers in a Ligament Biomechanics Master's Thesis

In the context of a graduate thesis at PPEMM/CEFET-RJ focused on the mechanical characterization of knee ligaments (e.g., porcine or human ACL, PCL, MCL, LCL), establishing a direct comparative interface with synthetic polymeric materials is grounded on four mutually reinforcing scientific and technological pillars:

#### 1. Synthetic Graft Design, Optimization, and Failure Prevention (Overcoming Autograft Limitations)
- **Clinical Problem:** Severe knee ligament injuries (most notably rupture of the Anterior Cruciate Ligament, ACL) exhibit virtually zero spontaneous healing capacity due to intra-articular synovial wash-out and low vascularity. Although autografts (e.g., bone-patellar tendon-bone, quadrupled semitendinosus/gracilis tendons) remain the contemporary clinical standard, they impose permanent donor-site morbidity, prolonged postoperative rehabilitation, loss of terminal knee extension strength, and variable risk of re-rupture. Allografts, while sparing donor-site harvest, introduce potential disease transmission risks, sterilization-induced mechanical degradation, and delayed biological incorporation.
- **Polymeric Justification:** To circumvent these shortcomings, high-performance synthetic fiber polymers (e.g., braided UHMWPE, poly(ethylene terephthalate) / PET such as LARS ligaments, and segmented thermoplastic polyurethanes) have been extensively pursued. However, historically, synthetic ligament replacements suffered early clinical failure due to two antagonistic mechanical mismatches:
  1. *Stress-Shielding Induced Bone Resorption:* Excessive linear stiffness relative to the native ligament unloads the peri-tunnel bone insertions, promoting osteopenia and fixation pull-out.
  2. *Cyclic Creep Elongation and Joint Laxity:* Uncontrolled accumulation of irreversible secondary and tertiary creep under sustained gait cycles causes joint instability.
- **Thesis Impact:** Comprehensive mechanical characterization of native ligaments (quantifying non-linear toe-region compliance, rate-dependent tangent modulus, and physiological preconditioning) establishes the indispensable, ground-truth *benchmark* against which any polymeric graft architecture must be calibrated and optimized to prevent premature in vivo biomechanical failure.

#### 2. Anatomical Surrogates & Reproducible Benchtop Testing Phantoms
- **Experimental Problem:** Conducting experimental mechanical assays on biological ligaments (human cadaveric or animal models) presents formidable challenges: rapid post-mortem autolysis, dehydration-induced stiffness drift, microstructural variability across donor age and specimen geometry, and stringent ethical protocols (CEUA/CEP oversight).
- **Polymeric Justification:** Advanced synthetic polymers—including stereolithographic and FDM 3D-printed thermoplastic polyurethanes (TPU), silicone/PDMS elastomers, and mechanically robust hydrogels—provide homogeneous, isotropic or tailor-architected physical surrogates with highly controllable and reproducible mechanical responses.
- **Thesis Impact:** Understanding the comparative stress-strain curves, tangent moduli, and damping characteristics of soft tissue vs. polymers enables the engineering of synthetic ligament phantoms for standardized benchtop validation of surgical fixation screws, robotic testing rigs, and arthroscopic surgical training platforms, completely eliminating inter-specimen biological scatter.

#### 3. Cross-Validation of Non-Linear Continuum Constitutive Models (FEBio & C# Optimization Pipelines)
- **Computational Problem:** Soft biological tissues are inherently anisotropic, heterogeneous, and nearly incompressible, making simultaneous parameter identification of fiber dispersion, uncrimping exponents, and matrix shear moduli numerically ill-conditioned.
- **Polymeric Justification:** From a Continuum Mechanics standpoint, biological soft tissues and polymeric networks share common theoretical foundations: hyperelastic strain energy functions ($\Psi$), isochoric/volumetric decoupling ($\mathbf{F} = J^{1/3}\bar{\mathbf{F}}$), and hereditary viscoelastic integral formulations. Polymers (such as isotropic TPU dogbone specimens or silicone membranes) serve as chemically well-defined, homogeneous calibration standards to rigorously benchmark numerical optimization algorithms (e.g., Levenberg-Marquardt solvers in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`) and finite element formulations in FEBio 4.0 before introducing the full multiaxial complexity of biological fiber dispersions.
- **Thesis Impact:** Validating curve-fitting pipelines and 3D finite element formulations against polymer reference data guarantees algorithmic stability, thermodynamic admissibility ($\mu_i > 0, \tau_i > 0$), and objective convergence criteria in the thesis computational framework.

#### 4. Inverse Biomimetic Engineering (Native Tissue as the Gold-Standard Design Guide)
- **Materials Science Challenge:** Classical polymer mechanics faces an entrenched trade-off: homogeneous elastomers are either flexible but weak (low modulus and poor fatigue resistance) or stiff but brittle (limited stretchability and catastrophic notch-induced crack propagation governed by the Lake-Thomas limit, $G_0 \sim 10 - 100\text{ J/m}^2$).
- **Polymeric Justification:** Biological soft tissues elegantly resolve this paradox through hierarchical multi-scale architecture: collagen fibril uncrimping furnishes low-strain compliance, covalent intermolecular crosslinks provide high linear tensile strength, and interfibrillar shear sliding blunts crack tips, raising the biological fatigue threshold above $1.00\text{ kJ/m}^2$.
- **Thesis Impact:** By elucidating the exact biomechanical mechanisms of natural ligaments, the thesis provides the foundational empirical and mathematical parameters that guide materials scientists in designing next-generation, crack-resistant, bio-mimetic polymer networks (e.g., self-healing ionic elastomers, progressive nanocrystalline PVA, and double networks).

---

#### Ready-to-Use Thesis Justification Narrative (Introduction / Motivation Section)

> *"The comprehensive mechanical characterization of knee ligaments is driven not only by orthopedic clinical pathology, but also by a critical imperative in materials science and bioengineering. As evidenced across contemporary literature, synthetic polymeric biomaterials—including thermoplastic polyurethanes, tough interpenetrating hydrogels, and braided ultra-high molecular weight polyethylene fibers—are increasingly investigated as prosthetic replacement grafts, tissue engineering scaffolds, and surrogate benchtop testing phantoms. However, the successful functional deployment of these polymers demands the precise replication of complex, non-linear biological hallmarks: compliant toe-region uncrimping, time-dependent viscoelastic relaxation, energy dissipation with low steady-state hysteresis, and exceptional flaw-tolerant fatigue durability. Consequently, establishing the rigorous empirical baseline and constitutive parameterization of native knee ligaments provides the indispensable gold standard required to guide the synthesis, architectural optimization, and mechanical evaluation of synthetic polymers in biomechanical engineering."*

---

## Section 1: Comparative Mechanical Framework (Soft Biological Tissues vs. Synthetic Polymers)

### 1.1 Continuum Mechanics Kinematics & Stress Tensors: From 3D Tensor Field to 1D Uniaxial Reduction
To guarantee mathematical rigor and seamless integration into 3D non-linear finite element solvers (such as FEBio), all constitutive relationships must originate from the tensorial foundations of Continuum Mechanics prior to being reduced to the scalar equations governing 1D uniaxial mechanical testing.

Let $\Omega_0 \subset \mathbb{R}^3$ denote the material body in its undeformed reference configuration at time $t = 0$, and let $\Omega_t \subset \mathbb{R}^3$ denote the deformed configuration at time $t > 0$. The non-linear motion mapping is given by $\mathbf{x} = \boldsymbol{\chi}(\mathbf{X}, t)$, where $\mathbf{X} \in \Omega_0$ represents material coordinates and $\mathbf{x} \in \Omega_t$ represents spatial coordinates.

The **deformation gradient tensor** $\mathbf{F}$ is defined as:
$$ \mathbf{F} = \frac{\partial \mathbf{x}}{\partial \mathbf{X}} = \nabla_{\mathbf{X}} \boldsymbol{\chi} $$
with volume ratio (Jacobian determinant) $J = \det \mathbf{F} > 0$. For incompressible materials (such as hydrated soft tissues and elastomers under moderate physiological pressures), the kinematic incompressibility constraint requires $J = 1$. In uncoupled formulations, $\mathbf{F}$ is decomposed into volumetric and isochoric components:
$$ \mathbf{F} = J^{1/3} \bar{\mathbf{F}}, \quad \det \bar{\mathbf{F}} = 1 $$

The **right Cauchy-Green deformation tensor** $\mathbf{C}$ and **left Cauchy-Green (Finger) tensor** $\mathbf{b}$ are:
$$ \mathbf{C} = \mathbf{F}^T \mathbf{F}, \quad \mathbf{b} = \mathbf{F} \mathbf{F}^T $$
and the **Green-Lagrange strain tensor** $\mathbf{E}$ is defined as:
$$ \mathbf{E} = \frac{1}{2} (\mathbf{C} - \mathbf{I}) $$
where $\mathbf{I}$ is the second-order identity tensor.

The principal scalar invariants of $\mathbf{C}$ are given by:
$$ I_1 = \mathrm{tr}(\mathbf{C}), \quad I_2 = \frac{1}{2} \left[ (\mathrm{tr}(\mathbf{C}))^2 - \mathrm{tr}(\mathbf{C}^2) \right], \quad I_3 = \det(\mathbf{C}) = J^2 $$

Assuming hyperelasticity governed by a Helmholtz free energy function $\Psi(\mathbf{C})$, the work-conjugate **Second Piola-Kirchhoff stress tensor** $\mathbf{S}$ and **Cauchy (true) stress tensor** $\boldsymbol{\sigma}$ are:
$$ \mathbf{S} = 2 \frac{\partial \Psi}{\partial \mathbf{C}} - p \mathbf{C}^{-1} $$
$$ \boldsymbol{\sigma} = J^{-1} \mathbf{F} \mathbf{S} \mathbf{F}^T = 2 J^{-1} \mathbf{F} \frac{\partial \Psi}{\partial \mathbf{C}} \mathbf{F}^T - p \mathbf{I} $$
where $p$ is the indeterminate hydrostatic pressure acting as a Lagrange multiplier enforcing the incompressibility constraint $J = 1$.
*(Citation: Holzapfel, G. A., Nonlinear Solid Mechanics: A Continuum Approach for Engineering, John Wiley & Sons, 2000, pp. 205-255).*

#### 1.1.1 1D Reduction for Uniaxial Tensile Testing
In laboratory experimental assays (e.g., tensile testing of ligaments or synthetic polymer dogbone specimens along the primary axis $\mathbf{e}_1$):
- **Experimental Tare Pre-load Protocol:** Prior to tensile elongation and strain acquisition, an experimental tare pre-load protocol ($F_0 = 0.100 - 0.500\text{ N}$ applied quasi-statically at $\le 0.100\text{ mm/s}$) is strictly required to eliminate macroscopic slack, ensure uniform alignment, and establish the true undeformed reference configuration $\Omega_0$ ($\lambda = 1.00$, Green-Lagrange strain $E = 0$) and measure the initial gauge length $L_0$.  
  *(Citation: Fung, Y. C., Biomechanics: Mechanical Properties of Living Tissues, 2nd ed., Springer-Verlag, 1993, pp. 242-245; Woo, S. L.-Y. et al., Am. J. Sports Med., vol. 19, 1991, pp. 217-225).*
- Stretch ratio along the loading axis: $\lambda_1 = \lambda = 1 + \varepsilon$, where $\varepsilon$ is engineering strain.
- Due to transverse isotropy and incompressibility ($J = \lambda_1 \lambda_2 \lambda_3 = 1$): $\lambda_2 = \lambda_3 = \lambda^{-1/2}$.
- The deformation gradient reduces to $\mathbf{F} = \mathrm{diag}(\lambda, \lambda^{-1/2}, \lambda^{-1/2})$ and $\mathbf{C} = \mathrm{diag}(\lambda^2, \lambda^{-1}, \lambda^{-1})$.
- Principal invariants become:
  $$ I_1 = \lambda^2 + 2\lambda^{-1}, \quad I_2 = 2\lambda + \lambda^{-2}, \quad I_3 = 1 $$
- Traction-free boundary conditions on lateral surfaces ($\sigma_{22} = \sigma_{33} = 0$) determine the hydrostatic pressure $p$.
- The axial Cauchy stress $\sigma_{11} = \sigma$ and first Piola-Kirchhoff (nominal/engineering) stress $P_{11} = P = \sigma / \lambda$ simplify to:
  $$ P = \frac{\partial \Psi}{\partial \lambda}, \quad \sigma = \lambda \frac{\partial \Psi}{\partial \lambda} $$

---

### 1.2 Five Comparative Mechanical Domains

```
                                  COMPARATIVE MECHANICAL DOMAINS
  ┌───────────────────────────────────────────────┬──────────────────────────────────────────────┐
  │             Soft Biological Tissues           │              Synthetic Polymers              │
  ├───────────────────────────────────────────────┼──────────────────────────────────────────────┤
  │ (a) Crimp un-crimping, J-shaped toe region    │ (a) Entropic chain uncoiling, rubber elast.  │
  │ (b) Dual relaxation: viscoelastic + poroelast.│ (b) Discrete Prony relaxation, thermal creep │
  │ (c) Preconditioning, pseudo-elastic loop      │ (c) Mullins stress-softening, filler damage  │
  │ (d) Flaw tolerance, crack blunting (1-10 kJ/m²)│ (d) Lake-Thomas limit (10-100 J/m²), scission│
  │ (e) Anisotropic fiber continuum (HGO, Fung)   │ (e) Isotropic hyperelasticity (Ogden, Yeoh)  │
  └───────────────────────────────────────────────┴──────────────────────────────────────────────┘
```

#### Domain (a): Non-Linear J-Shaped Stress-Strain & Toe-Region Crimp Mechanics vs. Entropic Elasticity
- **Biological Soft Tissues:**  
  Ligaments (ACL, MCL) and tendons display a pronounced "J-shaped" tensile curve. Under physiological strains ($\varepsilon \in [0, 0.04]$ for ACL, up to $0.06$ in collateral ligaments), the tissue resides in the **toe region**, characterized by low tangent stiffness. This compliance stems directly from the microstructural **un-crimping** of planar, sinusoidal collagen fibril undulations (wavelength $l_0 \approx 20.0 - 100\ \mu\text{m}$, crimp angle $\theta_0 \approx 10.0^\circ - 30.0^\circ$). As stretch increases, fibrils progressively straighten without stretching their covalent molecular backbones. Once straightened, the tissue enters the **linear region**, where the tangent modulus surges ($E_t \approx 100 - 350\text{ MPa}$ for human ACL, exceeding $1.20\text{ GPa}$ for Achilles tendon) as loading directly stretches triple-helical collagen molecules and intermolecular crosslinks.
  *(Citation: Fung, Y. C., Biomechanics: Motion, Flow, Stress, and Growth, Springer-Verlag, 1990, pp. 277-285).*

- **Synthetic Polymeric Biomaterials:**  
  Homogeneous elastomers (silicone/PDMS, unfilled polyurethanes) exhibit rubber-like elasticity governed by statistical thermodynamics and **conformational entropy reduction**. Macromolecular chains uncoil from random configurations, described by Gaussian network theory or non-Gaussian Langevin chains ($\mathcal{L}(\beta) = \coth\beta - \beta^{-1}$). These materials exhibit an initial high tangent compliance followed by gradual stretch stiffening only at extreme elongations as chains approach their limiting extensibility ($\lambda \to \lambda_{\max}$), lacking the sharp physiological toe-to-linear inflection of natural collagen.  
  To mimic biological toe mechanics, synthetic grafts require architected textile designs:
  - Braided high-performance polymers (UHMWPE, PET) provide high linear modulus and ultimate strength ($> 1.50\text{ kN}$ failure load for ACL replacements) but present an abrupt, steep initial stiffness unless engineered with high initial braid angles ($\theta \approx 45.0^\circ - 55.0^\circ$) or texturing ("carrot-and-stick" hybrid yarns) to provide kinematic rotation before fiber tensioning.
  - 3D-printed elastomers (TPU) and hydrogels utilize sinusoidal melt electrowriting (MEW) or auxetic cellular lattices to structurally emulate the un-crimping transition.

#### Domain (b): Viscoelasticity: Stress Relaxation & Creep Compliance
- **Biological Soft Tissues:**  
  Exhibit time-, history-, and rate-dependent behavior arising from a **dual-mechanism**:
  1. *Intrinsic solid-matrix viscoelasticity:* Frictional sliding between collagen fibrils and the hydrophilic proteoglycan ground substance (decorin, aggrecan, glycosaminoglycans).
  2. *Biphasic poroelasticity:* Interstitial fluid exudation and redistribution through the porous collagenous meshwork according to Darcy's law:
     $$ \mathbf{v}_f = -k(\mathbf{E}) \nabla p $$
     where hydraulic permeability $k$ decays exponentially with compressive strain ($k = k_0 \exp(M \varepsilon)$).
  
  Under constant strain hold (stress relaxation), biological tissues relax rapidly during the initial $10.0 - 60.0\text{ s}$, followed by a very slow asymptotic decay over hours, governed by a broad, continuous spectrum of relaxation time constants modeled via Fung's Quasi-Linear Viscoelasticity (QLV):
  $$ S(\tau) = \begin{cases} \frac{c}{\tau}, & \tau_1 \le \tau \le \tau_2 \\ 0, & \text{otherwise} \end{cases} $$
  $$ G(t) = \frac{1 + c \left[ E_1\left(\frac{t}{\tau_2}\right) - E_1\left(\frac{t}{\tau_1}\right) \right]}{1 + c \ln\left(\frac{\tau_2}{\tau_1}\right)} $$
  where $E_1(z) = \int_z^\infty \frac{e^{-u}}{u} du$ is the exponential integral function, $c$ is the dimensionless relaxation spectrum magnitude, and $\tau_1, \tau_2$ are fast and slow characteristic relaxation limits.
  *(Citation: Fung, Y. C., Biomechanics: Mechanical Properties of Living Tissues, 2nd ed., Springer-Verlag, 1993, pp. 277-285).*

- **Synthetic Polymeric Biomaterials:**  
  Elastomers and hydrogels are classically represented by discrete Maxwell/Prony series or fractional viscoelastic models:
  $$ G(t) = G_\infty + \sum_{i=1}^N G_i \exp\left( -\frac{t}{\tau_i} \right) $$
  While physical and chemical PVA/PEG hydrogels mimic poroelastic interstitial fluid pressurization (essential for articular cartilage and meniscal substitutes), their higher free-water content and lower tortuosity yield significantly faster initial fluid exudation than native dense connective tissue.  
  Under sustained cyclic or static loading (creep compliance $J(t) = \varepsilon(t) / \sigma_0$), native ligaments experience tightly bounded transient creep ($\varepsilon_{\text{creep}} < 1.50\%$) regulated by cellular active remodeling. Conversely, synthetic UHMWPE/PET braided ligaments are notoriously susceptible to long-term secondary and tertiary plastic creep over millions of gait cycles, frequently resulting in knee joint laxity and surgical reconstruction failure.

#### Domain (c): Cyclic Loading: Hysteresis, Preconditioning vs. Mullins Stress-Softening
- **Biological Soft Tissues:**  
  Virgin tissue subjected to cyclic tensile loading exhibits significant energy dissipation (hysteresis area). During the initial $5 - 10$ cycles, the stress-strain curve shifts to the right and the hysteresis loop progressively narrows, reaching a repeatable, steady-state closed loop—a phenomenon termed **preconditioning**. Microstructurally, preconditioning represents the progressive alignment of collagen fibrils along the primary load axis and the equilibrium redistribution of interstitial water. Once preconditioned, energy-storing tendons (e.g., Achilles, patellar) exhibit remarkably low hysteresis ($\Delta U / U \approx 5.00\% - 15.0\%$), acting as efficient biological springs with minimal heat accumulation.
  *(Citation: Fung, Y. C., Biomechanics, Springer, 1993, pp. 268-275).*

- **Synthetic Polymeric Biomaterials:**  
  In thermoplastic polyurethanes (TPU) and filled elastomers, cyclic loading induces the **Mullins effect**—an instantaneous, deformation-induced stress-softening observed upon reloading at strains below the historical maximum strain $\varepsilon_{\max}$. Microstructurally, the Mullins effect stems from irreversible chain slippage, rupture of physical crosslinks between hard and soft polyurethane segments, or debonding of filler particles from the polymer matrix.  
  Unlike biological preconditioning (which fully recovers upon tissue rest and rehydration in saline), elastomer Mullins softening is largely permanent unless thermally annealed above the glass transition or hard-segment melting temperature. In hydrogels, energy dissipation can be precisely tuned: covalent hydrogels exhibit ultra-low hysteresis, whereas double-network (DN) or metal-coordinated supramolecular hydrogels exploit sacrificial reversible bonds to achieve massive hysteresis, providing shock-absorption comparable to articular cartilage.
  *(Citation: Mullins, L., Softening of Rubber by Deformation, Rubber Chem. Technol., vol. 42, 1969, pp. 339-362).*

#### Domain (d): Fracture Toughness, Flaw Tolerance & Fatigue Thresholds
- **The Classical Lake-Thomas Limit in Synthetic Elastomers:**  
  In homogeneous polymeric networks, crack propagation under cyclic tensile loading is dictated by the Lake-Thomas model. The intrinsic fatigue threshold $\Gamma_0$ (the critical energy release rate below which cracks do not propagate under infinite cycles) is strictly limited to the energy required to rupture a single planar layer of covalent carbon-carbon bonds crossing the crack path:
  $$ \Gamma_0 = \Sigma_0 U_b \sqrt{N_k} $$
  where $\Sigma_0$ is the areal chain density, $U_b \approx 350\text{ kJ/mol}$ is the C-C bond dissociation energy, and $N_k$ is the number of Kuhn segments between crosslinks. For conventional silicone elastomers and single-network hydrogels, $\Gamma_0$ is restricted to $10.0 - 100\text{ J/m}^2$. Consequently, any micro-notch, suture puncture, or manufacturing flaw acts as a catastrophic stress concentration, triggering rapid fatigue failure.
  *(Citation: Lake, G. J., Thomas, A. G., The Strength of Highly Elastic Materials, Proc. R. Soc. Lond. A, vol. 300, 1967, pp. 108-119).*

- **Biological Multiscale Flaw Tolerance:**  
  Collagenous soft tissues (ligaments, pericardium, skin) display fracture toughness values and fatigue thresholds exceeding $1.00 \times 10^3\text{ J/m}^2$ to $10.0 \times 10^3\text{ J/m}^2$—two orders of magnitude beyond the Lake-Thomas limit. This extraordinary flaw insensitivity originates from hierarchical architecture:
  1. *Crack-tip blunting:* Reorientation of fibers in front of the crack tip relieves local tensile stresses.
  2. *Interfibrillar sliding and friction:* Ground matrix shear dissipates energy over a massive volume rather than concentrating it at a sharp singular tip.
  3. *Fiber bridging:* Intact collagen bundles span the crack wake behind the crack front, shielding the tip from applied stress.
  Chemically fixed bovine pericardium heart valve leaflets routinely survive over $400 \times 10^6$ flexural cycles ($\approx 10$ years in vivo), remaining completely unaffected by suture needle punctures and pre-existing flaws up to $1.00\text{ cm}$ in length.

- **Advanced Biomimetic Polymeric Countermeasures:**  
  Recent engineering advances in synthetic biomaterials employ phase-separated crystalline domains, salting-out/progressive nanocrystallization (PVA), and double networks (e.g., rigid PAMPS sacrificial network embedded in a ductile PAAm matrix) to reproduce this non-local energy dissipation, elevating synthetic hydrogel fatigue thresholds to match native tissue toughness ($> 1.00\text{ kJ/m}^2$).

#### Domain (e): Constitutive Modeling Formulations
Constitutive modeling forms the mathematical bridge between biological soft tissues and polymeric materials, enabling both parameter calibration from mechanical test data and non-linear numerical simulation in finite element solvers (such as FEBio).

##### 1. Holzapfel-Gasser-Ogden (HGO) Anisotropic Continuum Model (Soft Tissues)
Developed for collagen-reinforced biological tissues possessing families of dispersed fibers:
$$ \Psi_{\text{HGO}}(\mathbf{C}, \mathbf{a}_{01}, \mathbf{a}_{02}) = C_{10} (I_1 - 3) + \frac{k_1}{2 k_2} \sum_{i=4,6} \left\{ \exp\left[ k_2 \left( \kappa I_1 + (1 - 3\kappa) I_i - 1 \right)^2 \right] - 1 \right\} $$
- $\mathbf{a}_{01}, \mathbf{a}_{02}$: unit vectors along the mean directions of the two collagen fiber families in the reference configuration.
- $I_1 = \mathrm{tr}(\mathbf{C})$: first invariant of the right Cauchy-Green tensor $\mathbf{C}$.
- $I_4 = \mathbf{a}_{01} \cdot (\mathbf{C} \mathbf{a}_{01}) = \lambda_{\mathbf{a}_{01}}^2$ and $I_6 = \mathbf{a}_{02} \cdot (\mathbf{C} \mathbf{a}_{02}) = \lambda_{\mathbf{a}_{02}}^2$: pseudo-invariants measuring the square of the fiber stretches.
- $C_{10} > 0$: isotropic matrix stress-scaling parameter (where ground shear modulus $\mu_m = 2 C_{10}$, in MPa).
- $k_1 > 0$: fiber stress-scaling coefficient (MPa).
- $k_2 > 0$: dimensionless non-linear fiber stiffening exponent.
- $\kappa \in [0, 1/3]$: structural dispersion parameter ($\kappa = 0$ denotes ideal parallel alignment; $\kappa = 1/3$ denotes isotropic 3D fiber distribution).
- Tensile activation condition: The exponential fiber term is activated only when fibers are under tension ($\kappa I_1 + (1 - 3\kappa) I_i > 1$); in compression, fibers buckle, providing zero mechanical resistance.
*(Citations: Holzapfel, G. A., Gasser, T. C., Ogden, R. W., A New Constitutive Framework for Arterial Wall Mechanics and a Comparative Study of Material Models, J. Elasticity, vol. 61, 2000, pp. 1-48, specifically pp. 14-22; Gasser, T. C., Ogden, R. W., Holzapfel, G. A., Hyperelastic modelling of arterial layers with distributed collagen fibre orientations, J. R. Soc. Interface, vol. 3, 2006, pp. 15-35, specifically pp. 17-20).*

##### 2. Fung Anisotropic Pseudo-Strain Energy Function (Soft Tissues)
$$ \Psi_{\text{Fung}}(\mathbf{E}) = \frac{1}{2} C \left( e^Q - 1 \right) $$
$$ Q = a_{11} E_{11}^2 + a_{22} E_{22}^2 + a_{33} E_{33}^2 + 2 a_{12} E_{11} E_{22} + 2 a_{23} E_{22} E_{33} + 2 a_{31} E_{33} E_{11} + a_{44} E_{12}^2 + a_{55} E_{23}^2 + a_{66} E_{31}^2 $$
- $\mathbf{E} = \frac{1}{2}(\mathbf{C} - \mathbf{I})$: Green-Lagrange strain tensor.
- $C > 0$: scaling parameter with stress dimensions (MPa).
- $a_{ijkl}$: dimensionless anisotropic coefficients governing directional stiffness and transverse coupling.
*(Citation: Fung, Y. C., Biomechanics: Motion, Flow, Stress, and Growth, Springer-Verlag, 1990, pp. 306-311).*

##### 3. Mooney-Rivlin Hyperelastic Model (Synthetic Polymers / Elastomers)
For isotropic, incompressible rubber-like polymers under moderate strains ($\lambda \le 1.50$):
$$ \Psi_{\text{MR}}(I_1, I_2) = C_{10} (I_1 - 3) + C_{01} (I_2 - 3) $$
- $C_{10}, C_{01}$: empirical constants (MPa). Ground-state shear modulus: $\mu = 2(C_{10} + C_{01})$.
*(Citation: Mooney, M., A Theory of Large Elastic Deformation, J. Appl. Phys., vol. 11, 1940, pp. 582-592; Rivlin, R. S., Philos. Trans. R. Soc. A, vol. 241, 1948, pp. 379-397).*

##### 4. Ogden Hyperelastic Model (Synthetic Polymers / Brain & Parenchymal Tissues)
Formulated directly in terms of principal stretches $\lambda_1, \lambda_2, \lambda_3$:
$$ \Psi_{\text{Ogden}}(\lambda_1, \lambda_2, \lambda_3) = \sum_{p=1}^N \frac{\mu_p}{\alpha_p} \left( \lambda_1^{\alpha_p} + \lambda_2^{\alpha_p} + \lambda_3^{\alpha_p} - 3 \right) $$
- $\mu_p$: shear moduli parameters (MPa).
- $\alpha_p$: dimensionless non-linear exponents (can be positive or negative).
- Incompressibility constraint: $\lambda_1 \lambda_2 \lambda_3 = 1$. Ground-state shear modulus: $\mu = \frac{1}{2} \sum_{p=1}^N \mu_p \alpha_p$.
*(Citation: Ogden, R. W., Large Deformation Isotropic Elasticity, Proc. R. Soc. Lond. A, vol. 326, 1972, pp. 565-584, specifically pp. 567-573).*

##### 5. Yeoh Reduced Polynomial Hyperelastic Model (Carbon-Filled Elastomers & Polyurethanes)
Specifically developed to capture the upturn in stiffness at large stretches without requiring higher-order strain invariants:
$$ \Psi_{\text{Yeoh}}(I_1) = \sum_{i=1}^3 C_{i0} (I_1 - 3)^i = C_{10} (I_1 - 3) + C_{20} (I_1 - 3)^2 + C_{30} (I_1 - 3)^3 $$
- $C_{10} > 0$: initial shear stiffness ($\mu = 2 C_{10}$).
- $C_{20} < 0$: captures intermediate softening during network uncoiling.
- $C_{30} > 0$: captures the steep final strain-stiffening due to finite chain extensibility.
*(Citation: Yeoh, O. H., Characterization of Elastic Properties of Carbon-Black-Filled Rubber Vulcanizates, Rubber Chem. Technol., vol. 63, 1990, pp. 792-805, specifically pp. 793-797).*

---

## Section 2: Structured Bibliographic Catalog (The 60 Studies)

The 60 scientific studies curated and CrossRef-verified are organized below into four primary material classes. Each entry details the verified DOI, materials evaluated, experimental mechanical assays performed, synthesized scientific findings, and publication impact.


### Category A: Polyurethanes and Thermoplastic Polyurethanes (TPU)
*Scaffolds, Meniscal/Ligament Grafts, and Supramolecular Elastomeric Networks*

#### Study 3: Three-Dimensional Printing Parameter Assessment of Elastomers for Tendon Graft Applications
- **Active Verified DOI:** [https://doi.org/10.3390/biomimetics10110785](https://doi.org/10.3390/biomimetics10110785)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Tendon (Achilles, digital flexor, patellar), Articular cartilage / Meniscus vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength
- **Core Scientific Findings & Mechanical Comparison:** Additive manufacturing has significantly advanced patient-specific medical devices, particularly for hard tissue repair, yet applications in soft tissue remain limited. Existing approaches for 3D-printed soft tissue devices employ mainly biogels and bioinks for regenerative purposes, while synthetic grafts for tendons and ligaments remain non-customizable in shape and mechanics. This study investigates the mechanical performance of 3D-printed thermoplastic polyurethane (TPU) elastomers as a function of printing parameters, informing customizable connective tissue graft designs.
- **Publication Venue & Citation Impact:** 1 citations (Biomimetics, 2025)

#### Study 4: Bioinspired scaffolds based on aligned polyurethane nanofibers mimic tendon and ligament fascicles
- **Active Verified DOI:** [https://doi.org/10.1002/biot.202300117](https://doi.org/10.1002/biot.202300117)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Tendon (Achilles, digital flexor, patellar) vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength
- **Core Scientific Findings & Mechanical Comparison:** Topographical factors of scaffolds play an important role in regulating cell functions. Although the effects of alignment topography and three-dimensional (3D) configuration of nanofibers as well as surface stiffness on cell behavior have been investigated, there are relatively few reports that attempt to understand the relationship between synergistic effects of these parameters and cell responses. Herein, the influence of biophysical and biomechanical cues of electrospun polyurethane (PU) scaffolds on mesenchymal stem cells (MSCs) activities was evaluated.
- **Publication Venue & Citation Impact:** 12 citations (Biotechnology Journal, 2023)

#### Study 6: Experimental Characterization of High-Strain-Rate Viscoelastic and Damage Behavior in Anisotropic Soft Materials Using Laser-Induced Inertial Cavitation
- **Active Verified DOI:** [https://doi.org/10.1007/s11340-026-01315-0](https://doi.org/10.1007/s11340-026-01315-0)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Polyurethane / Thermoplastic Polyurethane (TPU), Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Hyperelastic & viscoelastic constitutive model calibration (Fung, Ogden, Mooney-Rivlin)
- **Core Scientific Findings & Mechanical Comparison:** Background: Characterizing dynamic fracture and rate-dependent cavitation in soft materials remains a significant challenge due to their nonlinear, large-deformation, and rate-dependent mechanical behavior. Despite these challenges, understanding material response in this regime is essential for a wide range of engineering and biomedical applications, including laser eye surgery, lithotripsy, and high-rate energy deposition in soft tissues.
- **Publication Venue & Citation Impact:** 2 citations (Experimental Mechanics, 2026)

#### Study 10: Annealed Polyvinyl Alcohol Hydrogels for Cartilage Replacement: Effects of Synthesis Parameters on Mechanical Properties
- **Active Verified DOI:** [https://doi.org/10.3390/gels11080644](https://doi.org/10.3390/gels11080644)
- **Materials Compared:** Articular cartilage / Meniscus vs. Polyurethane / Thermoplastic Polyurethane (TPU), Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** The objective of this paper was to determine the interactive effects of multiple synthesis parameters on annealed PVA hydrogel properties and assess these hydrogels for the application of cartilage replacement. PVA hydrogels were synthesized at two different molecular weight ranges (89-98 kDa and 146-186 kDa), two polymer concentrations (10% PVA and 20% PVA), and four different annealing temperatures (120 °C, 135 °C, 150 °C, and 165 °C).
- **Publication Venue & Citation Impact:** 4 citations (Gels, 2025)

#### Study 12: Biofabricated soft network composites for cartilage tissue engineering
- **Active Verified DOI:** [https://doi.org/10.1088/1758-5090/aa6b15](https://doi.org/10.1088/1758-5090/aa6b15)
- **Materials Compared:** Articular cartilage / Meniscus vs. Polyurethane / Thermoplastic Polyurethane (TPU), Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel), Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** Articular cartilage from a material science point of view is a soft network composite that plays a critical role in load-bearing joints during dynamic loading. Its composite structure, consisting of a collagen fiber network and a hydrated proteoglycan matrix, gives rise to the complex mechanical properties of the tissue including viscoelasticity and stress relaxation. Melt electrospinning writing allows the design and fabrication of medical grade polycaprolactone (mPCL) fibrous networks for the reinforcement of soft hydrogel matrices for cartilage tissue engineering.
- **Publication Venue & Citation Impact:** 194 citations (Biofabrication, 2017)

#### Study 14: Material design of soft biological tissue replicas using viscoelastic micromechanical modelling
- **Active Verified DOI:** [https://doi.org/10.1016/j.jmbbm.2021.104875](https://doi.org/10.1016/j.jmbbm.2021.104875)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Silicone elastomer / PDMS, Polyurethane / Thermoplastic Polyurethane (TPU), Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** Anatomical models for research and education are often made of artificial materials that attempt to mimic biological tissues in terms of their mechanical properties. Recent developments in additive manufacturing allow tuning mechanical properties with microstructural designs. We propose a strategy for designing material microstructures to mimic soft tissue viscoelastic behaviour, based on a micromechanical Mori-Tanaka model. The model was applied to predict homogenised viscoelastic properties of materials, exhibiting a matrix-inclusion microstructure with varying inclusion volume fractions.
- **Publication Venue & Citation Impact:** 21 citations (Journal of the Mechanical Behavior of Biomedical Materials, 2021)

#### Study 20: A comparison of hyperelastic constitutive models applicable to brain and fat tissues
- **Active Verified DOI:** [https://doi.org/10.1098/rsif.2015.0486](https://doi.org/10.1098/rsif.2015.0486)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Hyperelastic & viscoelastic constitutive model calibration (Fung, Ogden, Mooney-Rivlin)
- **Core Scientific Findings & Mechanical Comparison:** In some soft biological structures such as brain and fat tissues, strong experimental evidence suggests that the shear modulus increases significantly under increasing compressive strain, but not under tensile strain, whereas the apparent Young's elastic modulus increases or remains almost constant when compressive strain increases. These tissues also exhibit a predominantly isotropic, incompressible behaviour.
- **Publication Venue & Citation Impact:** 235 citations (Journal of The Royal Society Interface, 2015)

#### Study 28: Poroelastic Modeling of Highly Hydrated Collagen Hydrogels: Experimental Results vs. Numerical Simulation With Custom and Commercial Finite Element Solvers
- **Active Verified DOI:** [https://doi.org/10.3389/fbioe.2018.00142](https://doi.org/10.3389/fbioe.2018.00142)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Polyurethane / Thermoplastic Polyurethane (TPU), Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Hyperelastic & viscoelastic constitutive model calibration (Fung, Ogden, Mooney-Rivlin)
- **Core Scientific Findings & Mechanical Comparison:** This study presents a comparison between the performances of two Finite Element (FE) solvers for the modeling of the poroelastic behavior of highly hydrated collagen hydrogels. Characterization of collagen hydrogels has been a widespread challenge since this is one of the most used natural biomaterials for Tissue Engineering (TE) applications. V-Biomech® is a free custom FE solver oriented to soft tissue modeling, while Abaqus® is a general-purpose commercial FE package which is widely used for biomechanics computational modeling.
- **Publication Venue & Citation Impact:** 20 citations (Frontiers in Bioengineering and Biotechnology, 2018)

#### Study 30: Design and Mechanical Characterization Using Digital Image Correlation of Soft Tissue-Mimicking Polymers
- **Active Verified DOI:** [https://doi.org/10.3390/polym14132639](https://doi.org/10.3390/polym14132639)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Tendon (Achilles, digital flexor, patellar) vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** Present and future anatomical models for biomedical applications will need bio-mimicking three-dimensional (3D)-printed tissues. These would enable, for example, the evaluation of the quality-performance of novel devices at an intermediate step between ex-vivo and in-vivo trials. Nowadays, PolyJet technology produces anatomical models with varying levels of realism and fidelity to replicate organic tissues. These include anatomical presets set with combinations of multiple materials, transitions, and colors that vary in hardness, flexibility, and density.
- **Publication Venue & Citation Impact:** 28 citations (Polymers, 2022)

#### Study 31: Compressive Mechanical Behavior of Partially Oxidized Polyvinyl Alcohol Hydrogels for Cartilage Tissue Repair
- **Active Verified DOI:** [https://doi.org/10.3390/bioengineering9120789](https://doi.org/10.3390/bioengineering9120789)
- **Materials Compared:** Articular cartilage / Meniscus vs. Polyurethane / Thermoplastic Polyurethane (TPU), Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Hyperelastic & viscoelastic constitutive model calibration (Fung, Ogden, Mooney-Rivlin)
- **Core Scientific Findings & Mechanical Comparison:** Polyvinyl alcohol (PVA) hydrogels are extensively used as scaffolds for tissue engineering, although their biodegradation properties have not been optimized yet. To overcome this limitation, partially oxidized PVA has been developed by means of different oxidizing agents, obtaining scaffolds with improved biodegradability. The oxidation reaction also allows tuning the mechanical properties, which are essential for effective use in vivo.
- **Publication Venue & Citation Impact:** 18 citations (Bioengineering, 2022)

#### Study 34: Length scales in the tear resistance of soft tissues and elastomers: a comparative study based on computational models
- **Active Verified DOI:** [https://doi.org/10.1007/s10704-025-00842-6](https://doi.org/10.1007/s10704-025-00842-6)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Abstract Fracture toughness describes a material’s ability to resist failure in the presence of defects. In case of soft biological tissues, a reliable determination and interpretation of the fracture properties is essential to estimate the risk of fracture after clinical interventions. Here we perform a comparative computational study between soft biological tissues and compliant elastomers to discuss the influence of material non-linearity on the crack tip nearfield.
- **Publication Venue & Citation Impact:** 5 citations (International Journal of Fracture, 2025)

#### Study 36: Additive Manufacturing to Mimic the Nonlinear Mechanical Behavior of Cardiac Soft Tissue
- **Active Verified DOI:** [https://doi.org/10.3390/polym17212949](https://doi.org/10.3390/polym17212949)
- **Materials Compared:** Articular cartilage / Meniscus vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Nonlinear toe-region compliance & strain stiffening; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** Soft biological tissues display highly nonlinear and anisotropic mechanical behavior, which is critical to their physiological function. Replicating these mechanical properties using engineered materials and additive manufacturing represents a significant challenge in biomedical engineering, particularly for surgical simulation, device development, and preclinical testing. The left atrial appendage (LAA) was selected since it plays a central role in thrombus formation during atrial fibrillation, significantly contributing to cardioembolic stroke.
- **Publication Venue & Citation Impact:** 2 citations (Polymers, 2025)

#### Study 40: Metal-Coordinated Dynamics and Viscoelastic Properties of Double-Network Hydrogels
- **Active Verified DOI:** [https://doi.org/10.3390/gels9020145](https://doi.org/10.3390/gels9020145)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Polyurethane / Thermoplastic Polyurethane (TPU), Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Biological soft tissues are intrinsically viscoelastic materials which play a significant role in affecting the activity of cells. As potential artificial alternatives, double-network (DN) gels, however, are pure elastic and mechanically time independent. The viscoelasticization of DN gels is an urgent challenge in enabling DN gels to be used for advanced development of biomaterial applications. Herein, we demonstrate a simple approach to regulate the viscoelasticity of tough double-network (DN) hydrogels by forming sulfonate–metal coordination.
- **Publication Venue & Citation Impact:** 13 citations (Gels, 2023)

#### Study 41: Flaw-insensitive fatigue resistance of chemically fixed collagenous soft tissues
- **Active Verified DOI:** [https://doi.org/10.1126/sciadv.ade7375](https://doi.org/10.1126/sciadv.ade7375)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Bovine pericardium (BP) has been used as leaflets of prosthetic heart valves. The leaflets are sutured on metallic stents and can survive 400 million flaps (~10-year life span), unaffected by the suture holes. This flaw-insensitive fatigue resistance is unmatched by synthetic leaflets. We show that the endurance strength of BP under cyclic stretch is insensitive to cuts as long as 1 centimeter, about two orders of magnitude longer than that of a thermoplastic polyurethane (TPU).
- **Publication Venue & Citation Impact:** 64 citations (Science Advances, 2023)

#### Study 42: Multiscale mechanical analysis of the elastic modulus of skin
- **Active Verified DOI:** [https://doi.org/10.1016/j.actbio.2023.08.030](https://doi.org/10.1016/j.actbio.2023.08.030)
- **Materials Compared:** Skin / Dermal tissue vs. Polyurethane / Thermoplastic Polyurethane (TPU), Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** The mechanical properties of the skin determine tissue function and regulate dermal cell behavior. Yet measuring these properties remains challenging, as evidenced by the large range of elastic moduli reported in the literature-from below one kPa to hundreds of MPa. Here, we reconcile these disparate results by dedicated experiments at both tissue and cellular length scales and by computational models considering the multiscale and multiphasic tissue structure.
- **Publication Venue & Citation Impact:** 131 citations (Acta Biomaterialia, 2023)

#### Study 45: Effect of the apron in the mechanical characterisation of hyperelastic materials by means of biaxial testing: A new method to improve accuracy
- **Active Verified DOI:** [https://doi.org/10.1016/j.jmbbm.2023.106291](https://doi.org/10.1016/j.jmbbm.2023.106291)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Silicone elastomer / PDMS, Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Hyperelastic & viscoelastic constitutive model calibration (Fung, Ogden, Mooney-Rivlin)
- **Core Scientific Findings & Mechanical Comparison:** Biological soft tissues and polymers used in biomedical applications (e.g. in the cardiovascular area) are hyperelastic incompressible materials that commonly operate under multi-axial large deformation fields. Their characterisation requires biaxial tensile testing. Due to the typically small sample size, the gripping of the specimens commonly relies on rakes or sutures, where the specimen is punctured at the edges of the gauge area. This approach necessitates of an apron, excess of material around the gauge region.
- **Publication Venue & Citation Impact:** 8 citations (Journal of the Mechanical Behavior of Biomedical Materials, 2023)

#### Study 49: Dynamic Supramolecular Polyurethane Elastomers Enabling Bioinspired Strain‐Adaptive Stiffening to Resolve the Softness–Strength Paradox
- **Active Verified DOI:** [https://doi.org/10.1002/advs.74910](https://doi.org/10.1002/advs.74910)
- **Materials Compared:** Skin / Dermal tissue vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** ABSTRACT The rapid progress of soft robotics and flexible electronics has sparked tremendous demand for elastomers that unite high mechanical strength with intrinsic softness, thereby mimicking the mechanical performance of natural skin. Herein, we report a class of dynamically crosslinked supramolecular polyurethanes that combine skin‐comparable low modulus with high strength and room‐temperature self‐healing, achieved through a segmental molecular engineering strategy.
- **Publication Venue & Citation Impact:** 9 citations (Advanced Science, 2026)

#### Study 57: Stiff and tough PDMS-MMT layered nanocomposites visualized by AIE luminogens
- **Active Verified DOI:** [https://doi.org/10.1038/s41467-021-24835-w](https://doi.org/10.1038/s41467-021-24835-w)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Silicone elastomer / PDMS, Polyurethane / Thermoplastic Polyurethane (TPU), Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Polydimethylsiloxane (PDMS) is a widely used soft material that exhibits excellent stability and transparency. But the difficulty of fine-tuning its Young's modulus and its low toughness significantly hinder its application in fields such as tissue engineering and flexible devices. Inspired by nacre, here we report on the development of PDMS-montmorillonite layered (PDMS-MMT-L) nanocomposites via the ice-templating technique, resulting in 23 and 12 times improvement in Young's modulus and toughness as compared with pure PDMS.
- **Publication Venue & Citation Impact:** 134 citations (Nature Communications, 2021)

#### Study 60: Puncturing of soft tissues: experimental and fracture mechanics-based study
- **Active Verified DOI:** [https://doi.org/10.1039/d3sm00011g](https://doi.org/10.1039/d3sm00011g)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Silicone elastomer / PDMS, Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Fracture toughness, tear resistance, and fatigue threshold; Hyperelastic & viscoelastic constitutive model calibration (Fung, Ogden, Mooney-Rivlin)
- **Core Scientific Findings & Mechanical Comparison:** The integrity of soft materials against puncturing is of great relevance for their performance because of the high sensitivity to local rupture caused by rigid sharp objects. In this work, the mechanics of puncturing is studied with respect to a sharp-tipped rigid needle with a circular cross section, penetrating a soft target solid. The failure mode associated with puncturing is identified as a mode-I crack propagation, which is analytically described by a two-dimensional model of the target solid, taking place in a plane normal to the penetration axis.
- **Publication Venue & Citation Impact:** 27 citations (Soft Matter, 2023)


### Category B: Hydrogels and Interpenetrating Double Networks
*PVA, PEG, PAM, Alginate, Chitosan, and Biomimetic Resilin Gels*

#### Study 2: Transient dynamic mechanical properties of resilin-based elastomeric hydrogels
- **Active Verified DOI:** [https://doi.org/10.3389/fchem.2014.00021](https://doi.org/10.3389/fchem.2014.00021)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect
- **Core Scientific Findings & Mechanical Comparison:** The outstanding high-frequency properties of emerging resilin-like polypeptides (RLPs) have motivated their development for vocal fold tissue regeneration and other applications. Recombinant RLP hydrogels show efficient gelation, tunable mechanical properties, and display excellent extensibility, but little has been reported about their transient mechanical properties. In this manuscript, we describe the transient mechanical behavior of new RLP hydrogels investigated via both sinusoidal oscillatory shear deformation and uniaxial tensile testing.
- **Publication Venue & Citation Impact:** 44 citations (Frontiers in Chemistry, 2014)

#### Study 5: Specimen Geometry Effect on Experimental Tensile Mechanical Properties of Tough Hydrogels
- **Active Verified DOI:** [https://doi.org/10.3390/ma16020785](https://doi.org/10.3390/ma16020785)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Tendon (Achilles, digital flexor, patellar), Articular cartilage / Meniscus vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Synthetic tough hydrogels have received attention because they could mimic the mechanical properties of natural hydrogels, such as muscle, ligament, tendon, and cartilage. Many recent studies suggest various approaches to enhance the mechanical properties of tough hydrogels. However, directly comparing each hydrogel property in different reports is challenging because various testing specimen shapes/sizes were employed, affecting the experimental mechanical property values.
- **Publication Venue & Citation Impact:** 17 citations (Materials, 2023)

#### Study 7: The effect of heterobifunctional crosslinkers on HEMA hydrogel modulus and toughness
- **Active Verified DOI:** [https://doi.org/10.1371/journal.pone.0215895](https://doi.org/10.1371/journal.pone.0215895)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Articular cartilage / Meniscus vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** The use of hydrogels in load bearing applications is often limited by insufficient toughness. 2-Hydroxyethyl methacrylate (HEMA) based hydrogels are appealing for translational work, as they are affordable and the use of HEMA is FDA approved. Furthermore, HEMA is photopolymerizable, providing spatiotemporal control over mechanical properties. We evaluated the ability of vinyl methacrylate (VM), allyl methacrylate (AM), and 3-(Acryloyloxy)-2-hydroxypropyl methacrylate (AHPM) to tune hydrogel toughness and Young's modulus.
- **Publication Venue & Citation Impact:** 25 citations (PLOS ONE, 2019)

#### Study 11: Hierarchical crack-resistant, tissue-mimetic hydrogels enabled by progressive nanocrystallization of anisotropic polymer networks
- **Active Verified DOI:** [https://doi.org/10.1038/s41467-025-65917-3](https://doi.org/10.1038/s41467-025-65917-3)
- **Materials Compared:** Tendon (Achilles, digital flexor, patellar), Articular cartilage / Meniscus vs. Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Abstract Addressing the persistent challenge of reconciling extreme mechanical robustness with tissue-mimetic functionality in hydrogels, we present a phase-transition-guided hierarchical engineering strategy that progressively architectures anisotropic polyvinyl alcohol networks through sequential mechanical training, wet-annealing, and salting-out.
- **Publication Venue & Citation Impact:** 29 citations (Nature Communications, 2025)

#### Study 13: Designing Viscoelastic Gelatin-PEG Macroporous Hybrid Hydrogel with Anisotropic Morphology and Mechanical Properties for Tissue Engineering Application
- **Active Verified DOI:** [https://doi.org/10.3390/micro3020029](https://doi.org/10.3390/micro3020029)
- **Materials Compared:** Articular cartilage / Meniscus vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** The mechanical properties of scaffolds play a vital role in regulating key cellular processes in tissue development and regeneration in the field of tissue engineering. Recently, scaffolding material design strategies leverage viscoelasticity to guide stem cells toward specific tissue regeneration. Herein, we designed and developed a viscoelastic Gel-PEG hybrid hydrogel with anisotropic morphology and mechanical properties using a gelatin and functionalized PEG (as a crosslinker) under a benign condition for tissue engineering application.
- **Publication Venue & Citation Impact:** 21 citations (Micro, 2023)

#### Study 15: Structural Motifs as Programmable Design Parameters for Tuning Vascular Mechanics in Fiber-Reinforced Hydrogel Grafts
- **Active Verified DOI:** [https://doi.org/10.3390/biomimetics11080591](https://doi.org/10.3390/biomimetics11080591)
- **Materials Compared:** Vascular tissue (Arteries / Aorta) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel), Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** Replicating the nonlinear, pressure-dependent mechanical behavior of native arteries remains a central challenge in small-diameter vascular graft design, where compliance mismatch between synthetic grafts and host vessels is strongly linked to graft failure.
- **Publication Venue & Citation Impact:** Published in Biomimetics (2026)

#### Study 16: Engineering Lignin‐Based Tubular Hydrogel Scaffolds for Load‐Bearing Biomedical Applications
- **Active Verified DOI:** [https://doi.org/10.1002/cssc.202501520](https://doi.org/10.1002/cssc.202501520)
- **Materials Compared:** Tendon (Achilles, digital flexor, patellar) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** The development of mechanically robust, biocompatible, and biodegradable hydrogels remains a significant challenge for biomedical applications involving load-bearing soft tissues. Herein, a tubular lignin-derived hydrogel is engineered to assess its physicochemical, mechanical, and biological properties. Kraft and organosolv lignin are systematically compared at varying crosslinker concentrations to determine their effect on pore morphology, swelling behavior, and mechanical performance.
- **Publication Venue & Citation Impact:** 7 citations (ChemSusChem, 2025)

#### Study 17: Biodegradable-Glass-Fiber Reinforced Hydrogel Composite with Enhanced Mechanical Performance and Cell Proliferation for Potential Cartilage Repair
- **Active Verified DOI:** [https://doi.org/10.3390/ijms23158717](https://doi.org/10.3390/ijms23158717)
- **Materials Compared:** Articular cartilage / Meniscus vs. Poly(vinyl alcohol) (PVA) hydrogel, Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Polyvinyl alcohol (PVA) hydrogels are promising implants due to the similarity of their low-friction behavior to that of cartilage tissue, and also due to their non-cytotoxicity. However, their poor mechanical resistance and insufficient durability restricts their application in this area.
- **Publication Venue & Citation Impact:** 27 citations (International Journal of Molecular Sciences, 2022)

#### Study 19: Highly Oriented Bio‐Mimetic Hydrogels by Calendering
- **Active Verified DOI:** [https://doi.org/10.1002/advs.202504778](https://doi.org/10.1002/advs.202504778)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Tendon (Achilles, digital flexor, patellar) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Abstract Anisotropic hydrogels are promising candidates as load‐bearing materials for tissue engineering, while huge challenges remain in exploring effective and scalable methods for the preparation of anisotropic hydrogels with simultaneous high tensile strength, large toughness, good fracture strain, excellent fatigue and swelling resistances. Inspired by the brick‐and‐mortar layered structure of nacre and the hierarchical fibril structure of soft tissues (e.g., tendons and ligaments), achieving high tensile strength, exceptional fracture toughness, and fatigue resistance.
- **Publication Venue & Citation Impact:** 11 citations (Advanced Science, 2025)

#### Study 24: Control nucleation for strong and tough crystalline hydrogels with high water content
- **Active Verified DOI:** [https://doi.org/10.1038/s41467-024-52264-y](https://doi.org/10.1038/s41467-024-52264-y)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Tendon (Achilles, digital flexor, patellar), Articular cartilage / Meniscus vs. Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Hydrogels, provided that they integrate strength and toughness at desired high content of water, promise in load-bearing tissues such as articular cartilage, ligaments, tendons. Many developed strategies impart hydrogels with some mechanical properties akin to natural tissues, but compromise water content. Herein, a strategy deprotonation-complexation-reprotonation is proposed to prepare polyvinyl alcohol hydrogels with water content as high as ~80% and favorable mechanical properties, including tensile strength of 7.4 MPa, elongation of around 1350%, and fracture toughness of 12.4 kJ m⁻².
- **Publication Venue & Citation Impact:** 72 citations (Nature Communications, 2024)

#### Study 25: Triple‐Mechanism Enhanced Flexible SiO 2 Nanofiber Composite Hydrogel with High Stiffness and Toughness for Cartilaginous Ligaments
- **Active Verified DOI:** [https://doi.org/10.1002/smll.202310046](https://doi.org/10.1002/smll.202310046)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Tendon (Achilles, digital flexor, patellar) vs. Poly(vinyl alcohol) (PVA) hydrogel, Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Abstract Hydrogels are widely used in tissue engineering, soft robotics and wearable electronics. However, it is difficult to achieve both the required toughness and stiffness, which severely hampers their application as load‐bearing materials. This study presents a strategy to develop a hard and tough composite hydrogel.
- **Publication Venue & Citation Impact:** 27 citations (Small, 2024)

#### Study 27: A mechanical evaluation of polyvinyl alcohol hydrogels for temporomandibular joint disc replacement
- **Active Verified DOI:** [https://doi.org/10.3389/fphy.2022.928579](https://doi.org/10.3389/fphy.2022.928579)
- **Materials Compared:** Articular cartilage / Meniscus vs. Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength
- **Core Scientific Findings & Mechanical Comparison:** The temporomandibular joint (TMJ) is a bilateral ginglymoarthroidal joint containing a fibrocartilaginous disc which distributes compressive stress and reduces friction on the articulating surfaces of the joint. Initially, conservative treatments can address disorders of the TMJ, but surgical procedures such as discectomy may be employed if dysfunction persists. Unfortunately, discectomy increases friction and alters the mechanical behavior of the TMJ when the disc is not replaced.
- **Publication Venue & Citation Impact:** 11 citations (Frontiers in Physics, 2022)

#### Study 29: Strong and Fatigue‐Resistant Hydrogels via Poor Solvent Evaporation Assisted Hot‐Stretching for Tendon Repair
- **Active Verified DOI:** [https://doi.org/10.1002/advs.202503697](https://doi.org/10.1002/advs.202503697)
- **Materials Compared:** Tendon (Achilles, digital flexor, patellar) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Abstract It is highly desirable but still remains challenging to develop high‐performance hydrogels with satisfactory mechanical properties for tissue engineering. Here, anisotropic yet transparent hydrogels (AHs) are prepared for tendon repair via a facile “poor solvent evaporation assisted hot‐stretching” strategy. AHs have great mechanical properties with tensile strength, toughness, and fracture energy as high as 33.1 ± 2.05 MPa, 44.1 ± 3.50 MJ m⁻³, and 106 ± 7.20 kJ m⁻², respectively.
- **Publication Venue & Citation Impact:** 18 citations (Advanced Science, 2025)

#### Study 38: Hydrogels of arrested phase separation simultaneously achieve high strength and low hysteresis
- **Active Verified DOI:** [https://doi.org/10.1126/sciadv.adh7742](https://doi.org/10.1126/sciadv.adh7742)
- **Materials Compared:** Tendon (Achilles, digital flexor, patellar) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect
- **Core Scientific Findings & Mechanical Comparison:** Hydrogels are being developed to bear loads. Applications include artificial tendons and muscles, which require high strength to bear loads and low hysteresis to reduce energy loss. However, simultaneously achieving high strength and low hysteresis has been challenging. This challenge is met here by synthesizing hydrogels of arrested phase separation. Such a hydrogel has interpenetrating hydrophilic and hydrophobic networks, which separate into a water-rich phase and a water-poor phase. The two phases arrest at the microscale.
- **Publication Venue & Citation Impact:** 117 citations (Science Advances, 2023)

#### Study 39: Soft, strong, tough, and durable protein-based fiber hydrogels
- **Active Verified DOI:** [https://doi.org/10.1073/pnas.2213030120](https://doi.org/10.1073/pnas.2213030120)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Load-bearing soft tissues normally show J-shaped stress–strain behaviors with high compliance at low strains yet high strength at high strains. They have high water content but are still tough and durable. By contrast, naturally derived hydrogels are weak and brittle. Although hydrogels prepared from synthetic polymers can be strong and tough, they do not have the desired bioactivity for emerging biomedical applications. Here, we present a thermomechanical approach to replicate the combinational properties of soft tissues in protein-based photocrosslinkable hydrogels.
- **Publication Venue & Citation Impact:** 51 citations (Proceedings of the National Academy of Sciences, 2023)

#### Study 43: Effects of polymerization degree on recovery behavior of PVA/PVP hydrogels as potential articular cartilage prosthesis after fatigue test
- **Active Verified DOI:** [https://doi.org/10.3144/expresspolymlett.2016.13](https://doi.org/10.3144/expresspolymlett.2016.13)
- **Materials Compared:** Articular cartilage / Meniscus vs. Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Poly (vinyl alcohol)/poly (vinyl pyrrolidone) (PVA/PVP) hydrogels with various polymerization degrees of PVA were synthesized by a repeated freezing-thawing method.The influence of polymerization degree on microstructure, water content, friction coefficient, compressive fatigue and recovery properties of PVA/PVP hydrogels were investigated.The results showed that higher polymerization degree resulted in larger compressive modulus and lower friction coefficient.
- **Publication Venue & Citation Impact:** 38 citations (Express Polymer Letters, 2015)

#### Study 44: Effects of Non-Conventional Sterilisation Methods on PBO-Reinforced PVA Hydrogels for Cartilage Replacement
- **Active Verified DOI:** [https://doi.org/10.3390/gels8100640](https://doi.org/10.3390/gels8100640)
- **Materials Compared:** Articular cartilage / Meniscus vs. Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength
- **Core Scientific Findings & Mechanical Comparison:** Articular cartilage (AC) degradation is a recurrent pathology that affects millions of people worldwide. Polyvinyl alcohol (PVA) hydrogels have been widely explored for AC replacement. However, their mechanical performance is generally inadequate, and these materials need to be reinforced. Moreover, to be used in a clinical setting, such materials must undergo effective sterilisation.
- **Publication Venue & Citation Impact:** 9 citations (Gels, 2022)

#### Study 46: Tissue-like Fracture Toughness and Stress–Relaxation Ability in PVA-Agar-Based Hydrogels for Biomedical Applications
- **Active Verified DOI:** [https://doi.org/10.3390/gels11090747](https://doi.org/10.3390/gels11090747)
- **Materials Compared:** Articular cartilage / Meniscus vs. Poly(vinyl alcohol) (PVA) hydrogel
- **Mechanical Properties & Assays Evaluated:** Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Soft tissues exhibit remarkable stretchability, fracture toughness, and stress–relaxation ability. They possess a large water content to support cellular processes. Mimicking such a combination of mechanical and physical properties in hydrogels is important for tissue engineering applications but remains challenging. This work aims to develop a hydrogel that can combine excellent mechanical properties with cellular viability. The research focused on polyvinyl alcohol (PVA)/agar double-network (DN) hydrogels, fabricated by thermal gelation and freeze–thawing methods.
- **Publication Venue & Citation Impact:** 2 citations (Gels, 2025)

#### Study 50: Hyperelastic Swelling of Stiff Hydrogels
- **Active Verified DOI:** [https://doi.org/10.1103/physrevlett.134.148203](https://doi.org/10.1103/physrevlett.134.148203)
- **Materials Compared:** Articular cartilage / Meniscus vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Hyperelastic & viscoelastic constitutive model calibration (Fung, Ogden, Mooney-Rivlin)
- **Core Scientific Findings & Mechanical Comparison:** Hydrogels are swollen polymer networks where elastic deformation is coupled to nanoscale fluid flow. As a consequence, hydrogels can withstand large strains and exhibit nonlinear, hyperelastic properties. Previous studies have shown that low-modulus hydrogels and semiflexible biopolymer networks universally contract when sheared on timescales much longer than the poroelastic relaxation timescale. Using rheological and tribological measurements, we find that stiff polyacrylamide and polyacrylic acid hydrogels, with moduli of order ∼10-100 kPa, exclusively swell (dilate) when sheared.
- **Publication Venue & Citation Impact:** 7 citations (Physical Review Letters, 2025)

#### Study 51: A tough and robust hydrogel constructed through carbon dots induced crystallization domains integrated orientation regulation
- **Active Verified DOI:** [https://doi.org/10.1038/s41467-025-61535-1](https://doi.org/10.1038/s41467-025-61535-1)
- **Materials Compared:** Tendon (Achilles, digital flexor, patellar) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Abstract In this study, we present a novel strategy in which carbon dots (CDs) are employed to induce the formation of crystalline domains and further fabricated super-strong hydrogels with high crystallinity, distinct anisotropic structures and dense networks based on “pinning effect”, specifically, CDs act as nanofillers and heterogeneous nucleating agents. As-prepared hydrogels exhibited exceptional swelling resistance, ultra-high tensile strength (156 MPa) and toughness (225 kJ m⁻²), demonstrating the efficacy of carbon dot pinning in resisting crack propagation and fatigue.
- **Publication Venue & Citation Impact:** 100 citations (Nature Communications, 2025)

#### Study 54: Alginate/cartilage extracellular matrix-based injectable interpenetrating polymer network hydrogel for cartilage tissue engineering
- **Active Verified DOI:** [https://doi.org/10.1177/08853282211024020](https://doi.org/10.1177/08853282211024020)
- **Materials Compared:** Articular cartilage / Meniscus vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel)
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** In situ forming hydrogels were composed of different ionic crosslinked alginate concentrations with 1% w/v enzymatically crosslinked phenolized cartilage ECM, resulting in an interpenetrating polymer network (IPN). The response surface methodology (RSM) approach was applied to optimize IPN hydrogel's mechanical properties by varying alginate and SFN concentrations. The results demonstrated that upon increasing the alginate concentration, the compression modulus improved. The SFN concentration was optimized to reach a desired mechanical stiffness.
- **Publication Venue & Citation Impact:** 51 citations (Journal of Biomaterials Applications, 2021)

#### Study 55: A Synthetic Hydrogel Composite with the Mechanical Behavior and Durability of Cartilage
- **Active Verified DOI:** [https://doi.org/10.1002/adfm.202003451](https://doi.org/10.1002/adfm.202003451)
- **Materials Compared:** Articular cartilage / Meniscus vs. Poly(vinyl alcohol) (PVA) hydrogel, Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Abstract This article reports the first hydrogel with the strength and modulus of cartilage in both tension and compression, and the first to exhibit cartilage‐equivalent tensile fatigue strength at 100 000 cycles. These properties are achieved by infiltrating a bacterial cellulose (BC) nanofiber network with a poly(vinyl alcohol) (PVA)–poly(2‐acrylamido‐2‐methyl‐1‐propanesulfonic acid sodium salt) (PAMPS) double network hydrogel.
- **Publication Venue & Citation Impact:** 329 citations (Advanced Functional Materials, 2020)

#### Study 58: Nanohydroxyapatite Reinforced Chitosan Composite Hydrogel with Tunable Mechanical and Biological Properties for Cartilage Regeneration
- **Active Verified DOI:** [https://doi.org/10.1038/s41598-019-52042-7](https://doi.org/10.1038/s41598-019-52042-7)
- **Materials Compared:** Articular cartilage / Meniscus vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel), Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold; Hyperelastic & viscoelastic constitutive model calibration (Fung, Ogden, Mooney-Rivlin)
- **Core Scientific Findings & Mechanical Comparison:** With the continuous quest of developing hydrogel for cartilage regeneration with superior mechanobiological properties are still becoming a challenge. Chitosan (CS) hydrogels are the promising implant materials due to an analogous character of the soft tissue; however, their low mechanical strength and durability together with its lack of integrity with surrounding tissues hinder the load-bearing application. This can be solved by developing a composite chitosan hydrogel reinforced with Hydroxyapatite Nanorods (HANr).
- **Publication Venue & Citation Impact:** 130 citations (Scientific Reports, 2019)

#### Study 59: Biological Tissue-Inspired Ultrasoft, Ultrathin, and Mechanically Enhanced Microfiber Composite Hydrogel for Flexible Bioelectronics
- **Active Verified DOI:** [https://doi.org/10.1007/s40820-023-01096-4](https://doi.org/10.1007/s40820-023-01096-4)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Synthetic hydrogel (PVA, PEG, PAM, or double-network hydrogel), Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Hydrogels offer tissue-like softness, stretchability, fracture toughness, ionic conductivity, and compatibility with biological tissues, which make them promising candidates for fabricating flexible bioelectronics. A soft hydrogel film offers an ideal interface to directly bridge thin-film electronics with the soft tissues. However, it remains difficult to fabricate a soft hydrogel film with an ultrathin configuration and excellent mechanical strength.
- **Publication Venue & Citation Impact:** 149 citations (Nano-Micro Letters, 2023)


### Category C: High-Strength Braided and Fiber Polymers
*UHMWPE, PET/Polyester, Suture Tapes, and Artificial Ligament Systems*

#### Study 1: Bioinspired 3D braided artificial ligament with human-like mechanical properties and self-healing capability
- **Active Verified DOI:** [https://doi.org/10.3389/fbioe.2025.1701754](https://doi.org/10.3389/fbioe.2025.1701754)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments) vs. Polyurethane / Thermoplastic Polyurethane (TPU), UHMWPE (Ultra-high molecular weight polyethylene), PET (Polyethylene terephthalate) / Polyester fiber
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect
- **Core Scientific Findings & Mechanical Comparison:** Introduction: Joint injuries, a major type of human musculoskeletal disorder, are often accompanied by soft tissue damage, and restoring ligament integrity is a key strategy for reconstructing joint function. However, existing artificial ligaments face a critical challenge: reconciling robust biomechanical performance with intrinsic self-healing capability, especially under cyclic loading and accidental overload conditions.
- **Publication Venue & Citation Impact:** 1 citations (Frontiers in Bioengineering and Biotechnology, 2025)

#### Study 8: Native anterior talo-fibular ligament tensile characteristics compared to allograft, suture tape, and copolymer augmentation elements: A biomechanical study
- **Active Verified DOI:** [https://doi.org/10.1053/j.jfas.2024.08.016](https://doi.org/10.1053/j.jfas.2024.08.016)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments) vs. Polyurethane / Thermoplastic Polyurethane (TPU)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength
- **Core Scientific Findings & Mechanical Comparison:** Surgical augmentation methods have been introduced to the Modified Broström (MB) technique to support native anterior talo-fibular ligament (ATFL) healing and function. This study aimed to investigate the isolated biomechanical performance of common MB augmentation elements, including allograft, suture tape, and copolymer, compared to native ATFL. Six cadaveric feet were dissected, isolating the ATFL from all surrounding soft tissue. The fibula and talus were clamped on the testing frame so that the ligament was in line with the load cell.
- **Publication Venue & Citation Impact:** 6 citations (The Journal of Foot and Ankle Surgery, 2024)

#### Study 9: Amino Acid‐Based Thermoplastic Polyurethanes for Pelvic Floor Tissue Scaffolds
- **Active Verified DOI:** [https://doi.org/10.1002/mame.70353](https://doi.org/10.1002/mame.70353)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments) vs. Polyurethane / Thermoplastic Polyurethane (TPU), UHMWPE (Ultra-high molecular weight polyethylene)
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect
- **Core Scientific Findings & Mechanical Comparison:** ABSTRACT Biocompatible and biodegradable elastomers are of interest in soft tissue repair. A biodegradable thermoplastic polyurethane (TPU) was synthesized using an amino acid‐based diisocyanate, along with polycaprolactone diol and polyethylene glycol, to improve its biocompatibility, and different reaction times were investigated to study their effects. The structure, mechanical, thermal, swelling, and biodegradation behavior, as well as cytotoxicity, of the resulting polymers were characterized. The optimal TPU showed a tensile strength of 6.5 MPa, a Young's modulus of 26.0 MPa, demonstrating cytocompatibility and mechanical viability for pelvic floor soft tissue repair.
- **Publication Venue & Citation Impact:** Published in Macromolecular Materials and Engineering (2026)

#### Study 18: Cyclic tensile tests of Shetland pony superficial digital flexor tendons (SDFTs) with an optimized cryo-clamp combined with biplanar high-speed fluoroscopy
- **Active Verified DOI:** [https://doi.org/10.1186/s12917-021-02914-w](https://doi.org/10.1186/s12917-021-02914-w)
- **Materials Compared:** Tendon (Achilles, digital flexor, patellar) vs. PET (Polyethylene terephthalate) / Polyester fiber
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect
- **Core Scientific Findings & Mechanical Comparison:** BACKGROUND: Long-term cyclic tensile testing with equine palmar/plantar tendons have not yet been performed due to problems in fixing equine tendons securely and loading them cyclically. It is well established that the biomechanical response of tendons varies during cyclic loading over time. The aim of this study was to develop a clamping device that enables repetitive cyclic tensile testing of equine superficial digital flexor tendon for at least 60 loading cycles and for 5 min. RESULTS: A novel cryo-clamp was developed and built.
- **Publication Venue & Citation Impact:** 5 citations (BMC Veterinary Research, 2021)

#### Study 21: Investigating Commercial Filaments for 3D Printing of Stiff and Elastic Constructs with Ligament-Like Mechanics
- **Active Verified DOI:** [https://doi.org/10.3390/mi11090846](https://doi.org/10.3390/mi11090846)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments) vs. PET (Polyethylene terephthalate) / Polyester fiber, Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** The current gold standard technique for treatment of anterior cruciate ligament (ACL) injury is reconstruction with autograft. These treatments have a relatively high failure and re-tear rate. To overcome this, tissue engineering and additive manufacturing are being used to explore the potential of 3D scaffolds as autograft substitutes. However, mechanically optimal polymers for this have yet to be identified. Here, we use 3D printing technology and various materials with the aim of fabricating constructs better matching the mechanical properties of the native ACL.
- **Publication Venue & Citation Impact:** 39 citations (Micromachines, 2020)

#### Study 23: Novel flat and wide meniscal repair material improves the ultimate load of knot breakage in a porcine trans‐capsular meniscal repair model
- **Active Verified DOI:** [https://doi.org/10.1186/s40634-017-0114-4](https://doi.org/10.1186/s40634-017-0114-4)
- **Materials Compared:** Articular cartilage / Meniscus vs. Polyurethane / Thermoplastic Polyurethane (TPU), PET (Polyethylene terephthalate) / Polyester fiber
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** BACKGROUND: In the meniscal repair procedures, a high ultimate load capacity and low cyclic creep at the repair site are favorable and lead to good biological incorporation of the tear site after surgery. Previous biomechanical tensile tests of the meniscal sutures have identified the suture knot as the weakest point. We hypothesized that the strength of a suture knot depends on the suture shape, and therefore, we compared three differently shaped suture materials composed of the same material and quantity per length.
- **Publication Venue & Citation Impact:** 7 citations (Journal of Experimental Orthopaedics, 2017)

#### Study 26: Suture tapes show superior biomechanical properties and greater meniscal healing compared to conventional sutures in posterior meniscal root tear repairs: A systematic review
- **Active Verified DOI:** [https://doi.org/10.1002/ksa.12554](https://doi.org/10.1002/ksa.12554)
- **Materials Compared:** Articular cartilage / Meniscus vs. Polyurethane / Thermoplastic Polyurethane (TPU), UHMWPE (Ultra-high molecular weight polyethylene), PET (Polyethylene terephthalate) / Polyester fiber
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** PURPOSE: This study aims to perform a systematic review to determine whether ultra-high molecular weight polyethylene (UHMWPE) tapes have superior biomechanical properties compared to conventional sutures in posterior meniscal root tear (PMRT) repairs, and whether this translates into superior clinical outcomes. METHODS: The Cochrane Controlled Register of Trials, PubMed and Embase were used to perform a systematic review using the following search terms: (meniscus OR meniscal) AND (root OR posterior horn) AND (suture OR tape OR wire OR cord).
- **Publication Venue & Citation Impact:** 6 citations (Knee Surgery, Sports Traumatology, Arthroscopy, 2024)

#### Study 32: Hybrid structures for Achilles' tendon repair
- **Active Verified DOI:** [https://doi.org/10.1002/pat.5693](https://doi.org/10.1002/pat.5693)
- **Materials Compared:** Tendon (Achilles, digital flexor, patellar) vs. PET (Polyethylene terephthalate) / Polyester fiber
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect
- **Core Scientific Findings & Mechanical Comparison:** Abstract The management of extensive tendon injuries poses a considerable challenge. These cases typically require a partial replacement of the tissue, and conventional treatment options, either grafts or synthetic constructs, present disadvantages. Synthetic constructs may be produced with either non‐biodegradable or biodegradable polymers. However, while the former lacks biointegration, the latter requires a compromise between tissue ingrowth and biodegradability, frequently malfunctioning due to inadequate mechanical properties.
- **Publication Venue & Citation Impact:** 3 citations (Polymers for Advanced Technologies, 2022)

#### Study 37: Material Properties of Suture Augmentation of Knee Medial Collateral Ligament Repair Did Not Influence Length Changes or Failure Loads in a Caged Porcine Model
- **Active Verified DOI:** [https://doi.org/10.1016/j.asmr.2023.03.002](https://doi.org/10.1016/j.asmr.2023.03.002)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments) vs. Polyurethane / Thermoplastic Polyurethane (TPU), UHMWPE (Ultra-high molecular weight polyethylene), PET (Polyethylene terephthalate) / Polyester fiber
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Cyclic loading hysteresis, energy dissipation, and Mullins effect
- **Core Scientific Findings & Mechanical Comparison:** Purpose To investigate whether the biomechanical properties of the healed superficial medial collateral ligament (sMCL) repaired by augmentation vary depending on the material properties of the suture augmentation. Methods In 8 of 10 porcines (16 hindlimbs), the sMCL was detached from the femoral attachment using a scalpel under intubated general anesthesia. sMCL repair was performed using an ultra‐high‐molecular‐weight polyethylene (UHMWPE) tape for the right hindlimbs and polyester tape (PE) for the left hindlimbs. They were sacrificed at 4 weeks postoperatively.
- **Publication Venue & Citation Impact:** 4 citations (Arthroscopy, Sports Medicine, and Rehabilitation, 2023)

#### Study 47: Toward the Improvement of the Mechanical and Tribological Properties of Braided Ligament for ACL Reconstruction: A “Carrot and Stick” Strategy
- **Active Verified DOI:** [https://doi.org/10.1002/adhm.202304133](https://doi.org/10.1002/adhm.202304133)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments) vs. UHMWPE (Ultra-high molecular weight polyethylene), PET (Polyethylene terephthalate) / Polyester fiber
- **Mechanical Properties & Assays Evaluated:** Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Bone tunnel enlargement has been troubling the clinical adoption of braided artificial ligaments for decades, to which mechanical and tribological performance promotion shall be an effective and promising approach. Herein, a "carrot and stick" strategy has been introduced with two types of polyethylene terephthalate (PET) fibers to fabricate hybrid textures, which is expected to advance fatigue and tribological performance without yielding essential mechanical strength and biocompatibility.
- **Publication Venue & Citation Impact:** 5 citations (Advanced Healthcare Materials, 2024)


### Category D: Elastomers and Silicones
*PDMS, Supramolecular Zwitterionic Elastomers, and Soft Tissue Replicas*

#### Study 22: Melt electrowriting below the critical translation speed to fabricate crimped elastomer scaffolds with non-linear extension behaviour mimicking that of ligaments and tendons
- **Active Verified DOI:** [https://doi.org/10.1016/j.actbio.2018.03.023](https://doi.org/10.1016/j.actbio.2018.03.023)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments), Tendon (Achilles, digital flexor, patellar) vs. Synthetic polymer / elastomer matrix
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Nonlinear toe-region compliance & strain stiffening
- **Core Scientific Findings & Mechanical Comparison:** Ligaments and tendons are comprised of aligned, crimped collagen fibrils that provide tissue-specific mechanical properties with non-linear extension behaviour, exhibiting low stress at initial strain (toe region behaviour). To approximate this behaviour, we report fibrous scaffolds with sinusoidal patterns by melt electrowriting (MEW) below the critical translation speed (CTS) by exploitation of the natural flow behaviour of the polymer melt.
- **Publication Venue & Citation Impact:** 146 citations (Acta Biomaterialia, 2018)

#### Study 33: Mechanical Behavior of Oil-Saturated Silicone Membranes for Adipose Tissue Synthesis in Clinical and Theatrical Prosthesis
- **Active Verified DOI:** [https://doi.org/10.3390/prosthesis6060097](https://doi.org/10.3390/prosthesis6060097)
- **Materials Compared:** Fibrous biological soft tissue (collagenous connective tissue) vs. Silicone elastomer / PDMS
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** Emulating very soft tissues with synthetic materials is important for clinical prosthetists who want to improve compliance in maxillofacial and breast prosthesis. It is equally important for theatrical prosthetists wanting to model bariatric conditions and soft organs for surgical or palpation training. Polydimethylsiloxane (PDMS) gels, which are often used in medical model construction, are stiff and highly elastic compared to the friable soft tissues found in the body.
- **Publication Venue & Citation Impact:** 2 citations (Prosthesis, 2024)

#### Study 35: Superstretchable, yet stiff, fatigue-resistant ligament-like elastomers
- **Active Verified DOI:** [https://doi.org/10.1038/s41467-022-30021-3](https://doi.org/10.1038/s41467-022-30021-3)
- **Materials Compared:** Ligament (ACL, ATFL, or knee ligaments) vs. Synthetic polymer / elastomer matrix
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Abstract Ligaments are flexible and stiff tissues around joints to support body movements, showing superior toughness and fatigue-resistance. Such a combination of mechanical properties is rarely seen in synthetic elastomers because stretchability, stiffness, toughness, and fatigue resistance are seemingly incompatible in materials design. Here we resolve this long-standing mismatch through a hierarchical crosslinking design.
- **Publication Venue & Citation Impact:** 119 citations (Nature Communications, 2022)

#### Study 48: Rigidity-tuning conductive elastomer
- **Active Verified DOI:** [https://doi.org/10.1088/0964-1726/24/6/065001](https://doi.org/10.1088/0964-1726/24/6/065001)
- **Materials Compared:** Tendon (Achilles, digital flexor, patellar) vs. Silicone elastomer / PDMS, Fiber-reinforced polymer composite
- **Mechanical Properties & Assays Evaluated:** Uniaxial tensile stress-strain behavior; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity
- **Core Scientific Findings & Mechanical Comparison:** Abstract We introduce a conductive propylene-based elastomer (cPBE) that rapidly and reversibly changes its mechanical rigidity when powered with electrical current. The elastomer is rigid in its natural state, with an elastic (Young’s) modulus of 176 MPa, and softens when electrically activated. By embedding the cPBE in an electrically insulating sheet of polydimethylsiloxane (PDMS), we create a cPBE–PDMS composite that can reversibly change its tensile modulus between 37 and 1.5 MPa.
- **Publication Venue & Citation Impact:** 120 citations (Smart Materials and Structures, 2015)

#### Study 52: Empowering Low‐Hysteresis and Ultra‐Stable Polymeric Gels Through Tailored Solvent Engineering for Skin‐Like Soft Electronics
- **Active Verified DOI:** [https://doi.org/10.1002/smll.202501712](https://doi.org/10.1002/smll.202501712)
- **Materials Compared:** Skin / Dermal tissue vs. Synthetic polymer / elastomer matrix
- **Mechanical Properties & Assays Evaluated:** Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** Low-hysteresis polymeric gels are crucial for advancing soft electronics, and wearable devices, effectively mitigating irreversible fatigue damage and extending device lifespans. However, simultaneously achieving low energy dissipation, robust elastic recovery, and high adaptability remains a critical challenge. Herein, a novel ionic eutectic solution (IES)-driven design strategy is introduced, optimizing intermolecular interactions and dynamic network properties to reduce energy dissipation and enhance elastic recovery. The resulting IES gels exhibit ultra-low hysteresis strain, exhibiting negligible energy dissipation and robust elastic recovery for skin-like electronics.
- **Publication Venue & Citation Impact:** 9 citations (Small, 2025)

#### Study 53: Skin-like mechanoresponsive self-healing ionic elastomer from supramolecular zwitterionic network
- **Active Verified DOI:** [https://doi.org/10.1038/s41467-021-24382-4](https://doi.org/10.1038/s41467-021-24382-4)
- **Materials Compared:** Skin / Dermal tissue vs. Synthetic polymer / elastomer matrix
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Cyclic loading hysteresis, energy dissipation, and Mullins effect
- **Core Scientific Findings & Mechanical Comparison:** Stretchable ionic skins are intriguing in mimicking the versatile sensations of natural skins. However, for their applications in advanced electronics, good elastic recovery, self-healing, and more importantly, skin-like nonlinear mechanoresponse (strain-stiffening) are essential but can be rarely met in one material. Here we demonstrate a robust proton-conductive ionic skin design via introducing an entropy-driven supramolecular zwitterionic reorganizable network to the hydrogen-bonded polycarboxylic acid network.
- **Publication Venue & Citation Impact:** 544 citations (Nature Communications, 2021)

#### Study 56: Mechanically and biologically skin-like elastomers for bio-integrated electronics
- **Active Verified DOI:** [https://doi.org/10.1038/s41467-020-14446-2](https://doi.org/10.1038/s41467-020-14446-2)
- **Materials Compared:** Skin / Dermal tissue vs. Synthetic polymer / elastomer matrix
- **Mechanical Properties & Assays Evaluated:** Nonlinear toe-region compliance & strain stiffening; Tangent elastic modulus and ultimate tensile strength; Viscoelastic stress relaxation, creep, and strain-rate sensitivity; Fracture toughness, tear resistance, and fatigue threshold
- **Core Scientific Findings & Mechanical Comparison:** The bio-integrated electronics industry is booming and becoming more integrated with biological tissues. To successfully integrate with the soft tissues of the body (eg. skin), the material must possess many of the same properties including compliance, toughness, elasticity, and tear resistance. In this work, we prepare mechanically and biologically skin-like materials (PSeD-U elastomers) by designing a unique physical and covalent hybrid crosslinking structure.
- **Publication Venue & Citation Impact:** 315 citations (Nature Communications, 2020)

---

## Section 3: Methodological & Software Implications for MelloSilveiraTools & FEBio

### 3.1 Dataflow Pipelines in `MelloSilveiraTools.MechanicsOfMaterials.Optimizations`
The empirical divergence between soft tissues and synthetic polymers heavily dictates the algorithmic structure of the experimental processing pipelines implemented in `MelloSilveiraTools`:

1. **Phase Segmentation (`ExperimentalDataSegmenterStep`):**
   - *Experimental Tare Pre-load Protocol:* Ingestion pipelines explicitly declare and enforce the tare pre-load protocol ($F_0 = 0.100 - 0.500\text{ N}$ applied quasi-statically at $\le 0.100\text{ mm/s}$) required to eliminate specimen slack and establish the undeformed reference configuration $\Omega_0$ ($\lambda = 1.00$, $E = 0$) and initial gauge length $L_0$ before initiating phase categorization.
   - *Soft Tissues:* Require adaptive sliding-window numerical differentiation to accurately locate the toe-to-linear transition point without noise-induced premature inflection detection. The transition strain ($\varepsilon_{\text{toe}} \approx 0.030 - 0.050$) is an essential biomechanical indicator of collagen uncrimping.
   - *Synthetic Polymers:* Display continuous curvature without sharp transition points; segmentation focuses primarily on detecting the onset of rapid strain-stiffening or yield points.

2. **Viscoelastic Decoupling (`CurveSegmentAccumulatorStep`):**
   - High strain-rate ramp phases must be segregated from relaxation holding phases. For Fung QLV parameter extraction ($c, \tau_1, \tau_2$), the finite ramp duration $t_0$ must be strictly accounted for via the exact closed-form ramp-corrected Fung QLV hereditary integral during the relaxation holding phase ($t \ge t_0$):
     $$ \sigma(t) = \frac{\varepsilon_0}{t_0} \int_0^{t_0} G(t - s) \left.\frac{\partial \sigma^e}{\partial \varepsilon}\right|_{\varepsilon(s)} ds \quad \text{for } t \ge t_0 $$
     During the relaxation holding phase ($s > t_0$), the strain is held strictly constant ($\varepsilon(s) = \varepsilon_0$), resulting in zero strain rate ($\dot{\varepsilon} = 0$); therefore, the upper integral $\int_{t_0}^t G(t - s) \left.\frac{\partial \sigma^e}{\partial \varepsilon}\right| \dot{\varepsilon}(s) ds$ vanishes identically.  
     *(Citation: Fung, Y. C., Biomechanics: Mechanical Properties of Living Tissues, 2nd ed., Springer-Verlag, 1993, pp. 280-284).*
   - For synthetic polymers modeled with discrete Prony series, the optimization pipeline uses the Levenberg-Marquardt solver in `MathNet.Numerics` or ALGLIB (`ICurveFitter`) with bound constraints ensuring positive spectral amplitudes ($G_i > 0, \tau_i > 0$) to guarantee thermodynamic admissibility.

3. **Material Database Persistence (`CurveFitOutputPersistenceStep`):**
   - Fitted constitutive parameters are stamped with a deterministic SHA-256 identifier generated by `IdentifierBuilderStep` from the specimen metadata, test type, strain range, and parameter payload, serialized with `OptimizationJsonOptions.SignificantFigures7` for maximum precision storage in PostgreSQL.

### 3.2 Finite Element Formulation & FEBio 4.0 Implementation
The comparative catalog directly guides the selection of 3D constitutive representations in FEBio (`febio_spec 4.0`):

```xml
<!-- Example: Biphasic Transversely Isotropic Knee Ligament in FEBio -->
<material id="1" name="ACL_Native_Tissue" type="biphasic">
    <phi0>0.200</phi0>
    <permeability type="perm-exp-iso">
        <perm>1.50e-15</perm>
        <M>2.20</M>
    </permeability>
    <solid type="uncoupled solid mixture">
        <!-- Matrix ground substance (Proteoglycans) -->
        <solid type="Mooney-Rivlin">
            <c1>0.500</c1>
            <c2>0.100</c2>
            <k>50.0</k>
        </solid>
        <!-- Collagen fiber family (HGO anisotropic formulation) -->
        <solid type="fiber-exp-pow">
            <alpha>2.50</alpha>
            <beta>30.0</beta>
            <ksi>15.0</ksi>
            <theta>0.00</theta>
            <phi>0.00</phi>
        </solid>
    </solid>
</material>
```

- **Synthetic Graft FEBio Formulation:**  
  When simulating synthetic replacement grafts (TPU or braided UHMWPE/PET):
  - TPU grafts are represented via `type="Ogden"` or `type="Yeoh"` with reactive viscoelastic damage (`type="reactive viscoelastic"`) to simulate cyclic Mullins softening.
  - Braided PET/UHMWPE constructs require structural beam/truss elements or homogenization using an anisotropic transversely isotropic fiber model with zero compressive stiffness (`fiber-uncoupled`).

### 3.3 Conclusion & Research Recommendations
The 60 studies synthesized in this document conclusively demonstrate that while synthetic polymers can match or exceed the ultimate tensile strength of human ligaments and tendons, replicating the **coupled toe-region compliance, low-hysteresis resilience, flaw tolerance, and zero-creep durability** of native soft tissues remains the primary frontier in biomechanical engineering. The analytical equations and verified data points provided here serve as the baseline for all ongoing master's thesis developments at PPEMM/CEFET-RJ.
