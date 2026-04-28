from __future__ import annotations

import re
from dataclasses import dataclass, field
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
SRS_PATH = DOCS / "justgo-platform-software-requirements-specification.md"
INDEX_PATH = DOCS / "justgo-platform-srs-obsidian-index.md"
OUTPUT_DIR = DOCS / "SRS"
SOURCE_NOTE = "justgo-platform-software-requirements-specification"

HEADING_RE = re.compile(r"^(#{2,4})\s+(.+?)\s*$")
FR_RE = re.compile(r"^\|\s*(FR-(\d{3}))\.\s*\|\s*(.*?)\s*\|\s*(.*?)\s*\|")
MAP_LINK_RE = re.compile(r"^\[Obsidian map: \[\[[^\]]+\]\]\]\s*$")


@dataclass
class Requirement:
    id: str
    number: int
    text: str
    endpoints: str
    subsection: "Node"


@dataclass
class Node:
    title: str
    level: int
    filename: str
    parent: "Node | None" = None
    children: list["Node"] = field(default_factory=list)
    requirements: list[Requirement] = field(default_factory=list)

    @property
    def type(self) -> str:
        if self.title == "Document Control":
            return "document-control"
        if self.title == "Glossary":
            return "glossary"
        if re.match(r"^3\.\d+\. ", self.title):
            return "feature-area"
        if re.match(r"^3\.\d+\.\d+\. ", self.title):
            return "functional-topic"
        if re.match(r"^\d+\. ", self.title):
            return "section"
        if re.match(r"^[A-Z]\.\d+\. ", self.title):
            return "appendix-topic"
        return "heading"


def strip_generated_map_links(text: str) -> str:
    lines = text.splitlines()
    return "\n".join(line for line in lines if not MAP_LINK_RE.match(line)) + "\n"


def slugify(title: str) -> str:
    value = title.strip()
    value = re.sub(r"^(\d+(?:\.\d+)*)\.\s+", r"\1-", value)
    value = re.sub(r"^([A-Z]\.\d+)\.\s+", r"\1-", value)
    value = re.sub(r"[^A-Za-z0-9.-]+", "-", value)
    value = re.sub(r"-+", "-", value).strip("-")
    return value or "Untitled"


def wiki(filename: str, alias: str | None = None) -> str:
    stem = filename[:-3] if filename.endswith(".md") else filename
    return f"[[{stem}|{alias}]]" if alias else f"[[{stem}]]"


def plain_parent_title(node: Node) -> str:
    return node.parent.title if node.parent else "None"


def parse_srs(text: str) -> tuple[list[Node], list[Requirement]]:
    nodes: list[Node] = []
    requirements: list[Requirement] = []
    stack: dict[int, Node] = {}
    filenames: set[str] = set()
    current_subsection: Node | None = None

    for line in text.splitlines():
        heading = HEADING_RE.match(line)
        if heading:
            level = len(heading.group(1))
            title = heading.group(2).strip()
            if title == "Table of Contents":
                current_subsection = None
                continue

            base_filename = slugify(title)
            filename = f"{base_filename}.md"
            suffix = 2
            while filename in filenames:
                filename = f"{base_filename}-{suffix}.md"
                suffix += 1
            filenames.add(filename)

            parent = None
            for parent_level in range(level - 1, 1, -1):
                if parent_level in stack:
                    parent = stack[parent_level]
                    break

            node = Node(title=title, level=level, filename=filename, parent=parent)
            if parent:
                parent.children.append(node)
            nodes.append(node)
            stack[level] = node
            for stale_level in list(stack):
                if stale_level > level:
                    del stack[stale_level]

            current_subsection = node if re.match(r"^3\.\d+\.\d+\. ", title) else None
            continue

        fr = FR_RE.match(line)
        if fr and current_subsection:
            req = Requirement(
                id=fr.group(1),
                number=int(fr.group(2)),
                text=fr.group(3).strip(),
                endpoints=fr.group(4).strip(),
                subsection=current_subsection,
            )
            requirements.append(req)
            current_subsection.requirements.append(req)

    return nodes, requirements


def fr_range(requirements: list[Requirement]) -> str:
    if not requirements:
        return "None"
    ordered = sorted(requirements, key=lambda item: item.number)
    return f"{ordered[0].id} to {ordered[-1].id}"


def descendant_requirements(node: Node) -> list[Requirement]:
    found = list(node.requirements)
    for child in node.children:
        found.extend(descendant_requirements(child))
    return sorted(found, key=lambda item: item.number)


def ancestors(node: Node) -> list[Node]:
    result: list[Node] = []
    current = node.parent
    while current:
        result.append(current)
        current = current.parent
    return list(reversed(result))


