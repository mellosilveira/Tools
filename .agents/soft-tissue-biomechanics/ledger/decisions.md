# Decisions and Scientific Traceability Ledger

## Execution: Smoke Test Validation
- **Date**: 2026-10-06T01:00:06Z
- **Task IDs**: `smoke-test-task-1`, `smoke-test`
- **What**: Successful execution of the Biomechanics Research ecosystem smoke test using Docker.
- **From where**: Executed by `senior-backend-software-engineer-net` (`docker-compose`) and `experimental-data-analyst` (`docker run`).
- **Why**: To validate the infrastructure setup, ensuring isolated and reproducible execution via Docker, thereby complying with the absolute prohibition of non-containerized (local) installations and tool executions.
- **Category**: Execution

### 2026-10-06 Task: baseline-style-guide-and-previous-cycle
**Category**: Decision & Execution

#### Style Guide (Decision)
- **What**: Established standard Style Guide for the research ecosystem.
  - **Tone**: Academic, impersonal, fluid, rigorous, and with impeccable logical linking.
  - **Methodological Approach**: Emphasizes strict geometric and kinematic boundary conditions. Acknowledges limitations openly.
  - **Mathematical Exposition**: Defines all variables immediately after equations. Addresses singularities explicitly. Moves heavy deductions to appendices.
  - **Terminology**: Employs rigorous continuum mechanics and viscoelastic terminology.
  - **Formatting**: Uses `$...$` and `$$...$$`. Limits significant figures in descriptive text.
- **From where**: Extracted by the Academic Writer from the user's baseline paper `Texto base para revista_Schapery.docx`.
- **Why**: To ensure all future research outputs and written texts strictly respect this foundation and maintain high editorial and scientific standards.

#### Previous Cycle Baseline (Execution)
- **What**: Registered the achievements and findings of the previous cycle.
  - **Methodology**: Developed a 1D non-linear viscoelastic load-sharing framework for porcine knee ligaments based on Schapery’s theory. Evaluated 81 combinatorial joint scenarios under a 75 N step-like vertical distraction, comparing 0% and 3% initial pre-strain.
  - **Results**: Demonstrated viscoelasticity reduces joint load capacity over time. 0% pre-strain dropped to 43.07 N; 3% pre-strain mitigated decay to 60.48 N. MCL experienced most relative force reduction.
- **From where**: Extracted by the Academic Writer from the user's baseline paper `Texto base para revista_Schapery.docx`.
- **Why**: To serve as the state-of-the-art reference and methodological starting point for the next iterations of the study.

### 2026-10-06 Task: methodological-directive-evidence-funnel
**Category**: Decision (Methodological Directive for Deep Research)

#### Evidence Funnel Approach
- **What**: Established the "Evidence Funnel" approach for theoretical research and literature reviews. The process strictly follows:
  1. **Broad search**: Identify which constitutive models the state-of-the-art uses to predict soft tissue mechanical behavior in general.
  2. **Narrowing**: Focus specifically on knee ligaments.
  3. **Computational Analysis**: Investigate which software and computational technologies are being used for these simulations.
  4. **Specific validation**: Look at how specific target tools (e.g., FEBio) fit into that landscape.
- **From where**: User directive (received 2026-10-06).
- **Why**: To avoid confirmation bias in literature reviews (i.e., preventing searches that only target pre-chosen tools or models).

### 2026-10-06 Task: theoretical-advance-3d-continuum-febio
**Category**: Decision & Future Work

#### Transition to 3D Continuum Mechanics (Decision)
- **What**: Expanding the 1D baseline modeling into a 3D Tensorial Continuum Mechanics framework.
- **From where**: Biomechanics Co-advisor.
- **Why**: 1D models are insufficient for capturing 3D behavior. 3D tensorial generalizations provide the necessary rigor.

#### Action Plan for Theoretical Advance
- **What**: Formulate the 3D tensorial generalization of the existing 1D models (defining Helmholtz free energy and Cauchy stress tensor) to run in a 3D open-source FEA software.
- **From where**: Biomechanics Co-advisor.
- **Why**: To bridge the gap between 1D simplifications and full 3D continuum mechanics required by advanced models.

