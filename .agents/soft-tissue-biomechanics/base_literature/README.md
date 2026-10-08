# 📚 Base Literature (Progressive PDF Parsing)

Welcome! This folder is the core of our **Progressive Reading and Healing System for Academic Books**. 
The goal of this structure is to allow dense and heavy PDF books (containing complex mathematical equations) to be converted into lightweight text formats (Markdown) so our Artificial Intelligence can read them without "hallucinations" or parsing errors.

## 📂 Folder Structure:

* **/markdown_books**: Books converted to .md format (plain text). This is your digital "bedside book". You can open it, read it, and when you find a broken formula, request a targeted "healing" for that page.
* **/automation_scripts**: Contains the Python tools the AI agents use to read PDFs, slice pages, and automate the heavy lifting. This is the engine room.
* **/temp_images**: Whenever you request a targeted healing for a specific equation, we take a "snapshot" of that specific PDF page and store it here temporarily for the Vision AI to process.

**Summary:** The professor or domain expert analyzes the files in markdown_books, and the machine handles the heavy lifting in the background!
