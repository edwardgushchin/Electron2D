#!/usr/bin/env python3
"""Regression check for overload pairing, texture pages and inventory accounting."""

import json
import re
from unittest.mock import Mock, patch

from render import ALIASES, CLASS_PAGES, DATA, cell, choose, coverage_target, engine_link, render, validate_public_type_names
from physics_report import scope


def check_physics_report(pages):
    upstream = json.loads((DATA / "godot-4.7.2.json").read_text())
    families, bases, cross = scope(upstream["types"])
    assert {"PhysicalBone2D", "WorldBoundaryShape2D", "PhysicsDirectBodyState2DExtension"} <= families
    assert {"Object", "RefCounted", "Resource", "Node", "CanvasItem", "Node2D", "MainLoop"} <= bases
    assert "TileMapLayer::method:get_coords_for_body_rid(RID:)" in cross["TileMapLayer"]
    assert "ProjectSettings::property:physics/2d/time_before_sleep" in cross["ProjectSettings"]
    assert "Engine::property:time_scale" in cross["Engine"]
    assert "TileMap" not in families | bases | cross.keys()
    # A newly pinned descendant must enter the audit without a hand-maintained
    # allowlist update, including one not directly beneath the root.
    extra = {"name": "FutureRigidBody", "id": "class:FutureRigidBody", "inherits": "RigidBody2D", "members": []}
    assert "FutureRigidBody" in scope(upstream["types"] + [extra])[0]
    report = pages[CLASS_PAGES.parent / "physics-status.md"]
    actual = re.findall(r"^\| \[`([^`]+)`\].*? \| (Partial|Blocked|Unimplemented) \|", report, re.MULTILINE)
    assert len(actual) == len(dict(actual)), "Inherited physics declarations were double counted"
    expected = {}
    for item in upstream["types"]:
        name = item["name"]
        if name not in families | bases | cross.keys():
            continue
        declarations = [item] + item["members"]
        table = [line for line in pages[CLASS_PAGES / coverage_target(name)].splitlines() if line.startswith("| [`")]
        assert len(table) == len(declarations)
        for declaration, row in zip(declarations, table):
            if name in cross and declaration["id"] not in cross[name]:
                continue
            state = row.split(" | ")[2]
            if "deprecated" in item.get("attributes", {}) or "deprecated" in declaration.get("attributes", {}):
                assert state == "Excluded", f"Deprecated physics/inherited API must not remain implementation work: {declaration['id']}"
                assert row.split(" | ")[1] == "—", f"Deprecated physics/inherited API must not map a public mirror: {declaration['id']}"
                message = declaration.get("attributes", {}).get("deprecated", item.get("attributes", {}).get("deprecated"))
                assert upstream["godot_commit"] in row and cell(f'deprecated="{message}"') in row, f"Deprecation exclusion needs exact pinned evidence: {declaration['id']}"
            if state in {"Partial", "Blocked", "Unimplemented"}:
                expected[declaration["id"]] = state
    assert dict(actual) == expected, "Physics ledger must retain every open row from the class classifier"
    object_page = pages[CLASS_PAGES / "Object.md"]
    projections = {"_iter_init": "GetEnumerator() and first MoveNext()", "_iter_next": "IEnumerator<T>.MoveNext()", "_iter_get": "IEnumerator<T>.Current"}
    for method, projection in projections.items():
        rows = [row for row in object_page.splitlines() if row.startswith("| [`method " + method + "(")]
        assert len(rows) == 1 and " | Implemented | " in rows[0] and "C# " + projection in rows[0], "Iteration must name its actual typed C# protocol"
        assert not any(key.startswith("Object::method:" + method + "(") for key in expected), "Verified iteration must not remain open"
    init_rows = [row for row in object_page.splitlines() if row.startswith("| [`method _init(")]
    assert len(init_rows) == 1 and " | Implemented | " in init_rows[0] and "compiled-c-initialization" in init_rows[0]
    assert "Object::method:_init()" not in expected, "Qualified typed constructor initialization must not remain open"
    source_rows = [row for row in object_page.splitlines() if row.startswith("| [`method get_script(")]
    assert len(source_rows) == 1 and " | Implemented | " in source_rows[0] and "public Electron2D.Script GetScript()" in source_rows[0]
    assert "Object::method:get_script()" not in expected, "Qualified source identity must not remain open"
    assert expected["Object::method:set_script(Variant:)"] == "Blocked", "Source observation must not close live reassignment"
    node_page = pages[CLASS_PAGES / "Node.md"]
    for method in ("call_thread_safe", "set_thread_safe", "notify_thread_safe",
                   "call_deferred_thread_group", "set_deferred_thread_group", "notify_deferred_thread_group"):
        rows = [row for row in node_page.splitlines() if row.startswith("| [`method " + method + "(")]
        assert len(rows) == 1 and " | Implemented | " in rows[0] and "scene-thread-messages.md" in rows[0]
        assert not any(key.startswith("Node::method:" + method + "(") for key in expected)
    assert expected["class:Node"] == "Partial", "Invocation completion must retain unrelated receiver gaps"
    rigid_rows = [row for row in pages[CLASS_PAGES / "RigidBody2D.md"].splitlines() if row.startswith("| [`")]
    assert len(rigid_rows) == 55 and all(" | Implemented | " in row for row in rigid_rows)
    assert "physics-rigid-body-contract.md" in rigid_rows[0]
    assert "class:RigidBody2D" not in expected, "The complete own-type audit retains inherited gaps on their declaring types"
    propagation = [row for row in node_page.splitlines() if row.startswith("| [`method propagate_notification(")]
    assert len(propagation) == 1 and " | Implemented | " in propagation[0] and "notification-propagation-contract" in propagation[0]
    assert "Node::method:propagate_notification(int:)" not in expected
    loop_page = pages[CLASS_PAGES / "MainLoop.md"]
    permission = [row for row in loop_page.splitlines() if row.startswith("| [`signal on_request_permissions_result(")]
    assert len(permission) == 1 and " | Implemented | " in permission[0] and "native-permission-results" in permission[0]
    assert "MainLoop::signal:on_request_permissions_result(String:,bool:)" not in expected
    loop_rows = [row for row in loop_page.splitlines() if row.startswith("| [`")]
    assert len(loop_rows) == 18 and all(" | Implemented | " in row for row in loop_rows)
    assert "whole-type-contract-audit" in loop_rows[0]
    assert "class:MainLoop" not in expected, "The complete own-type audit must retain Object gaps on their declaring type"
    notification = [row for row in object_page.splitlines() if row.startswith("| [`method notification(")]
    assert len(notification) == 1 and " | Implemented | " in notification[0] and "Notify(System.Int32 what, System.Boolean reversed)" in notification[0]
    assert "Object::method:notification(int:,bool:)" not in expected
    callback = [row for row in object_page.splitlines() if row.startswith("| [`method _notification(")]
    assert len(callback) == 1 and " | Implemented | " in callback[0] and "compiled-construction-notification-boundary" in callback[0]
    assert "Object::method:_notification(int:)" not in expected, "The compiled callback must not require a later native-only construction event"
    assert expected["class:Object"] == "Partial", "Directional dispatch does not complete the inherited object class"
    resource_page = pages[CLASS_PAGES / "Resource.md"]
    for method in ("copy_from_resource", "duplicate", "duplicate_deep"):
        rows = [row for row in resource_page.splitlines() if row.startswith("| [`method " + method + "(")]
        assert len(rows) == 1 and " | Implemented | " in rows[0] and "resources.md#copy-and-duplicate-contract" in rows[0]
        assert not any(key.startswith("Resource::method:" + method + "(") for key in expected)
    assert expected["class:Resource"] == "Partial", "Copy completion must retain inherited live-script gaps"
    canvas_page = pages[CLASS_PAGES / "CanvasItem.md"]
    for method in ("draw_arc", "draw_circle", "draw_dashed_line", "draw_ellipse", "draw_ellipse_arc",
                   "draw_line", "draw_multiline", "draw_multiline_colors", "draw_polyline", "draw_rect"):
        rows = [row for row in canvas_page.splitlines() if row.startswith("| [`method " + method + "(")]
        assert len(rows) == 1 and " | Implemented | " in rows[0] and "uniform-stroke-contract-2026-10-25" in rows[0]
        assert not any(key.startswith("CanvasItem::method:" + method + "(") for key in expected)
    for method in ("draw_polyline_colors", "draw_primitive"):
        rows = [row for row in canvas_page.splitlines() if row.startswith("| [`method " + method + "(")]
        assert len(rows) == 1 and " | Implemented | " in rows[0] and "native-attribute-composition-2026-10-26" in rows[0]
        assert not any(key.startswith("CanvasItem::method:" + method + "(") for key in expected)
    assert "class:CanvasItem" not in expected
    assert " | Implemented | " in next(row for row in canvas_page.splitlines() if row.startswith("| [`class CanvasItem`"))
    assert "docs/components/canvas-item-contract.md" in canvas_page
    server_page = pages[CLASS_PAGES / "RenderingServer.md"]
    for method in ("canvas_item_add_primitive", "canvas_item_add_polyline", "canvas_item_add_multiline"):
        rows = [row for row in server_page.splitlines() if row.startswith("| [`method " + method + "(")]
        assert len(rows) == 1 and " | Implemented | " in rows[0] and "native-attribute-composition-2026-10-26" in rows[0]
    for method in ("draw_texture", "draw_texture_rect", "draw_texture_rect_region"):
        rows = [row for row in canvas_page.splitlines() if row.startswith("| [`method " + method + "(")]
        assert len(rows) == 1 and " | Implemented | " in rows[0] and "texture-drawing-contract-2026-10-26" in rows[0]
        assert not any(key.startswith("CanvasItem::method:" + method + "(") for key in expected)
    texture_page = pages[CLASS_PAGES / "Texture.md"]
    for method in ("draw", "draw_rect", "draw_rect_region", "_draw", "_draw_rect", "_draw_rect_region"):
        rows = [row for row in texture_page.splitlines() if row.startswith("| [`method " + method + "(")]
        assert len(rows) == 1 and " | Implemented | " in rows[0] and "texture-drawing-contract-2026-10-26" in rows[0]
    texture_classes = [row for row in texture_page.splitlines() if row.startswith("| [`class ")]
    assert len(texture_classes) == 2 and all(" | Partial | " in row for row in texture_classes)
    assert "Joint2D::property:bias" not in expected
    assert "PinJoint2D::property:softness" not in expected
    assert "RigidBody2D::property:continuous_cd" not in expected
    assert "PhysicsServer2D::method:space_set_param(RID:,int:PhysicsServer2D.SpaceParameter,float:)" not in expected
    assert "PhysicsServer2D::enum:SpaceParameter" not in expected
    assert "StaticBody2D::property:constant_angular_velocity" not in expected