#### Mandatory Human-in-the-Loop QA Gate for Automated Parsing
- **What**: For any automated text generation or parsing loop (e.g., Vision LaTeX extraction), the system is FORBIDDEN from writing directly to the final markdown file. 
  1. The agent must process only a small batch (short loop).
  2. The output must be saved to a temporary file (e.g., draft.md).
  3. The system MUST halt execution and explicitly prompt the human user to validate the temporary file.
  4. Only after the user expressly approves the content will the changes be migrated and merged into the final target file.
- **From where**: User directive after observing LLM loop fatigue and lazy scripting behaviors.
- **Why**: To prevent data corruption, AI hallucinations, "lazy shortcut" scripting, and unnecessary token burn. Ensures absolute human control over the final repository state.

### 2026-10-06 Task: safeguard-anti-hallucination-guardrails
**Category**: Decision (System Safeguards against LLM Fatigue & Shortcuts)

#### The 4 Pillars of Bulk Processing Defense
To ensure data integrity and prevent runaway token consumption during automated text/math parsing, the ecosystem is bound by these 4 interconnected guardrails:
1. **Architectural Guardrail (Short Loops & Mandatory Validation)**: Massive batching is strictly forbidden. 
2. **Deterministic QA Gate (Draft Verification Script)**: A deterministic Python script will inspect drafts for valid mathematical syntax before presenting to humans.
3. **Anti-Laziness Prompt Clause**: Vision and Parsing Workers are injected with a strict cognitive penalty clause prohibiting blind regex wrapping.
4. **Token Budgeting & Circuit Breaker**: Orchestrator terminates agents bypassing the "short loop" rule.
- **From where**: User directive following the failure of blind bulk processing in Chapter 7.
- **Why**: To lock the system into a deterministic workflow that prevents AI hallucinations.

## [2026-10-07] - Deadline Restructuring and Focus on Uniaxial Tension
**Category:** Decision (Scope Reduction)
- **What:** Focus exclusively on isolated uniaxial loading for the 4 porcine knee ligaments.
- **From where:** Researcher / Human directive.
- **Why:** Full-knee 3D simulations with complex articular loading present a high risk of non-convergence in a 12-month timeframe. Complex kinematics are moved to post-thesis future works.

## [2026-10-07] - The 5 Guiding Constitutive Models
**Category:** Decision (Methodological Baseline)
- **What:** Selected 5 models to guide the initial research development:
  1. *Fung QLV* (Historical gold-standard)
  2. *Schapery Tensorial* (Mathematical advance)
  3. *Generalized Maxwell / Prony Series* (Industry standard)
  4. *Weiss / Transversely Isotropic* (Hyperelastic anatomical baseline)
  5. *Poroviscoelasticity* (Fluid-solid interaction)
- **From where:** Prior preliminary analyses by the research group.
- **Why:** To serve as a guiding hypothesis for the computational development. *Note: These are not set in stone and may oscillate or be altered as new information emerges from the evidence funnel.*

## [2026-10-07] - Computational Architecture Pivot (Backend/Frontend and C# Engine)
**Category:** Decision (Software Architecture)
- **What:** The C# engine (`MelloSilveiraTools`) will be restricted to scalar (1D) curve fitting and data orchestration. 3D tensorial simulations will be delegated to open-source FEA software (priority: FEBio). The architecture will orchestrate 3 data sources: experimental, 1D C#, and 3D FEBio.
- **From where:** Researcher / Human directive.
- **Why:** To automate and accelerate research. Expanding C# to 3D tensors (meshes) would consume critical time, missing the 12-month deadline. C# serves best as an orchestrator and 1D baseline, leveraging the researcher's existing fluency.

## [2026-10-07] - FEBio as the Primary 3D FEA Tool
**Category:** Decision (Tooling)
- **What:** Preferential selection of FEBio over other software.
- **From where:** Prior preliminary analyses.
- **Why:** FEBio is free, open-source, and specifically tailored for biomechanics (supporting soft tissues natively). *Note: Like the models, this is a guiding hypothesis that can change if future evidence requires.*

## [2026-10-07] - Strategy for Missing Native FEA Models
**Category:** Decision (Computational Implementation)
- **What:** Any constitutive model lacking native support in the chosen 3D FEA software will be custom-implemented.
- **From where:** Researcher / Human directive.
- **Why:** To ensure all 5 hypothesized models can be compared. The ecosystem will compile User-Defined Material (UDM) plugins (e.g., in C++) to inject the custom 3D tensorial math into the solver.

## Ledger Entry: 2026-10-08

