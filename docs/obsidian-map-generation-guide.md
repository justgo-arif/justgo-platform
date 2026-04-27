# Obsidian Map Generation Guide

How to generate a navigable Obsidian vault from a structured markdown SRS document.

---

## Overview

The goal is a 3-level hierarchy of linked notes:

```
MOC.md  (Map of Content — entry point)
  └── 3.x-Section.md  (feature area)
        └── 3.x.y-Subsection.md  (topic group)
              └── FR-NNN.md  (leaf — one requirement per file)
```

Obsidian resolves `[[wikilinks]]` by filename. Every file links up to its parent and down to its children. The MOC is the single entry point for graph view and navigation.

---

## Prerequisites

- Python 3.x installed
- Source SRS as a single structured `.md` file
- Obsidian vault pointed at (or including) the output folder

---

## Step 1 — Structure Your Source Document

The generator parses the SRS by regex. Your source document must follow this pattern:

**Section heading** (level 2):
```markdown
### 3.1. User Management and Authentication
```

**Subsection heading** (level 3):
```markdown
#### 3.1.6. Multi-Factor Authentication
```

**Requirement table row**:
```markdown
| FR-42. | The system SHALL allow authorized users to ... |
```

Rules:
- FR IDs must be `FR-NN.` or `FR-NNN.` with a trailing period inside the table cell.
- Section numbers must be `3.x` and subsection numbers `3.x.y` (or `3.x.yy`).
- No FR ID may appear more than once.

---

## Step 2 — Define the Data Map

In your generator script, declare two data structures:

### `data_sections` dict

```python
data_sections = {
    "3.1": {
        "name": "User Management and Authentication",
        "scope": "one-line business scope description",
        "fr_range": "FR-01 to FR-80",
    },
    # ... one entry per section
}
```

### `subsections` list of tuples

```python
subsections = [
    # (section, subsection, display_name, first_fr, last_fr)
    ("3.1", "3.1.1", "Authorization",              "FR-01", "FR-04"),
    ("3.1", "3.1.2", "Accounts",                   "FR-05", "FR-16"),
    ("3.1", "3.1.6", "Multi-Factor Authentication", "FR-42", "FR-56"),
    # ... one entry per subsection
]
```

> The `first_fr` / `last_fr` range is **inclusive**. Single-requirement subsections use the same value for both (`"FR-63"`, `"FR-63"`).

---

## Step 3 — Parse FR Entries from the SRS

```python
import re

with open("path/to/srs.md", encoding="utf-8") as f:
    srs_text = f.read()

fr_pattern = re.compile(r'\|\s*FR-(\d+)\.\s*\|\s*(.+?)\s*\|', re.MULTILINE)
fr_entries = {}
for m in fr_pattern.finditer(srs_text):
    fr_num  = int(m.group(1))
    fr_text = m.group(2).strip()
    fr_entries[fr_num] = fr_text
```

Verify: `len(fr_entries)` should equal your expected total.

---

## Step 4 — Build FR → Subsection Mapping

```python
fr_to_sub = {}
for (sec, subsec, subname, fr_start, fr_end) in subsections:
    s = int(fr_start.replace("FR-", ""))
    e = int(fr_end.replace("FR-", ""))
    for n in range(s, e + 1):
        fr_to_sub[n] = (sec, subsec, subname)
```

---

## Step 5 — File Naming Convention

Consistent slugs keep Obsidian graph clean and wikilinks resolvable:

```python
def sec_slug(sec_num, sec_name):
    # "3.1" + "User Management and Authentication" -> "3.1-User-Management-and-Authentication"
    return sec_num + "-" + sec_name.replace(" ", "-").replace("/", "-")

def subsec_slug(subsec_num, subname):
    # "3.1.6" + "Multi-Factor Authentication" -> "3.1.6-Multi-Factor-Authentication"
    return f"{subsec_num}-{subname.replace(' ', '-')}"

def fr_filename(fr_num):
    # 42 -> "FR-042"
    return f"FR-{fr_num:03d}"
```

Zero-pad FR numbers to 3 digits so filenames sort correctly in Obsidian file explorer.

---

## Step 6 — Write Leaf Files (FR-NNN.md)

One file per requirement. Links up to both section and subsection.

```python
for fr_num in range(1, total_frs + 1):
    fr_text = fr_entries[fr_num]
    sec, subsec, subname = fr_to_sub[fr_num]
    sec_name = data_sections[sec]["name"]

    content = f"""---
id: FR-{fr_num:03d}
type: functional-requirement
section: "{subsec}"
subsection: "{subname}"
feature: "{sec_name}"
tags:
  - srs
  - functional-requirement
---

# FR-{fr_num:03d}

**Feature Area:** [[{sec_slug(sec, sec_name)}|{sec}. {sec_name}]]
**Subsection:** [[{subsec_slug(subsec, subname)}|{subsec}. {subname}]]

## Requirement

{fr_text}
"""
    with open(f"{output_dir}/FR-{fr_num:03d}.md", "w", encoding="utf-8") as f:
        f.write(content)
```

---

## Step 7 — Write Subsection Files

One file per subsection. Links up to section, lists all FR leaves.