def check_public_type_names():
    def declaration(name):
        return {"kind": "type", "name": name, "id": f"T:Electron2D.{name}"}

    validate_public_type_names([declaration(name) for name in (
        "World", "PhysicsRayResult", "Curve2D", "AnimationNodeBlendSpace1D", "AnimationNodeBlendSpace2D",
        "CustomResource<T>", "Filter12DB")])
    invalid = [declaration(name) for name in (
        "World2D", "NewQueryResult2D", "NewResource2D<T>", "NewResource2D`1", "Texture2DArray",
        "NewNode3D", "NewType1D", "World2d")]
    invalid.append({"kind": "type", "name": "Curve2D", "id": "T:Electron2D.Other.Curve2D"})
    for item in invalid:
        try:
            validate_public_type_names([item])
        except ValueError as error:
            assert "ADR 0004" in str(error) and item["id"] in str(error)
        else:
            raise AssertionError(f"Unapproved dimensional type accepted: {item['id']}")
    with patch("render.ENGINE", Mock(read_text=lambda: json.dumps([declaration("NewQueryResult2D")]))):
        try:
            render()
        except ValueError as error:
            assert "ADR 0004" in str(error) and "NewQueryResult2D" in str(error)
        else:
            raise AssertionError("Coverage rendering bypassed the public type naming check")


def check_alignment_enum_owners():
    def declaration(name):
        return {"kind": "type", "name": name.rsplit(".", 1)[-1], "id": f"T:Electron2D.{name}"}

    valid = [declaration(name) for name in (
        "BoxContainer.AlignmentMode", "AspectRatioContainer.AlignmentMode", "FlowContainer.AlignmentMode",
        "TabBar.AlignmentMode", "FlowContainer.LastWrapAlignmentMode", "HorizontalAlignment",
        "VerticalAlignment", "InlineAlignment", "RenderingServer.ParticlesTransformAlign",
        "RenderingServer.ParticlesTransformAlignAxis", "RenderingServer.ParticlesTransformAlignCustomSrc")]
    validate_public_type_names(valid)
    for name in ("AlignmentMode", "Label.AlignmentMode", "BoxContainer.HorizontalAlignment",
                 "FlowContainer.VerticalAlignment", "LastWrapAlignmentMode", "BoxContainer.LastWrapAlignmentMode",
                 "Label.InlineAlignment", "ParticlesTransformAlign", "ParticlesTransformAlignAxis",
                 "ParticlesTransformAlignCustomSrc"):
        item = declaration(name)
        try:
            validate_public_type_names([item])
        except ValueError as error:
            assert "ADR 0051" in str(error) and item["id"] in str(error)
        else:
            raise AssertionError(f"Unapproved alignment enum owner accepted: {item['id']}")
    with patch("render.ENGINE", Mock(read_text=lambda: json.dumps([declaration("AlignmentMode")]))):
        try:
            render()
        except ValueError as error:
            assert "ADR 0051" in str(error) and "T:Electron2D.AlignmentMode" in str(error)
        else:
            raise AssertionError("Coverage rendering bypassed the alignment owner check")


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


def check_object_registry_exclusions(pages, upstream):
    owner = next(item for item in upstream["types"] if item["name"] == "Object")
    names = {"get_method_argument_count", "has_method", "get_incoming_connections",
             "get_signal_connection_list", "has_connections", "has_signal", "has_user_signal", "remove_user_signal"}
    members = [member for member in owner["members"]
               if (member["kind"] == "method" and member["name"] in names)
               or (member["kind"] == "enum" and member["name"] == "ConnectFlags")
               or (member["kind"] == "enum_value" and member["attributes"].get("enum") == "ConnectFlags")]
    assert len(members) == 14, "A changed pinned registry family needs a fresh ADR audit"
    rows = [line for line in pages[CLASS_PAGES / "Object.md"].splitlines() if line.startswith("| [`")]
    by_id = dict(zip([owner["id"]] + [member["id"] for member in owner["members"]], rows))
    for member in members:
        row = by_id[member["id"]]
        assert " | — | Excluded | " in row and "0001/0002/0010" in row, row
        assert "trigger:" not in row.lower(), row
    # Typed lifecycle, property, deferred and owned-connection capabilities remain applicable.
    for member in owner["members"]:
        if member["name"] in {"_init", "_iter_get", "_iter_init", "_iter_next", "get_script", "set_script",
                              "set_block_signals", "is_blocking_signals", "is_connected", "call_deferred",
                              "set_deferred", "_get_property_list", "get_property_list"}:
            assert " | Excluded | " not in by_id[member["id"]], by_id[member["id"]]


