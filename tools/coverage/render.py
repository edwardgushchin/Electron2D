#!/usr/bin/env python3
"""Render and verify the bidirectional class-reference coverage snapshot."""

import argparse
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

from physics_report import render_physics


ROOT = Path(__file__).resolve().parents[2]
COVERAGE = ROOT / "docs/coverage"
DATA = COVERAGE / "data"
CLASS_PAGES = COVERAGE / "classes"
UPSTREAM = DATA / "godot-4.7.2.json"
ENGINE = DATA / "electron2d.json"
ALIASES = Path(__file__).with_name("type_aliases.json")
OVERRIDES = [Path(__file__).with_name(f"overrides_{family}.json") for family in ("math", "scene", "core", "display", "rendering", "device", "navigation", "resources", "physics", "text", "buttons", "scroll", "item_list", "tree", "tabs", "popup", "menu", "menu_button", "menu_bar", "file_dialog", "spinbox", "color_picker", "dialogs", "layout_containers", "gui_drag", "text_delivery", "code_edit", "rich_text", "graph", "audio", "mesh", "particles", "skeleton", "networking", "tiles")]
COMMIT = "ed1daf0bf001b61586d9930840f2f1394092c079"
PHYSICS_AUDITED_TYPES = {
    "AnimatableBody2D",
    "Area2D",
    "CapsuleShape2D",
    "CharacterBody2D",
    "CircleShape2D",
    "CollisionObject2D",
    "CollisionPolygon2D",
    "CollisionShape2D",
    "ConcavePolygonShape2D",
    "ConvexPolygonShape2D",
    "DampedSpringJoint2D",
    "GrooveJoint2D",
    "Joint2D",
    "KinematicCollision2D",
    "PhysicalBone2D",
    "PhysicsBody2D",
    "PhysicsDirectBodyState2D",
    "PhysicsDirectBodyState2DExtension",
    "PhysicsDirectSpaceState2D",
    "PhysicsDirectSpaceState2DExtension",
    "PhysicsMaterial",
    "PhysicsPointQueryParameters2D",
    "PhysicsRayQueryParameters2D",
    "PhysicsServer2D",
    "PhysicsServer2DExtension",
    "PhysicsServer2DManager",
    "PhysicsShapeQueryParameters2D",
    "PhysicsTestMotionParameters2D",
    "PhysicsTestMotionResult2D",
    "PinJoint2D",
    "RayCast2D",
    "RectangleShape2D",
    "RigidBody2D",
    "SegmentShape2D",
    "SeparationRayShape2D",
    "Shape2D",
    "ShapeCast2D",
    "StaticBody2D",
    "WorldBoundaryShape2D",
}
TEXTURE_NAMES = {
    "Texture2D": "Texture",
    "Texture2DArray": "TextureArray",
    "Texture2DArrayRD": "TextureArrayRD",
    "Texture2DRD": "TextureRD",
    "GradientTexture1D": "GradientRampTexture",
    "GradientTexture2D": "GradientTexture",
}
DIMENSIONAL_TYPE_EXCEPTIONS = {
    "T:Electron2D.Curve2D": "ADRs 0004 and 0013: spatial curve distinct from scalar Curve",
    "T:Electron2D.AnimationNodeBlendSpace1D": "ADR 0093: one-dimensional blend parameter domain",
    "T:Electron2D.AnimationNodeBlendSpace2D": "ADR 0093: two-dimensional blend parameter domain",
}


ALIGNMENT_ENUM_OWNERS = {
    "AlignmentMode": {"BoxContainer", "AspectRatioContainer", "FlowContainer", "TabBar"},
    "LastWrapAlignmentMode": {"FlowContainer"},
    "HorizontalAlignment": {""},
    "VerticalAlignment": {""},
    "InlineAlignment": {""},
    "ParticlesTransformAlign": {"RenderingServer"},
    "ParticlesTransformAlignAxis": {"RenderingServer"},
    "ParticlesTransformAlignCustomSrc": {"RenderingServer"},
}