def write_file(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")


def write_node(node: Node) -> None:
    child_links = "\n".join(f"- {wiki(child.filename, child.title)}" for child in node.children)
    reqs = node.requirements
    req_links = "\n".join(f"- [[{req.id}]]" for req in reqs)
    desc_reqs = descendant_requirements(node)
    breadcrumb = " > ".join(item.title for item in ancestors(node) + [node])

    sections = [
        "---",
        f'title: "{node.title}"',
        f'type: "{node.type}"',
        f'source: "{SOURCE_NOTE}"',
        f'source_heading: "{node.title}"',
        f'parent: "{plain_parent_title(node)}"',
        "tags:",
        "  - srs",
        f"  - {node.type}",
        "---",
        "",
        f"# {node.title}",
        "",
        f"Source heading: {node.title}",
    ]
    if node.parent:
        sections.extend(["", f"Parent: {node.parent.title}"])
    sections.extend(["", f"Path: {breadcrumb}"])
    sections.extend(["", f"Functional Requirement Range: {fr_range(desc_reqs)}"])

    if child_links:
        sections.extend(["", "## Child Notes", "", child_links])
    if req_links:
        sections.extend(["", "## Functional Requirement Leaves", "", req_links])
    if not child_links and not req_links:
        sections.extend(["", "## Notes", "", "This SRS heading has no generated child notes."])

    write_file(OUTPUT_DIR / node.filename, "\n".join(sections))


def write_requirement(req: Requirement) -> None:
    subsection = req.subsection
    feature = subsection.parent
    content = "\n".join(
        [
            "---",
            f"id: {req.id}",
            'type: "functional-requirement"',
            f'subsection: "{subsection.title}"',
            f'feature: "{feature.title if feature else ""}"',
            f'endpoint_trace_ids: "{req.endpoints}"',
            "tags:",
            "  - srs",
            "  - functional-requirement",
            "---",
            "",
            f"# {req.id}",
            "",
            f"Feature: {feature.title if feature else ''}",
            f"Subsection: {subsection.title}",
            f"Source heading: {subsection.title}",
            "",
            "## Requirement",
            "",
            req.text,
            "",
            "## Endpoint Trace IDs",
            "",
            req.endpoints,
        ]
    )
    write_file(OUTPUT_DIR / f"{req.id}.md", content)


def write_moc(nodes: list[Node], requirements: list[Requirement]) -> None:
    roots = [node for node in nodes if node.parent is None]

    feature_areas = [
        node
        for node in nodes
        if node.level == 3 and re.match(r"^3\.\d+\. ", node.title)
    ]

    root_lines = [f"- {wiki(root.filename, root.title)}" for root in roots]

    content = "\n".join(
        [
            "---",
            'title: "JustGo Platform SRS Map of Content"',
            'type: "moc"',
            "tags:",
            "  - srs",
            "  - moc",
            "  - justgo",
            "---",
            "",
            "# JustGo Platform SRS Map of Content",
            "",
            f"**Source:** [[{SOURCE_NOTE}|Full SRS Document]]",
            f"**Index:** [[justgo-platform-srs-obsidian-index|SRS Obsidian Index]]",
            "",
            f"{len(requirements)} functional requirements across {len(feature_areas)} feature areas.",
            "",
            "## Root Sections",
            "",
            "\n".join(root_lines),
        ]
    )
    write_file(OUTPUT_DIR / "MOC.md", content)


def update_index(nodes: list[Node]) -> None:
    roots = [node for node in nodes if node.parent is None]
    root_lines = [f"- {wiki(root.filename, root.title)}" for root in roots]

    lines = [
        "# JustGo Platform SRS Index",
        "",
        "- [[MOC|SRS Map of Content]]",
        "- [[justgo-platform-software-requirements-specification|Full SRS Document]]",
        "",
        "## Root Sections",
        "",
        *root_lines,
    ]
    write_file(INDEX_PATH, "\n".join(lines))


def update_srs_map_links(text: str, nodes: list[Node]) -> str:
    output: list[str] = []
    for line in strip_generated_map_links(text).splitlines():
        output.append(line)
        heading = HEADING_RE.match(line)
        if not heading:
            continue
        title = heading.group(2).strip()
        if title == "3. System Features and Functional Requirements":
            output.append("")
            output.append("[Obsidian SRS Map: [[MOC|Open layered map]]]")
    return "\n".join(output) + "\n"


def validate(nodes: list[Node], requirements: list[Requirement]) -> None:
    feature_areas = [
        node
        for node in nodes
        if node.level == 3 and re.match(r"^3\.\d+\. ", node.title)
    ]
    functional_subsections = [
        node
        for node in nodes
        if node.level == 4 and re.match(r"^3\.\d+\.\d+\. ", node.title)
    ]
    ids = [req.id for req in requirements]
    if len(ids) != len(set(ids)):
        raise ValueError("Duplicate FR IDs detected")
    expected = [f"FR-{number:03d}" for number in range(1, len(requirements) + 1)]
    if ids != expected:
        raise ValueError(f"FR IDs are not contiguous from FR-001 to FR-{len(requirements):03d}")
    unmapped = [req.id for req in requirements if not req.subsection]
    if unmapped:
        raise ValueError(f"Unmapped requirements: {', '.join(unmapped)}")

    print(f"Feature areas: {len(feature_areas)}")
    print(f"Functional subsections: {len(functional_subsections)}")
    print(f"FR leaf files: {len(requirements)}")


def main() -> None:
    original_text = SRS_PATH.read_text(encoding="utf-8")
    clean_text = strip_generated_map_links(original_text)
    nodes, requirements = parse_srs(clean_text)
    validate(nodes, requirements)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for node in nodes:
        write_node(node)
    for req in requirements:
        write_requirement(req)
    write_moc(nodes, requirements)
    update_index(nodes)
    SRS_PATH.write_text(update_srs_map_links(original_text, nodes), encoding="utf-8")

    total_files = len(list(OUTPUT_DIR.glob("*.md")))
    print(f"Heading note files: {len(nodes)}")
    print(f"Total files in docs/SRS: {total_files}")


if __name__ == "__main__":
    main()
