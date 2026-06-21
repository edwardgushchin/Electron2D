# API Compatibility

Status: GitHub Wiki source for `Electron2D 0.1.0 Preview`.
Updated: 2026-06-21.

Electron2D follows Godot architecture, terminology and expected behavior for the agreed 2D subset, but it does not promise source compatibility with Godot projects, GDScript or Godot C#.

The clean rewrite baseline currently exports the first object-model, resource UID, 2D math, random number generator, identity, Variant value-carrier, C# scripting marker attributes, keyboard/mouse input event baseline, texture/viewport/shader, text/UI baseline and rendering server boundary types. Planned entries below describe the target public surface for future tasks, not implemented API.

## Status Legend

| Status | Meaning |
| --- | --- |
| Supported | Implemented, tested and documented |
| Partial | Implemented only for the described subset |
| Experimental | Implemented but allowed to change before stable release |
| Planned | Required by `0.1.0 Preview`, not implemented yet |
| Not planned | Intentionally excluded from the public API |

## Current Public Runtime Surface

| API | Godot analogue | Status | Notes |
| --- | --- | --- | --- |
| `Electron2D.AtlasTexture` | `AtlasTexture` | Partial | Atlas region resource with atlas, region, margin, filter clip and transparency delegation. |
| `Electron2D.Callable` | `Callable` | Partial | Target-method and C# action callable baseline for synchronous signal emission and deferred calls. |
| `Electron2D.Camera2D` | `Camera2D` | Partial | Current 2D camera selection, target/center/rotation queries, offset, zoom and documented smoothing no-op baseline. |
| `Electron2D.CanvasItem` | `CanvasItem` | Partial | Visibility, inherited modulate, self-modulate, z-index, y-sort flag and show/hide baseline. |
| `Electron2D.CanvasLayer` | `CanvasLayer` | Partial | Layer, visibility and transform baseline for independent 2D canvas ordering. |
| `Electron2D.Collections.Array` | `Godot.Collections.Array` | Partial | Mutable reference-like Variant list for the 0.1 closed Variant type set. |
| `Electron2D.Collections.Dictionary` | `Godot.Collections.Dictionary` | Partial | Mutable reference-like Variant key/value map for the 0.1 closed Variant type set. |
| `Electron2D.Color` | `Color` | Partial | RGBA value type baseline with arithmetic, interpolation, clamp and HTML conversion. |
| `Electron2D.ConnectFlags` | `ConnectFlags` | Partial | Godot-like flag names are declared; advanced flag semantics are still planned. |
| `Electron2D.Control` | `Control` | Partial | UI base node with position, size and minimal font theme overrides for text baseline. |
| `Electron2D.Error` | `Error` | Partial | Minimal signal/runtime error result values. |
| `Electron2D.ExportAttribute` | `ExportAttribute` / `[Export]` | Partial | Marker attribute for script fields/properties included in explicit serialization and Inspector metadata. |
| `Electron2D.Font` | `Font` | Partial | Base font resource with string measurement, glyph availability, fallback layout and internal cache. |
| `Electron2D.HorizontalAlignment` | `HorizontalAlignment` | Partial | Godot-like horizontal alignment values used by text drawing APIs. |
| `Electron2D.InputEvent` | `InputEvent` | Partial | Base input event resource with device id for lifecycle dispatch. |
| `Electron2D.InputEventFromWindow` | `InputEventFromWindow` | Partial | Window id layer for SDL-backed keyboard and mouse events. |
| `Electron2D.InputEventKey` | `InputEventKey` | Partial | Keyboard key down/up, echo and SDL text input Unicode scalar baseline. |
| `Electron2D.InputEventMouse` | `InputEventMouse` | Partial | Shared mouse position and button mask baseline. |
| `Electron2D.InputEventMouseButton` | `InputEventMouseButton` | Partial | Mouse button down/up and wheel-as-button baseline. |
| `Electron2D.InputEventMouseMotion` | `InputEventMouseMotion` | Partial | Mouse position, relative motion and button mask baseline. |
| `Electron2D.InputEventWithModifiers` | `InputEventWithModifiers` | Partial | Modifier state layer for keyboard and future mouse input events. |
| `Electron2D.Key` | `Key` | Partial | Printable ASCII and core special key constants used by SDL keyboard mapping. |
| `Electron2D.KeyLocation` | `KeyLocation` | Partial | Left/right modifier key location baseline. |
| `Electron2D.Label` | `Label` | Partial | Single-line plain text control backed by theme font overrides and `CanvasItem.DrawString`. |
| `Electron2D.Material` | `Material` | Partial | Base visual material resource with `NextPass` and `RenderPriority` storage for future renderer ordering. |
| `Electron2D.Mathf` | `Mathf` | Partial | Basic constants, approximate comparison, clamp, interpolation, angle conversion, modulo and snapping helpers. |
| `Electron2D.MouseButton` | `MouseButton` | Partial | Left/right/middle, wheel and extra button constants for mouse events. |
| `Electron2D.MouseButtonMask` | `MouseButtonMask` | Partial | Godot-like held mouse button bit flags converted from SDL state. |
| `Electron2D.Object` | `GodotObject` / `Object` | Partial | Instance id, `Free()`, `CallDeferred()`, `IsQueuedForDeletion()` and `IsInstanceValid()` baseline. |
| `Electron2D.Node` | `Node` | Partial | Lifecycle, hierarchy, `Owner`, groups, reparent/move and `QueueFree()` baseline. |
| `Electron2D.Node2D` | `Node2D` | Partial | Local/global 2D position, rotation, scale, transform conversion and transform-preserving reparent baseline. |
| `Electron2D.NodePath` | `NodePath` | Partial | Relative/absolute node path parsing and `GetNode()`/`GetNodeOrNull()` resolution baseline. |
| `Electron2D.PackedScene` | `PackedScene` | Partial | In-memory pack/instantiate baseline for owned node subtrees. |
| `Electron2D.RandomNumberGenerator` | `RandomNumberGenerator` | Partial | Godot-like RNG baseline with seed/state replay, integer/float ranges and PCG32 sequence policy for 0.1. |
| `Electron2D.Rect2` | `Rect2` | Partial | Floating-point axis-aligned rectangle baseline with intersection, merge, grow and normalization helpers. |
| `Electron2D.Rect2I` | `Rect2I` | Partial | Integer axis-aligned rectangle baseline with intersection, merge, grow and normalization helpers. |
| `Electron2D.RefCounted` | `RefCounted` | Partial | Manual reference count baseline with `Reference()`, `Unreference()` and `GetReferenceCount()`. |
| `Electron2D.RenderingServer` | `RenderingServer` | Partial | Singleton-style facade for active renderer profile and feature flags; concrete backends remain internal. |
| `Electron2D.RenderingServer+RenderingFeature` | `RenderingServer` feature enum | Partial | 0.1 feature flags for compatibility and standard renderer profiles. |
| `Electron2D.RenderingServer+RenderingProfile` | `RenderingServer` profile enum | Partial | `Compatibility` and `Standard` renderer profiles. |
| `Electron2D.Resource` | `Resource` | Partial | `ResourceName`, `ResourcePath`, `ResourceLocalToScene`, `ResourceSceneUniqueId` and `TakeOverPath()`. |
| `Electron2D.ResourceUid` | `ResourceUID` / `ResourceUid` | Partial | Stable `uid://` conversion and in-memory UID-to-path mapping for resource references. |
| `Electron2D.Rid` | `RID` | Partial | Opaque resource identifier baseline with invalid ID `0`, equality, hashing and ordering. |
| `Electron2D.SceneTree` | `SceneTree` | Partial | Initial root node, current scene, deterministic tree traversal for tests and future editor/runtime tools, scene change, group queries/calls, deferred queue flush and queued deletion flush. |
| `Electron2D.Shader` | `Shader` | Partial | Canvas item shader resource storing source code for import-time compilation. |
| `Electron2D.Shader+Mode` | `Shader.Mode` | Partial | 0.1 canvas item shader mode subset. |
| `Electron2D.ShaderMaterial` | `ShaderMaterial` | Partial | Material resource with `Shader`, supported uniform values, `Texture2D` samplers and fail-closed reserved built-in validation. |
| `Electron2D.SignalAttribute` | `SignalAttribute` / `[Signal]` | Partial | Marker attribute for script delegates backed by explicit signal metadata and the existing `Connect()`/`EmitSignal()` API. |
| `Electron2D.Sprite2D` | `Sprite2D` | Partial | Texture, centered/offset drawing rect, region rect, flip flags, pixel opacity and internal submission baseline. |
| `Electron2D.StringName` | `StringName` | Partial | Immutable interned-name baseline with ordinal equality, hashing, empty/default semantics and string conversion. |
| `Electron2D.Texture2D` | `Texture2D` | Partial | Abstract texture resource baseline for size, alpha, mipmaps and pixel opacity queries. |
| `Electron2D.ToolAttribute` | `ToolAttribute` / `[Tool]` | Experimental | Marker attribute for editor-time script intent; current metadata marks it experimental and sandboxed. |
| `Electron2D.Transform2D` | `Transform2D` | Partial | 2D basis/origin transform baseline with point transforms, composition and inverse. |
| `Electron2D.Variant` | `Variant` | Partial | Closed 0.1 value carrier for nil, primitives, enum-as-int, 2D math, identity handles, Object-derived values, Callable and Godot-like collections. |
| `Electron2D.Variant+Type` | `Variant.Type` | Partial | Closed 0.1 Variant type enum; 3D, Signal and packed arrays are intentionally excluded for now. |
| `Electron2D.Vector2` | `Vector2` | Partial | Floating-point 2D vector baseline with arithmetic, length, dot/cross, interpolation and formatting. |
| `Electron2D.Vector2I` | `Vector2I` | Partial | Integer 2D vector baseline with arithmetic, length, aspect and conversions. |
| `Electron2D.VerticalAlignment` | `VerticalAlignment` | Partial | Godot-like vertical alignment values used by text controls. |
| `Electron2D.Viewport` | `Viewport` | Partial | Runtime 2D viewport subset with current camera, visible rect, canvas transform, pixel snapping and root instance baseline. |
| `Electron2D.ViewportTexture` | `ViewportTexture` | Partial | Dynamic viewport texture returned by `Viewport.GetTexture()` with scene-local texture metadata. |

