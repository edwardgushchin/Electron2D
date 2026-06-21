# API Compatibility

Status: GitHub Wiki source for `Electron2D 0.1.0 Preview`.
Updated: 2026-06-21.

Electron2D documents the agreed 2D runtime API subset here. This table is a release guard for public types and intentionally excludes removed legacy component APIs.

The clean rewrite baseline currently exports the first object-model, resource UID, 2D math, random number generator, identity, Variant value-carrier, C# scripting marker attributes, keyboard/mouse input event baseline, texture/viewport/shader, text/UI baseline, frame-based sprite animation, resource animation tracks, `AnimationPlayer`, `Tween`, rendering server boundary, physics server RID-boundary types, first 2D physics nodes, concrete 2D shape resources, physics material resource, `Area2D` overlap signals baseline, direct 2D physics query baseline, fixed physics tick, basic rigid body movement, `CharacterBody2D` kinematic movement baseline and debug collision shape hooks. Planned entries below describe the target public surface for future tasks, not implemented API.

## Status Legend

| Status | Meaning |
| --- | --- |
| Supported | Implemented, tested and documented |
| Partial | Implemented only for the described subset |
| Experimental | Implemented but allowed to change before stable release |
| Planned | Required by `0.1.0 Preview`, not implemented yet |
| Not planned | Intentionally excluded from the public API |

## Current Public Runtime Surface

