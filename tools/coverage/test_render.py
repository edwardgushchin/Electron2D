#!/usr/bin/env python3
"""Regression check for overload pairing, texture pages and inventory accounting."""

import json
import re

from render import CLASS_PAGES, DATA, choose, coverage_target, render


def check_texture_pages(pages, upstream):
    names = {"Texture": "Texture", "Texture2D": "Texture", "Texture2DArray": "TextureArray",
             "Texture2DArrayRD": "TextureArrayRD", "Texture2DRD": "TextureRD",
             "GradientTexture1D": "GradientRampTexture", "GradientTexture2D": "GradientTexture"}
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
        if name != names[name]:
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
    polygon = next(item for item in engine if item.get("name") == "DrawPolygon")
    defaults = {p["name"]: p["default"] for p in polygon["parameters"]}
    assert defaults["uvs"] == "default" and defaults["texture"] == "null", "Struct defaults are not nullable reference defaults"
    assert "uvs = default" in polygon["signature"] and "texture = null" in polygon["signature"]
    vector = next(item for item in upstream["types"] if item["name"] == "Vector2")
    constructors = [item for item in engine if item["declaringType"] == "Electron2D.Vector2"]
    copies = [item for item in vector["members"] if item["kind"] == "constructor" and
              len(item["attributes"].get("params", [])) == 1]
    by_parameter = {item["attributes"]["params"][0]["type"]: choose(vector, item, constructors, set())
                    for item in copies}
    assert by_parameter["Vector2"] == [], "A Vector2i constructor cannot represent a Vector2 copy"
    assert [item["id"] for item in by_parameter["Vector2i"]] == [
        "constructor:Electron2D.Vector2..ctor(Electron2D.Vector2i)"
    ]

    pages, summary = render()
    check_texture_pages(pages, upstream)
    class_rows = {
        name: next(line for line in pages[CLASS_PAGES / coverage_target(name)].splitlines()
                   if line.startswith(f"| [`class {name}`]"))
        for name in ("AStar2D", "AStarGrid2D", "AESContext", "InputEventMIDI", "Shortcut",
                     "Texture2DArray", "RenderingDevice", "FramebufferCacheRD", "BoxMesh",
                     "RefCounted", "Line2D", "NativeMenu", "GDScriptLanguageProtocol",
                     "EditorNode3DGizmo")
    }
    astar_rows = [row for row in pages[CLASS_PAGES / "AStar2D.md"].splitlines()
                  if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(astar_rows) == 28 and all(" | Implemented | " in row for row in astar_rows)
    assert "../../classes/AStar.md" in class_rows["AStar2D"]
    grid_rows = [row for row in pages[CLASS_PAGES / "AStarGrid2D.md"].splitlines()
                 if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(grid_rows) == 44 and all(" | Implemented | " in row for row in grid_rows)
    assert "../../classes/AStarGrid.md" in class_rows["AStarGrid2D"]
    assert "cryptography utility contract" in class_rows["AESContext"]
    assert "accepted MIDI-domain" in class_rows["InputEventMIDI"]
    assert "GUI/editor Shortcut" in class_rows["Shortcut"]
    assert "layered/array texture storage" in class_rows["Texture2DArray"]
    assert " | Partial | " in class_rows["Line2D"] and "../../classes/Line.md" in class_rows["Line2D"]
    line_rows = [row for row in pages[CLASS_PAGES / "Line2D.md"].splitlines() if row.startswith("| [`")]
    assert len(line_rows) == 33 and all(" | Unimplemented | " not in row and " | Blocked | " not in row for row in line_rows)
    assert "native-menu service" in class_rows["NativeMenu"]
    packed_types = [item["name"] for item in upstream["types"]
                    if item["name"].startswith("Packed") and item["name"].endswith("Array")]
    assert len(packed_types) == 10
    for name in packed_types:
        content = pages[CLASS_PAGES / f"{name}.md"]
        rows = [line for line in content.splitlines() if line.startswith("| [`")]
        assert rows and " | Excluded | " in rows[0], name
        assert " | Excluded | " in next(line for line in rows if line.startswith("| [`method append(")), name
        assert "dynamic/untyped" not in content and "ADR 0001/0002" not in content, name
    packed = pages[CLASS_PAGES / "PackedColorArray.md"]
    assert "Color[]" in packed and "ReadOnlySpan<Color>" in packed
    assert " | Blocked | " in next(line for line in packed.splitlines()
                                  if line.startswith("| [`method to_byte_array("))
    byte_array = pages[CLASS_PAGES / "PackedByteArray.md"]
    assert " | Blocked | " in next(line for line in byte_array.splitlines()
                                  if line.startswith("| [`method compress("))
    assert " | Excluded | " in next(line for line in byte_array.splitlines()
                                   if line.startswith("| [`method decode_var("))
    for name in ("RenderingDevice", "FramebufferCacheRD", "BoxMesh", "RefCounted",
                 "GDScriptLanguageProtocol", "EditorNode3DGizmo"):
        assert " | Excluded | " in class_rows[name]
        assert all(" | Excluded | " in line for line in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                   if line.startswith("| [`")), f"Excluded class has a blocked member: {name}"
    assert not any("first SDL3 GPU 2D rendering slice" in content for content in pages.values())
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
