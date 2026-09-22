#!/usr/bin/env python3
"""Render and verify the bidirectional class-reference coverage snapshot."""

import argparse
import json
import re
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
COVERAGE = ROOT / "docs/coverage"
DATA = COVERAGE / "data"
CLASS_PAGES = COVERAGE / "classes"
UPSTREAM = DATA / "godot-4.7.2.json"
ENGINE = DATA / "electron2d.json"
ALIASES = Path(__file__).with_name("type_aliases.json")
OVERRIDES = [Path(__file__).with_name(f"overrides_{family}.json") for family in ("math", "scene", "core", "display")]
COMMIT = "ed1daf0bf001b61586d9930840f2f1394092c079"
TEXTURE_NAMES = {
    "Texture2D": "Texture",
    "Texture2DArray": "TextureArray",
    "Texture2DArrayRD": "TextureArrayRD",
    "Texture2DRD": "TextureRD",
}


def coverage_target(name):
    page = TEXTURE_NAMES.get(name, name)
    anchor = f"#godot-{name.lower()}" if page == "Texture" else ""
    return f"{page}.md{anchor}"


def ident(value):
    return re.sub(r"[^a-z0-9]", "", value.lower())


def cell(value):
    return str(value).replace("|", "\\|").replace("\n", " ")


def code(value):
    return f"`{cell(value)}`"


def engine_name(value):
    return value if value.startswith("Electron2D.") else f"Electron2D.{value}"


def engine_link(entry, from_class=True):
    name = entry["declaringType"].removeprefix("Electron2D.")
    page = ROOT / "docs/classes" / f"{name}.md"
    if not page.exists() and "<" in name:
        page = ROOT / "docs/classes" / f"{name.split('<', 1)[0]}.Generic.md"
    if page.exists():
        prefix = "../../classes" if from_class else "../classes"
        return f"[{code(entry['signature'])}]({prefix}/{page.name})"
    return code(entry["signature"])


def integer(value):
    try:
        return int(str(value).removesuffix("U").removesuffix("L"), 0)
    except (ValueError, TypeError):
        return None


def comparable_type(value):
    """Compare unambiguous scalar and 2D value parameters before pairing overloads."""
    names = {
        "float": "float", "systemsingle": "float", "systemdouble": "float",
        "int": "int", "systemint32": "int", "systemint64": "int",
        "bool": "bool", "systemboolean": "bool",
        "string": "string", "systemstring": "string",
        "vector2": "vector2", "vector2i": "vector2i",
        "vector4": "vector4", "vector4i": "vector4i",
        "rect2": "rect", "rect2i": "recti", "rect": "rect", "recti": "recti",
        "transform2d": "transform", "transform": "transform", "color": "color",
    }
    return names.get(ident(value.removeprefix("Electron2D.")))


def return_note(member, matches):
    if member["kind"] not in {"method", "operator"}:
        return None
    upstream = member["attributes"].get("return", {}).get("type", "void")
    differences = []
    for match in matches:
        if match["kind"] == "event":
            continue
        local = match.get("returnType")
        if local is None:
            continue
        if (upstream == "void") != (local == "System.Void") or (
                upstream == "int" and local in {"System.UInt32", "System.UInt64"}):
            differences.append(f"{upstream} → {local}")
    return "Return type differs: " + ", ".join(sorted(set(differences))) + "; audit observable contract." if differences else None


