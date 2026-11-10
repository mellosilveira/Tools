# 📚 Base Literature (Progressive PDF Parsing & Constitutive Theory)

Welcome! This folder is the core repository for **Academic Literature, Progressive PDF Parsing, and Constitutive Theory Specifications** for soft-tissue biomechanics research at PPEMM/CEFET-RJ.

## 📂 Folder Structure:

* **[`comparative_mechanics_soft_tissues_vs_polymers.md`](./comparative_mechanics_soft_tissues_vs_polymers.md)**: Exhaustive comparative mechanical framework between native soft biological tissues (knee ligaments ACL/PCL/MCL/LCL, tendons, cartilage) and synthetic polymeric biomaterials (TPU, hydrogels, silicone elastomers, UHMWPE/PET fibers). Analyzes 5 key mechanical domains: (1) J-shaped toe-region crimp mechanics vs. entropic elasticity, (2) Fung QLV continuous relaxation spectrum vs. discrete Prony/fractional models and creep compliance, (3) Cyclic preconditioning vs. elastomer Mullins stress-softening, (4) Multiscale biological crack-tip blunting vs. the Lake-Thomas fracture limit, and (5) 3D anisotropic continuum models (HGO, Fung) vs. isotropic hyperelasticity (Ogden, Mooney-Rivlin, Yeoh). Includes explicit mathematical formulations with page citations and a catalog of 60 CrossRef-verified peer-reviewed studies.
* **/markdown_books**: Books converted to .md format (plain text). This is your digital "bedside book". You can open it, read it, and when you find a broken formula, request a targeted "healing" for that page.
* **/theory_cheat_sheets**: Rigorous mathematical and constitutive theory cheat sheets with explicit formulations, parameter definitions, and 100% CrossRef-verified DOIs for research and software implementation.
  * [`fung_qlv_3d.md`](./theory_cheat_sheets/fung_qlv_3d.md): 3D continuum and 1D uniaxial Fung QLV formulations, reduced relaxation function $G(t)$, and implementation details in `MelloSilveiraTools`.
  * [`sensitivity_analysis_and_continuation.md`](./theory_cheat_sheets/sensitivity_analysis_and_continuation.md): Parametric sensitivity gradient vectors ($\nabla_{\mathbf{p}} \sigma$), normalized elasticity indices, Euler-Newton predictor-corrector for iso-stress continuation, and CrossRef-verified bibliographic ledger.
* **/automation_scripts**: Contains the Python tools the AI agents use to read PDFs, slice pages, and automate the heavy lifting. This is the engine room.
* **/temp_images**: Whenever you request a targeted healing for a specific equation, we take a "snapshot" of that specific PDF page and store it here temporarily for the Vision AI to process.

**Summary:** The professor or domain expert analyzes the files in markdown_books, theory_cheat_sheets, and comparative literature, and the machine handles the heavy lifting in the background!
