import os
import sys
import docx
import re
import zipfile
import lxml.etree as ET

sys.stdout.reconfigure(encoding='utf-8')

DOCX_PATH = r"D:\Mello Silveira Serviços LTDA\Projetos\Tools\.agents\soft-tissue-biomechanics\thesis\Consolidated_Biomechanical_Formulations.docx"

if not os.path.exists(DOCX_PATH):
    print("ERROR: Document does not exist!")
    sys.exit(1)

doc = docx.Document(DOCX_PATH)

print(f"=== AUDITING DOCUMENT: {os.path.basename(DOCX_PATH)} ===")
print(f"File size: {os.path.getsize(DOCX_PATH)} bytes")
print(f"Paragraphs count: {len(doc.paragraphs)}")
print(f"Tables count: {len(doc.tables)}")

# 1. Audit Equation Numbers
eq_numbers = []
for t in doc.tables:
    if len(t.rows) == 1 and len(t.columns) == 2:
        cell_1 = t.cell(0, 1)
        text = cell_1.text.strip()
        m = re.match(r'\((\d+)\)', text)
        if m:
            eq_numbers.append(int(m.group(1)))

print(f"\n--- Equation Numbering Audit ---")
print(f"Total numbered equations found: {len(eq_numbers)}")
if eq_numbers:
    print(f"Range: ({eq_numbers[0]}) to ({eq_numbers[-1]})")
    is_sequential = eq_numbers == list(range(1, len(eq_numbers) + 1))
    print(f"Strict sequential numbering 1..{len(eq_numbers)}: {is_sequential}")
    if not is_sequential:
        missing = set(range(1, len(eq_numbers) + 1)) - set(eq_numbers)
        print(f"MISSING EQUATION NUMBERS: {missing}")
else:
    print("WARNING: No equations detected!")

# 2. Audit Headings
headings = [p.text for p in doc.paragraphs if p.style.name.startswith('Heading')]
print(f"\n--- Headings Audit (Found {len(headings)}) ---")
for h in headings[:25]:
    print(f"  {h}")
if len(headings) > 25:
    print(f"  ... and {len(headings) - 25} more headings")

# 3. Check for Leaked LaTeX
leaked_latex = []
for i, p in enumerate(doc.paragraphs):
    if any(k in p.text for k in [r'\frac', r'\begin{bmatrix}', r'\det(', r'\lambda(t) =', r'\mathbf{F}']):
        leaked_latex.append((i, p.text[:80]))

print(f"\n--- Leaked LaTeX Audit ---")
print(f"Leaked raw LaTeX occurrences in paragraphs: {len(leaked_latex)}")
for idx, txt in leaked_latex:
    print(f"  P[{idx}]: {txt}")

# 4. Check Colors (Preto Automático)
with zipfile.ZipFile(DOCX_PATH) as z:
    xml_content = z.read('word/document.xml')
    root = ET.fromstring(xml_content)

w_ns = '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}'
color_elements = root.findall(f'.//{w_ns}color')
non_auto_colors = []
for c in color_elements:
    val = c.attrib.get(f'{w_ns}val')
    if val != 'auto' and val != '000000':
        non_auto_colors.append(val)

print(f"\n--- Font Color Audit ---")
print(f"Total <w:color> tags: {len(color_elements)}")
print(f"Non-automatic / non-black color tags: {len(non_auto_colors)}")
if non_auto_colors:
    print(f"  Distinct non-auto colors found: {set(non_auto_colors)}")
else:
    print("  SUCCESS: 100% Preto Automático confirmed across all elements!")

# 5. Check OMML Math Elements
m_ns = '{http://schemas.openxmlformats.org/officeDocument/2006/math}'
omml_elements = root.findall(f'.//{m_ns}oMath')
omml_paras = root.findall(f'.//{m_ns}oMathPara')
print(f"\n--- OMML Math Elements Audit ---")
print(f"Total oMath elements: {len(omml_elements)}")
print(f"Total oMathPara elements: {len(omml_paras)}")

# 6. Check Specific Chapter 1 Topics
print(f"\n--- Chapter 1 Specific Topics Audit ---")
doc_text = "\n".join(p.text for p in doc.paragraphs)
topics_to_check = [
    "1.1 Coordinate Systems and Kinematics of Motion",
    "1.2 The Deformation Gradient Tensor and Fundamental Mapping",
    "1.3 Material and Spatial Displacement Fields and Their Gradients",
    "1.4 Length Elements and Cauchy-Green Deformation Tensors",
    "1.5 Finite Strain Tensors: Green-Lagrange and Euler-Almansi Formulations",
    "1.6 Physical Interpretation and Experimental Synthesis of Strain Tensors from Uniaxial Tensile Data",
    "1.7 Asymptotic Comparison: Finite Strain vs. Infinitesimal Strain",
    "1.8 Volume Transformation and Cross-Sectional Area Scaling under Isochoric Deformation",
    "1.9 Kinematic Rates: Spatial Velocity Gradient and Rate of Deformation Tensor",
    "1.10 Traction, Cauchy Stress Principle, and Quasi-Static Equilibrium",
    "1.11 Conjugated Stress Tensors and Internal Mechanical Work",
    "1.12 Mathematical Operators and Tensor Calculus Guide",
    "1.13 Continuum Mechanics of Hyperelasticity in Soft Tissues",
    "1.14 Continuum Mechanics of Viscoelasticity in Soft Tissues",
    "1.15 Continuum Mechanics of Viscohyperelasticity (Finite-Strain Viscoelasticity)",
    "1.16 Continuum Mechanics of Viscoporoelasticity and Biphasic Mixture Theory",
    "1.17 Isochoric Uniaxial Reduction for Incompressible Biological Soft Tissues"
]
for top in topics_to_check:
    present = top in doc_text
    status = "FOUND" if present else "MISSING"
    print(f"  [{status}] {top}")

# 7. Check Literature in 1.8 and Bibliography
print(f"\n--- Literature Citations Audit ---")
all_text = doc_text + "\n" + "\n".join(c.text for t in doc.tables for row in t.rows for c in row.cells)
refs_to_check = [
    "Flory",
    "Ogden",
    "Humphrey",
    "Weiss & Gardiner"
]
for ref in refs_to_check:
    in_text = ref in all_text
    print(f"  Ref '{ref}': in document (paragraphs + tables) = {in_text}")

# 8. Check Single File Rule
thesis_dir = os.path.dirname(DOCX_PATH)
docx_files = [f for f in os.listdir(thesis_dir) if f.endswith('.docx')]
print(f"\n--- Single File Rule Audit ---")
print(f"Files in thesis directory: {docx_files}")
if len(docx_files) == 1 and docx_files[0] == "Consolidated_Biomechanical_Formulations.docx":
    print("SUCCESS: Exactly one consolidated document exists in thesis/!")
else:
    print(f"WARNING: Multiple files found in thesis/: {docx_files}")