def reason_for_type(item, lookup):
    name = item["name"]
    ancestors = [name]
    parent = item["inherits"]
    while parent and parent in lookup and parent not in ancestors:
        ancestors.append(parent)
        parent = lookup[parent]["inherits"]
    lineage = " ".join(ancestors)
    if name in {"Variant", "Callable", "Signal", "ClassDB", "Array", "Dictionary", "String", "bool", "float", "int"} or name.startswith("Packed") and name.endswith("Array"):
        return "Excluded", "Engine-owned dynamic/untyped primitive or collection is replaced by C# types and typed contracts (ADR 0001/0002); no engine-owned duplicate."
    if name == "RefCounted":
        return "Excluded", "Public reference-count lifetime is excluded by ADR 0013; Resource uses managed ownership."
    if name in {"NodePath", "StringName"}:
        return "Excluded", "Separate path/name wrapper is excluded by the string-based Node and group contract (ADRs 0008 and 0001); use string."
    if name.startswith("RD") or name.startswith("UniformSetCacheRD"):
        return "Excluded", "Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028)."
    # Mesh is shared by MeshInstance2D; exclude individual 3D members, not its whole family.
    three_d_roots = {"NavigationMesh", "Environment", "Compositor", "CameraAttributes", "Cubemap", "Sky", "SkyMaterial", "LightmapGIData", "Lightmapper", "BoneMap", "Texture3D"}
    if (re.search(r"(^|[^A-Za-z0-9])3D([^A-Za-z0-9]|$)", lineage)
            or any(part.endswith("3D") for part in ancestors)
            or any(part in three_d_roots for part in ancestors)
            or name in {"AABB", "Basis", "Plane", "Projection", "Quaternion", "Vector3", "Vector3i", "Transform3D", "SkeletonProfile", "SkeletonProfileHumanoid", "Skin", "SkinReference", "MobileVRInterface", "WebXRInterface"}
            or name.startswith(("OpenXR", "XR", "Skeleton3D", "BoneAttachment3D", "Node3DGizmo", "GLTF", "FBX", "Lightmap", "Voxel", "FogVolume"))):
        return "Excluded", "3D/XR product scope is excluded by ADR 0004; no implementation trigger."
    if name == "@GlobalScope":
        return "Partial", "Global functions/constants/enums are distributed across typed C# declarations; audit each row."
    if item.get("api_type") == "editor" or name.startswith("Editor") or "Editor" in ancestors:
        return "Blocked", "Trigger: first self-hosted editor executable slice under ADR 0027."
    if name in {"@GDScript", "GDScript", "GDScriptFunctionState"}:
        return "Excluded", "GDScript runtime is outside the typed C# contract (ADR 0001)."
    accepted_slices = (
        ({"BitMap", "Color", "Geometry2D", "RandomNumberGenerator", "Rect2", "Rect2i", "Transform2D", "Vector2", "Vector2i", "Vector4", "Vector4i"},
         "first complete typed 2D math or geometry slice (ADRs 0024, 0032, 0033 and 0035)"),
        ({"Engine", "MainLoop", "Node", "Object"},
         "next core-object and scene API slice (ADRs 0001, 0005, 0008, 0015 and 0016)"),
        ({"Input", "ProjectSettings"},
         "next typed input or project-settings API slice (ADRs 0019 and 0038)"),
        ({"Timer", "Tween", "Tweener", "AwaitTweener", "CallbackTweener", "IntervalTweener", "MethodTweener", "PropertyTweener", "SubtweenTweener"},
         "next typed scene timing or tween slice (ADRs 0036 and 0037)"),
        ({"ConfigFile", "DirAccess", "FileAccess", "JSON", "XMLParser", "RegEx", "RegExMatch"},
         "next typed configuration, file or text-utility slice (ADRs 0018, 0020 and 0022)"),
        ({"Resource", "PackedScene", "SceneState", "Translation", "TranslationDomain", "TranslationServer", "OptimizedTranslation"},
         "next resource, packed-scene or localization slice (ADRs 0007, 0013 and 0023)"),
        ({"Image"}, "next managed image-buffer slice (ADR 0039)"),
        ({"Curve", "Curve2D", "FastNoiseLite", "Noise"},
         "first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013)"),
    )
    for names, trigger in accepted_slices:
        if name in names:
            return "Unimplemented", f"Accepted 2D capability; trigger: {trigger}."
    blocked_slices = (
        ({"ResourceFormatLoader", "ResourceFormatSaver", "ResourceImporter", "ResourcePreloader", "ResourceUID", "MissingResource", "MissingNode", "InstancePlaceholder", "PCKPacker", "ZIPReader", "ZIPPacker"},
         "first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023)"),
        ({"World2D", "WorldEnvironment"},
         "first 2D world/render-environment integration slice after SDL3 GPU rendering (ADRs 0008 and 0028)"),
        ({"Mesh", "ImporterMesh", "MeshConvexDecompositionSettings", "MeshDataTool", "MeshLibrary", "MultiMesh", "SurfaceTool", "TriangleMesh"},
         "first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028)"),
        ({"CompositorEffect", "RenderData", "RenderDataExtension", "RenderDataRD", "RenderSceneBuffers", "RenderSceneBuffersConfiguration", "RenderSceneBuffersExtension", "RenderSceneBuffersRD", "RenderSceneData", "RenderSceneDataExtension", "RenderSceneDataRD", "FramebufferCacheRD"},
         "a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028"),
        ({"CodeHighlighter", "SyntaxHighlighter", "UndoRedo", "FoldableGroup", "ColorPalette"},
         "first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028)"),
        ({"CharFXTransform"}, "first typed rich-text effect slice after 2D GUI and text rendering (ADR 0028)"),
        ({"SkeletonModification2D", "SkeletonModification2DCCDIK", "SkeletonModification2DFABRIK", "SkeletonModification2DJiggle", "SkeletonModification2DLookAt", "SkeletonModification2DPhysicalBones", "SkeletonModification2DStackHolder", "SkeletonModification2DTwoBoneIK", "SkeletonModificationStack2D"},
         "first 2D skeletal animation and inverse-kinematics slice"),
        ({"CSharpScript", "Script", "ScriptBacktrace", "ScriptLanguage", "ScriptLanguageExtension", "Expression", "GDExtension", "GDExtensionManager", "GodotInstance"},
         "an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001)"),
        ({"OS", "Time", "Performance", "EngineDebugger", "EngineProfiler", "Logger", "MovieWriter", "StatusIndicator"},
         "first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021)"),
        ({"JavaClass", "JavaClassWrapper", "JavaObject", "JavaScriptBridge", "JavaScriptObject", "JNISingleton"},
         "first Android or Web host-interoperability slice after the portable SDL host (ADR 0021)"),
        ({"SceneReplicationConfig"}, "first typed multiplayer replication slice after scene persistence (ADR 0023)"),
        ({"IP", "JSONRPC"}, "first typed networking, address-resolution and RPC slice"),
        ({"PolygonPathFinder"}, "first typed 2D navigation and pathfinding slice"),
        ({"OggPacketSequence", "OggPacketSequencePlayback"}, "first audio decoding and playback slice"),
        ({"Crypto", "CryptoKey", "X509Certificate"}, "first typed networking-security integration slice with a portable crypto backend (ADR 0021)"),
        ({"RID"}, "first backend-neutral 2D renderer resource-identity and lifetime slice (ADR 0028)"),
        ({"Thread", "Mutex", "Semaphore", "WorkerThreadPool"},
         "a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021)"),
        ({"WeakRef"},
         "an accepted public weak-reference contract beyond System.WeakReference<T>; Resource currently uses only an internal weak path cache (ADR 0013)"),
    )
    for names, trigger in blocked_slices:
        if name in names:
            return "Blocked", f"Trigger: {trigger}."
    families = (
        ("Physics2D", r"Physics|Collision|RigidBody2D|StaticBody2D|CharacterBody2D|Area2D|Joint2D|RayCast2D|ShapeCast2D|Shape2D|SpringArm2D", "first Box2D.NET-backed 2D physics slice (ADR 0012)"),
        ("GUI", r"Control|Button|Label|Text|Box|Container|Theme|Popup|Window|Scroll|LineEdit|Tab|Tree|Grid|Panel|Separator|GraphEdit|ColorPicker", "first typed 2D GUI and theme slice after rendering (ADR 0028)"),
        ("Audio", r"Audio|Sound|Microphone", "first audio mixing and playback slice"),
        ("Navigation2D", r"Navigation|AStar2D|PathFollow2D|Path2D", "first 2D navigation slice"),
        ("Animation", r"Animation|Skeleton2D|Bone2D", "first scene animation slice"),
        ("Tiles", r"Tile|Atlas", "first tile and atlas resource slice after 2D rendering"),
        ("Networking", r"Multiplayer|PacketPeer|ENet|WebRTC|WebSocket|HTTP|TLS|DTLS|TCP|UDP|IP$|SocketServer|StreamPeer|UDSServer|UPNP", "first networking and multiplayer slice"),
        ("Assets", r"ResourceLoader|ResourceSaver|CompressedTexture|StreamTexture|ImageTexture|Font|Video|PackedData|ImageFormatLoader|GLTF|FBX", "first concrete loader and native-backed asset slice (ADR 0013/0023)"),
        ("InputHost", r"InputEvent|InputMap|Input$|Shortcut|Joypad|TouchScreen|Sensor", "first SDL input-host integration slice (ADR 0038)"),
        ("Host", r"DisplayServer|OS$|Time$|NativeMenu|CameraServer|CameraFeed|AccessibilityServer", "first SDL-backed platform host and display slice (ADR 0021/0038)"),
        ("Rendering2D", r"Canvas|Sprite|Texture|Shader|Material|Light2D|Polygon2D|Viewport|Camera2D|Parallax|Rendering|Gradient|Particle|Occluder|Mesh", "first SDL3 GPU 2D rendering slice (ADR 0028)"),
    )
    for domain, pattern, trigger in families:
        if re.search(pattern, lineage, re.IGNORECASE):
            return "Blocked", f"{domain}: trigger is the {trigger}."
    return "Blocked", f"Trigger: explicit product decision to admit the {name} API family, then its first complete vertical slice."