def validate_public_type_names(engine):
    invalid = [item["id"] for item in engine if item["kind"] == "type"
               and re.search(r"(?<!\d)[123][dD](?:$|[A-Z_<`])", item["name"])
               and item["id"] not in DIMENSIONAL_TYPE_EXCEPTIONS]
    if invalid:
        raise ValueError("ADR 0004: unapproved dimensional public type names: " + ", ".join(invalid)
                         + ". Omit the dimension marker or follow an explicitly accepted ADR exception.")

    invalid = []
    for item in engine:
        if item["kind"] != "type":
            continue
        name = item["id"].removeprefix("T:Electron2D.")
        owner, _, enum = name.rpartition(".")
        if enum in ALIGNMENT_ENUM_OWNERS and owner not in ALIGNMENT_ENUM_OWNERS[enum]:
            invalid.append(item["id"])
    if invalid:
        raise ValueError("ADR 0051: alignment enum declaring owners must match the reference: "
                         + ", ".join(invalid))


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
        stem, arguments = name.split("<", 1)
        arity = arguments.count(",") + 1
        page = ROOT / "docs/classes" / f"{stem}.Generic{arity if arity > 1 else ''}.md"
        if not page.exists():
            page = ROOT / "docs/classes" / f"{stem}.Generic.md"
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
        "vector3": "vector3", "vector3i": "vector3i",
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
    if name.startswith("Packed") and name.endswith("Array"):
        return "Excluded", "Typed packed container and its ordinary collection methods use standard C# arrays, spans and lists instead of an Electron2D-owned duplicate (ADR 0001). Call-site behavior is audited separately."
    if name in {"Variant", "Callable", "Signal", "ClassDB", "Array", "Dictionary", "String", "bool", "float", "int"}:
        return "Excluded", "Engine-owned dynamic/untyped primitive or collection is replaced by C# types and typed contracts (ADR 0001/0002); no engine-owned duplicate."
    if name == "RefCounted":
        return "Excluded", "Public manual reference counting is excluded by ADRs 0003 and 0014. RefCounted ancestry maps to ElectronObject managed lifetime and IDisposable; descendant APIs are audited on their own pages."
    if name in {"NodePath", "StringName"}:
        return "Excluded", "Separate path/name wrapper is excluded by the string-based Node and group contract (ADRs 0008 and 0001); use string."
    if name == "Marshalls":
        return "Excluded", "Base64 and UTF-8 conversion use System.Convert and System.Text; Variant serialization is excluded by ADR 0001. No engine-owned wrapper is needed."
    if name.startswith("GDScript") or name == "@GDScript":
        return "Excluded", "GDScript runtime and tooling are outside the typed C# contract (ADR 0001)."
    if name in {"LightmapperRD", "RDAccelerationStructureGeometry", "RDAccelerationStructureInstance",
                "RDHitGroup", "RenderDataRD", "RenderSceneBuffersRD", "RenderSceneDataRD",
                "Texture3DRD", "TextureCubemapRD", "TextureCubemapArrayRD"}:
        return "Excluded", "Three-dimensional scene rendering, volume/cubemap textures and ray tracing remain outside the 2D product boundary (ADR 0028)."
    if (name.startswith("RD") or name.startswith("UniformSetCacheRD")
            or name.endswith("RD") or name == "RenderingDevice"):
        return "Blocked", "Trigger: the owning portable RenderingDevice pipeline/resource integration under revised ADR 0028. Local compute buffers and dispatch are the first connected slice; device graphics/texture pipelines, additional descriptor kinds, source import and their caches require their executing consumers. Three-dimensional and ray-tracing operations remain outside the 2D product boundary."
    three_d_only = {
        "BoxMesh", "CapsuleMesh", "CompositorEffect", "CompressedCubemap",
        "CompressedCubemapArray", "CubemapArray", "CylinderMesh", "EditorNode3DGizmo",
        "EditorNode3DGizmoPlugin", "EditorSceneFormatImporter", "EditorSceneFormatImporterBlend",
        "EditorSceneFormatImporterFBX2GLTF", "EditorSceneFormatImporterGLTF",
        "EditorSceneFormatImporterUFBX", "FogMaterial", "GridMapEditorPlugin",
        "MeshConvexDecompositionSettings", "MeshLibrary",
        "NavigationMeshGenerator", "NavigationServer3DManager", "PanoramaSkyMaterial",
        "PhysicalSkyMaterial", "PhysicsServer3DManager", "PhysicsServer3DRenderingServerHandler",
        "PlaceholderCubemap", "PlaceholderCubemapArray", "PlaneMesh", "PointMesh",
        "PrismMesh", "ProceduralSkyMaterial", "QuadMesh",
        "RenderData", "RenderDataExtension", "RenderSceneBuffers",
        "RenderSceneBuffersConfiguration", "RenderSceneBuffersExtension", "RenderSceneData",
        "RenderSceneDataExtension", "ResourceImporterOBJ", "ResourceImporterScene",
        "RibbonTrailMesh", "SphereMesh", "TextMesh", "TorusMesh", "TubeTrailMesh",
        "VisualShaderNodeBillboard", "VisualShaderNodeCubemap", "VisualShaderNodeCubemapParameter",
        "VisualShaderNodeLinearSceneDepth", "VisualShaderNodeParticleMeshEmitter",
        "VisualShaderNodeScreenNormalWorldSpace", "VisualShaderNodeTexture3DParameter",
        "VisualShaderNodeTextureParameterTriplanar", "VisualShaderNodeWorldPositionFromDepth",
        "VisualShaderNodeDeterminant", "VisualShaderNodeTransformCompose",
        "VisualShaderNodeTransformConstant", "VisualShaderNodeTransformDecompose",
        "VisualShaderNodeTransformFunc", "VisualShaderNodeTransformOp",
        "VisualShaderNodeTransformParameter", "VisualShaderNodeTransformVecMult",
    }
    # Mesh is shared by MeshInstance2D; exclude individual 3D members, not its whole family.
    three_d_roots = {"NavigationMesh", "Environment", "Compositor", "CameraAttributes", "Cubemap", "Sky", "SkyMaterial", "LightmapGIData", "Lightmapper", "BoneMap", "Texture3D"}
    if (re.search(r"(^|[^A-Za-z0-9])3D([^A-Za-z0-9]|$)", lineage)
            or any(part.endswith("3D") for part in ancestors)
            or any(part in three_d_roots for part in ancestors)
            or name in three_d_only
            or name in {"AABB", "Basis", "Plane", "Projection", "Quaternion", "Transform3D", "SkeletonProfile", "SkeletonProfileHumanoid", "Skin", "SkinReference", "MobileVRInterface", "WebXRInterface"}
            or name.startswith(("OpenXR", "XR", "Skeleton3D", "BoneAttachment3D", "Node3DGizmo", "GLTF", "FBX", "Lightmap", "Voxel", "FogVolume"))):
        return "Excluded", "3D/XR product scope is excluded by ADR 0004; no implementation trigger."
    if name == "@GlobalScope":
        return "Partial", "Global functions/constants/enums are distributed across typed C# declarations; audit each row."
    if item.get("api_type") == "editor" or name.startswith("Editor") or "Editor" in ancestors:
        return "Blocked", "Trigger: first self-hosted editor executable slice under ADR 0027."
    accepted_slices = (
        ({"BitMap", "Color", "Geometry2D", "RandomNumberGenerator", "Rect2", "Rect2i", "Transform2D", "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i"},
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
        ({"Path2D", "PathFollow2D"},
         "first scene path/follower slice using the implemented Curve2D and Entity; include progress, rotation, looping, change subscriptions and packing (ADRs 0008 and 0013); navigation is not a prerequisite"),
        ({"AStarGrid2D"},
         "standalone 2D grid pathfinding slice with cell shape, diagonals, weights and jump-point behavior (ADR 0052); no navigation-server backend is required"),
        ({"Line2D", "Marker2D", "Parallax2D", "ParallaxBackground", "ParallaxLayer", "Polygon2D", "RemoteTransform2D"},
         "next 2D scene-node slice using the existing Entity, CanvasItem, canvas layers and polygon renderer (ADRs 0008 and 0028)"),
        ({"CurveTexture", "CurveXYZTexture"},
         "first curve-texture slice using implemented Curve and Texture; integrate curve-change rebaking, float channel formats and verified backend sampling (ADRs 0013 and 0028); GUI is not a prerequisite"),
        ({"AnimatedTexture"},
         "next timed texture-frame resource slice using the existing Texture, SpriteFrames and engine clock (ADRs 0013 and 0028)"),
        ({"CapsuleShape2D", "SegmentShape2D", "SeparationRayShape2D", "WorldBoundaryShape2D", "ConvexPolygonShape2D", "ConcavePolygonShape2D"},
         "complete shape geometry, response and queries on CPU and independent GPU worlds under ADR 0054"),
        ({"PhysicsMaterial"},
         "typed friction and restitution resource with verified Box2D shape-material transfer (ADR 0013)"),
        ({"AnimatableBody2D", "Area2D", "CharacterBody2D"},
         "next kinematic or sensor scene-body slice using the implemented CollisionObject, PhysicsBody and Box2D world"),
        ({"Joint2D", "PinJoint2D", "GrooveJoint2D", "DampedSpringJoint2D"},
         "typed scene-joint ownership, anchors and Box2D solver integration using the implemented physics world"),
    )
    for names, trigger in accepted_slices:
        if name in names:
            return "Unimplemented", f"Accepted 2D capability; trigger: {trigger}."
    if name == "InputEventMIDI":
        return "Blocked", "Trigger: accepted MIDI-domain and native host-API decision, then the first MIDI device/event slice (ADR 0038)."
    if name in {"InputEventShortcut", "Shortcut"}:
        return "Blocked", "Trigger: first typed GUI/editor Shortcut ownership and focus-routing slice (ADR 0038)."
    if name == "AccessibilityServer":
        return "Blocked", "Trigger: first semantic accessibility-tree, focus and native screen-reader bridge slice (ADR 0041)."
    if name == "NativeMenu":
        return "Blocked", "Trigger: first native-menu service slice with ownership, callbacks and target checks (ADR 0041)."
    if name in {"CameraFeed", "CameraServer", "CameraTexture"}:
        return "Blocked", "Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021)."
    if name in {"TextureLayered", "ImageTextureLayered", "Texture2DArray", "CompressedTexture2DArray", "CompressedTextureLayered", "PlaceholderTexture2DArray", "PlaceholderTextureLayered"}:
        return "Blocked", "Trigger: first layered/array texture storage, upload and sampling slice in the 2D renderer (ADR 0028)."
    if name in {"PlaceholderMaterial", "PlaceholderTexture2D"}:
        return "Blocked", "Trigger: first typed missing-asset placeholder and loader slice (ADRs 0013 and 0023)."
    if name in {"BlitMaterial", "DrawableTexture2D"}:
        return "Blocked", "Trigger: first writable GPU texture and blit-command lifetime slice (ADR 0028)."
    if name in {"CanvasTexture", "MeshTexture"}:
        return "Blocked", "Trigger: first 2D light/mesh texture renderer integration (ADR 0028)."
    if name == "NoiseTexture2D":
        return "Blocked", "Trigger: complete the Noise resource and first noise-texture rebaking slice (ADR 0013)."
    if name == "ExternalTexture":
        return "Blocked", "Trigger: first portable external-image ownership and native texture-import decision (ADRs 0021 and 0028)."
    if name == "DPITexture":
        return "Blocked", "Trigger: first typed GUI DPI-scale and theme-texture slice (ADR 0028)."
    if name in {"PackedDataContainer", "PackedDataContainerRef"}:
        return "Blocked", "Trigger: first typed packed-asset container and loader slice (ADRs 0013 and 0023)."
    if name in {"AESContext", "HMACContext", "HashingContext"}:
        return "Blocked", "Trigger: accepted typed cryptography utility contract and first portable crypto-service slice (ADR 0001)."
    if name.startswith("VisualShader"):
        return "Blocked", "Trigger: first typed 2D visual-shader graph translation and shader-import slice (ADR 0028)."
    if name in {"GPUParticles2D", "ParticleProcessMaterial"}:
        return "Blocked", "Trigger: actual GPU particle simulation, process-material shader/data integration, native storage and compute/collision/attractor ownership under ADR 0028. CPUParticles baseline does not provide GPU compute or process-material conversion."
    if name in {"DirectionalLight2D", "Light2D", "LightOccluder2D", "OccluderPolygon2D", "PointLight2D"}:
        return "Blocked", "Trigger: first 2D light and occlusion renderer slice (ADR 0028)."
    if name in {"BackBufferCopy", "CanvasGroup"}:
        return "Blocked", "Trigger: first 2D offscreen composition and framebuffer-copy slice (ADR 0028)."
    if name in {"CanvasItemMaterial", "CanvasModulate", "ShaderGlobalsOverride"}:
        return "Blocked", "Trigger: first missing 2D material, canvas-modulation and shader-global renderer integration (ADR 0028)."
    if name in {"ShaderInclude", "ShaderIncludeDB"}:
        return "Blocked", "Trigger: first shader include import and dependency-tracking slice (ADR 0028)."
    if name in {"SubViewport", "ViewportTexture"}:
        return "Blocked", "Trigger: first independent offscreen viewport lifecycle and texture-output slice (ADRs 0008 and 0028)."
    if name in {"VisibleOnScreenEnabler2D", "VisibleOnScreenNotifier2D"}:
        return "Blocked", "Trigger: first retained-canvas visibility tracking and notification slice (ADR 0028)."
    if name in {"MultiMesh", "MultiMeshInstance2D"}:
        return "Unimplemented", "Applicable fixed-2D instance storage, visibility, interpolation and retained canvas consumers are executable; remaining members require individual audited ownership or shader-buffer dependencies (ADR 0092)."
    if name == "ImmediateMesh":
        return "Blocked", "Trigger: typed incremental surface begin/attribute/vertex/end builder with commit/rollback and real ArrayMesh-backed drawing; static surface rendering already executes (ADR 0092)."
    if name == "PrimitiveMesh":
        return "Blocked", "Trigger: first applicable typed 2D procedural geometry producer with concrete generation parameters and visible mesh output; static surface rendering already executes (ADR 0092)."
    if name == "PlaceholderMesh":
        return "Blocked", "Trigger: typed missing-asset mesh placeholder producer/loader and its 2D drawing/bounds policy over the implemented Mesh resource (ADR 0092)."
    if name in {"MeshDataTool", "SurfaceTool"}:
        return "Blocked", "Trigger: typed mesh topology/adjacency and incremental geometry editing, attribute conversion and transactional commit to the now executable ArrayMesh; missing advanced channels enter their own shader/skeleton producer slices (ADR 0092)."
    if name in {"ImporterMesh", "MeshLibrary"}:
        return "Blocked", "Trigger: concrete applicable 2D mesh import/library entry model, owned resource graphs and loader/authoring format integration over the implemented mesh resources (ADRs 0013/0092); audit 3D-only entry fields separately."
    blocked_slices = (
        ({"ResourceFormatSaver", "ResourceImporter", "ResourcePreloader", "ResourceUID", "MissingResource", "MissingNode", "InstancePlaceholder", "PCKPacker", "ZIPReader", "ZIPPacker"},
         "first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023)"),
        ({"ResourceFormatLoader"},
         "first public typed loader-plugin registration and callback slice after the concrete internal image-texture loader (ADR 0013)"),
        ({"CollisionPolygon2D"},
         "polygon collision-shape resource conversion and scene polygon owner integration after the first convex/concave shape slice"),
        ({"PhysicsServer2D", "PhysicsServer2DExtension", "PhysicsServer2DManager"},
         "complete typed backend registration/factory and world-scoped implementation dispatch, shared RID lifecycle, callbacks, direct state/query result construction and CPU/GPU scene integration (ADRs 0054 and 0103); current facade is static/sealed and attachment/step paths select built-in implementations only"),
        ({"PhysicsDirectBodyState2D", "PhysicsDirectBodyState2DExtension"},
         "complete typed body-state extension hooks and context construction preserving live attachment, owner/callback lifetime, contact identity and inherited operations (ADRs 0054 and 0103); current direct-body state is sealed and its constructor is internal"),
        ({"PhysicsDirectSpaceState2DExtension"},
         "complete typed direct-space extension dispatch, scoped exclusion helpers and usable typed result construction with shared identity/lifetime semantics (ADRs 0054 and 0103); validated query/motion result construction now executes from a separate CPU/GPU public consumer, but current direct-space state remains sealed and extension dispatch/exclusion hooks are absent"),
        ({"PhysicsDirectSpaceState2D", "PhysicsPointQueryParameters2D", "PhysicsRayQueryParameters2D", "PhysicsShapeQueryParameters2D", "PhysicsTestMotionParameters2D", "PhysicsTestMotionResult2D", "KinematicCollision2D"},
         "typed direct-space sweep/ray/point query and result lifecycle over the PhysicsServer space"),
        ({"RayCast2D", "ShapeCast2D"},
         "scene query nodes consuming the typed direct-space ray/shape query slice"),
        ({"ImageFormatLoader", "ImageFormatLoaderExtension"},
         "first public image-decoder plugin and format-discovery slice beyond the internal six-codec Image path (ADR 0039)"),
        ({"ResourceSaver"},
         "first concrete typed resource file format and serializer with ownership and rollback (ADRs 0013 and 0023)"),
        ({"CompressedTexture2D", "PortableCompressedTexture2D"},
         "first compressed-texture import, decoder and verified GPU sampling slice (ADRs 0028 and 0039)"),
        ({"FontVariation"},
         "variable-font instance coordinates, variation metadata and per-instance shaping/raster cache identity over the integrated FreeType/HarfBuzz backend (ADR 0046)"),
        ({"SystemFont"},
         "platform font discovery, matching and owned fallback faces over the integrated FontFile backend (ADR 0046)"),
        ({"VideoStream", "VideoStreamPlayback", "VideoStreamTheora"},
         "first video decoding, timed texture playback and audio synchronization slice"),
        ({"World2D"},
         "first executable owned world-resource slice: physics space and direct state under ADR 0063, canvas and navigation under ADRs 0028 and 0052"),
        ({"WorldEnvironment"},
         "first 2D world/render-environment integration slice after SDL3 GPU rendering (ADRs 0008 and 0028)"),
        ({"Mesh", "ImporterMesh", "MeshConvexDecompositionSettings", "MeshDataTool", "MeshLibrary", "MultiMesh", "SurfaceTool", "TriangleMesh"},
         "first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028)"),
        ({"CodeHighlighter", "SyntaxHighlighter", "UndoRedo", "FoldableGroup", "ColorPalette"},
         "first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028)"),
        ({"CharFXTransform"}, "first typed rich-text effect slice after 2D GUI and text rendering (ADR 0028)"),
        ({"SkeletonModification2DJiggle"},
         "bounded jiggle spring/damping/gravity state, reset/interpolation and copied joint settings driving the implemented Skeleton palette; optional collision queries require actual physics-world ownership (ADRs 0028/0092)"),
        ({"SkeletonModification2DStackHolder"},
         "borrowed child-stack binding, phase/strength composition, nested execution/cycle guards, copied scene ownership and actual modified pose output over the implemented stack (ADRs 0014/0028/0092)"),
        ({"CSharpScript", "Script", "ScriptBacktrace", "ScriptExtension", "ScriptLanguage", "ScriptLanguageExtension", "Expression", "GDExtension", "GDExtensionManager", "GodotInstance"},
         "an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001)"),
        ({"OS", "Time", "Performance", "EngineDebugger", "EngineProfiler", "Logger", "MovieWriter", "StatusIndicator"},
         "first type-specific OS, clock, diagnostics, logging, capture or tray-service integration beyond the existing SDL host, with target capability reporting (ADRs 0015, 0016 and 0021)"),
        ({"JavaClass", "JavaClassWrapper", "JavaObject", "JavaScriptBridge", "JavaScriptObject", "JNISingleton"},
         "first Android or Web host-interoperability slice after the portable SDL host (ADR 0021)"),
        ({"SceneReplicationConfig"}, "first typed multiplayer replication slice after scene persistence (ADR 0023)"),
        ({"IP", "JSONRPC"}, "first typed networking, address-resolution and RPC slice"),
        ({"PolygonPathFinder"}, "first typed 2D navigation and pathfinding slice"),
        ({"OggPacketSequence", "OggPacketSequencePlayback"}, "first audio decoding and playback slice"),
        ({"Crypto", "CryptoKey", "X509Certificate"}, "first typed networking-security integration slice with a portable crypto backend (ADR 0021)"),
        ({"RID"}, "first executable shared server resource-identity and lifetime slice under ADR 0063; renderer and navigation consumers retain their own domain gates"),
        ({"Thread", "Mutex", "Semaphore", "WorkerThreadPool"},
         "a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021)"),
        ({"WeakRef"},
         "an accepted public weak-reference contract beyond System.WeakReference<T>; Resource currently uses only an internal weak path cache (ADR 0013)"),
    )
    for names, trigger in blocked_slices:
        if name in names:
            return "Blocked", f"Trigger: {trigger}."
    gui_type = ("Control" in ancestors or "Window" in ancestors and name != "Window"
                or name in {"ButtonGroup", "LabelSettings", "PopupMenu", "RichTextEffect",
                            "TextLine", "TextParagraph", "TouchScreenButton", "TreeItem"}
                or name.startswith(("StyleBox", "TextServer", "Theme")))
    if gui_type:
        return "Blocked", "GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028)."
    families = (
        ("Physics2D", r"Physics|Collision|RigidBody2D|StaticBody2D|CharacterBody2D|Area2D|Joint2D|RayCast2D|ShapeCast2D|Shape2D|SpringArm2D", "next type-specific 2D physics operation beyond the implemented Box2D-backed scene-body slice (ADR 0012)"),
        ("Audio", r"Audio|Sound|Microphone", "first audio mixing and playback slice"),
        ("Navigation2D", r"Navigation", "next operation-specific navigation link, agent/obstacle avoidance, source-geometry bake or typed query slice beyond the authored map/region backend (ADRs 0052/0097)"),
        ("Animation", r"Animation", "first missing type-specific animation resource utility or persistence slice on the executable graph/state-machine/BlendSpace/action foundation (ADR 0093); applicable event tracks already execute"),
        ("Skeleton", r"Skeleton2D|Bone2D", "typed 2D bone hierarchy, rest/pose transforms and skinning integration"),
        ("Tiles", r"Tile|Atlas", "first tile and atlas resource slice after 2D rendering"),
        ("Networking", r"Multiplayer|PacketPeer|ENet|WebRTC|WebSocket|HTTP|TLS|DTLS|TCP|UDP|IP$|SocketServer|StreamPeer|UDSServer|UPNP", "first networking and multiplayer slice"),
        ("Assets", r"ResourceLoader|ResourceSaver|CompressedTexture|StreamTexture|ImageTexture|Font|Video|PackedData|ImageFormatLoader|GLTF|FBX", "first type-specific asset format and native-backed integration beyond the existing image-texture loader (ADR 0013/0023)"),
        ("InputHost", r"InputEvent|InputMap|Input$|Shortcut|Joypad|TouchScreen|Sensor", "first SDL input-host integration slice (ADR 0038)"),
        ("Host", r"DisplayServer|OS$|Time$|NativeMenu|CameraServer|CameraFeed|AccessibilityServer", "first SDL-backed platform host and display slice (ADR 0021/0038)"),
        ("Rendering2D", r"Canvas|Sprite|Texture|Shader|Material|Light2D|Polygon2D|Viewport|Camera2D|Parallax|Rendering|Gradient|Particle|Occluder|Mesh", "first missing type-specific 2D renderer integration beyond the existing GPU/fallback canvas slice (ADR 0028)"),
    )
    for domain, pattern, trigger in families:
        if re.search(pattern, lineage, re.IGNORECASE):
            return "Blocked", f"{domain}: trigger is the {trigger}."
    return "Blocked", f"Trigger: explicit product decision to admit the {name} API family, then its first complete vertical slice."


def special_reason(item, member):
    if item["name"].startswith("Packed") and item["name"].endswith("Array"):
        collection_methods = {"append", "append_array", "bsearch", "clear", "count", "duplicate", "erase", "fill", "find", "get", "has", "insert", "is_empty", "push_back", "remove_at", "resize", "reverse", "rfind", "set", "size", "slice", "sort"}
        if item["name"] == "PackedByteArray" and member["name"] in {"decode_var", "decode_var_size", "encode_var", "has_encoded_var"}:
            return "Excluded", "Variant-dependent byte conversion is excluded by ADR 0001."
        if member["kind"] == "method" and member["name"] not in collection_methods:
            return "Blocked", "Trigger: first typed binary-buffer utility slice under ADR 0020; audit byte layout, encoding, compression format and ownership for this operation before mapping or exclusion."
        return None
    name = member["name"].lower()
    signature = member["signature"]
    if item["name"] == "RenderingServer":
        enum = member["name"] if member["kind"] == "enum" else member["attributes"].get("enum", "")
        # These RID families belong to the 3D scene server; canvas lights and shared mesh/texture APIs do not.
        if (name.startswith(("camera_", "decal_", "directional_light_", "directional_shadow_",
                             "directional_soft_shadow_", "fog_volume_", "instance_", "light_", "lightmap_",
                             "omni_light_", "reflection_probe_", "scenario_", "sky_", "spot_light_", "voxel_gi_"))
                or enum.startswith(("CubeMapLayer", "Decal", "DOF", "FogVolume", "Instance", "Light",
                                    "ReflectionProbe", "ShadowCastingSetting", "SkyMode", "ViewportScaling3DMode",
                                    "VisibilityRangeFadeMode", "VoxelGI"))
                or re.search(r"(?:^|_)3d(?:$|_)", name)):
            return "Excluded", "3D scene, lighting or texture API is outside ADR 0004; canvas and shared 2D resources are audited separately."
    if item["name"] == "ProjectSettings" and member["kind"] == "property":
        root = name.split("/", 1)[0]
        if re.search(r"(?:^|[/_])3d(?:$|[/_])", name) or root in {"xr", "collada"}:
            return "Excluded", "3D/XR or 3D asset-setting family is outside the 2D product (ADR 0004)."
        settings = {
            "rendering": "first SDL3 GPU 2D renderer and typed rendering-settings slice (ADR 0028)",
            "layer_names": "first typed 2D rendering/physics layer registry after those domains exist (ADRs 0012 and 0028)",
            "debug": "first host diagnostics and typed debug-settings slice (ADRs 0015 and 0016)",
            "input": "typed built-in action definition and its concrete runtime, GUI or editor consumer (ADR 0038)",
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
            return "Unimplemented", "Trigger: text shaping, locale asset remaps, or scene translation-change integration for the specific setting (ADR 0007)."
        if root == "physics" and name.startswith("physics/common/"):
            return "Unimplemented", "Trigger: next core timing-settings slice in ProjectSettings (ADRs 0016 and 0019)."
        if root in settings:
            return "Blocked", f"Trigger: {settings[root]}."
        raise ValueError(f"Unclassified ProjectSettings family: {root}")
    if item["name"] == "Vector3" and (member["name"].startswith("MODEL_") or re.search(r"\b(Basis|Quaternion|Transform3D)\b", signature)):
        return "Excluded", "3D model orientation and transform types are outside ADR 0004; the numeric Vector3 is retained by ADR 0033."
    if item["name"] in {"Vector3", "Vector3i"}:
        return None
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
    if item["name"] in {"CurveTexture", "CurveXYZTexture"}:
        return None  # Audited curve-texture members use the rendering overrides, including texture-mode values.
    if any(token in name for token in ("draw_", "canvas_", "texture_", "shader_", "render_")):
        return "Blocked", "Trigger: first missing operation-specific retained-canvas, texture or shader integration in the existing 2D renderer (ADR 0028)."
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
    validate_public_type_names(engine)
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
    classified = defaultdict(list)
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
        updated = "2026-10-07" if name in {"Font", "FontFile", "FontVariation", "Script", "CSharpScript"} else "2026-10-03" if name in {"AudioEffectRecord", "AudioEffectHardLimiter", "AudioEffectLimiter", "AudioEffectPitchShift", "AudioEffectStereoEnhance", "AudioEffectPhaser"} else "2026-10-02" if name in {'AudioEffectEQ', 'AudioEffectEQ6', 'AudioEffectEQ10', 'AudioEffectEQ21', 'AudioEffectAmplify', 'AudioEffectPanner', 'AudioEffectHighShelfFilter', 'AudioEffectFilter', 'AudioEffectBandLimitFilter', 'AudioEffectNotchFilter', 'AudioEffectLowShelfFilter', 'AudioEffectLowPassFilter', 'AudioEffectHighPassFilter', 'AudioEffectBandPassFilter'} else "2026-09-24" if name in {"@GlobalScope", "AStar2D", "AStarGrid2D", "FileAccess", "InputEvent", "InputEventAction", "InputEventFromWindow", "InputEventGesture", "InputEventJoypadButton", "InputEventJoypadMotion", "InputEventKey", "InputEventMagnifyGesture", "InputEventMouse", "InputEventMouseButton", "InputEventMouseMotion", "InputEventPanGesture", "InputEventScreenDrag", "InputEventScreenTouch", "InputEventWithModifiers", "InputMap", "Node", "Object", "PackedScene", "ProjectSettings", "OptimizedTranslation", "CompressedTexture2D", "Font", "FontFile", "FontVariation", "ImageFormatLoader", "ImageFormatLoaderExtension", "PortableCompressedTexture2D", "ResourceFormatLoader", "ResourceLoader", "ResourceSaver", "SystemFont", "VideoStream", "VideoStreamPlayback", "VideoStreamTheora", "SceneTree", "SceneTreeTimer", "Translation", "Vector2", "Vector3", "Vector4", "WeakRef"} or name in PHYSICS_AUDITED_TYPES or (name.startswith("Packed") and name.endswith("Array")) else "2026-09-23"
        if name in {"RigidBody2D", "CapsuleShape2D", "SegmentShape2D", "ConvexPolygonShape2D", "ConcavePolygonShape2D", "CollisionPolygon2D", "CollisionShape2D", "CollisionObject2D", "PhysicsBody2D", "KinematicCollision2D", "PhysicsTestMotionParameters2D", "PhysicsTestMotionResult2D", "AnimatableBody2D", "StaticBody2D", "RID", "World2D", "PhysicsServer2D", "PhysicsRayQueryParameters2D", "PhysicsPointQueryParameters2D", "PhysicsShapeQueryParameters2D", "PhysicsDirectSpaceState2D", "CanvasItem", "RayCast2D", "ShapeCast2D"}:
            updated = "2026-09-25"
        if name in {"Area2D", "CharacterBody2D", "PhysicsBody2D", "PhysicsServer2D", "PhysicsDirectSpaceState2D", "Shape2D", "SeparationRayShape2D", "PhysicsTestMotionParameters2D", "Geometry2D", "PhysicsDirectBodyState2D", "RigidBody2D", "CollisionObject2D", "CollisionShape2D", "CollisionPolygon2D"}:
            updated = "2026-09-26"
        if name in {"AcceptDialog", "ConfirmationDialog", "MenuButton", "FileDialog", "SpinBox", "ColorPicker", "ColorPickerButton", "ColorPalette", "TranslationServer"}:
            updated = "2026-10-06"
        if name in {"Node", "Control", "ProjectSettings", "ScrollBar", "HScrollBar", "VScrollBar", "ScrollContainer", "Joint2D", "PinJoint2D", "DampedSpringJoint2D", "GrooveJoint2D"}:
            updated = "2026-09-30"
        if name in {"RenderingServer", "CanvasItem", "Resource", "Texture", "Texture2D", "PhysicsServer2D", "FlowContainer", "HFlowContainer", "VFlowContainer", "Script", "CSharpScript", "AudioServer", "AudioStream", "AudioStreamWAV", "AudioStreamPlayer", "AudioStreamPlayback", "AudioStreamPlaybackResampled"}:
            updated = "2026-10-01"
        if name in {"Mesh", "ArrayMesh", "MeshInstance2D", "AudioStreamRandomizer", "AudioStreamMP3", "AudioStreamOggVorbis", "AudioStreamPlaybackOggVorbis", "OggPacketSequence", "OggPacketSequencePlayback", "AudioStreamPlayback", "AudioStreamPlaybackResampled", "AudioStreamPlayer", "ResourceLoader"}:
            updated = "2026-10-02"
        if name in {"ImmediateMesh", "AudioStreamGenerator", "AudioStreamGeneratorPlayback"}:
            updated = "2026-10-02"
        if name in {"MultiMesh", "MultiMeshInstance2D", "Mesh", "CanvasItem", "RenderingServer", "AudioStreamSynchronized", "AudioStreamPlaybackSynchronized", "AudioStreamInteractive", "AudioStreamPlaybackInteractive", "AudioStreamPlayback"}:
            updated = "2026-10-03"
        if name in {"Animation", "AnimationLibrary", "AnimationMixer", "AnimationPlayer"}:
            updated = "2026-10-04"
        if name in {"PacketPeerDTLS", "DTLSServer", "UPNP", "UPNPDevice", "Crypto", "CryptoKey", "X509Certificate", "HashingContext", "HMACContext", "AESContext", "HTTPClient", "HTTPRequest", "StreamPeerGZIP", "StreamPeerTCP", "WebSocketPeer", "WebSocketMultiplayerPeer", "MultiplayerPeer", "MultiplayerPeerExtension", "OfflineMultiplayerPeer", "MultiplayerAPI", "MultiplayerAPIExtension", "SceneMultiplayer", "MultiplayerSpawner", "MultiplayerSynchronizer", "SceneReplicationConfig", "ENetConnection", "ENetPacketPeer", "ENetMultiplayerPeer", "WebRTCMultiplayerPeer"}:
            updated = "2026-10-04"
        if name in {"Engine", "ProjectSettings", "Input", "InputMap", "ThemeDB", "AudioServer", "PhysicsServer2D", "DisplayServer", "RenderingServer"}:
            updated = "2026-10-04"
        if name in {"RichTextLabel", "RichTextEffect", "CharFXTransform", "GraphElement", "GraphNode", "GraphFrame", "GraphEdit", "@GlobalScope", "TextServer"}:
            updated = "2026-10-07"
        if name in {"StaticBody2D", "AnimatableBody2D", "PhysicsDirectBodyState2D", "PhysicsServer2D", "RigidBody2D", "Area2D", "Joint2D", "PinJoint2D", "Shape2D", "WorldBoundaryShape2D", "PhysicsDirectBodyState2DExtension", "PhysicsDirectSpaceState2D", "PhysicsDirectSpaceState2DExtension", "PhysicsPointQueryParameters2D", "PhysicsServer2DExtension", "PhysicsServer2DManager", "Viewport", "CollisionObject2D", "PhysicsBody2D"}:
            updated = "2026-10-08"
        if name in {"PhysicsDirectSpaceState2D", "PhysicsBody2D", "PhysicsServer2D", "CharacterBody2D", "PhysicsTestMotionParameters2D", "RigidBody2D"}:
            updated = "2026-10-09"
        if name == "MenuBar":
            updated = "2026-10-10"
        lines = [] if page in page_text else [f"# {page_name} API coverage", "", f"Last updated: {updated}", ""]
        if page_name == "Texture":
            if page not in page_text:
                lines.extend(["The reference Texture and Texture2D contracts share one Electron2D Texture page under [ADR 0004](../../decisions/product.md#adr-0004). Each source declaration remains accounted for below.", ""])
            lines.extend([f"## Godot {name}", ""])
        lines.extend([f"Godot source: [{source}]({url}) at `{upstream['godot_version']}` (`{COMMIT}`).", "",
                 f"Godot base: {inherited}. "
                 f"Electron2D type: {', '.join(engine_link(engine_by_id[f'T:{owner}']) for owner in owners) if owners else '—'}.", "",
                 "Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.", ""])
        if name in {"Script", "CSharpScript"}:
            lines.extend(["[ADR 0091](../../decisions/scripting.md#adr-0091) maps both reference classes to one future concrete Electron2D `Script : Resource` for C#. No separate CSharpScript type is planned. All source declarations remain accounted for; the resource and its applicable API are not implemented.", ""])
        if name == "PackedColorArray":
            lines.extend(["The separate packed container is excluded under [ADR 0001](../../decisions/product.md#adr-0001). Implemented call sites project color sequences to `Color[]` or `ReadOnlySpan<Color>`; see [Gradient](Gradient.md) and [CanvasItem](CanvasItem.md). Their copying and ownership contracts are audited on those APIs. Binary conversion remains a separate row below.", ""])
        if name == "DisplayServer":
            lines.extend(["Current release verification requires Linux/Wayland only under [ADR 0021](../../decisions/product.md#adr-0021). The earlier self-contained host example, before Window/Engine.Run migration, started on Wayland with packaged SDL and advanced its scene; user-assisted physical arrow-key input and Escape exit passed. See the [class verification](../../classes/DisplayServer.md#verification). Other target platforms remain in the product matrix without blocking this stage.", ""])
        lines.extend(["| Godot API | Electron2D API | State | Reason / implementation trigger |",
                      "| --- | --- | --- | --- |"])
        class_engine = engine_by_id.get(f"T:{owners[0]}") if owners else None
        if class_engine:
            used_engine.add(class_engine["id"])
        seen_upstream.add(godot_type["id"])
        counts[class_state] += 1
        classified[name].append((godot_type["id"], class_state, class_reason))
        class_status[class_state] += 1
        lines.append(f"| [{code('class ' + name)}]({url}) | {engine_link(class_engine) if class_engine else '—'} | {class_state} | {cell(class_reason)} |")
        used_local = set()
        member_states = Counter()
        for member in godot_type["members"]:
            seen_upstream.add(member["id"])
            special = (special_reason(godot_type, member) if owners or class_state != "Excluded"
                       or name.startswith("Packed") and name.endswith("Array") else None)
            if class_state == "Blocked" and special and special[0] == "Blocked" and "operation-specific retained-canvas" in special[1]:
                special = (class_state, class_reason)
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
            classified[name].append((member["id"], state, reason))
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
    lines = ["# Electron2D declarations without an audited upstream row", "", "Last updated: 2026-10-07", "",
             "These declarations are present in the compiled runtime. A blank upstream cell means no exact counterpart was established by the conservative name-and-arity mapper; it does not claim an intentional extension. Review each against the linked Godot class page and record a rationale before declaring parity.", "",
             "| Godot API | Electron2D API | State | Reason / next action |", "| --- | --- | --- | --- |"]
    for entry in engine_only:
        extra = manual_extras.get(entry["id"])
        state = "Implemented" if extra else "Unmapped"
        reason = f"Electron2D-specific: {extra['reason']} ({extra.get('adr', 'class reference')})." if extra else "Audit the corresponding type family; document a typed-C# rationale or link the exact upstream row."
        lines.append(f"| — | {engine_link(entry, from_class=False)} | {state} | {cell(reason)} |")
    page_text[COVERAGE / "electron2d-unmapped.md"] = "\n".join(lines) + "\n"
    catalog = ["# Godot class-reference catalog", "", "Last updated: 2026-10-07", "",
               f"Source: Godot `{upstream['godot_version']}` at `{COMMIT}`. Every XML class is listed, including editor and 3D exclusions. Texture pages use Electron2D names; Texture and Texture2D share one page with separate source sections.", "",
               "| Godot class | Base | Class state | Declared members |", "| --- | --- | --- | ---: |"]
    for item in upstream["types"]:
        state = "Partial" if engine_name(aliases.get("classes", {}).get(item["name"], TEXTURE_NAMES.get(item["name"], item["name"]))) in engine_types else reason_for_type(item, type_lookup)[0]
        if item["id"] in manual_statuses:
            state = manual_statuses[item["id"]]["state"]
        catalog.append(f"| [{cell(item['name'])}](classes/{coverage_target(item['name'])}) | {cell(item['inherits'] or '—')} | {state} | {len(item['members'])} |")
    page_text[COVERAGE / "catalog.md"] = "\n".join(catalog) + "\n"
    actionable_note = (" Reassess dependencies for " + ", ".join(f"[{name}](classes/{coverage_target(name)})" for name in actionable) + " before selecting their slices.") if actionable else ""
    road = ["# Coverage roadmap", "", "Last updated: 2026-10-08", "",
            "Choose each next executable vertical slice by user API value, dependent work unlocked and current-backend feasibility. Resolve its applicable Partial rows with behavior evidence; do not treat easy isolated audits as the roadmap. `Unmapped` Electron2D rows need an exact upstream link or documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.", "",
            f"1. Close {counts['Partial']} partially implemented rows and {len(engine_only) - len(manual_extras)} unmapped Electron2D declarations within connected executable slices, including core, input, scene, resource and image domains.",
            f"2. Complete {counts['Unimplemented']} missing declarations in already represented type families; split each type by its documented dependency trigger.{actionable_note}",
            "3. Complete the missing 2D renderer integrations, then GUI/theme and tiles; complete CPU and independent GPU physics under ADR 0054; audio/navigation/animation; asset loaders and networking; and the self-hosted editor. Finish specific display/input host gaps at their documented triggers. The first executable GL/EGL/GLX fallback slice must audit each of the five blocked `DisplayServer.HandleType` identities against its actual driver and window-associated context under ADR 0042.", "",
            "## Existing type backlog", "",
            "These classes already have an Electron2D type. The counts scope work; they do not rank the next slice or authorize skipping a dependency.", "",
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
    page_text[COVERAGE / "physics-status.md"] = render_physics(upstream, classified, coverage_target)
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