def main():
    check_public_type_names()
    check_alignment_enum_owners()
    upstream = json.loads((DATA / "godot-4.7.2.json").read_text())
    engine = json.loads((DATA / "electron2d.json").read_text())
    assert all("`" not in item["signature"] for item in engine if item["kind"] == "constructor"), "Constructor syntax must omit CLR generic arity"
    for arity, suffix in [(1, "Generic"), (2, "Generic2")]:
        key_type = next(item for item in engine if item["kind"] == "type" and item["declaringType"].startswith("Electron2D.AnimationMethodKey<") and item["declaringType"].count(",") + 1 == arity)
        assert f"/AnimationMethodKey.{suffix}.md)" in engine_link(key_type), "Generic arities need their actual member-reference page"
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
    check_object_registry_exclusions(pages, upstream)
    check_physics_report(pages)
    graph = pages[CLASS_PAGES / "VisualShader.md"]
    offset = next(line for line in graph.splitlines() if line.startswith("| [`property Vector2 graph_offset"))
    assert " | Excluded | " in offset and 'This property does nothing and always equals to zero.' in offset
    assert "ed1daf0bf001b61586d9930840f2f1394092c079" in offset
    assert " | Implemented | " in pages[CLASS_PAGES / "VisualShaderNodeFloatOp.md"]
    for name, count in [("VisualShaderNodeFloatFunc", 36), ("VisualShaderNodeVectorFunc", 37)]:
        rows = [row for row in pages[CLASS_PAGES / (name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
        assert "FunctionEnum" in pages[CLASS_PAGES / (name + ".md")]
    for name in ("VisualShaderNodeVectorCompose", "VisualShaderNodeVectorDecompose", "VisualShaderNodeDotProduct",
                 "VisualShaderNodeVectorDistance", "VisualShaderNodeVectorLen", "VisualShaderNodeVectorRefract"):
        rows = [row for row in pages[CLASS_PAGES / (name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == 1 and " | Implemented | " in rows[0]
    for name, count in [("VisualShaderNodeClamp", 10), ("VisualShaderNodeMultiplyAdd", 8),
                        ("VisualShaderNodeMix", 11), ("VisualShaderNodeStep", 11),
                        ("VisualShaderNodeSmoothStep", 11), ("VisualShaderNodeRemap", 11)]:
        rows = [row for row in pages[CLASS_PAGES / (name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
        assert ("OpTypeEnum" if name == "VisualShaderNodeClamp" else "VisualShaderFloatOpType" if name == "VisualShaderNodeMultiplyAdd" else "VisualShaderNumericOpType") in pages[CLASS_PAGES / (name + ".md")]
    for name, count in [("VisualShaderNodeIntOp", 16), ("VisualShaderNodeUIntOp", 16),
                        ("VisualShaderNodeIntFunc", 8), ("VisualShaderNodeUIntFunc", 6)]:
        rows = [row for row in pages[CLASS_PAGES / (name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
        assert ("VisualShaderIntegerOperator" if name.endswith("Op") else "FunctionEnum") in pages[CLASS_PAGES / (name + ".md")]
    rows = [row for row in pages[CLASS_PAGES / "VisualShaderNodeDerivativeFunc.md"].splitlines() if row.startswith("| [`")]
    assert len(rows) == 20 and all(" | Implemented | " in row for row in rows)
    assert "VisualShaderFloatOpType" in pages[CLASS_PAGES / "VisualShaderNodeDerivativeFunc.md"]
    for name, count, excluded in [("VisualShaderNodeTexture", 19, 3), ("VisualShaderNodeTextureParameter", 37, 2),
                                   ("VisualShaderNodeTexture2DParameter", 1, 0), ("VisualShaderNodeTexture2DArrayParameter", 1, 0),
                                   ("VisualShaderNodeSample3D", 6, 0), ("VisualShaderNodeTexture2DArray", 2, 0)]:
        rows = [row for row in pages[CLASS_PAGES / (name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and sum(" | Excluded | " in row for row in rows) == excluded
        assert sum(" | Implemented | " in row for row in rows) == count - excluded
        assert all("Trigger:" not in row for row in rows)
    port_rows = pages[CLASS_PAGES / "VisualShaderNode.md"].splitlines()
    assert " | Implemented | " in next(row for row in port_rows if row.startswith("| [`enum PortType`"))
    assert " | Implemented | " in next(row for row in port_rows if "PORT_TYPE_SAMPLER" in row)
    assert "VisualShaderNodeLayeredSample" in pages[CLASS_PAGES / "VisualShaderNodeSample3D.md"]
    assert "VisualShaderNodeImageParameter" in pages[CLASS_PAGES / "VisualShaderNodeTexture2DParameter.md"]
    assert "TextureFilterEnum" in pages[CLASS_PAGES / "VisualShaderNodeTextureParameter.md"]
    for name, count in [("VisualShaderNodeUVFunc", 6), ("VisualShaderNodeUVPolarCoord", 1),
                        ("VisualShaderNodeColorFunc", 10), ("VisualShaderNodeColorOp", 13),
                        ("VisualShaderNodeTextureSDF", 1), ("VisualShaderNodeTextureSDFNormal", 1), ("VisualShaderNodeSDFRaymarch", 1), ("VisualShaderNodeSDFToScreenUV", 1), ("VisualShaderNodeScreenUVToSDF", 1)]:
        rows = [row for row in pages[CLASS_PAGES / (name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
        assert all("Trigger:" not in row for row in rows)
    for name, count in [("VisualShaderNodeResizableBase",2),("VisualShaderNodeGroupBase",22),("VisualShaderNodeExpression",2),("VisualShaderNodeGlobalExpression",1)]:
        rows = [row for row in pages[CLASS_PAGES / (name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
    particle_rows = [row for row in pages[CLASS_PAGES / "VisualShaderNodeParticleEmitter.md"].splitlines() if row.startswith("| [`")]
    assert len(particle_rows) == 2 and sum(" | Excluded | " in row for row in particle_rows) == 1
    for particle_name in ("MeshEmitter", "Randomness", "Emit", "Output", "Accelerator", "BoxEmitter", "SphereEmitter", "RingEmitter", "ConeVelocity", "MultiplyByAxisAngle"):
        particle_rows = [row for row in pages[CLASS_PAGES / ("VisualShaderNodeParticle" + particle_name + ".md")].splitlines() if row.startswith("| [`")]
        assert particle_rows and all(" | Implemented | " in row for row in particle_rows)
    assert "ParticlesEmitFlags" in pages[CLASS_PAGES / "VisualShaderNodeParticleEmit.md"]
    assert "VisualShaderFloatOpType" in pages[CLASS_PAGES / "VisualShaderNodeParticleRandomness.md"]
    custom_rows = [row for row in pages[CLASS_PAGES / "VisualShaderNodeCustom.md"].splitlines() if row.startswith("| [`")]
    assert len(custom_rows) == 23 and all(" | Implemented | " in row for row in custom_rows)
    assert "VisualShaderPortDefault" in pages[CLASS_PAGES / "VisualShaderNodeCustom.md"]
    for varying_name in ("VisualShaderNodeVarying", "VisualShaderNodeVaryingGetter", "VisualShaderNodeVaryingSetter"):
        varying_rows = [row for row in pages[CLASS_PAGES / (varying_name + ".md")].splitlines() if row.startswith("| [`")]
        assert varying_rows and all(" | Implemented | " in row for row in varying_rows)
    assert "VisualShaderVaryingType" in pages[CLASS_PAGES / "VisualShaderNodeVarying.md"]
    light_varying = next(row for row in pages[CLASS_PAGES / "VisualShader.md"].splitlines() if "VARYING_MODE_FRAG_TO_LIGHT" in row)
    assert " | Implemented | " in light_varying and "FragmentToLight" in light_varying
    light_stage = next(row for row in pages[CLASS_PAGES / "VisualShader.md"].splitlines() if "TYPE_LIGHT [" in row)
    assert " | Implemented | " in light_stage and "Type Light = 2" in light_stage
    assert "OperatorEnum" in pages[CLASS_PAGES / "VisualShaderNodeFloatOp.md"]
    assert "OpTypeEnum" in pages[CLASS_PAGES / "VisualShaderNodeVectorBase.md"]
    engine_ids = {item["id"] for item in json.loads((DATA / "electron2d.json").read_text())}
    assert not engine_ids.intersection({
        "T:Electron2D.VisualShaderNodeMultiplyAdd.OpTypeEnum",
        "T:Electron2D.VisualShaderNodeIntOp.OperatorEnum", "T:Electron2D.VisualShaderNodeUIntOp.OperatorEnum",
        "T:Electron2D.VisualShaderNodeFloatOp.Operator", "T:Electron2D.VisualShaderNodeVectorOp.Operator",
        "T:Electron2D.VisualShaderNodeVectorBase.OpType", "property:Electron2D.VisualShaderNodeFloatOp.Operation()",
        "property:Electron2D.VisualShaderNodeVectorOp.Operation()", "property:Electron2D.VisualShaderNodeVectorBase.OperationType()"})
    assert " | Implemented | " in pages[CLASS_PAGES / "VisualShaderNodeParameter.md"]
    assert " | Implemented | " in pages[CLASS_PAGES / "VisualShaderNodeTransformOp.md"]

    check_texture_pages(pages, upstream)
    aliases = json.loads(ALIASES.read_text())
    assert aliases["classes"]["CSharpScript"] == "Script", "C# scripts must share the concrete Script identity"
    for name in ("Script", "CSharpScript"):
        content = pages[CLASS_PAGES / f"{name}.md"]
        assert "ADR 0091" in content
        item = next(item for item in upstream["types"] if item["name"] == name)
        rows = [line for line in content.splitlines() if line.startswith("| [`")]
        assert len(rows) == len(item["members"]) + 1, f"Lost scripting declarations: {name}"
        if not any(item["id"] == "T:Electron2D.Script" for item in engine):
            assert "one future concrete" in content
            assert all("| Blocked |" in row for row in rows), "Naming must not claim an implemented scripting resource"
    class_rows = {
        name: next(line for line in pages[CLASS_PAGES / coverage_target(name)].splitlines()
                   if line.startswith(f"| [`class {name}`]"))
        for name in ("AStar2D", "AStarGrid2D", "Area2D", "AnimatableBody2D", "CharacterBody2D", "Shape2D", "CircleShape2D", "CapsuleShape2D", "SegmentShape2D", "SeparationRayShape2D", "ConvexPolygonShape2D", "ConcavePolygonShape2D", "CollisionPolygon2D", "RectangleShape2D", "RayCast2D", "ShapeCast2D", "KinematicCollision2D", "PhysicsTestMotionParameters2D", "PhysicsTestMotionResult2D", "RID", "World2D", "PhysicsServer2D", "PhysicsRayQueryParameters2D", "PhysicsPointQueryParameters2D", "PhysicsDirectSpaceState2D", "PhysicsDirectBodyState2D",
                     "PhysicsDirectBodyState2DExtension", "CollisionShape2D", "CollisionObject2D", "PhysicsBody2D", "StaticBody2D", "RigidBody2D",
                     "AESContext", "InputEventMIDI", "Shortcut",
                     "Texture2DArray", "TextureLayered", "PlaceholderTexture2DArray", "WorkerThreadPool", "RenderingDevice", "FramebufferCacheRD", "BoxMesh",
                     "RefCounted", "Line2D", "NativeMenu", "GDScriptLanguageProtocol",
                     "EditorNode3DGizmo")
    }
    worker_rows = [row for row in pages[CLASS_PAGES / "WorkerThreadPool.md"].splitlines() if row.startswith("| [`")]
    assert len(worker_rows) == 10 and all(" | Implemented | " in row for row in worker_rows)
    assert "../../classes/WorkerThreadPool.md" in class_rows["WorkerThreadPool"]
    astar_rows = [row for row in pages[CLASS_PAGES / "AStar2D.md"].splitlines()
                  if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(astar_rows) == 28 and all(" | Implemented | " in row for row in astar_rows)
    assert "../../classes/AStar.md" in class_rows["AStar2D"]
    grid_rows = [row for row in pages[CLASS_PAGES / "AStarGrid2D.md"].splitlines()
                 if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(grid_rows) == 44 and all(" | Implemented | " in row for row in grid_rows)
    assert "../../classes/AStarGrid.md" in class_rows["AStarGrid2D"]
    assert "../../classes/Area.md" in class_rows["Area2D"] and " | Implemented | " in class_rows["Area2D"]
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
                "Implemented": 22, "Partial": 0, "Blocked": 0}
    assert "../../classes/RayCast.md" in class_rows["RayCast2D"]
    shape_cast_rows = [row for row in pages[CLASS_PAGES / "ShapeCast2D.md"].splitlines()
                       if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(shape_cast_rows) == 28
    assert {state: sum(f" | {state} | " in row for row in shape_cast_rows)
            for state in ("Implemented", "Partial", "Blocked")} == {
                "Implemented": 28, "Partial": 0, "Blocked": 0}
    assert "../../classes/ShapeCast.md" in class_rows["ShapeCast2D"]
    character_rows = [row for row in pages[CLASS_PAGES / "CharacterBody2D.md"].splitlines()
                      if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(character_rows) == 41
    assert {state: sum(f" | {state} | " in row for row in character_rows)
            for state in ("Implemented", "Partial", "Unimplemented")} == {
                "Implemented": 41, "Partial": 0, "Unimplemented": 0}
    assert "../../classes/CharacterBody.md" in class_rows["CharacterBody2D"]
    for name, target, count in (
            ("PhysicsDirectBodyState2D", "PhysicsDirectBodyState", 43),
            ("PhysicsDirectBodyState2DExtension", "PhysicsDirectBodyStateExtension", 49)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                if row.startswith("| [`") and "github.com/godotengine" in row]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
        assert f"../../classes/{target}.md" in class_rows[name]
        assert "physics-body-state-contract.md" in class_rows[name]
    shape_rows = [row for row in pages[CLASS_PAGES / "Shape2D.md"].splitlines()
                  if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(shape_rows) == 8
    assert {state: sum(f" | {state} | " in row for row in shape_rows)
            for state in ("Implemented", "Partial", "Blocked", "Unimplemented")} == {
                "Implemented": 8, "Partial": 0, "Blocked": 0, "Unimplemented": 0}
    separation_rows = [row for row in pages[CLASS_PAGES / "SeparationRayShape2D.md"].splitlines()
                       if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(separation_rows) == 3
    assert sum(" | Implemented | " in row for row in separation_rows) == 3
    assert " | Implemented | " in class_rows["SeparationRayShape2D"]
    assert "SeparationRayDynamicsTests" in class_rows["SeparationRayShape2D"]
    for name, count in (("RID", 11), ("PhysicsRayQueryParameters2D", 9)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                if row.startswith("| [`") and "github.com/godotengine" in row]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
    for source, target in (
            ("PhysicsPointQueryParameters2D", "PhysicsPointQueryParameters"),
            ("PhysicsRayQueryParameters2D", "PhysicsRayQueryParameters"),
            ("PhysicsShapeQueryParameters2D", "PhysicsShapeQueryParameters"),
            ("PhysicsTestMotionParameters2D", "PhysicsTestMotionParameters"),
            ("PhysicsTestMotionResult2D", "PhysicsTestMotionResult"),
            ("KinematicCollision2D", "KinematicCollision"),
            ("World2D", "World"),
    ):
        assert f"../../classes/{target}.md" in pages[CLASS_PAGES / f"{source}.md"]
    world_rows = pages[CLASS_PAGES / "World2D.md"]
    assert any(item["id"] == "method:Electron2D.CanvasItem.GetWorld()" for item in engine)
    assert not any(item["name"] == "GetWorld2D" for item in engine)
    assert "| Implemented |" in next(row for row in world_rows.splitlines()
                                       if row.startswith("| [`property RID space"))
    direct_rows = pages[CLASS_PAGES / "PhysicsDirectSpaceState2D.md"]
    assert "../../classes/PhysicsDirectSpaceState.md" in class_rows["PhysicsDirectSpaceState2D"]
    assert "| Implemented |" in next(row for row in direct_rows.splitlines()
                                       if row.startswith("| [`method intersect_ray("))
    assert "| Implemented |" in next(row for row in direct_rows.splitlines()
                                   if row.startswith("| [`method intersect_point("))
    assert all(" | Implemented | " in next(row for row in direct_rows.splitlines()
                                          if row.startswith(f"| [`method {name}("))
               for name in ("intersect_shape", "cast_motion", "collide_shape", "get_rest_info"))
    shape_query_rows = [row for row in pages[CLASS_PAGES / "PhysicsShapeQueryParameters2D.md"].splitlines()
                        if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(shape_query_rows) == 10 and all(" | Implemented | " in row for row in shape_query_rows)
    for name, count in (("Range", 18), ("TextureProgressBar", 32), ("BoxContainer", 9), ("HBoxContainer", 1), ("VBoxContainer", 1), ("GridContainer", 4), ("FlowContainer", 17), ("HFlowContainer", 1), ("VFlowContainer", 1)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert len(rows) == count
        assert all(" | Implemented | " in row for row in rows)
    for name, count in (("StyleBox", 18), ("StyleBoxTexture", 25), ("StyleBoxLine", 6), ("StyleBoxEmpty", 1), ("StyleBoxFlat", 34)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
    assert " | Implemented | " in next(row for row in pages[CLASS_PAGES / "CanvasItem.md"].splitlines()
                                       if row.startswith("| [`method draw_style_box("))
    corner_rows = [row for row in pages[CLASS_PAGES / "@GlobalScope.md"].splitlines()
                   if row.startswith("| [`") and ("enum Corner" in row or "[Corner]" in row)]
    assert len(corner_rows) == 5 and all(" | Implemented | " in row for row in corner_rows)
    flag_rows = [row for row in pages[CLASS_PAGES / "Control.md"].splitlines() if row.startswith("| [`") and ("enum SizeFlags" in row or "[SizeFlags]" in row or "size_flags_" in row)]
    assert len(flag_rows) == 11 and all(" | Implemented | " in row for row in flag_rows)
    container_rows = [row for row in pages[CLASS_PAGES / "Container.md"].splitlines() if row.startswith("| [`")]
    assert len(container_rows) == 12
    assert all(" | Implemented | " in row for row in container_rows)
    engine_rows = pages[CLASS_PAGES / "Engine.md"].splitlines()
    assert " | Implemented | " in next(row for row in engine_rows if "method is_editor_hint(" in row)
    for owner in ("AudioStreamPlayer", "AudioStreamPlayer2D"):
        rows = pages[CLASS_PAGES / (owner + ".md")].splitlines()
        assert " | Implemented | " in next(row for row in rows if "property bool autoplay " in row)
    sprite_frames = pages[CLASS_PAGES / "SpriteFrames.md"].splitlines()
    for name in ("get_animation_loop", "set_animation_loop"):
        row = next(row for row in sprite_frames if f"method {name}(" in row)
        assert " | — | Excluded | " in row and "deprecated" in row
    accessibility_rows = [row for row in pages[CLASS_PAGES / "AccessibilityServer.md"].splitlines() if row.startswith("| [`")]
    assert " | Partial | " in next(row for row in accessibility_rows if "class AccessibilityServer" in row)
    assert all(" | Implemented | " in row for row in accessibility_rows if any(name in row for name in ("method create_element(", "method get_window_root(", "method update_set_bounds(")))
    assert " | Partial | " in next(row for row in accessibility_rows if "method update_add_action(" in row)
    assert " | Implemented | " in next(row for row in accessibility_rows if "method update_set_num_value(" in row)
    text_names = ("create_sub_text_edit_elements", "update_set_value", "update_set_text_selection", "update_set_next_on_line", "update_set_previous_on_line", "update_set_text_align", "update_set_text_orientation", "update_set_text_decorations", "update_set_foreground_color", "update_set_background_color", "update_set_language", "update_set_placeholder")
    text_rows = [next(row for row in accessibility_rows if f"method {name}(" in row) for name in text_names]
    assert len(text_rows) == 12 and all(" | Implemented | " in row for row in text_rows)
    scroll_names = ("update_set_scroll_x", "update_set_scroll_y", "update_set_scroll_x_range", "update_set_scroll_y_range")
    scroll_rows = [next(row for row in accessibility_rows if f"method {name}(" in row) for name in scroll_names]
    assert len(scroll_rows) == 4 and all(" | Implemented | " in row for row in scroll_rows)
    collection_names = ('update_set_list_item_count', 'update_set_list_item_index', 'update_set_list_item_level', 'update_set_list_item_selected', 'update_set_list_item_expanded', 'update_set_list_orientation', 'update_set_table_row_count', 'update_set_table_column_count', 'update_set_table_row_index', 'update_set_table_column_index', 'update_set_table_cell_position', 'update_set_table_cell_span')
    collection_rows = [next(row for row in accessibility_rows if f"method {name}(" in row) for name in collection_names]
    assert len(collection_rows) == 12 and all(" | Implemented | " in row for row in collection_rows)
    assert all(" | Implemented | " in next(row for row in accessibility_rows if f"enum_value ACTION_{name} [AccessibilityAction]" in row) for name in ("COLLAPSE", "EXPAND"))
    live_names = ("update_set_live", "update_set_popup_type", "update_set_tooltip")
    assert all(" | Implemented | " in next(row for row in accessibility_rows if f"method {name}(" in row) for name in live_names)
    live_enums = ("AccessibilityLiveMode", "AccessibilityPopupType")
    for kind in live_enums:
        rows = [row for row in accessibility_rows if f"enum {kind}" in row or f"[{kind}]" in row]
        assert len(rows) == (4 if kind == "AccessibilityLiveMode" else 5) and all(" | Implemented | " in row for row in rows)
    assert all(" | Implemented | " in next(row for row in accessibility_rows if f"enum_value ACTION_{name} [AccessibilityAction]" in row) for name in ("SHOW_TOOLTIP", "HIDE_TOOLTIP", "SHOW_CONTEXT_MENU"))
    control_rows = pages[CLASS_PAGES / "Control.md"].splitlines()
    assert " | Implemented | " in next(row for row in control_rows if "property int accessibility_live " in row)
    for owner in ("LineEdit", "TextEdit"):
        rows = pages[CLASS_PAGES / (owner + ".md")].splitlines()
        for name in ("MENU_SUBMENU_TEXT_DIR", "MENU_SUBMENU_INSERT_UCC"):
            assert " | Implemented | " in next(row for row in rows if f"enum_value {name} [MenuItems]" in row)
        if owner == "LineEdit":
            assert all(" | Implemented | " in next(row for row in rows if f"method {name}(" in row) for name in ("get_menu", "is_menu_visible"))
            assert " | Implemented | " in next(row for row in rows if "property bool context_menu_enabled " in row)
    for name in ("update_set_shortcut", "update_set_classname", "update_set_role_description", "update_set_url"):
        assert " | Implemented | " in next(row for row in accessibility_rows if f"method {name}(" in row)
    for name in ("_accessibility_get_contextual_info", "_get_accessibility_container_name"):
        assert " | Implemented | " in next(row for row in control_rows if f"method {name}(" in row)
    assert " | Implemented | " in next(row for row in accessibility_rows if "enum AccessibilityAction" in row)
    assert " | Implemented | " in next(row for row in accessibility_rows if "enum_value ACTION_CUSTOM [AccessibilityAction]" in row)
    for name in ("update_add_custom_action", "update_set_extra_info"):
        assert " | Implemented | " in next(row for row in accessibility_rows if f"method {name}(" in row)
    assert all(" | Implemented | " in next(row for row in control_rows if f"method {name}(" in row) for name in ("accessibility_drag", "accessibility_drop"))
    project_rows = pages[CLASS_PAGES / "ProjectSettings.md"].splitlines()
    assert " | Implemented | " in next(row for row in project_rows if " input/ui_accessibility_drag_and_drop`" in row)
    patch_rows = [row for row in pages[CLASS_PAGES / "NinePatchRect.md"].splitlines() if row.startswith("| [`")]
    assert len(patch_rows) == 18 and all(" | Implemented | " in row for row in patch_rows)
    screen_rows = [row for row in pages[CLASS_PAGES / "VisibleOnScreenNotifier2D.md"].splitlines() if row.startswith("| [`")]
    assert len(screen_rows) == 6 and all(" | Implemented | " in row for row in screen_rows)
    enabler_rows = [row for row in pages[CLASS_PAGES / "VisibleOnScreenEnabler2D.md"].splitlines() if row.startswith("| [`")]
    assert len(enabler_rows) == 7 and all(" | Implemented | " in row for row in enabler_rows)
    modification_rows = [row for row in pages[CLASS_PAGES / "SkeletonModification2D.md"].splitlines()
                         if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(modification_rows) == 12 and all(" | Implemented | " in row for row in modification_rows)
    assert all("skeletal-editor-gizmos.md" in row for row in modification_rows
               if any(name in row for name in ("method _draw_editor_gizmo(", "method get_editor_draw_gizmo(",
                                               "method set_editor_draw_gizmo(", "class SkeletonModification2D")))
    server_rows = [row for row in pages[CLASS_PAGES / "PhysicsServer2D.md"].splitlines()
                   if row.startswith("| [`") and "github.com/godotengine" in row]
    assert "../../classes/PhysicsServer.md" in class_rows["PhysicsServer2D"]
    extension_shape_reads = [row for row in pages[CLASS_PAGES / "PhysicsServer2DExtension.md"].splitlines()
                             if any(f"method _{kind}_get_shape{suffix}(" in row
                                    for kind in ("body", "area") for suffix in ("", "_count", "_transform"))]
    assert len(extension_shape_reads) == 6 and all(" | Implemented | " in row for row in extension_shape_reads)
    indexed_names = [f"_{role}_{operation}" for role in ("body", "area")
                     for operation in ("set_shape", "set_shape_transform", "set_shape_disabled", "remove_shape", "clear_shapes")]
    indexed_rows = [row for row in pages[CLASS_PAGES / "PhysicsServer2DExtension.md"].splitlines()
                    if any(f"method {name}(" in row for name in indexed_names)]
    assert len(indexed_rows) == 10
    assert all(" | Implemented | " in row and "deferred scene preparation" in row and "24" in row
               and "accepted rigid-pose contract" in row for row in indexed_rows)
    metadata_names = [f"_{kind}_get_{member}" for kind in ("body", "area")
                      for member in ("space", "collision_layer", "collision_mask", "object_instance_id", "canvas_instance_id")]
    metadata_names.append("_body_get_collision_priority")
    extension_metadata_reads = [row for row in pages[CLASS_PAGES / "PhysicsServer2DExtension.md"].splitlines()
                               if any(f"method {name}(" in row for name in metadata_names)]
    assert len(extension_metadata_reads) == 11 and all(" | Implemented | " in row for row in extension_metadata_reads)
    publication_names = [f"_{kind}_attach_{association}_instance_id" for kind in ("body", "area") for association in ("object", "canvas")]
    publication_names.append("_body_set_collision_priority")
    extension_publications = [row for row in pages[CLASS_PAGES / "PhysicsServer2DExtension.md"].splitlines()
                              if any(f"method {name}(" in row for name in publication_names)]
    assert len(extension_publications) == 5 and all(" | Implemented | " in row for row in extension_publications)
    boundary_rows = [row for row in pages[CLASS_PAGES / "WorldBoundaryShape2D.md"].splitlines() if row.startswith("| [`")]
    assert len(boundary_rows) == 3 and all(" | Implemented | " in row for row in boundary_rows)
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("world_boundary_shape_create", "shape_get_type", "shape_get_data", "shape_set_data"))
    for setting in ("sleep_threshold_linear", "sleep_threshold_angular", "time_before_sleep", "solver/default_contact_bias", "solver/contact_max_allowed_penetration", "solver/solver_iterations", "solver/contact_recycle_radius", "solver/contact_max_separation"):
        assert " | Implemented | " in next(row for row in pages[CLASS_PAGES / "ProjectSettings.md"].splitlines()
                                            if row.startswith("| [`property") and f"physics/2d/{setting}" in row)
    performance_rows = pages[CLASS_PAGES / "Performance.md"].splitlines()
    assert all(" | Implemented | " in next(row for row in performance_rows if f"enum_value PHYSICS_2D_{name} " in row)
               for name in ("ACTIVE_OBJECTS", "COLLISION_PAIRS", "ISLAND_COUNT"))
    assert " | Partial | " in next(row for row in performance_rows if "method get_monitor(" in row)
    assert " | Partial | " in next(row for row in performance_rows if "class Performance" in row)
    assert len(server_rows) == 215
    assert {state: sum(f" | {state} | " in row for row in server_rows)
            for state in ("Implemented", "Partial", "Unimplemented", "Blocked", "Excluded")} == {
                "Implemented": 160, "Partial": 0, "Unimplemented": 0, "Blocked": 0, "Excluded": 55}
    extension_rows = [row for row in pages[CLASS_PAGES / "PhysicsServer2DExtension.md"].splitlines()
                      if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(extension_rows) == 141 and all(" | Implemented | " in row for row in extension_rows)
    assert "physics-server-contract.md" in server_rows[0] and "physics-server-contract.md" in extension_rows[0]
    for owner, prefix in (("PhysicsServer2D", ""), ("PhysicsServer2DExtension", "_")):
        rows = pages[CLASS_PAGES / (owner + ".md")].splitlines()
        row = next(row for row in rows if f"method {prefix}free_rid(" in row)
        assert " | Implemented | " in row and "exact original instance/type/throwing stack" in row
        for role in ("body", "area"):
            row = next(row for row in rows if f"method {prefix}{role}_add_shape(" in row)
            assert " | Implemented | " in row and "accepted rigid-pose contract" in row and "52 fresh public" in row
    for name in ("body_set_mode", "body_get_mode"):
        row = next(row for row in server_rows if f"method {name}(" in row)
        assert " | Implemented | " in row and "all five scene siblings" in row
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("body_get_collision_layer", "body_get_collision_mask", "body_set_collision_layer", "body_set_collision_mask"))
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("body_attach_canvas_instance_id", "body_get_canvas_instance_id",
                            "area_attach_canvas_instance_id", "area_get_canvas_instance_id"))
    point_rows = pages[CLASS_PAGES / "PhysicsPointQueryParameters2D.md"].splitlines()
    assert " | Implemented | " in next(row for row in point_rows if row.startswith("| [`property int canvas_instance_id"))
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("area_set_monitor_callback", "area_set_area_monitor_callback", "area_get_collision_layer", "area_get_collision_mask", "area_get_transform"))
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("body_get_shape", "body_get_shape_transform", "body_set_shape", "body_set_shape_transform",
                            "body_clear_shapes", "body_set_shape_as_one_way_collision", "area_get_shape",
                            "area_get_shape_transform", "area_set_shape", "area_set_shape_transform", "area_clear_shapes"))
    resource_rows = pages[CLASS_PAGES / "Resource.md"].splitlines()
    node_rows = pages[CLASS_PAGES / "Node.md"].splitlines()
    assert all(" | Implemented | " in next(row for row in node_rows if f"constant NOTIFICATION_EDITOR_{name}" in row)
               for name in ("PRE_SAVE", "POST_SAVE"))
    assert " | Implemented | " in next(row for row in resource_rows if row.startswith("| [`method get_rid("))
    assert " | Implemented | " in next(row for row in resource_rows if row.startswith("| [`method _get_rid("))
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("set_active", "space_set_active", "space_is_active"))
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("body_get_continuous_collision_detection_mode", "body_set_continuous_collision_detection_mode"))
    for owner in ("RigidBody2D", "PhysicsServer2D"):
        rows = pages[CLASS_PAGES / f"{owner}.md"].splitlines()
        ccd = next(row for row in rows if row.startswith("| [`enum CCDMode"))
        assert "public enum Electron2D.CCDMode" in ccd and " | Implemented | " in ccd
        assert "ELECTRON2D_TEST_GPU_CCD_CONTRACT" in ccd and "public CPU and independent GPU worlds" in ccd
    area_parameter_rows = [row for row in server_rows if "AreaParameter" in row.split(" | ")[0] and "method" not in row.split(" | ")[0]]
    assert len(area_parameter_rows) == 11 and all(" | Excluded | " in row for row in area_parameter_rows)
    assert all(" | Implemented | " in next(row for row in server_rows if f"method area_{action}_param(" in row)
               for action in ("set", "get"))
    area_mode_rows = [row for row in server_rows if "AreaSpaceOverrideMode" in row.split(" | ")[0]]
    assert len(area_mode_rows) == 6 and all(" | Implemented | " in row for row in area_mode_rows)
    force_names = ("apply_central_force", "apply_force", "apply_torque", "apply_central_impulse", "apply_impulse",
                   "apply_torque_impulse", "add_constant_central_force", "add_constant_force", "add_constant_torque",
                   "set_constant_force", "get_constant_force", "set_constant_torque", "get_constant_torque")
    assert all(" | Implemented | " in next(row for row in server_rows if f"method body_{name}(" in row)
               for name in force_names)
    parameter_rows = [row for row in server_rows if "BodyParameter" in row.split(" | ")[0] and "method" not in row.split(" | ")[0]]
    assert len(parameter_rows) == 12 and all(" | Excluded | " in row for row in parameter_rows)
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("body_set_state", "body_get_state", "body_set_axis_velocity"))
    assert " | Implemented | " in next(row for row in server_rows if "method body_reset_mass_properties(" in row)
    assert all(" | Implemented | " in next(row for row in server_rows if f"method body_{action}_param(" in row)
               for action in ("set", "get"))
    mass_rows = [row for row in pages[CLASS_PAGES / "RigidBody2D.md"].splitlines()
                 if row.startswith("| [`") and ("CenterOfMassMode" in row or "center_of_mass" in row or "property float inertia" in row)]
    assert len(mass_rows) == 6 and all(" | Implemented | " in row for row in mass_rows)
    assert " | Implemented | " in next(row for row in server_rows if "method body_test_motion(" in row)
    assert " | Implemented | " in next(row for row in server_rows if "method area_set_collision_mask(" in row)
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("body_set_shape_disabled", "area_set_shape_disabled", "body_remove_shape", "area_remove_shape"))
    collision_owner_rows = [row for row in pages[CLASS_PAGES / "CollisionObject2D.md"].splitlines()
                            if row.startswith("| [`") and "github.com/godotengine" in row and
                            ("shape_owner" in row or "shape_find_owner" in row)]
    assert len(collision_owner_rows) == 21
    assert all(" | Implemented | " in row for row in collision_owner_rows)
    disable_rows = [row for row in pages[CLASS_PAGES / "CollisionObject2D.md"].splitlines()
                    if row.startswith("| [`") and ("DisableMode" in row.split("|")[1] or "disable_mode" in row.split("|")[1])]
    assert len(disable_rows) == 5 and all(" | Implemented | " in row for row in disable_rows)
    assert " | Implemented | " in next(row for row in pages[CLASS_PAGES / "CollisionObject2D.md"].splitlines()
                                        if row.startswith("| [`property float collision_priority"))
    assert all(" | Implemented | " in next(row for row in server_rows if f"method {name}(" in row)
               for name in ("body_get_collision_priority", "body_set_collision_priority"))
    freeze_rows = [row for row in pages[CLASS_PAGES / "RigidBody2D.md"].splitlines()
                   if row.startswith("| [`") and ("FreezeMode" in row.split("|")[1] or "freeze_mode" in row.split("|")[1])]
    assert len(freeze_rows) == 4 and all(" | Implemented | " in row for row in freeze_rows)
    static_rows = pages[CLASS_PAGES / "StaticBody2D.md"].splitlines()
    assert all(" | Implemented | " in next(row for row in static_rows if f"property {typ} {name}" in row)
               for typ, name in (("Vector2", "constant_linear_velocity"), ("float", "constant_angular_velocity")))
    shape_node_rows = [row for row in pages[CLASS_PAGES / "CollisionShape2D.md"].splitlines()
                       if row.startswith("| [`") and "github.com/godotengine" in row]
    assert all(" | Implemented | " in row for row in shape_node_rows
               if "one_way_collision" in row and "margin" not in row)
    assert " | Implemented | " in next(row for row in shape_node_rows if "one_way_collision_margin" in row)
    assert all(" | Implemented | " in row for row in shape_node_rows)
    assert " | Implemented | " in next(row for row in pages[CLASS_PAGES / "SceneTree.md"].splitlines() if "property bool debug_collisions_hint" in row)
    for setting in ("shape_color", "draw_2d_outlines", "contact_color", "max_contacts_displayed"):
        assert " | Implemented | " in next(row for row in pages[CLASS_PAGES / "ProjectSettings.md"].splitlines() if f"debug/shapes/collision/{setting}" in row)
    picking_rows = pages[CLASS_PAGES / "CollisionObject2D.md"].splitlines()
    for name in ("_input_event", "_mouse_enter", "_mouse_exit", "_mouse_shape_enter", "_mouse_shape_exit"):
        assert " | Implemented | " in next(row for row in picking_rows if f"method {name}(" in row)
    for name in ("input_event", "mouse_entered", "mouse_exited", "mouse_shape_entered", "mouse_shape_exited"):
        assert " | Implemented | " in next(row for row in picking_rows if f"signal {name}(" in row)
    for name in ("physics_object_picking", "physics_object_picking_sort", "physics_object_picking_first_only"):
        assert " | Implemented | " in next(row for row in pages[CLASS_PAGES / "Viewport.md"].splitlines() if f"property bool {name} =" in row)
    physics_body_rows = pages[CLASS_PAGES / "PhysicsBody2D.md"].splitlines()
    assert " | Implemented | " in next(row for row in physics_body_rows if "property bool input_pickable = false" in row)
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
        ("KinematicCollision2D", 14, 14, 0, 0),
        ("PhysicsTestMotionParameters2D", 8, 8, 0, 0),
        ("PhysicsTestMotionResult2D", 14, 14, 0, 0)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                if row.startswith("| [`") and "github.com/godotengine" in row]
        assert len(rows) == count
        assert {state: sum(f" | {state} | " in row for row in rows)
                for state in ("Implemented", "Partial", "Blocked")} == {
                    "Implemented": implemented, "Partial": partial, "Blocked": blocked}
    joint_rows = pages[CLASS_PAGES / "Joint2D.md"].splitlines()
    pin_rows = pages[CLASS_PAGES / "PinJoint2D.md"].splitlines()
    assert " | Implemented | " in next(row for row in joint_rows if row.startswith("| [`method get_rid()"))
    assert " | Implemented | " in next(row for row in joint_rows if row.startswith("| [`property float bias"))
    server_joint_rows = pages[CLASS_PAGES / "PhysicsServer2D.md"].splitlines()
    for name, state in (("joint_create", "Implemented"), ("joint_clear", "Implemented"),
                        ("joint_make_pin", "Implemented"), ("joint_make_groove", "Implemented"),
                        ("joint_make_damped_spring", "Implemented"), ("joint_get_type", "Implemented"),
                        ("pin_joint_get_param", "Implemented"), ("pin_joint_set_param", "Implemented"),
                        ("pin_joint_get_flag", "Implemented"), ("pin_joint_set_flag", "Implemented"),
                        ("damped_spring_joint_get_param", "Implemented"), ("damped_spring_joint_set_param", "Implemented"),
                        ("joint_get_param", "Implemented"), ("joint_set_param", "Implemented"),
                        ("space_get_param", "Implemented"), ("space_set_param", "Implemented")):
        assert f" | {state} | " in next(row for row in server_joint_rows if row.startswith(f"| [`method {name}("))
    assert " | Excluded | " in next(row for row in server_joint_rows if row.startswith("| [`enum_value PIN_JOINT_SOFTNESS"))
    assert " | Implemented | " in next(row for row in pin_rows if row.startswith("| [`property float softness"))
    groove_rows = pages[CLASS_PAGES / "GrooveJoint2D.md"].splitlines()
    assert " | Implemented | " in next(row for row in groove_rows if row.startswith("| [`class GrooveJoint2D"))
    assert all(" | Implemented | " in next(row for row in groove_rows if row.startswith(f"| [`property float {name}"))
               for name in ("initial_offset", "length"))
    spring_rows = pages[CLASS_PAGES / "DampedSpringJoint2D.md"].splitlines()
    assert " | Implemented | " in next(row for row in spring_rows if row.startswith("| [`class DampedSpringJoint2D"))
    assert all(" | Implemented | " in next(row for row in spring_rows if row.startswith(f"| [`property float {name}"))
               for name in ("damping", "length", "rest_length", "stiffness"))
    area_rows = [row for row in pages[CLASS_PAGES / "Area2D.md"].splitlines()
                 if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(area_rows) == 36
    assert {state: sum(f" | {state} | " in row for row in area_rows)
            for state in ("Implemented", "Partial", "Unimplemented", "Blocked")} == {
                "Implemented": 36, "Partial": 0, "Unimplemented": 0, "Blocked": 0}
    listener_rows = pages[CLASS_PAGES / "AudioListener2D.md"]
    assert "Electron2D.AudioListener" in listener_rows
    assert all(" | Implemented | " in row for row in listener_rows.splitlines() if row.startswith("| [`"))
    spatial_rows = pages[CLASS_PAGES / "AudioStreamPlayer2D.md"]
    assert json.loads(ALIASES.read_text())["classes"]["AudioStreamPlayer2D"] == "AudioStreamEmitter"
    assert "Electron2D.AudioStreamEmitter" in spatial_rows
    assert "../../classes/AudioStreamEmitter.md" in spatial_rows
    assert "Electron2D.AudioStreamPlayer2D" not in spatial_rows
    assert " | Implemented | " in next(row for row in spatial_rows.splitlines() if row.startswith("| [`class AudioStreamPlayer2D"))
    assert all(" | Implemented | " in next(row for row in spatial_rows.splitlines() if row.startswith(f"| [`property float {name}"))
               for name in ("attenuation", "max_distance", "panning_strength"))
    viewport_rows = pages[CLASS_PAGES / "Viewport.md"]
    assert " | Implemented | " in next(row for row in viewport_rows.splitlines() if row.startswith("| [`method get_audio_listener_2d"))
    assert " | Excluded | " in next(row for row in viewport_rows.splitlines() if row.startswith("| [`property bool audio_listener_enable_3d"))
    body_rows = pages[CLASS_PAGES / "RigidBody2D.md"]
    assert "../../classes/RigidBody.DampMode.md" in body_rows
    assert all(" | Implemented | " in next(row for row in body_rows.splitlines() if row.startswith(f"| [`{prefix}"))
               for prefix in ("method _integrate_forces", "property bool custom_integrator"))
    assert "| Implemented |" in next(row for row in body_rows.splitlines()
                                       if row.startswith("| [`property int linear_damp_mode"))
    for prefix, status in (("method get_colliding_bodies", "Implemented"),
                           ("method get_contact_count", "Implemented"),
                           ("property bool contact_monitor", "Implemented"),
                           ("property int max_contacts_reported", "Implemented"),
                           ("signal body_entered", "Implemented"),
                           ("signal body_exited", "Implemented"),
                           ("signal body_shape_entered", "Implemented"),
                           ("signal body_shape_exited", "Implemented"),
                           ("signal sleeping_state_changed", "Implemented")):
        assert f" | {status} | " in next(row for row in body_rows.splitlines()
                                       if row.startswith(f"| [`{prefix}"))
    for prefix in ("method add_constant_central_force(", "method add_constant_force(",
                   "method add_constant_torque(", "method apply_force(", "method apply_impulse(",
                   "method apply_torque(", "method apply_torque_impulse(", "method set_axis_velocity(",
                   "property Vector2 constant_force", "property float constant_torque"):
        assert "| Implemented |" in next(row for row in body_rows.splitlines()
                                           if row.startswith(f"| [`{prefix}")), prefix
    for name in ("NavigationPolygon", "NavigationMeshSourceGeometryData2D", "NavigationRegion2D", "NavigationObstacle2D"):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert all(" | Implemented | " in row or " | Excluded | " in row for row in rows), name
    assert "NavigationMeshSourceGeometryData.md" in pages[CLASS_PAGES / "NavigationMeshSourceGeometryData2D.md"]
    bridge = next(row for row in pages[CLASS_PAGES / "NavigationPolygon.md"].splitlines() if row.startswith("| [`method get_navigation_mesh()"))
    assert " | Excluded | " in bridge and "NavigationServer3D" in bridge and "No implementation trigger" in bridge
    loader_rows = [row for row in pages[CLASS_PAGES / "ResourceLoader.md"].splitlines()
                   if row.startswith("| [`") and "github.com/godotengine" in row]
    assert len(loader_rows) == 26
    assert {state: sum(f" | {state} | " in row for row in loader_rows)
            for state in ("Implemented", "Partial", "Blocked", "Unimplemented")} == {
                "Implemented": 21, "Partial": 5, "Blocked": 0, "Unimplemented": 0}
    listing = next(row for row in loader_rows if "method list_directory(" in row)
    assert "ListDirectory(System.String directoryPath)" in listing and " | Partial | " in listing
    assert "import/export metadata resolver" in listing
    for row in loader_rows:
        if "load_threaded_" in row or "ThreadLoadStatus" in row:
            assert " | Implemented | " in row, row
    abort_row = next(row for row in loader_rows if "set_abort_on_missing_resources" in row)
    assert " | Implemented | " in abort_row and "version-4" in abort_row and "ordinary-texture" in abort_row
    for name in ("MissingResource", "PlaceholderMesh"):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert rows and all(" | Implemented | " in row for row in rows), name
    assert "Rect2" in next(row for row in pages[CLASS_PAGES / "PlaceholderMesh.md"].splitlines() if row.startswith("| [`property"))
    missing_node_rows = [row for row in pages[CLASS_PAGES / "MissingNode.md"].splitlines() if row.startswith("| [`")]
    assert len(missing_node_rows) == 5 and all(" | Implemented | " in row and "version-7" in row for row in missing_node_rows)
    missing_node_api = [item for item in engine if item["declaringType"] == "Electron2D.MissingNode"]
    assert missing_node_api and not any(item["kind"] == "constructor" for item in missing_node_api)
    resource_rows = pages[CLASS_PAGES / "Resource.md"].splitlines()
    for name in ("method setup_local_to_scene()", "signal setup_local_to_scene_requested()"):
        row = next(row for row in resource_rows if row.startswith(f"| [`{name}"))
        assert " | Excluded | " in row and "deprecated=" in row and "No implementation trigger" in row
    for item in engine:
        assert "Electron2D.Resource.SetupLocalToScene(" not in item["id"]
        assert "Electron2D.Resource.SetupLocalToSceneRequested(" not in item["id"]
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
        assert (" | Implemented | " if name in {"Shape2D", "CollisionObject2D", "CollisionShape2D", "CircleShape2D", "CapsuleShape2D", "SegmentShape2D", "ConvexPolygonShape2D", "ConcavePolygonShape2D", "RectangleShape2D", "StaticBody2D", "PhysicsBody2D", "RigidBody2D", "SeparationRayShape2D"} else " | Partial | ") in class_rows[name]
    for name, count in (("UPNP", 45), ("UPNPDevice", 22)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows), name
        assert "UPNPTests" in rows[0] and "read-only" in rows[0], name
    for name, count in (("ENetConnection", 36), ("ENetPacketPeer", 48), ("ENetMultiplayerPeer", 8)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows), name
        assert "ENetTests" in rows[0] and "DTLS" in rows[0], name
    for name, count in (("PacketPeerDTLS", 11), ("DTLSServer", 3)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows), name
        assert "DTLSTests" in rows[0] and "OpenSSL" in rows[0], name
    assert "Electron2D.TLSStatus" in pages[CLASS_PAGES / "PacketPeerDTLS.md"]
    crypto_rows = 0
    for name in ("Crypto", "HashingContext", "HMACContext", "AESContext"):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                if row.startswith("| [`")]
        assert rows and all(" | Implemented | " in row for row in rows), name
        assert "CryptoTests" in rows[0] and "BCL" in rows[0], name
        crypto_rows += len(rows)
    assert crypto_rows == 33
    assert "Electron2D.HashType" in pages[CLASS_PAGES / "HashingContext.md"]
    assert "Electron2D.AESMode" in pages[CLASS_PAGES / "AESContext.md"]
    for name, expected in (("MultiplayerAPI", {"Implemented": 23}),
                           ("MultiplayerAPIExtension", {"Implemented": 10}),
                           ("SceneMultiplayer", {"Implemented": 17, "Excluded": 1})):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                if row.startswith("| [`")]
        assert len(rows) == sum(expected.values()), name
        assert {state: sum(f" | {state} | " in row for row in rows)
                for state in expected} == expected, name
    for name, expected in (("MultiplayerSpawner", {"Implemented": 11}),
                           ("MultiplayerSynchronizer", {"Implemented": 19}),
                           ("SceneReplicationConfig", {"Implemented": 18})):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert len(rows) == sum(expected.values()), name
        assert {state: sum(f" | {state} | " in row for row in rows) for state in expected} == expected, name
    assert "ResourceArchiveTests" in pages[CLASS_PAGES / "MultiplayerSpawner.md"]
    for name, count in (("ResourceSaver", 16), ("ResourceFormatSaver", 6), ("ResourceFormatLoader", 18), ("ResourceUID", 14)):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows), name
        assert "ResourceArchiveTests" in rows[0] and "typed archive" in rows[0], name
    assert "Electron2D.RPCMode" in pages[CLASS_PAGES / "MultiplayerAPI.md"]
    assert "accepted MIDI-domain" in class_rows["InputEventMIDI"]
    button_rows = 0
    for name in ("BaseButton", "ButtonGroup", "Button", "CheckBox", "CheckButton", "TextureButton", "Shortcut", "InputEventShortcut"):
        rows = [row for row in pages[CLASS_PAGES / f"{name}.md"].splitlines() if row.startswith("| [`")]
        assert rows and all(" | Implemented | " in row for row in rows), name
        button_rows += len(rows)
    assert button_rows == 136
    for name in ("Texture2DArray", "ImageTextureLayered", "CompressedTexture2D", "CompressedTexture2DArray", "CompressedTextureLayered"):
        row = next(line for line in pages[CLASS_PAGES / coverage_target(name)].splitlines()
                   if line.startswith(f"| [`class {name}`]"))
        assert " | Implemented | " in row and "raw unsigned RGBA16" in row, name
    assert "Implemented" in class_rows["TextureLayered"] and "separate typed layered resources" in class_rows["TextureLayered"]
    assert "Implemented" in class_rows["PlaceholderTexture2DArray"]
    placeholder_rows = [row for row in pages[CLASS_PAGES / coverage_target("PlaceholderTexture2D")].splitlines() if row.startswith("| [`")]
    assert len(placeholder_rows) == 3 and all(" | Implemented | " in row for row in placeholder_rows)
    assert "../../classes/PlaceholderTexture.md" in placeholder_rows[0]
    factory_row = next(row for row in pages[CLASS_PAGES / "Texture.md"].splitlines() if row.startswith("| [`method create_placeholder("))
    assert " | Implemented | " in factory_row and "CreatePlaceholder" in factory_row
    assert " | Implemented | " in class_rows["Line2D"] and "../../classes/Line.md" in class_rows["Line2D"]
    line_rows = [row for row in pages[CLASS_PAGES / "Line2D.md"].splitlines() if row.startswith("| [`")]
    assert len(line_rows) == 33 and all(" | Implemented | " in row for row in line_rows)
    item_rows = [row for row in pages[CLASS_PAGES / "ItemList.md"].splitlines() if row.startswith("| [`")]
    item_partial = [row for row in item_rows if " | Partial | " in row]
    assert not item_partial
    assert all(" | Implemented | " in row for row in item_rows if "[`signal " in row or "[`class ItemList`]" in row)
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
    assert " | Implemented | " in next(line for line in packed.splitlines()
                                  if line.startswith("| [`method to_byte_array("))
    navigation_controls = pages[CLASS_PAGES / "NavigationServer2D.md"]
    for prefix in ("get_debug_enabled(", "set_debug_enabled(", "set_active(", "map_get_obstacles("):
        row = next(line for line in navigation_controls.splitlines() if line.startswith("| [`method " + prefix))
        assert " | Implemented | " in row and "NavigationServer.md" in row, prefix
    for name in ("navigation_debug_changed", "avoidance_debug_changed"):
        row = next(line for line in navigation_controls.splitlines() if line.startswith("| [`signal " + name))
        assert " | Implemented | " in row, name
    byte_array = pages[CLASS_PAGES / "PackedByteArray.md"]
    assert " | Implemented | " in next(line for line in byte_array.splitlines()
                                  if line.startswith("| [`method compress("))
    byte_utilities = [line for line in byte_array.splitlines() if " | Implemented | " in line]
    assert len(byte_utilities) == 43 and all("ByteBuffer.md" in line for line in byte_utilities)
    for name in packed_types:
        if name == "PackedByteArray":
            continue
        row = next(line for line in pages[CLASS_PAGES / f"{name}.md"].splitlines()
                   if line.startswith("| [`method to_byte_array("))
        assert " | Implemented | " in row and "ByteBuffer.md" in row, name
    assert " | Excluded | " in next(line for line in byte_array.splitlines()
                                   if line.startswith("| [`method decode_var("))
    assert " | Partial | " in class_rows["RenderingDevice"]
    assert " | Blocked | " in class_rows["FramebufferCacheRD"]
    for name in ("BoxMesh", "RefCounted", "GDScriptLanguageProtocol", "EditorNode3DGizmo",
                 "LightmapperRD", "RDAccelerationStructureGeometry", "RDAccelerationStructureInstance",
                 "RDHitGroup", "RenderDataRD", "RenderSceneBuffersRD", "RenderSceneDataRD",
                 "Texture3DRD", "TextureCubemapRD", "TextureCubemapArrayRD"):
        rows = [line for line in pages[CLASS_PAGES / f"{name}.md"].splitlines() if line.startswith("| [`")]
        assert rows and all(" | Excluded | " in line for line in rows), f"Excluded class has a blocked member: {name}"
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
    for name in ('texture_2d_create(', 'texture_2d_placeholder_create(', 'texture_set_size_override(', 'texture_get_path(', 'texture_set_path('):
        row, = [line for line in rendering_rows if line.startswith(f'| [`method {name}')]
        assert ' | Implemented | ' in row and 'RenderingTextureRIDTests' in row, row
    for name in ('texture_2d_get(', 'texture_2d_update(', 'texture_get_format(', 'texture_replace(', 'free_rid('):
        row, = [line for line in rendering_rows if line.startswith(f'| [`method {name}')]
        assert ' | Partial | ' in row and 'RenderingTextureRIDTests' in row, row
    device_factory = next(row for row in rendering_rows if "get_rendering_device(" in row)
    assert " | Partial | " in device_factory and "renderer-owned" in device_factory
    for resource, target in (("TextureRD", "TextureRD"), ("TextureLayeredRD", "TextureLayeredRD"), ("TextureArrayRD", "TextureArrayRD")):
        device_rows = pages[CLASS_PAGES / f"{resource}.md"].splitlines()
        declaration = next(row for row in device_rows if row.startswith("| [`class "))
        assert f"Electron2D.{target}" in declaration and " | Implemented | " in declaration
        assert "IntegerLightingTests" in declaration and "IntegerCanvasTests" in declaration
    inverse_image = next(row for row in rendering_rows if "method texture_get_rd_texture(" in row)
    assert " | Partial | " in inverse_image and "exports actual ordinary/array renderer uploads" in inverse_image
    assert "completed viewport native allocations" in inverse_image and "Partial only for distinct sRGB reinterpretation" in inverse_image

    for name in ('texture_proxy_create(', 'texture_proxy_update('):
        row, = [line for line in rendering_rows if line.startswith(f'| [`method {name}')]
        assert ' | Partial | ' in row and 'RenderingTextureProxyTests' in row and 'actual methods' in row, row
    for name, count in [("VisualShaderNodeCompare", 26), ("VisualShaderNodeSwitch", 12), ("VisualShaderNodeIf", 1), ("VisualShaderNodeIs", 6)]:
        rows = [line for line in pages[CLASS_PAGES / (name + ".md")].splitlines() if line.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
        if name in ("VisualShaderNodeCompare", "VisualShaderNodeSwitch"): assert "VisualShaderValueType" in pages[CLASS_PAGES / (name + ".md")]
    for name, count in [("TransformConstant", 2), ("TransformOp", 13), ("TransformFunc", 6), ("TransformVecMult", 8), ("TransformCompose", 1), ("TransformDecompose", 1), ("Determinant", 1), ("OuterProduct", 1)]:
        rows = [row for row in pages[CLASS_PAGES / ("VisualShaderNode" + name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
    for name, count in [("Parameter", 10), ("FloatParameter", 12), ("IntParameter", 14), ("UIntParameter", 3), ("BooleanParameter", 3), ("Vec2Parameter", 3), ("Vec3Parameter", 3), ("Vec4Parameter", 3), ("ColorParameter", 3), ("TransformParameter", 3), ("ParameterRef", 2)]:
        rows = [row for row in pages[CLASS_PAGES / ("VisualShaderNode" + name + ".md")].splitlines() if row.startswith("| [`")]
        assert len(rows) == count and all(" | Implemented | " in row for row in rows)
    hint_rows = [row for row in pages[CLASS_PAGES / "@GlobalScope.md"].splitlines() if "PROPERTY_HINT_" in row.split(" | ")[0]]
    assert len(hint_rows) == 45
    assert sum(" | Implemented | " in row for row in hint_rows) == 40
    assert sum(" | Excluded | " in row for row in hint_rows) == 5
    base_rows = pages[CLASS_PAGES / "VisualShaderNode.md"].splitlines()
    assert " | Implemented | " in next(row for row in base_rows if "PORT_TYPE_TRANSFORM [" in row)
    assert " | Implemented | " in next(row for row in base_rows if "PORT_TYPE_SAMPLER [" in row)
    assert not any(t["id"] in ("T:Electron2D.VisualShaderNodeCompare.ComparisonType", "T:Electron2D.VisualShaderNodeSwitch.OpTypeEnum") for t in engine)
    canvas_rows = pages[CLASS_PAGES / "CanvasItem.md"].splitlines()
    for name in ('draw_texture(', 'draw_texture_rect(', 'draw_texture_rect_region('):
        row, = [line for line in canvas_rows if line.startswith(f'| [`method {name}')]
        assert 'Electron2D.RID texture' in row, row
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