```python
for (sec, subsec, subname, fr_start, fr_end) in subsections:
    sec_name = data_sections[sec]["name"]
    s = int(fr_start.replace("FR-", ""))
    e = int(fr_end.replace("FR-", ""))
    fr_links = "\n".join([f"- [[FR-{n:03d}]]" for n in range(s, e + 1)])

    content = f"""---
id: "{subsec}"
type: subsection
section: "{sec}"
feature: "{sec_name}"
tags:
  - srs
  - subsection
---

# {subsec}. {subname}

**Feature Area:** [[{sec_slug(sec, sec_name)}|{sec}. {sec_name}]]

Range: {fr_start} to {fr_end}

## Functional Requirements

{fr_links}
"""
    with open(f"{output_dir}/{subsec_slug(subsec, subname)}.md", "w", encoding="utf-8") as f:
        f.write(content)
```

---

## Step 8 — Write Section Files

One file per feature area. Links down to all its subsections.

```python
for sec_num, sec_data in data_sections.items():
    sec_name = sec_data["name"]
    subs = [(s,ss,sn,fs,fe) for (s,ss,sn,fs,fe) in subsections if s == sec_num]
    sub_links = "\n".join([
        f"- [[{subsec_slug(ss, sn)}|{ss}. {sn}]] ({fs} to {fe})"
        for (s, ss, sn, fs, fe) in subs
    ])

    content = f"""---
id: "{sec_num}"
type: section
feature: "{sec_name}"
fr_range: "{sec_data['fr_range']}"
tags:
  - srs
  - section
---

# {sec_num}. {sec_name}

**Source:** [[justgo-platform-software-requirements-specification|SRS Document]]

**Business Scope:** {sec_data['scope']}

**FR Range:** {sec_data['fr_range']}

## Subsections

{sub_links}
"""
    with open(f"{output_dir}/{sec_slug(sec_num, sec_name)}.md", "w", encoding="utf-8") as f:
        f.write(content)
```

---

## Step 9 — Write the MOC

The MOC is the single top-level entry point. It lists all sections and their subsections.

```python
moc_body = []
for sec_num, sec_data in data_sections.items():
    sec_name = sec_data["name"]
    moc_body.append(f"\n### [[{sec_slug(sec_num, sec_name)}|{sec_num}. {sec_name}]] — {sec_data['fr_range']}\n")
    subs = [(s,ss,sn,fs,fe) for (s,ss,sn,fs,fe) in subsections if s == sec_num]
    for (s, ss, sn, fs, fe) in subs:
        moc_body.append(f"- [[{subsec_slug(ss, sn)}|{ss}. {sn}]] ({fs}–{fe})\n")

moc_content = f"""---
title: SRS — Map of Content
tags:
  - srs
  - moc
---

# SRS — Map of Content

**Source:** [[your-srs-document|Full SRS Document]]

## Feature Areas

{"".join(moc_body)}
"""
with open(f"{output_dir}/MOC.md", "w", encoding="utf-8") as f:
    f.write(moc_content)
```

---

## Output Structure

```
docs/SRS/
├── MOC.md                                      ← entry point
├── 3.1-User-Management-and-Authentication.md   ← section (×11)
├── 3.1.1-Authorization.md                      ← subsection (×62)
├── 3.1.6-Multi-Factor-Authentication.md
├── ...
├── FR-001.md                                   ← leaf (×502)
├── FR-002.md
└── FR-502.md
```

Total for a 502-FR SRS: **576 files** (1 MOC + 11 sections + 62 subsections + 502 leaves).

---

## Obsidian Setup

1. Open Obsidian and set vault to the repo root (or the `docs/` folder).
2. Open `docs/SRS/MOC.md` — all `[[wikilinks]]` resolve by filename match.
3. **Graph view**: filter by tag `srs` to see only SRS notes. The MOC will be the most-connected hub.
4. **Dataview plugin** (optional): query by frontmatter fields, e.g.:

```dataview
TABLE subsection, feature
FROM #functional-requirement
WHERE section = "3.1.6"
SORT id ASC
```

---

## Regenerating After SRS Changes

The script is fully idempotent — re-running overwrites existing files.

| Change type | Action |
|---|---|
| New FR added to existing subsection | Update `subsections` tuple range, re-run script |
| New subsection added | Add tuple to `subsections`, re-run script |
| New section added | Add entry to `data_sections` and tuples to `subsections`, re-run script |
| Requirement text changed in SRS | Re-run script — FR files are regenerated from source |
| FR renumbered | Update `subsections` ranges, re-run script |

The generator script lives at `scripts/generate-obsidian-srs.py`.

---

## Checklist

- [ ] Source SRS follows the heading and table conventions in Step 1
- [ ] All FR IDs are unique in the source document
- [ ] Every FR number is covered by exactly one subsection range in `subsections`
- [ ] `data_sections` keys match the section numbers used in `subsections`
- [ ] `fr_range` in `data_sections` matches the union of its subsection ranges
- [ ] Script runs without `WARNING:` lines in output
- [ ] `len(fr_entries)` matches expected total
- [ ] Obsidian graph shows no broken (unresolved) links