def special_reason(item, member):
    name = member["name"].lower()
    signature = member["signature"]
    if item["name"] == "ProjectSettings" and member["kind"] == "property":
        root = name.split("/", 1)[0]
        if re.search(r"(?:^|[/_])3d(?:$|[/_])", name) or root in {"xr", "collada"}:
            return "Excluded", "3D/XR or 3D asset-setting family is outside the 2D product (ADR 0004)."
        settings = {
            "rendering": "first SDL3 GPU 2D renderer and typed rendering-settings slice (ADR 0028)",
            "layer_names": "first typed 2D rendering/physics layer registry after those domains exist (ADRs 0012 and 0028)",
            "debug": "first host diagnostics and typed debug-settings slice (ADRs 0015 and 0016)",
            "input": "first persistent InputMap action schema and host input integration (ADR 0038)",
            "physics": "first Box2D.NET 2D physics and typed physics-settings slice (ADR 0012)",
            "display": "first SDL display/window host and typed display-settings slice (ADR 0021)",
            "application": "first application host and typed application-settings slice (ADRs 0015 and 0019)",
            "navigation": "first 2D navigation and typed navigation-settings slice",
            "editor": "first self-hosted editor and editor-settings slice (ADR 0027)",
            "gui": "first typed 2D GUI and theme-settings slice after rendering (ADR 0028)",
            "audio": "first audio mixer and typed audio-settings slice",
            "input_devices": "first SDL input-device host integration (ADR 0038)",
            "network": "first networking and multiplayer settings slice",
            "filesystem": "first asset loader, pack, and file-format settings slice (ADRs 0013 and 0023)",
            "compression": "first concrete compression-codec integration and settings slice (ADR 0020)",
            "animation": "first 2D animation and typed animation-settings slice",
            "accessibility": "first typed GUI accessibility integration and settings slice",
            "dotnet": "first self-hosted editor C# scripting workflow (ADR 0027)",
            "threading": "an accepted portable engine job-system contract and settings slice (ADR 0021)",
            "memory": "an accepted runtime memory-settings contract after measured need (ADR 0014)",
        }
        if root == "internationalization":
            return "Unimplemented", "Trigger: next typed localization-settings slice in the existing TranslationServer domain (ADR 0007)."
        if root == "physics" and name.startswith("physics/common/"):
            return "Unimplemented", "Trigger: next core timing-settings slice in ProjectSettings (ADRs 0016 and 0019)."
        if root in settings:
            return "Blocked", f"Trigger: {settings[root]}."
        raise ValueError(f"Unclassified ProjectSettings family: {root}")
    if re.search(r"\b(Vector3i?|Transform3D|Basis|Quaternion|AABB|Projection|Plane|[A-Za-z]+3D)\b", signature):
        return "Excluded", "3D-only signature is outside ADR 0004; no implementation trigger."
    if member["kind"] == "annotation":
        return "Excluded", "GDScript annotation syntax is outside typed C# (ADR 0001)."
    if member["kind"] == "theme_item":
        return "Blocked", "Trigger: first typed 2D GUI and theme slice after rendering (ADR 0028)."
    if item["name"] == "@GlobalScope" and re.fullmatch(r"U?INT(8|16|32|64)_(MIN|MAX)", member["name"]):
        return "Excluded", "Typed C# uses the corresponding BCL integral MinValue/MaxValue; no engine-owned duplicate (ADR 0001)."
    if name in {"get", "set", "call", "callv", "get_meta", "set_meta", "remove_meta", "has_meta", "get_meta_list", "connect", "disconnect", "emit_signal", "add_user_signal", "get_signal_list", "get_method_list", "get_property_list"}:
        return "Excluded", "Dynamic name/Variant/Callable API is replaced by typed C# contracts (ADRs 0001 and 0002)."
    if any(token in name for token in ("rpc", "multiplayer", "network_peer")):
        return "Blocked", "Trigger: first typed networking and multiplayer slice."
    if any(token in name for token in ("accessibility", "theme_", "tooltip", "gui_")):
        return "Blocked", "Trigger: first typed 2D GUI and accessibility slice after rendering."
    if any(token in name for token in ("draw_", "canvas_", "texture_", "shader_", "render_")):
        return "Blocked", "Trigger: first SDL3 GPU 2D rendering slice (ADR 0028)."
    return None