### Decision & Execution
* **Task_ID:** task-1.1
* **Date:** 2026-10-08
* **What:** Selection of primary constitutive models for soft tissue mechanics, specifically hyperelastic and viscoelastic models (Fung QLV, Mooney-Rivlin, Neo-Hookean, Ogden, Holzapfel-Gasser-Ogden).
* **From where:** senior-applied-biomechanics-researcher-deep-research (Evidence includes DOIs: 10.1115/1.2834307, 10.1098/rsif.2015.0486, 10.1111/j.1475-1305.2006.00257.x, etc.)
* **Why:** These models are chosen because they map well to the hierarchical structure of soft tissues like ligaments and tendons, representing large deformations, hysteresis, non-linear, time-dependent, and anisotropic behaviors.

### Decision & Execution
* **Task_ID:** task-1.2
* **Date:** 2026-10-08
* **What:** Application of Hyperelastic and viscoelastic constitutive models for knee ligaments.
* **From where:** senior-applied-biomechanics-researcher-deep-research (Evidence includes DOIs: 10.1038/s41598-018-20739-w, 10.3389/fbioe.2014.00054, 10.1016/s0736-0266(03)00113-x, etc.)
* **Why:** They correctly model the inherent nonlinearity, anisotropy (due to collagen bundles), and time-dependent behaviors (such as creep, hysteresis, and stress relaxation) under physiological and supra-physiological loads.

### Decision & Execution
* **Task_ID:** task-1.3
* **Date:** 2026-10-08
* **What:** Selection of FEBio as the core finite element solver instead of general-purpose solvers like Abaqus and Ansys.
* **From where:** senior-applied-biomechanics-researcher-deep-research (Evidence includes DOIs: 10.1007/s10439-022-03074-0, 10.1111/os.13980, etc.)
* **Why:** FEBio natively handles complex biphasic, multiphasic, and poroelastic materials essential for hydrated soft tissues. As open-source, it allows complete transparency, rigorous validation, and custom plugins for new constitutive models without licensing constraints.

### Opportunity/Gap & Future Work
* **Task_ID:** task-1.1, task-1.2, task-1.3
* **Date:** 2026-10-08
* **What:** Implementation of FEBio UDM (User Defined Material) and converting 1D constants to 3D models.
* **From where:** Traceability QA identification / human input logic based on limitations of standard models in 3D settings.
* **Why:** To fully leverage FEBio's open-source architecture by implementing custom plugins for specific constitutive models, moving beyond simple 1D constants to accurately model 3D biomechanical behaviors.

### Execution & Decision
* **Task_ID:** task-2.1
* **Date:** 2026-10-08
* **What:** Validated and maintained Fung QLV 1D model objective function logic using inline lambdas in FungRelaxationOnlyCurveFitterStep.cs.
* **From where:** senior-backend-software-engineer-net (File: C:\Pessoal\Projetos\Tools\.agents\soft-tissue-biomechanics\outputs\task-2.1-result.json)
* **Why:** To strictly adhere to the "Do Not Reinvent the Wheel" directive and maintain architectural consistency across the MelloSilveiraTools ecosystem. Avoided creating redundant custom ObjectiveFunction classes, ensuring a unified approach to curve-fitting constraints.
### 2026-10-08 Task: roadmap-revision-phase1-findings
**Category**: Decision & Future Work

#### Inclusion of Holzapfel-Gasser-Ogden (HGO) Model
- **What**: Added the HGO structural model to the thesis scope (Tasks 1.9, 2.6, 4.2).
- **From where**: Outputs of deep research Tasks 1.1 and 1.2.
- **Why**: The literature strongly supports that soft tissues (especially knee ligaments) require anisotropic and structural constitutive models to account for collagen fiber dispersion in a ground matrix. Purely phenomenological polynomial models (Mooney-Rivlin, Neo-Hookean) were deemed insufficient, whereas HGO is a direct biological fit and is highly supported by modern FEA solvers like FEBio.

#### Validation of FEBio and Poroviscoelasticity
- **What**: Confirmed the selection of FEBio and the inclusion of Poroviscoelasticity in the roadmap.
- **From where**: Outputs of deep research Task 1.3.
- **Why**: FEBio natively supports complex biphasic and poroelastic materials critical for hydrated tissues. Its open-source nature allows for writing custom User-Defined Materials (UDMs) in C++ without licensing constraints, validating Task 3.4 of the roadmap.

