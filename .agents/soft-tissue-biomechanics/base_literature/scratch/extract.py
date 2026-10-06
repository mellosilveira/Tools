import fitz
import sys

def extract_pages(input_pdf, output_pdf, start_page, end_page):
    doc = fitz.open(input_pdf)
    doc2 = fitz.open()
    doc2.insert_pdf(doc, from_page=start_page-1, to_page=end_page-1)
    doc2.save(output_pdf)
    doc2.close()
    doc.close()

if __name__ == '__main__':
    extract_pages(sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]))