def choose(godot_type, member, candidates, used):
    kind = member["kind"]
    allowed = {
        "constructor": {"constructor"}, "method": {"method"},
        "property": {"property", "field"}, "signal": {"event"},
        "constant": {"constant", "field", "property"},
        "enum": {"type"}, "enum_value": {"enumValue"},
        "operator": {"operator"},
    }.get(kind, set())
    if not allowed:
        return None
    name = ident(member["name"])
    getter = kind == "method" and member["name"].startswith("get_") and not member["attributes"].get("params")
    if getter:
        allowed.update({"property", "field"})
        name = ident(member["name"][4:])
    if kind == "constructor":
        name = "ctor"
    if kind == "operator":
        ops = {"+": "opaddition", "-": "opsubtraction", "*": "opmultiply", "/": "opdivision", "%": "opmodulus", "==": "opequality", "!=": "opinequality", "<": "oplessthan", ">": "opgreaterthan", "<=": "oplessthanorequal", ">=": "opgreaterthanorequal", "unary-": "opunarynegation"}
        name = ops.get(member["name"].replace("operator ", ""), name)
    if kind == "enum_value":
        enum_name = ident(member["attributes"].get("enum", "").split(".")[-1])
        if name.startswith(enum_name):
            name = name[len(enum_name):]
    matches = []
    for candidate in candidates:
        if candidate["id"] in used or candidate["kind"] not in allowed:
            continue
        candidate_name = ident(candidate["name"])
        if kind == "constructor":
            candidate_name = "ctor"
        if kind == "enum":
            candidate_name = ident(candidate["name"].split(".")[-1])
        if kind == "enum_value" and candidate_name.startswith("k") and candidate_name[1:] == name:
            candidate_name = candidate_name[1:]
        if kind == "enum_value" and candidate_name != name:
            raw = member["attributes"].get("value", "")
            try:
                same_value = int(raw, 0) == int(str(candidate.get("value", "")), 0)
            except ValueError:
                same_value = False
            if not (len(candidate_name) > 1 and name.endswith(candidate_name) and same_value):
                continue
        elif candidate_name != name:
            continue
        if kind in {"method", "constructor", "operator"}:
            if candidate["kind"] in {"property", "field"}:
                pass
            else:
                upstream_params = sorted(member["attributes"].get("params", []), key=lambda param: int(param["index"]))
                local_params = candidate["parameters"] or []
                if len(local_params) != len(upstream_params) + (kind == "operator"):
                    continue
                if kind == "operator":
                    local_params = local_params[1:]
                if any(comparable_type(upstream["type"]) and comparable_type(local["type"])
                       and comparable_type(upstream["type"]) != comparable_type(local["type"])
                       for upstream, local in zip(upstream_params, local_params)):
                    continue
        matches.append(candidate)
    if len(matches) == 1:
        return matches
    if kind == "method" and matches and any(param["type"] == "Variant" for param in member["attributes"].get("params", [])):
        return matches
    return []


