# 0008: Combine scene and 2D spatial behavior in one Node

Last updated: 2026-09-21

- Status: Accepted, except the rejection of a separate `Transform2D`, which is superseded by [ADR 0026](0026-separate-transform2d-type.md)
- Scope: Scene-domain public object model

## Context

Godot separates non-spatial hierarchy/lifecycle behavior (`Node`) from 2D spatial behavior (`Node2D`, through `CanvasItem`). Electron2D is exclusively a 2D engine, and its intended game-object API needs both sets of behavior on ordinary nodes. Preserving a second spatial base class would add a hierarchy choice that has no 3D counterpart or non-spatial engine requirement here.

## Decision

- Electron2D exposes one public game-object class named `Node`; it does not expose `Node2D`.
- `Node` combines hierarchy, lifecycle, paths, groups, processing, deletion, 2D local/global transforms, visibility, and Z ordering.
- The current Node transform vocabulary uses `System.Numerics.Vector2` and `Matrix3x2` directly. The original decision not to introduce `Transform2D` is preserved here as history but superseded by ADR 0026; ADR 0029 implements the standalone value while leaving Node migration pending.
- Godot-like concepts keep recognizable names where they remain useful, but the API stays typed C#: strings represent paths/groups/names, delegates and virtual methods represent callbacks, and C# events represent signals.
- Renderer-independent canvas state (`Visible`, `ZIndex`, `ZAsRelative`) belongs on `Node` now. Renderer-bound drawing, materials, canvas handles, lights, clipping, input picking, and viewport behavior wait for their actual domains.
- `SceneTree` is the host-driven frame boundary. It delivers explicitly enabled process and physics-process callbacks in priority/tree order and flushes deferred work afterward; it does not create a hidden thread or clock.

## Consequences

- Every game object can be positioned immediately; users never choose between `Node` and `Node2D`.
- Scene and transform lifetime share one parent tree, making global transforms, inherited visibility, Z state, paths, groups, and processing coherent.
- The public API is intentionally similar rather than source-compatible with Godot: there is no Variant, NodePath, StringName, CanvasItem, or automatic method-name dispatch.
- `System.Numerics` fixes the current matrix convention and keeps Electron2D.dll free of another managed math dependency.
- Future renderer and input work extends `Node` or adds purpose-specific derived types; it must not recreate a parallel `Node2D` hierarchy.

## Rejected alternatives

- Keep separate `Node` and `Node2D`: rejected because the user-facing engine is 2D-only and requires spatial behavior on its single node type.
- Put transforms in a detachable component: rejected because it makes the primary 2D object more indirect without a demonstrated non-spatial use case.
- Create Electron2D-specific vector/matrix wrappers in this initial Node slice: rejected at the time because the .NET standard-library types covered the implemented behavior. ADR 0026 later required a complete standalone `Transform2D`, and ADR 0029 delivered it without silently changing Node's existing surface.
- Copy all `CanvasItem` API before a renderer exists: rejected because those members would be non-functional promises rather than a completed runtime contract.
