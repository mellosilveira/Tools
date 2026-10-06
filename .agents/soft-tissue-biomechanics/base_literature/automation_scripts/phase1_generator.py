# Phase 1 Script: Markdown Skeleton Generation from PDF
# Author: Antigravity Agent
# Description: Extracts raw text from a PDF using PyMuPDF, replacing problematic blocks with safe tags.

import fitz  # PyMuPDF
import sys
import os

def extract_pdf_to_markdown(pdf_path, output_md_path):
    try:
        doc = fitz.open(pdf_path)
        with open(output_md_path, 'w', encoding='utf-8') as f:
            f.write(f"# Base Document: {os.path.basename(pdf_path)}\n\n")
            
            for page_num in range(len(doc)):
                page = doc.load_page(page_num)
                text = page.get_text("text")
                images = page.get_images(full=True)
                
                f.write(f"\n\n---\n## [PAGE {page_num + 1}]\n---\n\n")
                
                if images:
                    f.write(f"> ⚠️ **Note:** Detected {len(images)} images/charts on this page. If necessary, request targeted healing (Phase 3).\n\n")
                
                if text.strip():
                    f.write(text)
                else:
                    f.write("> [BLANK PAGE OR IMAGES ONLY]")
                    
        print(f"Success! Skeleton generated at: {output_md_path}")
    except Exception as e:
        print(f"Error processing PDF: {e}")

if __name__ == '__main__':
    if len(sys.argv) < 3:
        print("Usage: python phase1_generator.py <pdf_path> <output_md_path>")
    else:
        extract_pdf_to_markdown(sys.argv[1], sys.argv[2])
