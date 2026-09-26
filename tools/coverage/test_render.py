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
        for name in ("AStar2D", "AStarGrid2D", "Area2D", "AnimatableBody2D", "CharacterBody2D", "Shape2D", "CircleShape2D", "CapsuleShape2D", "SegmentShape2D", "SeparationRayShape2D", "ConvexPolygonShape2D", "ConcavePolygonShape2D", "CollisionPolygon2D", "RectangleShape2D", "RayCast2D", "ShapeCast2D", "KinematicCollision2D", "PhysicsTestMotionParameters2D", "PhysicsTestMotionResult2D", "RID", "World2D", "PhysicsServer2D", "PhysicsRayQueryParameters2D", "PhysicsPointQueryParameters2D", "PhysicsDirectSpaceState2D", "PhysicsDirectBodyState2D",
                     "CollisionShape2D", "CollisionObject2D", "PhysicsBody2D", "StaticBody2D", "RigidBody2D",
                     "AESContext", "InputEventMIDI", "Shortcut",
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
    assert "../../classes/Area.md" in class_rows["Area2D"] and " | Partial | " in class_rows["Area2D"]
    assert "../../classes/AnimatableBody.md" in class_rows["AnimatableBody2D"] and " | Implemented | " in class_rows["AnimatableBody2D"]
    animatable_rows = [row for row in pages[CLASS_PAGES / "AnimatableBody2D.md"].splitlines()
                       if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(animatable_rows) == 2 and all(" | Implemented | " in row for row in animatable_rows)
    capsule_rows = [row for row in pages[CLASS_PAGES / "CapsuleShape2D.md"].splitlines()
                    if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(capsule_rows) == 4 and all(" | Implemented | " in row for row in capsule_rows)
    segment_rows = [row for row in pages[CLASS_PAGES / "SegmentShape2D.md"].splitlines()
                    if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(segment_rows) == 3 and all(" | Implemented | " in row for row in segment_rows)
    convex_rows = [row for row in pages[CLASS_PAGES / "ConvexPolygonShape2D.md"].splitlines()
                   if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(convex_rows) == 3 and all(" | Implemented | " in row for row in convex_rows)
    concave_rows = [row for row in pages[CLASS_PAGES / "ConcavePolygonShape2D.md"].splitlines()
                    if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(concave_rows) == 2 and all(" | Implemented | " in row for row in concave_rows)
    polygon_node_rows = [row for row in pages[CLASS_PAGES / "CollisionPolygon2D.md"].splitlines()
                         if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(polygon_node_rows) == 10
    assert all(" | Implemented | " in row for row in polygon_node_rows)
    ray_node_rows = [row for row in pages[CLASS_PAGES / "RayCast2D.md"].splitlines()
                     if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(ray_node_rows) == 22
    assert {state: sum(f" | {state} | " in row for row in ray_node_rows)
            for state in ("Implemented", "Partial", "Blocked")} == {
                "Implemented": 20, "Partial": 2, "Blocked": 0}
    assert "../../classes/RayCast.md" in class_rows["RayCast2D"]
    shape_cast_rows = [row for row in pages[CLASS_PAGES / "ShapeCast2D.md"].splitlines()
                       if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(shape_cast_rows) == 28
    assert {state: sum(f" | {state} | " in row for row in shape_cast_rows)
            for state in ("Implemented", "Partial", "Blocked")} == {
                "Implemented": 26, "Partial": 2, "Blocked": 0}
    assert "../../classes/ShapeCast.md" in class_rows["ShapeCast2D"]
    character_rows = [row for row in pages[CLASS_PAGES / "CharacterBody2D.md"].splitlines()
                      if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(character_rows) == 41
    assert {state: sum(f" | {state} | " in row for row in character_rows)
            for state in ("Implemented", "Partial", "Unimplemented")} == {
                "Implemented": 41, "Partial": 0, "Unimplemented": 0}
    assert "../../classes/CharacterBody.md" in class_rows["CharacterBody2D"]
    body_state_rows = [row for row in pages[CLASS_PAGES / "PhysicsDirectBodyState2D.md"].splitlines()
                       if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(body_state_rows) == 43
    assert {state: sum(f" | {state} | " in row for row in body_state_rows)
            for state in ("Implemented", "Partial", "Blocked")} == {
                "Implemented": 40, "Partial": 3, "Blocked": 0}
    assert "../../classes/PhysicsDirectBodyState.md" in class_rows["PhysicsDirectBodyState2D"]
    shape_rows = [row for row in pages[CLASS_PAGES / "Shape2D.md"].splitlines()
                  if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(shape_rows) == 8
    assert {state: sum(f" | {state} | " in row for row in shape_rows)
            for state in ("Implemented", "Partial", "Blocked", "Unimplemented")} == {
                "Implemented": 5, "Partial": 1, "Blocked": 2, "Unimplemented": 0}
    separation_rows = [row for row in pages[CLASS_PAGES / "SeparationRayShape2D.md"].splitlines()
                       if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(separation_rows) == 3
    assert sum(" | Implemented | " in row for row in separation_rows) == 2
    assert " | Partial | " in class_rows["SeparationRayShape2D"]
    assert "alternative directed solver manifolds" in class_rows["SeparationRayShape2D"]
    for name, count in (("RID", 11), ("PhysicsRayQueryParameters2D", 9)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                if row.startswith("| [`") and "github.com/godotengine" in row]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
    world_rows = pages[CLASS_PAGES / "World2D.md"]
    assert "| Implemented |" in next(row for row in world_rows.splitlines()
                                       if row.startswith("| [`property RID space"))
    direct_rows = pages[CLASS_PAGES / "PhysicsDirectSpaceState2D.md"]
    assert "../../classes/PhysicsDirectSpaceState.md" in class_rows["PhysicsDirectSpaceState2D"]
    assert "| Implemented |" in next(row for row in direct_rows.splitlines()
                                       if row.startswith("| [`method intersect_ray("))
    assert "| Partial |" in next(row for row in direct_rows.splitlines()
                                   if row.startswith("| [`method intersect_point("))
    assert all(" | Implemented | " in next(row for row in direct_rows.splitlines()
                                          if row.startswith(f"| [`method {name}("))
               for name in ("intersect_shape", "cast_motion", "collide_shape", "get_rest_info"))
    shape_query_rows = [row for row in pages[CLASS_PAGES / "PhysicsShapeQueryParameters2D.md"].splitlines()
                        if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(shape_query_rows) == 10 and all(" | Implemented | " in row for row in shape_query_rows)
    server_rows = [row for row in pages[CLASS_PAGES / "PhysicsServer2D.md"].splitlines()
                   if row.startswith("| [`") and "github.com/godotengine" in row]
    assert "../../classes/PhysicsServer.md" in class_rows["PhysicsServer2D"]
    assert len(server_rows) == 215
    assert {state: sum(f" | {state} | " in row for row in server_rows)
            for state in ("Implemented", "Partial", "Unimplemented", "Blocked")} == {
                "Implemented": 42, "Partial": 8, "Unimplemented": 160, "Blocked": 5}
    assert " | Implemented | " in next(row for row in server_rows if "method body_test_motion(" in row)
    assert " | Blocked | " in next(row for row in server_rows if "method area_set_collision_mask(" in row)
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("body_set_shape_disabled", "area_set_shape_disabled", "body_remove_shape", "area_remove_shape"))
    shape_node_rows = [row for row in pages[CLASS_PAGES / "CollisionShape2D.md"].splitlines()
                       if row.startswith("| [`") and "github.com/godotengine" in row]
    assert all(" | Implemented | " in row for row in shape_node_rows
               if "one_way_collision" in row and "margin" not in row)
    assert " | Implemented | " in next(row for row in shape_node_rows if "one_way_collision_margin" in row)
    physics_body_rows = pages[CLASS_PAGES / "PhysicsBody2D.md"].splitlines()
    assert " | Implemented | " in next(row for row in physics_body_rows
                                        if row.startswith("| [`method get_gravity("))
    assert all(" | Implemented | " in next(row for row in physics_body_rows
                                          if row.startswith(f"| [`method {name}("))
               for name in ("add_collision_exception_with", "get_collision_exceptions",
                            "remove_collision_exception_with"))
    assert all(" | Implemented | " in next(row for row in physics_body_rows
                                          if row.startswith(f"| [`method {name}("))
               for name in ("move_and_collide", "test_move"))
    for name, count, implemented, partial, blocked in (
        ("KinematicCollision2D", 14, 11, 3, 0),
        ("PhysicsTestMotionParameters2D", 8, 8, 0, 0),
        ("PhysicsTestMotionResult2D", 14, 12, 2, 0)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                if row.startswith("| [`") and "github.com/godotengine" in row]
        assert len(rows) == count
        assert {state: sum(f" | {state} | " in row for row in rows)
                for state in ("Implemented", "Partial", "Blocked")} == {
                    "Implemented": implemented, "Partial": partial, "Blocked": blocked}
    joint_rows = pages[CLASS_PAGES / "Joint2D.md"].splitlines()
    pin_rows = pages[CLASS_PAGES / "PinJoint2D.md"].splitlines()
    assert all(" | Blocked | " in next(row for row in joint_rows if row.startswith(f"| [`{prefix}"))
               for prefix in ("method get_rid()", "property float bias"))
    assert " | Blocked | " in next(row for row in pin_rows if row.startswith("| [`property float softness"))
    area_rows = [row for row in pages[CLASS_PAGES / "Area2D.md"].splitlines()
                 if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(area_rows) == 36
    assert {state: sum(f" | {state} | " in row for row in area_rows)
            for state in ("Implemented", "Partial", "Unimplemented", "Blocked")} == {
                "Implemented": 24, "Partial": 6, "Unimplemented": 0, "Blocked": 6}
    body_rows = pages[CLASS_PAGES / "RigidBody2D.md"]
    assert "../../classes/RigidBody.DampMode.md" in body_rows
    assert all(" | Implemented | " in next(row for row in body_rows.splitlines() if row.startswith(f"| [`{prefix}"))
               for prefix in ("method _integrate_forces", "property bool custom_integrator"))
    assert "| Implemented |" in next(row for row in body_rows.splitlines()
                                       if row.startswith("| [`property int linear_damp_mode"))
    for prefix, status in (("method get_colliding_bodies", "Partial"),
                           ("method get_contact_count", "Partial"),
                           ("property bool contact_monitor", "Implemented"),
                           ("property int max_contacts_reported", "Partial"),
                           ("signal body_entered", "Partial"),
                           ("signal body_exited", "Partial"),
                           ("signal body_shape_entered", "Blocked"),
                           ("signal body_shape_exited", "Blocked"),
                           ("signal sleeping_state_changed", "Implemented")):
        assert f" | {status} | " in next(row for row in body_rows.splitlines()
                                       if row.startswith(f"| [`{prefix}"))
    for prefix in ("method add_constant_central_force(", "method add_constant_force(",
                   "method add_constant_torque(", "method apply_force(", "method apply_impulse(",
                   "method apply_torque(", "method apply_torque_impulse(", "method set_axis_velocity(",
                   "property Vector2 constant_force", "property float constant_torque"):
        assert "| Implemented |" in next(row for row in body_rows.splitlines()
                                           if row.startswith(f"| [`{prefix}")), prefix
    loader_rows = [row for row in pages[CLASS_PAGES / "ResourceLoader.md"].splitlines()
                   if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(loader_rows) == 26
    assert {state: sum(f" | {state} | " in row for row in loader_rows)
            for state in ("Implemented", "Partial", "Blocked", "Unimplemented")} == {
                "Implemented": 8, "Partial": 4, "Blocked": 13, "Unimplemented": 1}
    for name, target in (("Shape2D", "Shape"), ("CircleShape2D", "CircleShape"),
                         ("CapsuleShape2D", "CapsuleShape"),
                         ("SegmentShape2D", "SegmentShape"),
                         ("SeparationRayShape2D", "SeparationRayShape"),
                         ("ConvexPolygonShape2D", "ConvexPolygonShape"),
                         ("ConcavePolygonShape2D", "ConcavePolygonShape"),
                         ("RectangleShape2D", "RectangleShape"), ("CollisionShape2D", "CollisionShape"),
                         ("CollisionObject2D", "CollisionObject"), ("PhysicsBody2D", "PhysicsBody"),
                         ("StaticBody2D", "StaticBody"), ("RigidBody2D", "RigidBody")):
        assert f"../../classes/{target}.md" in class_rows[name]
        assert (" | Implemented | " if name in {"CircleShape2D", "CapsuleShape2D", "SegmentShape2D", "ConvexPolygonShape2D", "ConcavePolygonShape2D", "RectangleShape2D"} else " | Partial | ") in class_rows[name]
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
