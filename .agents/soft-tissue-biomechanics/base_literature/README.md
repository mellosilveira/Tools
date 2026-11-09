# 📚 Base Literature (Progressive PDF Parsing & Constitutive Theory)

Welcome! This folder is the core repository for **Academic Literature, Progressive PDF Parsing, and Constitutive Theory Specifications** for soft-tissue biomechanics research at PPEMM/CEFET-RJ.

## 📂 Folder Structure:

* **/markdown_books**: Books converted to .md format (plain text). This is your digital "bedside book". You can open it, read it, and when you find a broken formula, request a targeted "healing" for that page.
* **/theory_cheat_sheets**: Rigorous mathematical and constitutive theory cheat sheets with explicit formulations, parameter definitions, and 100% CrossRef-verified DOIs for research and software implementation.
  * [`fung_qlv_3d.md`](./theory_cheat_sheets/fung_qlv_3d.md): 3D continuum and 1D uniaxial Fung QLV formulations, reduced relaxation function $G(t)$, and implementation details in `MelloSilveiraTools`.
  * [`sensitivity_analysis_and_continuation.md`](./theory_cheat_sheets/sensitivity_analysis_and_continuation.md): Parametric sensitivity gradient vectors ($\nabla_{\mathbf{p}} \sigma$), normalized elasticity indices, Euler-Newton predictor-corrector for iso-stress continuation, and CrossRef-verified bibliographic ledger.
* **/automation_scripts**: Contains the Python tools the AI agents use to read PDFs, slice pages, and automate the heavy lifting. This is the engine room.
* **/temp_images**: Whenever you request a targeted healing for a specific equation, we take a "snapshot" of that specific PDF page and store it here temporarily for the Vision AI to process.

**Summary:** The professor or domain expert analyzes the files in markdown_books and theory_cheat_sheets, and the machine handles the heavy lifting in the background!
