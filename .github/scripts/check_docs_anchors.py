#!/usr/bin/env python3
"""Check that every docs/ anchor the code links to exists.

Readarr links into docs/*.md by heading anchor, as GitHub renders them. A renamed heading, a new
health check or a new integration can silently break those links, so this collects every anchor
the code uses and checks each against the headings in docs/:

- health check fragments ("#...") in src/NzbDrone.Core/HealthCheck
- integration display names (`Name => "..."`), which docs/supported.md must have as headings
- explicit links: DocsUrls.<Page> + "#anchor" in C#, getDocsUrl('page', 'anchor') in the frontend,
  hardcoded docs URLs, and links between the docs pages themselves

Run from the repository root: python3 .github/scripts/check_docs_anchors.py
"""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOCS = ROOT / "docs"
PAGES = {"HealthChecks": "health-checks", "Settings": "settings", "Supported": "supported", "Troubleshooting": "troubleshooting"}
PROVIDER_DIRS = ["Indexers", "Download/Clients", "ImportLists", "Notifications"]


def slug(heading: str) -> str:
    """GitHub's heading anchor: lower case, punctuation dropped, spaces as hyphens."""
    return re.sub(r"[^\w\- ]", "", heading.strip().lower()).replace(" ", "-")


def anchors(page: str) -> set[str]:
    seen: dict[str, int] = {}
    result = set()
    in_code = False
    for line in (DOCS / f"{page}.md").read_text(encoding="utf-8").splitlines():
        if line.startswith("```"):
            in_code = not in_code
        if in_code or not re.match(r"#{1,6} ", line):
            continue
        base = slug(line.lstrip("#"))
        count = seen.get(base, 0)
        result.add(base if count == 0 else f"{base}-{count}")
        seen[base] = count + 1
    return result


def source_files(directory: str, pattern: str):
    for path in (ROOT / directory).rglob(pattern):
        if ".Test" not in str(path) and "node_modules" not in str(path):
            yield path


def required() -> list[tuple[str, str, str]]:
    """(page, anchor, where) for every link the code makes."""
    needed = []

    for path in source_files("src/NzbDrone.Core/HealthCheck", "*.cs"):
        for frag in re.findall(r'"#([a-z0-9-]+)"', path.read_text(encoding="utf-8")):
            needed.append(("health-checks", frag, path.name))

    for directory in PROVIDER_DIRS:
        for path in source_files(f"src/NzbDrone.Core/{directory}", "*.cs"):
            text = path.read_text(encoding="utf-8")
            if re.search(r"\babstract class\b", text):
                continue
            for name in re.findall(r'public override string Name => "([^"]+)";', text):
                needed.append(("supported", slug(name), f"{path.name} ({name})"))

    for path in source_files("src", "*.cs"):
        for page, frag in re.findall(r'DocsUrls\.(\w+) \+ "#([a-z0-9-]+)"', path.read_text(encoding="utf-8")):
            needed.append((PAGES[page], frag, path.name))

    for ext in ("*.js", "*.ts", "*.tsx"):
        for path in source_files("frontend/src", ext):
            for page, frag in re.findall(r"getDocsUrl\('([a-z-]+)', '([a-z0-9-]+)'\)", path.read_text(encoding="utf-8")):
                needed.append((page, frag, path.name))

    # Hardcoded links, such as the static login page's
    for directory, ext in (("frontend/src", "*.html"), ("frontend/src", "*.js"), ("src", "*.cs"), ("docs", "*.md")):
        for path in source_files(directory, ext):
            text = path.read_text(encoding="utf-8")
            for page, frag in re.findall(r"readarr-rresurrected/blob/develop/docs/([a-z-]+)\.md#([a-z0-9-]+)", text):
                needed.append((page, frag, path.name))
            if directory == "docs":
                for page, frag in re.findall(r"\]\(([a-z-]+)\.md#([a-z0-9-]+)\)", text):
                    needed.append((page, frag, f"docs/{path.name}"))
                for frag in re.findall(r"\]\(#([a-z0-9-]+)\)", text):
                    needed.append((path.stem, frag, f"docs/{path.name}"))

    return needed


def main() -> int:
    available = {page: anchors(page) for page in PAGES.values()}
    needed = required()
    missing = [(p, a, w) for p, a, w in needed if a not in available.get(p, set())]

    for page, anchor, where in sorted(set(missing)):
        print(f"missing: docs/{page}.md#{anchor}  (linked from {where})")

    print(f"{len(set((p, a) for p, a, _ in needed))} anchors linked, {len(set((p, a) for p, a, _ in missing))} missing")
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main())