def render():
    upstream = json.loads(UPSTREAM.read_text())
    engine = json.loads(ENGINE.read_text())
    aliases = json.loads(ALIASES.read_text()) if ALIASES.exists() else {"classes": {}, "enums": {}}
    if upstream["godot_commit"] != COMMIT:
        raise ValueError("Unexpected upstream revision")
    type_lookup = {item["name"]: item for item in upstream["types"]}
    if len(type_lookup) != len(upstream["types"]):
        raise ValueError("Duplicate upstream type")
    engine_by_id = {item["id"]: item for item in engine}
    if len(engine_by_id) != len(engine):
        raise ValueError("Duplicate Electron2D declaration")
    engine_by_owner = defaultdict(list)
    for item in engine:
        engine_by_owner[item["declaringType"]].append(item)
    engine_types = {item["declaringType"] for item in engine if item["kind"] == "type"}
    expected_upstream = {item["id"] for item in upstream["types"]}
    expected_upstream.update(member["id"] for item in upstream["types"] for member in item["members"])
    if len(expected_upstream) != len(upstream["types"]) + sum(len(item["members"]) for item in upstream["types"]):
        raise ValueError("Duplicate upstream declaration")
    manual_mappings = defaultdict(list)
    manual_extras = {}
    manual_statuses = {}
    for path in OVERRIDES:
        if not path.exists():
            continue
        overrides = json.loads(path.read_text())
        for row in overrides.get("mappings", []):
            if row["godot"] not in expected_upstream or row["electron2d"] not in engine_by_id or not row.get("reason"):
                raise ValueError(f"Invalid mapping in {path}: {row}")
            manual_mappings[row["godot"]].append(row)
        for row in overrides.get("extras", []):
            if row["electron2d"] not in engine_by_id or not row.get("reason") or row["electron2d"] in manual_extras:
                raise ValueError(f"Invalid extra in {path}: {row}")
            manual_extras[row["electron2d"]] = row
        for group in overrides.get("statusGroups", []):
            if (group.get("state") not in {"Implemented", "Partial", "Unimplemented", "Blocked", "Excluded"}
                    or not group.get("reason") or not group.get("godot")):
                raise ValueError(f"Invalid status group in {path}: {group}")
            for godot in group["godot"]:
                if godot not in expected_upstream or godot in manual_statuses:
                    raise ValueError(f"Invalid status target in {path}: {godot}")
                manual_statuses[godot] = group

    seen_upstream, used_engine = set(), set()
    page_text = {}
    counts = Counter()
    class_status = Counter()
    roadmap = defaultdict(list)
    actionable = []
    represented = []
    for godot_type in upstream["types"]:
        name = godot_type["name"]
        mapped = aliases.get("classes", {}).get(name, TEXTURE_NAMES.get(name, name))
        owners = mapped if isinstance(mapped, list) else [mapped]
        owners = [engine_name(owner) for owner in owners if engine_name(owner) in engine_types]
        class_state, class_reason = reason_for_type(godot_type, type_lookup)
        if owners:
            class_state, class_reason = "Partial", "Typed C# type exists; inheritance, signatures and behavior require row-level audit."
        if godot_type["id"] in manual_statuses:
            row = manual_statuses[godot_type["id"]]
            if row["state"] == "Implemented" and not owners:
                raise ValueError(f"Implemented class has no Electron2D declaration: {godot_type['id']}")
            class_state, class_reason = row["state"], row["reason"]
        source = godot_type["source"]
        url = f"https://github.com/godotengine/godot/blob/{COMMIT}/{source}"
        inherited = f"[{godot_type['inherits']}]({coverage_target(godot_type['inherits'])})" if godot_type["inherits"] else "—"
        page_name = TEXTURE_NAMES.get(name, name)
        page = CLASS_PAGES / f"{page_name}.md"
        lines = [] if page in page_text else [f"# {page_name} API coverage", "", "Last updated: 2026-09-22", ""]
        if page_name == "Texture":
            if page not in page_text:
                lines.extend(["The reference Texture and Texture2D contracts share one Electron2D Texture page under [ADR 0004](../../decisions/product.md#adr-0004). Each source declaration remains accounted for below.", ""])
            lines.extend([f"## Godot {name}", ""])
        lines.extend([f"Godot source: [{source}]({url}) at `{upstream['godot_version']}` (`{COMMIT}`).", "",
                 f"Godot base: {inherited}. "
                 f"Electron2D type: {', '.join(engine_link(engine_by_id[f'T:{owner}']) for owner in owners) if owners else '—'}.", "",
                 "Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.", ""])
        if name == "DisplayServer":
            lines.extend(["Current release verification requires Linux/Wayland only under [ADR 0021](../../decisions/product.md#adr-0021). The earlier self-contained host example, before Window/Engine.Run migration, started on Wayland with packaged SDL and advanced its scene; user-assisted physical arrow-key input and Escape exit passed. See the [class verification](../../classes/DisplayServer.md#verification). Other target platforms remain in the product matrix without blocking this stage.", ""])
        lines.extend(["| Godot API | Electron2D API | State | Reason / implementation trigger |",
                      "| --- | --- | --- | --- |"])
        class_engine = engine_by_id.get(f"T:{owners[0]}") if owners else None
        if class_engine:
            used_engine.add(class_engine["id"])
        seen_upstream.add(godot_type["id"])
        counts[class_state] += 1
        class_status[class_state] += 1
        lines.append(f"| [{code('class ' + name)}]({url}) | {engine_link(class_engine) if class_engine else '—'} | {class_state} | {cell(class_reason)} |")
        used_local = set()
        member_states = Counter()
        for member in godot_type["members"]:
            seen_upstream.add(member["id"])
            special = special_reason(godot_type, member)
            candidates = []
            if name == "@GlobalScope":
                if member["kind"] in {"method", "constant"}:
                    candidates.extend(engine_by_owner["Electron2D.Mathf"])
                if member["kind"] in {"enum", "enum_value"}:
                    enum = member["name"] if member["kind"] == "enum" else member["attributes"].get("enum", "")
                    owner = engine_name(aliases.get("enums", {}).get(f"@GlobalScope.{enum}", enum))
                    candidates.extend(engine_by_owner[owner])
            else:
                for owner in owners:
                    candidates.extend(engine_by_owner[owner])
                if member["kind"] in {"enum", "enum_value"}:
                    enum = member["name"] if member["kind"] == "enum" else member["attributes"].get("enum", "")
                    owner = engine_name(aliases.get("enums", {}).get(f"{name}.{enum}", f"{name}.{enum}"))
                    candidates.extend(engine_by_owner[owner])
                if name == "Color" and member["kind"] == "constant":
                    candidates.extend(engine_by_owner["Electron2D.Colors"])
            if member["kind"] == "enum" and candidates:
                exact_enum = [entry for entry in candidates if entry["kind"] == "type" and entry["declaringType"] == owner]
            else:
                exact_enum = []
            override = manual_mappings.get(member["id"], [])
            matches = ([engine_by_id[row["electron2d"]] for row in override] if override else
                       [] if special and special[0] == "Excluded" else exact_enum or choose(godot_type, member, candidates, used_local))
            adapted = None
            if not matches and member["kind"] == "constructor" and owners and " struct " in class_engine["signature"]:
                parameters = member["attributes"].get("params", [])
                if not parameters:
                    adapted = f"default({owners[0]})"
                elif len(parameters) == 1 and parameters[0]["type"] == name:
                    adapted = f"{owners[0]} copy assignment"
            if matches:
                used_engine.update(match["id"] for match in matches)
                used_local.update(match["id"] for match in matches)
                state, reason = "Partial", "; ".join(row["reason"] for row in override) if override else "Declaration mapping is structural; return/default/value and observable behavior require audit."
                mismatch = return_note(member, matches)
                if mismatch:
                    reason += " " + mismatch
                if member["kind"] in {"enum_value", "constant"} and len(matches) == 1:
                    upstream_value = integer(member["attributes"].get("value"))
                    engine_value = integer(matches[0].get("value"))
                    if upstream_value is not None and engine_value is not None:
                        if upstream_value == engine_value:
                            state = "Implemented"
                            reason = "Numeric identity matches the compiled C# declaration. " + (reason if override else "")
                        else:
                            if "numeric mismatch" not in reason.lower():
                                reason += f" Numeric mismatch: Godot {upstream_value}, Electron2D {engine_value}."
            elif adapted:
                state, reason = "Partial", "C# value-type construction or copy semantics supply this form; verify observable behavior."
            elif special:
                state, reason = special
            elif owners:
                state, reason = "Unimplemented", f"No mapped C# declaration; trigger: next complete {name} API slice."
            else:
                state, reason = class_state, class_reason
            if member["id"] in manual_statuses:
                row = manual_statuses[member["id"]]
                if row["state"] == "Implemented" and not (matches or adapted):
                    raise ValueError(f"Implemented member has no Electron2D declaration: {member['id']}")
                state, reason = row["state"], row["reason"]
            counts[state] += 1
            member_states[state] += 1
            target = "<br>".join(engine_link(match) for match in matches) if matches else code(adapted) if adapted else "—"
            lines.append(f"| [{code(member['kind'] + ' ' + member['signature'])}]({url}) | {target} | {state} | {cell(reason)} |")
        page_text[page] = page_text.get(page, "") + ("\n" if page in page_text else "") + "\n".join(lines) + "\n"
        if owners and (member_states["Unimplemented"] or member_states["Partial"]):
            represented.append((name, member_states["Unimplemented"], member_states["Partial"]))
        if class_state == "Blocked":
            roadmap[class_reason].append(name)
        elif class_state == "Unimplemented":
            actionable.append(name)

    if seen_upstream != expected_upstream:
        raise ValueError(f"Upstream accounting mismatch: {len(expected_upstream - seen_upstream)} missing")
    if used_engine.intersection(manual_extras):
        raise ValueError("Reviewed extra also maps to upstream declaration")
    engine_only = [entry for entry in engine if entry["id"] not in used_engine]
    if len(used_engine) + len(engine_only) != len(engine):
        raise ValueError("Electron2D accounting mismatch")
    lines = ["# Electron2D declarations without an audited upstream row", "", "Last updated: 2026-09-22", "",
             "These declarations are present in the compiled runtime. A blank upstream cell means no exact counterpart was established by the conservative name-and-arity mapper; it does not claim an intentional extension. Review each against the linked Godot class page and record a rationale before declaring parity.", "",
             "| Godot API | Electron2D API | State | Reason / next action |", "| --- | --- | --- | --- |"]
    for entry in engine_only:
        extra = manual_extras.get(entry["id"])
        state = "Implemented" if extra else "Unmapped"
        reason = f"Electron2D-specific: {extra['reason']} ({extra.get('adr', 'class reference')})." if extra else "Audit the corresponding type family; document a typed-C# rationale or link the exact upstream row."
        lines.append(f"| — | {engine_link(entry, from_class=False)} | {state} | {cell(reason)} |")
    page_text[COVERAGE / "electron2d-unmapped.md"] = "\n".join(lines) + "\n"
    catalog = ["# Godot class-reference catalog", "", "Last updated: 2026-09-22", "",
               f"Source: Godot `{upstream['godot_version']}` at `{COMMIT}`. Every XML class is listed, including editor and 3D exclusions. Texture pages use Electron2D names; Texture and Texture2D share one page with separate source sections.", "",
               "| Godot class | Base | Class state | Declared members |", "| --- | --- | --- | ---: |"]
    for item in upstream["types"]:
        state = "Partial" if engine_name(aliases.get("classes", {}).get(item["name"], TEXTURE_NAMES.get(item["name"], item["name"]))) in engine_types else reason_for_type(item, type_lookup)[0]
        if item["id"] in manual_statuses:
            state = manual_statuses[item["id"]]["state"]
        catalog.append(f"| [{cell(item['name'])}](classes/{coverage_target(item['name'])}) | {cell(item['inherits'] or '—')} | {state} | {len(item['members'])} |")
    page_text[COVERAGE / "catalog.md"] = "\n".join(catalog) + "\n"
    actionable_note = (" Start with the independent " + ", ".join(f"[{name}](classes/{coverage_target(name)})" for name in actionable) + " class slices.") if actionable else ""
    road = ["# Coverage roadmap", "", "Last updated: 2026-09-22", "",
            "The order follows concrete dependencies. `Partial` rows need either a semantic audit or resolution of a documented behavior gap; `Unmapped` Electron2D rows need an exact upstream link or a documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.", "",
            f"1. Review {counts['Partial']} partially implemented rows and {len(engine_only) - len(manual_extras)} unmapped Electron2D declarations, beginning with the existing core, input, scene, resource and image domains.",
            f"2. Complete {counts['Unimplemented']} missing declarations in already represented type families; split each type by its documented dependency trigger.{actionable_note}",
            "3. Implement the remaining domains in dependency order: SDL3 GPU 2D rendering with the accepted SDL_Renderer fallback; GUI/theme and tiles; Box2D.NET physics; audio/navigation/animation; asset loaders and networking; self-hosted editor. Finish specific display/input host gaps at their documented triggers. The first executable fallback slice must audit each of the five blocked GL/EGL/GLX `DisplayServer.HandleType` identities against its actual driver and window-associated context under ADR 0042.", "",
            "## Existing type backlog", "",
            "These classes already have an Electron2D type. Sort by missing member count, then unaudited mapped count; this is workload order, not a claim that dependencies can be skipped.", "",
            "| Godot class | Unimplemented members | Partial members |", "| --- | ---: | ---: |"]
    for name, missing, partial in sorted(represented, key=lambda row: (-row[1], -row[2], row[0])):
        road.append(f"| [{cell(name)}](classes/{coverage_target(name)}) | {missing} | {partial} |")
    road.extend(["", "## Blocked type families", "", "| Exact trigger | Classes |", "| --- | ---: |"])
    scope_names = []
    for trigger, names in sorted(roadmap.items(), key=lambda pair: (-len(pair[1]), pair[0])):
        if trigger.startswith("Trigger: explicit product decision"):
            scope_names.extend(names)
            continue
        road.append(f"| {cell(trigger)} | {len(names)} |")
    if scope_names:
        road.append(f"| Separate product-scope decision for each of {len(scope_names)} currently unassigned families; see their catalog pages for exact names. | {len(scope_names)} |")
    road.extend(["", "Each [catalog entry](catalog.md) opens the complete member table. Excluded rows have an accepted product reason and no implementation task.", ""])
    page_text[COVERAGE / "roadmap.md"] = "\n".join(road)
    summary = {"upstream_types": len(upstream["types"]), "upstream_members": sum(len(item["members"]) for item in upstream["types"]),
               "electron2d_declarations": len(engine), "mapped_engine": len(used_engine), "reviewed_extras": len(manual_extras),
               "unmapped_engine": len(engine_only) - len(manual_extras),
               "states": dict(sorted(counts.items()))}
    return page_text, summary


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    pages, summary = render()
    expected = set(pages)
    existing = set(CLASS_PAGES.glob("*.md")) | {path for path in pages if path.exists()}
    stale = [str(path.relative_to(ROOT)) for path, content in pages.items()
             if not path.exists() or path.read_text() != content]
    stale.extend(str(path.relative_to(ROOT)) for path in existing - expected)
    if args.check:
        if stale:
            raise SystemExit("Stale coverage pages: " + ", ".join(stale[:20]))
    else:
        CLASS_PAGES.mkdir(parents=True, exist_ok=True)
        for path in existing - expected:
            path.unlink()
        for path, content in pages.items():
            path.write_text(content)
    print(json.dumps(summary, sort_keys=True))


if __name__ == "__main__":
    main()
