#!/usr/bin/env python3
"""Small regression check for overload pairing and inventory accounting."""

import json
import re

from render import CLASS_PAGES, DATA, choose, render


def main():
    upstream = json.loads((DATA / "godot-4.7.2.json").read_text())
    engine = json.loads((DATA / "electron2d.json").read_text())
    vector = next(item for item in upstream["types"] if item["name"] == "Vector2")
    constructors = [item for item in engine if item["declaringType"] == "Electron2D.Vector2"]
    copies = [item for item in vector["members"] if item["kind"] == "constructor" and
              len(item["attributes"].get("params", [])) == 1]
    by_parameter = {item["attributes"]["params"][0]["type"]: choose(vector, item, constructors, set())
                    for item in copies}
    assert by_parameter["Vector2"] == [], "A Vector2I constructor cannot represent a Vector2 copy"
    assert [item["id"] for item in by_parameter["Vector2i"]] == [
        "constructor:Electron2D.Vector2..ctor(Electron2D.Vector2I)"
    ]

    pages, summary = render()
    assert sum(summary["states"].values()) == summary["upstream_types"] + summary["upstream_members"]
    assert (summary["mapped_engine"] + summary["reviewed_extras"] + summary["unmapped_engine"]
            == summary["electron2d_declarations"])
    assert summary["unmapped_engine"] == 0, "Every Electron2D declaration needs a pairing or rationale"
    vector2i_rows = [line for line in pages[CLASS_PAGES / "Vector2i.md"].splitlines() if line.startswith("| [`")]
    assert len(vector2i_rows) == 54
    assert sum(" | Partial | " in line for line in vector2i_rows) == 5
    assert sum(" | Implemented | " in line for line in vector2i_rows) == 49
    assert all("Declaration mapping is structural" not in line for line in vector2i_rows)
    for page, content in pages.items():
        for link in re.findall(r"\]\(([^)]+\.md)\)", content):
            target = (page.parent / link).resolve()
            assert target in pages or target.exists(), f"Broken link in {page}: {link}"


if __name__ == "__main__":
    main()
