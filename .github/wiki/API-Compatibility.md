# API Compatibility

Status: GitHub Wiki source for `Electron2D 0.1.0 Preview`.
Updated: 2026-06-21.

Electron2D follows Godot architecture, terminology and expected behavior for the agreed 2D subset, but it does not promise source compatibility with Godot projects, GDScript or Godot C#.

The clean rewrite baseline currently exports the first object-model, 2D math, random number generator, identity and Variant value-carrier types. Planned entries below describe the target public surface for future tasks, not implemented API.

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
| `Electron2D.Callable` | `Callable` | Partial | Target-method and C# action callable baseline for synchronous signal emission and deferred calls. |
| `Electron2D.Collections.Array` | `Godot.Collections.Array` | Partial | Mutable reference-like Variant list for the 0.1 closed Variant type set. |
| `Electron2D.Collections.Dictionary` | `Godot.Collections.Dictionary` | Partial | Mutable reference-like Variant key/value map for the 0.1 closed Variant type set. |
| `Electron2D.Color` | `Color` | Partial | RGBA value type baseline with arithmetic, interpolation, clamp and HTML conversion. |
| `Electron2D.ConnectFlags` | `ConnectFlags` | Partial | Godot-like flag names are declared; advanced flag semantics are still planned. |
| `Electron2D.Error` | `Error` | Partial | Minimal signal/runtime error result values. |
| `Electron2D.InputEvent` | `InputEvent` | Partial | Placeholder base input event type for lifecycle dispatch. |
| `Electron2D.Mathf` | `Mathf` | Partial | Basic constants, approximate comparison, clamp, interpolation, angle conversion, modulo and snapping helpers. |
| `Electron2D.Object` | `GodotObject` / `Object` | Partial | Instance id, `Free()`, `CallDeferred()`, `IsQueuedForDeletion()` and `IsInstanceValid()` baseline. |
| `Electron2D.Node` | `Node` | Partial | Lifecycle, hierarchy, `Owner`, groups, reparent/move and `QueueFree()` baseline. |
| `Electron2D.NodePath` | `NodePath` | Partial | Relative/absolute node path parsing and `GetNode()`/`GetNodeOrNull()` resolution baseline. |
| `Electron2D.PackedScene` | `PackedScene` | Partial | In-memory pack/instantiate baseline for owned node subtrees. |
| `Electron2D.RandomNumberGenerator` | `RandomNumberGenerator` | Partial | Godot-like RNG baseline with seed/state replay, integer/float ranges and PCG32 sequence policy for 0.1. |
| `Electron2D.Rect2` | `Rect2` | Partial | Floating-point axis-aligned rectangle baseline with intersection, merge, grow and normalization helpers. |
| `Electron2D.Rect2I` | `Rect2I` | Partial | Integer axis-aligned rectangle baseline with intersection, merge, grow and normalization helpers. |
| `Electron2D.RefCounted` | `RefCounted` | Partial | Manual reference count baseline with `Reference()`, `Unreference()` and `GetReferenceCount()`. |
| `Electron2D.Resource` | `Resource` | Partial | `ResourceName`, `ResourcePath`, `ResourceLocalToScene`, `ResourceSceneUniqueId` and `TakeOverPath()`. |
| `Electron2D.Rid` | `RID` | Partial | Opaque resource identifier baseline with invalid ID `0`, equality, hashing and ordering. |
| `Electron2D.SceneTree` | `SceneTree` | Partial | Initial root node, current scene, deterministic tree traversal for tests and future editor/runtime tools, scene change, group queries/calls, deferred queue flush and queued deletion flush. |
| `Electron2D.StringName` | `StringName` | Partial | Immutable interned-name baseline with ordinal equality, hashing, empty/default semantics and string conversion. |
| `Electron2D.Transform2D` | `Transform2D` | Partial | 2D basis/origin transform baseline with point transforms, composition and inverse. |
| `Electron2D.Variant` | `Variant` | Partial | Closed 0.1 value carrier for nil, primitives, enum-as-int, 2D math, identity handles, Object-derived values, Callable and Godot-like collections. |
| `Electron2D.Variant+Type` | `Variant.Type` | Partial | Closed 0.1 Variant type enum; 3D, Signal and packed arrays are intentionally excluded for now. |
| `Electron2D.Vector2` | `Vector2` | Partial | Floating-point 2D vector baseline with arithmetic, length, dot/cross, interpolation and formatting. |
| `Electron2D.Vector2I` | `Vector2I` | Partial | Integer 2D vector baseline with arithmetic, length, aspect and conversions. |

## Planned Godot-like 2D Surface

| API | Godot analogue | Status | Notes |
| --- | --- | --- | --- |
| `Electron2D.Node2D` | `Node2D` | Planned | 2D transform node. |
| `Electron2D.CanvasItem` | `CanvasItem` | Planned | Visibility, modulate and draw ordering base. |
| `Electron2D.CanvasLayer` | `CanvasLayer` | Planned | 2D canvas layer support. |
| `Electron2D.Viewport` | `Viewport` | Planned | Runtime viewport subset for 2D projects. |
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
