# Unified 2D node component

Last updated: 2026-09-21

## Scope

This Scene component provides Electron2D's primary and only public game-object base. A game object, composed subsystem, or complete world is an ordered hierarchy of [`Node`](../classes/Node.md) instances. `Node` deliberately combines Godot-like hierarchy/lifecycle behavior with the transform and canvas-state behavior normally associated with `Node2D`; specialized gameplay objects derive from it and may compose child Nodes and typed resources. A separate `Node2D`, `GameObject`, or public entity hierarchy does not exist.

## Owned types

| Type | Role |
| --- | --- |
| [`Node`](../classes/Node.md) | Hierarchical game object with 2D spatial, visibility, process, group, path, lifecycle, and deletion behavior |
| [`NodeProcessMode`](../classes/NodeProcessMode.md) | Pause-aware policy used to decide whether a node can process |

## Runtime flow

Local transforms are stored as `Electron2D.Transform`. A non-top-level node computes its global transform by composing its local matrix with its parent's global matrix. Position, rotation, scale, and skew are typed projections over that matrix; global setters solve back to local space through the inverse parent transform. Transform changes synchronously notify the node and non-top-level descendants.

Hierarchy operations maintain one parent, ordered unique-name children, path addressability, optional ancestor `Owner` metadata for packed storage, and at most one active [`SceneTree`](../classes/SceneTree.md). Tree membership drives enter/ready/exit callbacks, tree-level node/change events, pause-aware processing, group operations, queued deletion, and depth-first propagation of MainLoop system notifications. Core [`Engine`](../classes/Engine.md) can schedule the tree and applies time scaling before process deltas reach nodes. Lifecycle and disposal cleanup attempt every owned stage before aggregating callback failures. Child-related events pass the publishing parent first and the affected child second; self events pass the publishing node. Visibility and relative Z state are inherited through the same hierarchy but do not render by themselves.

The [Packed scenes](packed-scenes.md) component is the reuse boundary for Node hierarchies. It freezes a source hierarchy during capture, stores explicitly enabled properties and persistent groups, reconstructs independent detached instances through a static factory hook, and transfers per-instance resource ownership to the new root. The packed hierarchy may be one reusable game object or a complete level; `SceneFilePath` records only an external packed-scene source on an instantiated root.

## Dependencies

- Core's [`ElectronObject`](../classes/ElectronObject.md), [`MainLoop`](../classes/MainLoop.md) notification identifiers, and typed property descriptors.
- Core [`Mathf`](../classes/Mathf.md), [`Vector2`](../classes/Vector2.md), and [`Transform`](../classes/Transform.md) as the implemented Node scalar and spatial surface.
- `System.IO.Enumeration.FileSystemName` for `*`/`?` hierarchy-name matching.
- [`SceneTree`](../classes/SceneTree.md) for active lifecycle, frame delivery, pause state, group operations, tree events, and deferred deletion.
- [`PackedScene`](../classes/PackedScene.md) and the Resource base for capture factories, owner selection, stored state, and per-instance resource ownership.

The component has no SDL3-CS, renderer, input, audio, collision, or file-serialization dependency.

## Invariants and errors

- Node names are non-blank, cannot be `.` or `..`, cannot contain `/`, and are unique among siblings using ordinal comparison.
- Self-parenting, cycles, multiple parents, and direct insertion of an already tree-attached child are rejected. `Reparent` may move a node between trees when both trees' owner-thread requirements are satisfied.
- All scalar/vector/matrix transform inputs must be finite. Operations requiring an inverse reject singular transforms.
- `ZIndex` is restricted to `-4096..4095`; effective relative Z is clamped to the same range.
- Mutation of an attached node is restricted to the tree's owner thread. `QueueFree`/`CancelFree` remain atomic request operations.
- Constructor lifecycle failure rolls tree membership and newly consumed ready state back; later exit/disposal failures complete cleanup and are aggregated.
- Lifecycle snapshots revalidate membership; a node cannot be removed, reparented, or disposed during its active enter/ready/exit delivery, and children cannot escape an exiting or disposing parent through re-entrant lifecycle, pre-delete, or cleanup mutation.
- Process callbacks are explicitly enabled and are synchronous. A callback failure is aggregated by `SceneTree` after the remaining scheduled nodes and deferred phase are attempted.
- Node events have explicit typed source arguments and can be wrapped by Core's [`EventConnection`](../classes/EventConnection.md) for owned, one-shot, or deferred delivery.
- `Owner` is null or a strict ancestor. Detach/reparent clears owner references that stop naming an ancestor.
- Packed capture rejects mutation, disposal, and deletion requests across the source hierarchy. Derived stored-property setters must call `EnsureMutable()`.

## Current implementation status

Implemented: ordered hierarchy and reparenting, lifecycle and typed events, relative/absolute paths, wildcard searches, persistent/runtime groups, owner metadata, packed-scene factory/capture hooks and source path, local/global transform properties and helpers, top-level transforms, visibility, relative/absolute Z state, transform/visibility notifications, process and physics-process callbacks with independent priorities, pause modes, ready reset, and queued deletion.

## Exclusions

- No separate spatial-node subclass, canvas base, typed path/name wrappers, internal processing lane, input callbacks, multiplayer/RPC, editable-instance metadata, nested/inherited scene authoring, persistent event endpoints, or child-name auto-generation.
- No drawing API, canvas/render-server handle, material, texture filter/repeat, clipping, light mask, Y sorting, or viewport behavior. Current visibility and Z values are logical state for the future renderer.
- No collision/rigid-body physics. `PhysicsFrame` is only a fixed-step callback lane; Engine can schedule it from host-supplied elapsed time.
- `Transform` decomposition returns a canonical representation; equivalent matrices involving negative scale may not reproduce the exact originally assigned scalar tuple.

## Verification

`tests/Electron2D.Tests/Program.cs` covers transform composition/decomposition, local/global conversion, singular rejection, top-level isolation, transform notifications, hierarchy paths/search/reparent/order/name rules, owner cleanup, persistent groups, sender-first child events, activation rollback, stale lifecycle snapshot rejection, teardown failure continuation and mutation guards, capture mutation rejection, packed factories/instances, visibility and Z inheritance, movement helpers, process priorities/pause modes/delta values, inherited disable/enable notifications, direct disposal, and detached/cross-tree queued subtree disposal.

## Decision

- [0008: Unified Node combines Node and Node2D](../decisions/scene.md#adr-0008)
- [0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
- [0026: Separate Transform foundational type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform value and affine semantics](../decisions/core-math.md#adr-0029)
- [0031: Node trees and reusable scenes as the primary game-object model](../decisions/scene.md#adr-0031)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
