"""
Generate Obsidian index.md from JustGoAPI_SRS.pdf Table of Contents.
Usage: python docs/generate_index.py
"""

import fitz  # PyMuPDF
import re
from pathlib import Path

PDF_PATH = Path(__file__).parent / "JustGoAPI_SRS.pdf"
OUT_PATH = Path(__file__).parent / "index.md"


def extract_toc_page(doc: fitz.Document) -> str:
    """Find and return the raw text of the Table of Contents page."""
    for page in doc:
        text = page.get_text()
        if "Table of Contents" in text:
            return text
    return ""


def parse_toc(raw: str) -> list[dict]:
    """
    Parse TOC entries from raw page text.
    Returns list of {level, title, number} dicts.
    """
    entries = []
    lines = raw.splitlines()
    in_toc = False

    for line in lines:
        line = line.strip()
        if line == "Table of Contents":
            in_toc = True
            continue
        if not in_toc or not line:
            continue

        # Match numbered sections like "1.", "1.1.", "1.1.1." followed by title
        m = re.match(r'^(\d+(?:\.\d+)*)\.?\s+(.+)$', line)
        if m:
            num = m.group(1)
            title = m.group(2).strip()
            depth = num.count('.') + 1  # "1" = 1, "1.1" = 2, etc.
            entries.append({"number": num, "title": title, "level": depth})

    return entries


def slugify(number: str, title: str) -> str:
    """Create Obsidian-friendly wikilink text."""
    slug = re.sub(r'[^\w\s-]', '', title).strip()
    slug = re.sub(r'\s+', '-', slug).lower()
    return f"{number}-{slug}"


def section_anchor(number: str, title: str) -> str:
    """Obsidian wikilink: [[filename#heading]]"""
    return f"[[SRS/{slugify(number, title)}|{number} {title}]]"


def build_markdown(entries: list[dict]) -> str:
    lines = [
        "# JustGo API SRS — Index",
        "",
        "> Source: `docs/JustGoAPI_SRS.pdf`  ",
        "> Version: 0.1 (First Draft — April 25, 2026)  ",
        "> Status: Reverse-engineered from codebase",
        "",
        "---",
        "",
        "## Table of Contents",
        "",
    ]

    for e in entries:
        indent = "  " * (e["level"] - 1)
        link = section_anchor(e["number"], e["title"])
        lines.append(f"{indent}- {link}")

    lines += [
        "",
        "---",
        "",
        "## Quick Reference",
        "",
        "| Section | Topic |",
        "|---------|-------|",
    ]

    # Top-level sections only for quick ref table
    for e in entries:
        if e["level"] == 1:
            lines.append(f"| {e['number']} | {e['title']} |")

    lines += [
        "",
        "---",
        "",
        "## Tags",
        "",
        "#srs #justgo #api #documentation",
        "",
    ]

    return "\n".join(lines)


def main() -> None:
    doc = fitz.open(str(PDF_PATH))
    raw_toc = extract_toc_page(doc)

    if not raw_toc:
        print("ERROR: Table of Contents page not found in PDF.")
        return

    entries = parse_toc(raw_toc)
    if not entries:
        print("ERROR: No TOC entries parsed. Check PDF text extraction.")
        return

    md = build_markdown(entries)
    OUT_PATH.write_text(md, encoding="utf-8")
    print(f"Written: {OUT_PATH} ({len(entries)} entries)")

    # Print preview
    print("\n--- Preview ---")
    print(md[:1200])


if __name__ == "__main__":
    main()
