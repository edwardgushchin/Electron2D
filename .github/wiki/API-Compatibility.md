# API Compatibility

Status: GitHub Wiki source for `Electron2D 0.1.0 Preview`.
Updated: 2026-06-20.

Electron2D follows Godot architecture, terminology and expected behavior for the agreed 2D subset, but it does not promise source compatibility with Godot projects, GDScript or Godot C#.

The clean rewrite baseline currently exports only the first object-model types. Planned entries below describe the target public surface for future tasks, not implemented API.

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
| `Electron2D.Object` | `GodotObject` / `Object` | Partial | Instance id, `Free()` and `IsInstanceValid()` baseline. |
| `Electron2D.RefCounted` | `RefCounted` | Partial | Manual reference count baseline with `Reference()`, `Unreference()` and `GetReferenceCount()`. |
| `Electron2D.Resource` | `Resource` | Partial | `ResourceName`, `ResourcePath`, `ResourceLocalToScene`, `ResourceSceneUniqueId` and `TakeOverPath()`. |
| `Electron2D.Node` | `Node` | Partial | Initial lifecycle baseline with tree entry, ready, process, physics process, input and exit callbacks. |
| `Electron2D.SceneTree` | `SceneTree` | Partial | Initial root node and deterministic lifecycle/test host traversal. |
| `Electron2D.InputEvent` | `InputEvent` | Partial | Placeholder base input event type for lifecycle dispatch. |

## Planned Godot-like 2D Surface

| API | Godot analogue | Status | Notes |
| --- | --- | --- | --- |
| `Electron2D.PackedScene` | `PackedScene` | Planned | Save/load/instantiate scene resources. |
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