| API | Reference | Status | Notes |
| --- | --- | --- | --- |
| `Electron2D.AnimatedSprite2D` | `AnimatedSprite2D` | Partial | Frame-based 2D animation node with `SpriteFrames`, autoplay, play/pause/stop, frame progress, loop handling and canvas submission of the current frame. |
| `Electron2D.Animation` | `Animation` | Partial | Resource for value tracks and method call tracks with deterministic key ordering and interpolation for the supported Variant subset. |
| `Electron2D.Animation+InterpolationTypeEnum` | `Animation.InterpolationType` | Partial | Nearest and linear interpolation modes for value tracks. |
| `Electron2D.Animation+LoopModeEnum` | `Animation.LoopMode` | Partial | None and linear loop modes for resource animation playback. |
| `Electron2D.Animation+TrackTypeEnum` | `Animation.TrackType` | Partial | Value and method track kinds for the 0.1 animation baseline. |
| `Electron2D.AnimationLibrary` | `AnimationLibrary` | Partial | Named animation resource collection with deterministic name listing and add/remove/rename operations. |
| `Electron2D.AnimationPlayer` | `AnimationPlayer` | Partial | Node that plays animation libraries, applies value tracks through `NodePath`, calls method tracks, queues animations and emits `animation_finished`. |
| `Electron2D.Area2D` | `Area2D` | Partial | RID lifecycle, monitoring flags, priority, transform sync, AABB overlap snapshots, `body_entered`/`body_exited` and `area_entered`/`area_exited` signals. |
| `Electron2D.AtlasTexture` | `AtlasTexture` | Partial | Atlas region resource with atlas, region, margin, filter clip and transparency delegation. |
| `Electron2D.Callable` | `Callable` | Partial | Target-method and C# action callable baseline for synchronous signal emission and deferred calls. |
| `Electron2D.CallbackTweener` | `CallbackTweener` | Partial | Tween step that invokes a `Callable` once when the sequence reaches it. |
| `Electron2D.Camera2D` | `Camera2D` | Partial | Current 2D camera selection, target/center/rotation queries, offset, zoom and documented smoothing no-op baseline. |
| `Electron2D.CanvasItem` | `CanvasItem` | Partial | Visibility, inherited modulate, self-modulate, z-index, y-sort flag, show/hide and `GetWorld2D()` baseline. |
| `Electron2D.CanvasLayer` | `CanvasLayer` | Partial | Layer, visibility and transform baseline for independent 2D canvas ordering. |
| `Electron2D.CapsuleShape2D` | `CapsuleShape2D` | Partial | Capsule shape resource with radius/height validation, RID creation and AOT-safe serialization metadata. |
| `Electron2D.CharacterBody2D` | `CharacterBody2D` | Partial | Kinematic character body with `Velocity`, `MoveAndSlide()`, floor/wall/ceiling state, floor snap and platform velocity baseline. |
| `Electron2D.CharacterBody2D+MotionModeEnum` | `CharacterBody2D.MotionMode` | Partial | Grounded and floating collision classification modes. |
| `Electron2D.CharacterBody2D+PlatformOnLeaveEnum` | `CharacterBody2D.PlatformOnLeave` | Partial | Platform leave policy values reserved for the kinematic movement API. |
| `Electron2D.CircleShape2D` | `CircleShape2D` | Partial | Circle shape resource with radius validation, RID creation and AOT-safe serialization metadata. |
| `Electron2D.Collections.Array` | `Array` | Partial | Mutable reference-like Variant list for the 0.1 closed Variant type set. |
| `Electron2D.Collections.Dictionary` | `Dictionary` | Partial | Mutable reference-like Variant key/value map for the 0.1 closed Variant type set. |
| `Electron2D.CollisionObject2D` | `CollisionObject2D` | Partial | Base collision object with `GetRid()`, collision layer/mask storage, RID lifecycle and transform sync. |
| `Electron2D.CollisionShape2D` | `CollisionShape2D` | Partial | Shape reference, disabled/one-way flags, debug visualization color and concave-shape static-body validation. |
| `Electron2D.Color` | `Color` | Partial | RGBA value type baseline with arithmetic, interpolation, clamp and HTML conversion. |
| `Electron2D.ConcavePolygonShape2D` | `ConcavePolygonShape2D` | Partial | Concave segment-pair shape resource restricted to `StaticBody2D` through `CollisionShape2D`. |
| `Electron2D.ConnectFlags` | `ConnectFlags` | Partial | Signal connection flag names are declared; advanced flag semantics are still planned. |
| `Electron2D.Control` | `Control` | Partial | UI base node with position, size and minimal font theme overrides for text baseline. |
| `Electron2D.ConvexPolygonShape2D` | `ConvexPolygonShape2D` | Partial | Convex polygon shape resource with finite point validation and AOT-safe array serialization. |
| `Electron2D.Error` | `Error` | Partial | Minimal signal/runtime error result values. |
| `Electron2D.ExportAttribute` | `ExportAttribute` / `[Export]` | Partial | Marker attribute for script fields/properties included in explicit serialization and Inspector metadata. |
| `Electron2D.Font` | `Font` | Partial | Base font resource with string measurement, glyph availability, fallback layout and internal cache. |
| `Electron2D.HorizontalAlignment` | `HorizontalAlignment` | Partial | Horizontal alignment values used by text drawing APIs. |
| `Electron2D.InputEvent` | `InputEvent` | Partial | Base input event resource with device id for lifecycle dispatch. |
| `Electron2D.InputEventFromWindow` | `InputEventFromWindow` | Partial | Window id layer for platform-backed keyboard and mouse events. |
| `Electron2D.InputEventKey` | `InputEventKey` | Partial | Keyboard key down/up, echo and native text input Unicode scalar baseline. |
| `Electron2D.InputEventMouse` | `InputEventMouse` | Partial | Shared mouse position and button mask baseline. |
| `Electron2D.InputEventMouseButton` | `InputEventMouseButton` | Partial | Mouse button down/up and wheel-as-button baseline. |
| `Electron2D.InputEventMouseMotion` | `InputEventMouseMotion` | Partial | Mouse position, relative motion and button mask baseline. |
| `Electron2D.InputEventWithModifiers` | `InputEventWithModifiers` | Partial | Modifier state layer for keyboard and future mouse input events. |
| `Electron2D.IntervalTweener` | `IntervalTweener` | Partial | Tween step that consumes sequence time without writing properties or calling user callbacks. |
| `Electron2D.Key` | `Key` | Partial | Printable ASCII and core special key constants used by keyboard mapping. |
| `Electron2D.KeyLocation` | `KeyLocation` | Partial | Left/right modifier key location baseline. |
| `Electron2D.KinematicCollision2D` | `KinematicCollision2D` | Partial | Collision result object returned by body movement methods, with collider, normal, travel, remainder and velocity data. |
| `Electron2D.Label` | `Label` | Partial | Single-line plain text control backed by theme font overrides and `CanvasItem.DrawString`. |
| `Electron2D.Material` | `Material` | Partial | Base visual material resource with `NextPass` and `RenderPriority` storage for future renderer ordering. |
| `Electron2D.Mathf` | `Mathf` | Partial | Basic constants, approximate comparison, clamp, interpolation, angle conversion, modulo and snapping helpers. |
| `Electron2D.MouseButton` | `MouseButton` | Partial | Left/right/middle, wheel and extra button constants for mouse events. |
| `Electron2D.MouseButtonMask` | `MouseButtonMask` | Partial | Held mouse button bit flags converted from platform state. |
| `Electron2D.Object` | `Object` | Partial | Instance id, `Free()`, `CallDeferred()`, `IsQueuedForDeletion()` and `IsInstanceValid()` baseline. |
| `Electron2D.Node` | `Node` | Partial | Lifecycle, hierarchy, `Owner`, groups, reparent/move and `QueueFree()` baseline. |
| `Electron2D.Node2D` | `Node2D` | Partial | Local/global 2D position, rotation, scale, transform conversion and transform-preserving reparent baseline. |
| `Electron2D.NodePath` | `NodePath` | Partial | Relative/absolute node path parsing and `GetNode()`/`GetNodeOrNull()` resolution baseline. |
| `Electron2D.PackedScene` | `PackedScene` | Partial | In-memory pack/instantiate baseline for owned node subtrees. |
| `Electron2D.PhysicsBody2D` | `PhysicsBody2D` | Partial | Base class for body nodes with shared `PhysicsMaterialOverride` and `MoveAndCollide()` kinematic movement helper. |
| `Electron2D.PhysicsDirectSpaceState2D` | `PhysicsDirectSpaceState2D` | Partial | Managed AABB baseline for `IntersectRay`, `IntersectPoint` and `IntersectShape` over active `CollisionShape2D` nodes. |
| `Electron2D.PhysicsMaterial` | `PhysicsMaterial` | Partial | Body material resource with `Friction`, `Bounce`, `Rough`, `Absorbent`, validation and AOT-safe serialization metadata. |
| `Electron2D.PhysicsPointQueryParameters2D` | `PhysicsPointQueryParameters2D` | Partial | Point query parameters for direct 2D physics queries, including mask, body/area flags and excluded RIDs. |
| `Electron2D.PhysicsRayQueryParameters2D` | `PhysicsRayQueryParameters2D` | Partial | Ray query parameters for direct 2D physics queries, including endpoints, mask, body/area flags, hit-from-inside and excluded RIDs. |
| `Electron2D.PhysicsServer2D` | `PhysicsServer2D` | Partial | Low-level 2D physics server facade for RID allocation, spaces, shape kinds and internal backend swapping; no real simulation yet. |
| `Electron2D.PhysicsServer2D+ProcessInfo` | `PhysicsServer2D.ProcessInfo` | Partial | Process statistic enum; values currently report `0` until real simulation is implemented. |
| `Electron2D.PhysicsServer2D+ShapeType` | `PhysicsServer2D.ShapeType` | Partial | Shape type enum for physics server shape RID creation. |
| `Electron2D.PhysicsServer2D+SpaceParameter` | `PhysicsServer2D.SpaceParameter` | Partial | Space parameter enum with value round-trip through the server boundary. |
| `Electron2D.PhysicsShapeQueryParameters2D` | `PhysicsShapeQueryParameters2D` | Partial | Shape query parameters for direct 2D physics queries, including shape resource, transform, motion, margin, mask and excluded RIDs. |
| `Electron2D.PropertyTweener` | `PropertyTweener` | Partial | Tween step that captures a public property or field value and writes eased values until completion. |
| `Electron2D.RandomNumberGenerator` | `RandomNumberGenerator` | Partial | RNG baseline with seed/state replay, integer/float ranges and PCG32 sequence policy for 0.1. |
| `Electron2D.RayCast2D` | `RayCast2D` | Partial | Ray query settings, forced update and cached AABB hit result through `PhysicsDirectSpaceState2D`. |
| `Electron2D.Rect2` | `Rect2` | Partial | Floating-point axis-aligned rectangle baseline with intersection, merge, grow and normalization helpers. |
| `Electron2D.Rect2I` | `Rect2I` | Partial | Integer axis-aligned rectangle baseline with intersection, merge, grow and normalization helpers. |
| `Electron2D.RectangleShape2D` | `RectangleShape2D` | Partial | Rectangle shape resource with size validation, RID creation and AOT-safe serialization metadata. |
| `Electron2D.RefCounted` | `RefCounted` | Partial | Manual reference count baseline with `Reference()`, `Unreference()` and `GetReferenceCount()`. |
| `Electron2D.RenderingServer` | `RenderingServer` | Partial | Singleton-style facade for active renderer profile and feature flags; concrete backends remain internal. |
| `Electron2D.RenderingServer+RenderingFeature` | `RenderingServer` feature enum | Partial | 0.1 feature flags for compatibility and standard renderer profiles. |
| `Electron2D.RenderingServer+RenderingProfile` | `RenderingServer` profile enum | Partial | `Compatibility` and `Standard` renderer profiles. |
| `Electron2D.Resource` | `Resource` | Partial | `ResourceName`, `ResourcePath`, `ResourceLocalToScene`, `ResourceSceneUniqueId` and `TakeOverPath()`. |
| `Electron2D.ResourceUid` | `ResourceUID` / `ResourceUid` | Partial | Stable `uid://` conversion and in-memory UID-to-path mapping for resource references. |
| `Electron2D.Rid` | `RID` | Partial | Opaque resource identifier baseline with invalid ID `0`, equality, hashing and ordering. |
| `Electron2D.RigidBody2D` | `RigidBody2D` | Partial | RID lifecycle, stored rigid body properties, transform sync, fixed-step velocity movement and AABB sweep against static bodies. |
| `Electron2D.RigidBody2D+CenterOfMassModeEnum` | `RigidBody2D.CenterOfMassMode` | Partial | Center-of-mass mode values for `RigidBody2D`. |
| `Electron2D.RigidBody2D+FreezeModeEnum` | `RigidBody2D.FreezeMode` | Partial | Freeze behavior values for `RigidBody2D`. |
| `Electron2D.SceneTree` | `SceneTree` | Partial | Initial root node, current scene, deterministic tree traversal for tests and future editor/runtime tools, debug collision hint, scene change, group queries/calls, deferred queue flush and queued deletion flush. |
| `Electron2D.SegmentShape2D` | `SegmentShape2D` | Partial | Segment shape resource with endpoint validation, RID creation and AOT-safe serialization metadata. |
| `Electron2D.Shader` | `Shader` | Partial | Canvas item shader resource storing source code for import-time compilation. |
| `Electron2D.Shader+Mode` | `Shader.Mode` | Partial | 0.1 canvas item shader mode subset. |
| `Electron2D.ShaderMaterial` | `ShaderMaterial` | Partial | Material resource with `Shader`, supported uniform values, `Texture2D` samplers and fail-closed reserved built-in validation. |
| `Electron2D.Shape2D` | `Shape2D` | Partial | Base shape resource for `CollisionShape2D` with lazy physics RID creation and cleanup. |
| `Electron2D.SignalAttribute` | `SignalAttribute` / `[Signal]` | Partial | Marker attribute for script delegates backed by explicit signal metadata and the existing `Connect()`/`EmitSignal()` API. |
| `Electron2D.Sprite2D` | `Sprite2D` | Partial | Texture, centered/offset drawing rect, region rect, flip flags, pixel opacity and internal submission baseline. |
| `Electron2D.SpriteFrames` | `SpriteFrames` | Partial | Resource for named texture-frame animations, frame durations, speed and loop modes consumed by `AnimatedSprite2D`. |
| `Electron2D.SpriteFrames+LoopModeEnum` | `SpriteFrames.LoopMode` | Partial | Loop mode values for none, linear and pingpong playback. |
| `Electron2D.StaticBody2D` | `StaticBody2D` | Partial | RID lifecycle, constant velocity storage and transform sync baseline. |
| `Electron2D.StringName` | `StringName` | Partial | Immutable interned-name baseline with ordinal equality, hashing, empty/default semantics and string conversion. |
| `Electron2D.Texture2D` | `Texture2D` | Partial | Abstract texture resource baseline for size, alpha, mipmaps and pixel opacity queries. |
| `Electron2D.ToolAttribute` | `ToolAttribute` / `[Tool]` | Experimental | Marker attribute for editor-time script intent; current metadata marks it experimental and sandboxed. |
| `Electron2D.Transform2D` | `Transform2D` | Partial | 2D basis/origin transform baseline with point transforms, composition and inverse. |
| `Electron2D.Tween` | `Tween` | Partial | Sequential runtime tween with property, interval and callback steps, easing, pause/play/stop, kill, manual stepping and completion signals. |
| `Electron2D.Tween+EaseType` | `Tween.EaseType` | Partial | Ease mode enum for in, out, in-out and out-in interpolation. |
| `Electron2D.Tween+TransitionType` | `Tween.TransitionType` | Partial | Transition curve enum for linear and deterministic curved interpolation modes. |
| `Electron2D.Tweener` | `Tweener` | Partial | Public base type for concrete tween sequence steps. |
| `Electron2D.Variant` | `Variant` | Partial | Closed 0.1 value carrier for nil, primitives, enum-as-int, 2D math, identity handles, Object-derived values, Callable and Electron2D collections. |
| `Electron2D.Variant+Type` | `Variant.Type` | Partial | Closed 0.1 Variant type enum; 3D, Signal and packed arrays are intentionally excluded for now. |
| `Electron2D.Vector2` | `Vector2` | Partial | Floating-point 2D vector baseline with arithmetic, length, dot/cross, interpolation and formatting. |
| `Electron2D.Vector2I` | `Vector2I` | Partial | Integer 2D vector baseline with arithmetic, length, aspect and conversions. |
| `Electron2D.VerticalAlignment` | `VerticalAlignment` | Partial | Vertical alignment values used by text controls. |
| `Electron2D.Viewport` | `Viewport` | Partial | Runtime 2D viewport subset with current camera, visible rect, canvas transform, pixel snapping and root instance baseline. |
| `Electron2D.ViewportTexture` | `ViewportTexture` | Partial | Dynamic viewport texture returned by `Viewport.GetTexture()` with scene-local texture metadata. |
| `Electron2D.World2D` | `World2D` | Partial | 2D world object exposing `DirectSpaceState` for direct physics queries without backend handles. |

