# Analysis of Constitutive Models for Knee Ligaments
*Ranking based on literature prevalence and Finite Element Analysis (FEA) solver integration*

Below is the synthesis of findings from the Bibliographic Evidence Funnel, categorizing and ranking constitutive models specifically applied to knee ligaments.

## 1. Hyperelastic + Generalized Maxwell Family (Prony Series)
**Literature Prevalence:** Very High (Industry Standard)
**Reason for Dominance:** Native ("Built-in") viscoelastic formulation across Abaqus, ANSYS, and FEBio. Pairs a hyperelastic strain-energy potential for instantaneous elastic response with a Prony series to compute stress relaxation via Internal State Variables.
- **Sub-models included:**
  - **Mooney-Rivlin / Neo-Hookean:** Used for more isotropic tissues or isotropic base matrix.
  - **Holzapfel-Gasser-Ogden (HGO):** Widely adopted because it captures angular dispersion ($\kappa$) of collagen fibers, becoming the modern gold standard for anisotropic hyperelasticity.

## 2. Weiss Transversely Isotropic Hyperelastic Model
**Literature Prevalence:** Very High (Especially in FEBio)
**Reason for Dominance:** Designed specifically for ligaments. Mathematically splits ligament strain energy into an isotropic ground matrix contribution and tension-only collagen fibers. Forms the basis of a large share of Anterior Cruciate Ligament (ACL) simulations in FEBio.

## 3. Fung Quasi-Linear Viscoelasticity (QLV)
**Literature Prevalence:** High (Historical Gold Standard)
**Reason for Dominance:** Introduced by Y. C. Fung, QLV assumes multiplicative separability between non-linear instantaneous elastic stress and normalized continuous viscous relaxation. Remains the most extensively validated model in *ex vivo* uniaxial tests over recent decades. Somewhat less common in 3D FEA due to the history-integral cost compared to Prony series, while retaining foundational analytical significance.

## 4. Poroelastic / Poroviscoelastic Models (Biphasic)
**Literature Prevalence:** Medium for Ligaments (Very High for Cartilage/Meniscus)
**Reason for Dominance:** Models soft tissue as a permeable porous solid matrix saturated with interstitial fluid (water), where apparent viscoelasticity emerges from fluid drag under compression or complex multi-axial loading. Common in full-knee contact FEA, though computationally expensive due to coupled solid-fluid solvers.

## 5. Schapery Non-Linear Integral Viscoelasticity
**Literature Prevalence:** Low / Emerging in 3D Biomechanics
**Reason for Dominance:** Originated in polymer rheology and aerospace composites. Provides strong thermodynamic rigor and strain-dependent non-linearity (`h_e`, `h_1`, `h_2`, `a_sigma`) beyond Fung QLV, though 3D finite-element implementations often produce asymmetric tangent Jacobians. Widely tractable in 1D uniaxial testing; extending Schapery into 3D FEBio (Task 1.5 / Task 3.4) represents a key scientific differentiator in the thesis.
