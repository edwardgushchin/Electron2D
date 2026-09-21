# Packed scenes component

Last updated: 2026-09-21

## Scope

This Scene component provides typed, runtime-only, in-memory capture and reconstruction of detached 2D node hierarchies. It connects the [Unified 2D node](unified-node.md), [Typed editor properties](editor-properties.md), and [Resource base](resources.md) components without adding a dynamic value system, reflection-driven invocation, filesystem scene format, editor runtime, or second assembly.

## Owned types

| Type | Role |
| --- | --- |
| [`PackedScene`](../classes/PackedScene.md) | Resource that captures a reusable owned-node snapshot and constructs independent detached hierarchies |
| [`SceneState`](../classes/SceneState.md) | Live read-only typed metadata view over current packed data |
| [`PackedSceneEditState`](../classes/PackedSceneEditState.md) | Runtime/editor instantiation policy; only runtime `Disabled` is implemented |

All three types ship in `Electron2D.dll`. Production sources live in `src/Scene/Resources/`.

## Runtime flow

1. `Pack(root)` clears a previous non-null capture, freezes the complete source hierarchy against mutation, selects the root plus depth-first branches owned by it, and captures static factories, names, parent/owner relationships, persistent groups, and storage-enabled typed properties.
2. A live `SceneState` is updated whenever packed data or the source resource path changes. A disposed state is replaced; an external state survives source disposal as a final snapshot.
3. `Instantiate()` captures immutable packed data, creates fresh exact-type nodes parent-first, restores typed properties and persistent groups, builds the hierarchy, and assigns owners.
4. One graph session duplicates scene-local resource graphs with aliases and cycles preserved, assigns the new root before setup, invokes setup once per local duplicate, and transfers ownership of every created resource to the root.
5. Each new node remains under an instantiation barrier while unfinished. The root receives the external scene path when applicable. The complete topology is validated, only the root receives notification `20`, topology is validated again, the barriers are removed, and the detached root is returned.
6. Any failure attempts to dispose every returned node and resource duplicate acquired by the operation and aggregates rollback failures. Source nodes and shared source resources are never disposed by this rollback; an allocation a factory never returns remains outside engine ownership.

## Dependencies

- [`Node`](../classes/Node.md) supplies `Owner`, persistent group metadata, stored 2D/runtime descriptors, the static exact-type factory hook, source capture barriers, `SceneFilePath`, and notification `20`.
- [`PropertyDescriptor`](../classes/PropertyDescriptor.md) supplies explicit `IsStored` metadata and typed capture/restore.
- [`Resource`](../classes/Resource.md) supplies identity, changed state, duplication hooks, local-to-scene policy, local-scene association, setup callbacks, and graph-preserving duplication.
- Normal instantiation creates no [`SceneTree`](../classes/SceneTree.md). `SceneTree` construction and existing-tree entry consult the Node factory/instantiation barriers so callbacks cannot start lifecycle before the detached result is complete.
- Core typed events remain ordinary C# events. [`EventConnection`](../classes/EventConnection.md) subscribers are not discoverable or serialized.

The component has no SDL3-CS, renderer, input, audio, physics, native handle, loader/saver, import, scripting, networking, or editor dependency.

## Invariants and error behavior

- Packed data contains no live source node, instance-bound factory, arbitrary object graph, `SceneTree`, or event delegate.
- Factories must be static and source-independent and must create a fresh default node of the exact captured type. Their execution context cannot construct a new `SceneTree` or enter an existing one; source and previously issued identities are rejected.
- Only root-owned descendant branches are present. Parent order, sibling order, owner paths, persistent groups, and typed stored values are deterministic.
- Reference-free values such as [`Color`](../classes/Color.md) and [`Rect2`](../classes/Rect2.md) are captured and restored directly, including HDR or negative components, without conversion to strings or a universal container.
- Capture blocks node mutation/disposal/deletion for the complete source hierarchy. Derived stored-property setters must call `Node.EnsureMutable()`.
- Instance reconstruction starts and ends detached. An unfinished node cannot be disposed or enter a `SceneTree`, either as its root or as a child of an active node. Linear-time topology validation detects attachment to an unrelated detached hierarchy, and rollback removes the escaped node.
- Scene-local duplication preserves graph identity. External non-local resources remain shared; created duplicates are owned and disposed by the returned root.
- Null pack input preserves old data. A failure after capture begins leaves data empty. Instantiation failure never returns a partial hierarchy; rollback cleanup itself may fail and is then reported in the aggregate.
- Callback and cleanup exceptions are not swallowed; multiple failures are aggregated after all owned cleanup is attempted.

## Threading

Packed state and live-state replacement are lock-serialized. Concurrent instantiations use immutable snapshots and factory identity tracking. `SceneState` supports concurrent reads. Resource path callbacks use the currently committed path rather than stale callback order.

User factories, descriptor delegates, resource copy/setup callbacks, node notifications, and events execute synchronously on the initiating thread. Attached-node capture must run on the tree owner thread. Detached derived state and callback code require caller coordination. Packing and instantiation are not real-time or allocation-free paths.

## Current implementation status

Implemented and covered locally: in-memory owned-branch capture, storage-enabled typed properties, derived node factories, persistent groups, owner/path metadata, live typed state inspection, independent detached instantiation, root-only scene notification, scene-local resource duplication/setup/ownership, packed-resource duplication/reset/copy behavior, capture and instantiation barriers, factory identity checks, callback topology validation, and failure rollback.

## Exclusions

- No text/binary scene loader or saver, exported-pack integration, UID/import remapping, dependency scanning, or missing-resource recovery.
- No inherited/nested scene authoring, editable-instance metadata, placeholders, pinned properties, script preservation, or implemented editor edit state.
- No persistent typed event endpoint schema. Runtime C# event subscriptions and `EventConnection` tokens are intentionally not copied.
- No general node-reference property encoding/remapping and no arbitrary reference-shaped stored property values.
- No hidden scene activation: `Instantiate()` returns detached; `SceneTree` lifecycle remains explicit.

## Verification

`tests/Electron2D.Tests/Program.cs` exercises positive capture/metadata/instantiation, pruned branches, stored state and groups including HDR `Color` and `Rect2` values, source disposal, repeated local resource graphs, live-state/path races, copy/reset/failure state, invalid edit states, capturing/source-returning/wrong-type/reused factories, capture mutation, setup cleanup, detached-parent and active-tree escape rollback, and final-state survival. Full verification also includes format, Release build, generated XML, link/inventory checks, and the repository-wide executable harness.

## Decision

- [0023: Typed in-memory packed scenes](../decisions/0023-typed-packed-scenes.md)
- [0024: Typed color values and portable quantization](../decisions/0024-typed-color-values.md)
- [0025: Typed axis-aligned rectangle geometry](../decisions/0025-typed-rectangle-geometry.md)