## Planned 2D Surface

| API | Reference | Status | Notes |
| --- | --- | --- | --- |
| `Electron2D.Timer` | `Timer` | Planned | Timer node. |

## Explicitly Not Planned Legacy API

| API | Reference | Status | Notes |
| --- | --- | --- | --- |
| `Electron2D.IComponent` | None | Not planned | Legacy component history is removed. |
| `Electron2D.SpriteRenderer` | None | Not planned | Rendering must use dedicated scene nodes such as `Sprite2D`. |
| `Electron2D.SpriteAnimator` | None | Not planned | Animation must use dedicated resources/nodes such as future `SpriteFrames` and `AnimatedSprite2D`. |
| `Electron2D.AudioSource` | None | Not planned | Legacy audio component is excluded. |
| `Electron2D.Rigidbody` | None | Not planned | Legacy component physics is excluded. |
| `Electron2D.Collider` | None | Not planned | Legacy component physics is excluded. |
| `Electron2D.BoxCollider` | None | Not planned | Collision API must use `CollisionShape2D` and shape resources. |
| `Electron2D.CircleCollider` | None | Not planned | Collision API must use `CollisionShape2D` and shape resources. |
| `Electron2D.PolygonCollider` | None | Not planned | Collision API must use the agreed shape subset. |
| `Electron2D.PhysicsBodyType` | None | Not planned | Physics body selection must use concrete body node types. |
