# Scene domain

Last updated: 2026-09-21

## Responsibility

Scene owns Electron2D's unified hierarchical/spatial game objects, typed in-memory packed scenes, and the active [`MainLoop`](../classes/MainLoop.md) implementation that delivers lifecycle, frame, pause, deferred-work, and deletion phases. It is a 2D-only runtime domain for Linux, Windows, macOS, Android, and iOS and compiles into the single `Electron2D.dll` assembly.

Its production sources live under `src/Scene/Main/` and `src/Scene/Resources/`, matching their engine-module ownership without changing the flat public `Electron2D` namespace.

## Component inventory

| Component | Responsibility | State |
| --- | --- | --- |
| [Unified 2D node](../components/unified-node.md) | Hierarchy, 2D transforms, paths, groups, visibility/Z state, process policy, lifecycle endpoints, and deletion requests | Implemented and verified |
| [Scene tree](../components/scene-tree.md) | Active-root ownership, exception-safe lifecycle, pause state, frame dispatch/events/counts, typed group operations, one-shot timers, deferred work, and deletion execution | Implemented and verified |
| [Packed scenes](../components/packed-scenes.md) | Typed in-memory owned-hierarchy capture, live metadata, detached reconstruction, and per-instance local resources | Implemented and verified |

Production types are [`Node`](../classes/Node.md), [`NodeProcessMode`](../classes/NodeProcessMode.md), [`SceneTree`](../classes/SceneTree.md), [`SceneTreeTimer`](../classes/SceneTreeTimer.md), [`GroupCallFlags`](../classes/GroupCallFlags.md), [`PackedScene`](../classes/PackedScene.md), [`SceneState`](../classes/SceneState.md), and [`PackedSceneEditState`](../classes/PackedSceneEditState.md).

## Public surface

- `Node`: one combined Godot-style `Node` + `Node2D` abstraction with ordered hierarchy, lifecycle, local/global `Matrix3x2` transforms, `Vector2` spatial helpers, logical canvas state, paths/search/groups, processing configuration, and queued deletion.
- `NodeProcessMode`: inherited, pausable, paused-only, always, and disabled process policies.
- `SceneTree`: concrete main loop and active hierarchy owner with failure-safe lifecycle/finalization, system-notification propagation, pause state, caller-driven process/physics frames, frame/tree events and counters, typed group work, timers, deferred actions, and deletion flushing.
- `SceneTreeTimer`: lightweight one-shot delay advanced by one selected frame lane and automatically disposed after timeout.
- `GroupCallFlags`: immediate/reverse/deferred/unique policy for typed group operations.
- `PackedScene`: `Resource` that captures one typed owned-node hierarchy and reconstructs independent detached instances.
- `SceneState`: live read-only typed metadata view for current packed data.
- `PackedSceneEditState`: instantiation policy whose runtime `Disabled` value is implemented and whose editor values fail explicitly.

## Dependency direction

- Scene depends on Core, Resources, including `Resource`, and the .NET Base Class Library, including `System.Numerics` and concurrent collections.
- Resources has a narrow reciprocal dependency on `Node` for `Resource.GetLocalScene()` under ADR 0023. This is an intentional in-assembly type cycle, not another managed assembly.
- Scene does not depend on SDL3-CS, rendering, input, audio, collision physics, asset loading/saving, file serialization, tweening, scripting, networking, or Localization.
- Future gameplay, rendering, input, and 2D physics types may depend on Scene.
- Scene must not introduce 3D types or a separate `Node2D` hierarchy.
- Scene lifecycle and game-state semantics must not vary by target platform; native event generation remains a host boundary.

## Domain-wide invariants

