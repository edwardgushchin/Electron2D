#!/usr/bin/env python3
"""Regression check for overload pairing, texture pages and inventory accounting."""

import json
import re

from render import CLASS_PAGES, DATA, choose, render


def check_texture_pages(pages, upstream):
    names = {"Texture": "Texture", "Texture2D": "Texture", "Texture2DArray": "TextureArray",
             "Texture2DArrayRD": "TextureArrayRD", "Texture2DRD": "TextureRD"}
    expected_rows = {}
    for item in upstream["types"]:
        if item["name"] not in names:
            continue
        page = CLASS_PAGES / f"{names[item['name']]}.md"
        rows = expected_rows.setdefault(page, [])
        rows.append(f"| [`class {item['name']}`]")
        rows.extend(f"| [`{member['kind']} {member['signature']}`]" for member in item["members"])
    for page, rows in expected_rows.items():
        assert pages[page].startswith(f"# {page.stem} API coverage\n")
        actual = [line.split("](", 1)[0] + "]" for line in pages[page].splitlines() if line.startswith("| [`")]
        assert sorted(actual) == sorted(rows), f"Lost or duplicated texture declarations in {page}"
    for name in names:
        if "2D" in name:
            assert CLASS_PAGES / f"{name}.md" not in pages, f"Redundant texture page: {name}"
    for page, content in pages.items():
        for link, anchor in re.findall(r"\]\(([^)]+\.md)(#[^)]+)?\)", content):
            target = (page.parent / link).resolve()
            if target.parent == CLASS_PAGES:
                assert target in pages, f"Link to obsolete class page in {page}: {link}"
                if target.stem == "Texture":
                    assert anchor in {"#godot-texture", "#godot-texture2d"}
                    assert f"## Godot {anchor.removeprefix('#godot-')}".lower() in pages[target].lower()
            else:
                assert target in pages or target.exists(), f"Broken link in {page}: {link}"


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
    check_texture_pages(pages, upstream)
    for name in ("CurveTexture", "CurveXYZTexture"):
        rows = [line for line in pages[CLASS_PAGES / f"{name}.md"].splitlines() if line.startswith("| [`")]
        assert rows and all(" | Implemented | " in line for line in rows), f"Audited curve texture member lost implementation evidence: {name}"
    rendering_rows = pages[CLASS_PAGES / "RenderingServer.md"].splitlines()
    for kind, name in [('method', 'camera_create('), ('method', 'light_set_color('),
                       ('method', 'texture_3d_create('), ('method', 'voxel_gi_create('),
                       ('enum', 'LightType'), ('enum_value', 'LIGHT_DIRECTIONAL [LightType]')]:
        row, = [line for line in rendering_rows if line.startswith(f'| [`{kind} {name}')]
        assert ' | Excluded | ' in row, f'3D API appeared in the implementation roadmap: {row}'
    for name in ('canvas_light_create(', 'canvas_item_create(', 'mesh_create(', 'texture_2d_create('):
        row, = [line for line in rendering_rows if line.startswith(f'| [`method {name}')]
        assert ' | Excluded | ' not in row, f'Shared or 2D API was excluded: {row}'
    assert sum(summary["states"].values()) == summary["upstream_types"] + summary["upstream_members"]
    assert (summary["mapped_engine"] + summary["reviewed_extras"] + summary["unmapped_engine"]
            == summary["electron2d_declarations"])
    assert summary["unmapped_engine"] == 0, "Every Electron2D declaration needs a pairing or rationale"
    vector2i_rows = [line for line in pages[CLASS_PAGES / "Vector2i.md"].splitlines() if line.startswith("| [`")]
    assert len(vector2i_rows) == 54
    assert sum(" | Partial | " in line for line in vector2i_rows) == 0
    assert sum(" | Implemented | " in line for line in vector2i_rows) == 54
    assert all("Declaration mapping is structural" not in line for line in vector2i_rows)
    vector4i_norm_rows = [line for line in pages[CLASS_PAGES / "Vector4i.md"].splitlines()
                          if line.startswith("| [`method ") and any(name in line for name in
                              ("distance_squared_to(", "distance_to(", "length()", "length_squared()"))]
    assert len(vector4i_norm_rows) == 4
    assert all(" | Implemented | " in line for line in vector4i_norm_rows)


if __name__ == "__main__":
    main()