## Planned Godot-like 2D Surface

| API | Godot analogue | Status | Notes |
| --- | --- | --- | --- |
| `Electron2D.Timer` | `Timer` | Planned | Godot-like timer node. |

## Explicitly Not Planned Legacy API

| API | Godot analogue | Status | Notes |
| --- | --- | --- | --- |
| `Electron2D.IComponent` | None | Not planned | Unity-like component history is removed. |
| `Electron2D.SpriteRenderer` | None | Not planned | Rendering must use Godot-like nodes such as future `Sprite2D`. |
| `Electron2D.SpriteAnimator` | None | Not planned | Animation must use Godot-like resources/nodes such as future `SpriteFrames` and `AnimatedSprite2D`. |
| `Electron2D.AudioSource` | None | Not planned | Unity-like audio component is excluded. |
| `Electron2D.Rigidbody` | None | Not planned | Legacy component physics is excluded. |
| `Electron2D.Collider` | None | Not planned | Legacy component physics is excluded. |
| `Electron2D.BoxCollider` | None | Not planned | Future collision API must follow Godot-like `CollisionShape2D` and shape resources. |
| `Electron2D.CircleCollider` | None | Not planned | Future collision API must follow Godot-like `CollisionShape2D` and shape resources. |
| `Electron2D.PolygonCollider` | None | Not planned | Future collision API must follow Godot-like `CollisionPolygon2D` or agreed shape subset. |
| `Electron2D.PhysicsBodyType` | None | Not planned | Future physics body selection must use Godot-like body node types. |