- A node has at most one parent and one active `SceneTree`; cycles and cross-tree insertion are rejected before mutation.
- An active root can be disposed only by its owning `SceneTree`.
- Sibling names are ordinal-unique, and path separators/reserved path tokens cannot be names.
- SceneTree-managed enter runs parent-first, ready runs child-first and once unless explicitly reset, and exit runs child-first. Lifecycle snapshots revalidate membership and lifecycle re-entry is rejected. Constructor failure terminally closes the failed tree, rolls membership and newly consumed ready state back, and disposes activation-created timers; later lifecycle failures complete their state transition and are aggregated. Manual `Notify(int)` dispatch is outside that state machine.
- Attached state mutation, lifecycle delivery, frame execution, flushing, and disposal use the tree's creating thread. Deferred and deletion requests may be enqueued from other threads.
- A non-top-level global transform is the local transform composed with ancestor transforms. Transform inputs must be finite; operations needing an inverse reject singular matrices.
- Process/physics callbacks are opt-in, synchronous, pause-aware, and ordered by their independent priority then captured tree order.
- Queue acceptance is atomic with tree-disposal closure. Deferred work queued during a flush waits for the next flush. Captured queued deletion runs after deferred actions, survives detachment, transfers safely between trees, and disposes the complete subtree despite detach callback failures.
- Frame and flush execution cannot be re-entered or started during lifecycle delivery. Timers advance after node callbacks and before deferred work in their selected lane. Pause delivery visits each eligible node at most once and rejects opposite re-entry.
- Typed group operations run in hierarchy/reverse order, revalidate membership, and can be deferred and coalesced without reflection or untyped values.
- Typed node events pass their publisher first when an additional payload is present; Core event connections can schedule handlers through `SceneTree.Defer`.
- Packed-scene capture stores only root-owned branches, static exact-type factories, persistent groups, and explicitly storage-enabled typed properties. It stores no live source nodes or event subscribers.
- Packed-scene instances are reconstructed detached. Node factories and unfinished instances cannot activate a `SceneTree`; scene-local resource graphs preserve aliases/cycles, know their new root before setup, and are disposed with that root.
- Capture blocks source hierarchy mutation. Failed reconstruction attempts cleanup of every returned node and resource duplicate it acquired, reports cleanup failures, and never returns a partial result.
- `SceneTree` is initialized when construction succeeds, returns no quit request from its two inherited frame lanes, and releases all owned scene state from explicit finalization or disposal.
- System notifications are propagated depth-first to live attached nodes; native generation and platform-specific input effects belong to absent host/Input domains.
- The warmed idle process and physics frame paths reuse scheduler/timer storage and do not allocate managed memory.

## Current limitations

- A caller may supply deltas directly through inherited `Process`/`PhysicsProcess` or wrappers. Core `Engine` can instead apply time scaling and fixed-step accumulation from host-supplied elapsed time. There is still no automatic SDL pump/clock, frame-wait policy, or background scene thread.
- Visibility and Z ordering are logical state only until a renderer consumes them.
- There is no drawing, viewport, render server, input propagation, collision/rigid-body physics, automatic scene switching, scene file loader/saver, tweening, RPC/multiplayer, accessibility backend, or scripting.
- Packed scenes are in-memory only. Nested/inherited scene authoring, placeholders, editable instances, persistent event endpoint storage, node-reference remapping, UID/import integration, and every editor edit mode remain absent.
- Paths are typed as `string`, not a separate `NodePath`; groups are strings; wildcard search covers names with `*` and `?`.
- A detached node may remember `QueueFree`, but deletion occurs only after attachment to a tree and a flush/frame boundary.
- The standalone Core `Transform2D` value is implemented under ADR 0029. Current Node transform members remain `Matrix3x2` until their explicit source-breaking migration is delivered.
- There is no five-platform host/package/test matrix; current executable verification is Linux-only.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies transform and hierarchy behavior, lifecycle order and failure rollback, cleanup continuation, inherited loop driving/finalization, system-notification propagation, tree/frame events and counters, typed group operations, timer behavior, paths/search/groups, visibility/Z, pause-aware process ordering, owner-thread enforcement, deferred batch isolation, concurrent enqueue/disposal stress, queued deletion, direct deterministic disposal, zero warmed idle-frame allocations, packed owned-branch capture/state/instantiation, local resources, factory/capture rejection, and packed rollback. It does not prove renderer, SDL, visual behavior, real-time cadence, disk scene compatibility, editor behavior, or large-scene performance.

## Relevant decisions

- [0002: C# events for signals](../decisions/0002-csharp-events-for-signals.md)
- [0004: 2D scene-oriented API in one Electron2D-owned assembly](../decisions/0004-2d-api-single-assembly.md)
- [0012: External runtime dependencies and Box2D.NET](../decisions/0012-external-runtime-dependencies.md)
- [0005: Notifications and typed editor properties](../decisions/0005-notifications-and-typed-properties.md)
- [0006: Scene-tree deferred work and queued deletion](../decisions/0006-scene-tree-deferred-and-deletion.md)
- [0008: Unified Node combines Node and Node2D](../decisions/0008-unified-2d-node.md)
- [0010: Typed event connections](../decisions/0010-typed-event-connections.md)
- [0011: SceneTree production contract](../decisions/0011-scene-tree-production-contract.md)
- [0015: Main-loop lifecycle and host boundary](../decisions/0015-main-loop-contract.md)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/0016-engine-runtime.md)
- [0017: Source-tree module layout](../decisions/0017-source-tree-layout.md)
- [0021: Cross-platform runtime target matrix](../decisions/0021-cross-platform-runtime-targets.md)
- [0023: Typed in-memory packed scenes](../decisions/0023-typed-packed-scenes.md)
- [0026: Separate Transform2D foundational type](../decisions/0026-separate-transform2d-type.md)
- [0029: Typed Transform2D value and affine semantics](../decisions/0029-typed-transform2d-value.md)
