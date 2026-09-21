# Scene tree component

Last updated: 2026-09-21

## Scope

This Scene component provides the concrete [Main loop](main-loop.md), owns one active hierarchy, propagates system notifications, delivers lifecycle and pause changes, accepts direct or [Engine](engine-runtime.md)-scheduled process/physics frame boundaries, frame counters and events, tree-change events, reusable Node timers, lightweight one-shot timers, typed group operations, deferred work, and queued deletion. Per-node hierarchy and 2D state belong to the [Unified 2D node component](unified-node.md).

## Owned types

| Type | Role |
| --- | --- |
| [`SceneTree`](../classes/SceneTree.md) | Concrete `MainLoop`, hierarchy owner, frame/system-notification dispatcher, group service, timer owner, deferred scheduler, and deletion queue |
| [`Timer`](../classes/Timer.md) | Reusable Node countdown with process/physics, pause, repeat, autostart, time-scale, event, and packed-configuration behavior |
| [`TimerProcessCallback`](../classes/TimerProcessCallback.md) | Stable choice between physics and process countdown lanes |
| [`SceneTreeTimer`](../classes/SceneTreeTimer.md) | Auto-disposed one-shot timer advanced by a selected tree frame lane |
| [`GroupCallFlags`](../classes/GroupCallFlags.md) | Order, deferral, and uniqueness policy for typed group operations |

## Runtime flow

Construction rejects calls made from inside a packed-scene node factory and roots still marked as unfinished by packed-scene instantiation. It otherwise validates the supplied root, initializes inherited loop state, enters every reachable node parent-first, and readies nodes child-first. Enter/ready callback failure closes acceptance, terminally marks the failed tree, exits attached nodes, restores ready state, disposes activation-created timers, and clears queues while leaving the supplied hierarchy caller-owned. User side effects outside owned lifecycle state are not transactional.

An inherited or wrapper frame increments its lane counter, raises its start event, reuses an owned snapshot buffer to priority-order nodes, invokes all still-eligible internal then public callbacks, advances matching lightweight tree timers, then runs one deferred/deletion safe point. `Timer` nodes run in the node phase and can choose the Engine-supplied original delta; `SceneTreeTimer` runs in the later timer phase with the delivered scaled delta. Timer, node, event, deferred, and deletion failures do not prevent later phases and are reported together. Frame/flush execution and tree finalization/disposal are rejected during frame, flush, and node-lifecycle delivery before queue consumption or lifetime mutation.

System notifications are snapshotted and propagated depth-first to every still-live attached node. Explicit inherited finalization and disposal both close work acceptance and release the hierarchy/timers; explicit finalization leaves only the tree object undisposed and terminal.

Immediate typed group operations snapshot members in hierarchy or reverse order on the owner thread. Deferred operations resolve membership when their queued wrapper starts. `Unique` coalesces equal queued operations and retains the first setter value. String-based method/property dispatch is absent.

Queue acceptance uses one lock shared with finalization closure. A successful cross-thread enqueue is either executed by a later safe point or deliberately discarded by later finalization; an enqueue that reaches the closed tree is rejected. Finalization closes the queues, exits and recursively disposes the hierarchy, disposes timers, and clears subscribers while aggregating every failure.

## Dependencies

The component depends on Core's `MainLoop` and `EventConnection`, the unified `Node` including its internal lanes and packed-scene construction barriers, `Timer`, reusable lists, concurrent queues, and ordinary .NET synchronization. Core `Engine` may drive it through `MainLoop` and supplies scaled/original deltas without a Scene-to-Engine dependency. It has no SDL3-CS, clock, renderer, input, audio, collision-physics, asset loader/serializer, tween, networking, or editor dependency.

## Invariants

- The root is live, parentless, unattached, and not queued when construction begins.
- Tree activation cannot start inside a packed-scene node factory or with any node whose packed-scene instantiation barrier is active.
- The active root cannot be directly disposed or queued; tree disposal owns its complete lifetime.
- Membership is never left partially attached after constructor failure or lifecycle callback failure.
- Lifecycle, immediate group work, timers, frames, flushing, hierarchy mutation, and disposal use the creating thread.
- Deferred actions, deferred group operations, generic queued deletion, and node deletion requests may originate on another thread.
- Enter is parent-first, ready is child-first, and exit is child-first; all applicable lifecycle callbacks are attempted before failures are reported.
- Lifecycle snapshots revalidate current parent/tree membership; a node in enter/ready/exit delivery cannot be removed, reparented, or disposed re-entrantly.
- Pause traversal revalidates lifetime/membership and visits each node at most once; opposite or teardown-time transitions are rejected.
- One action batch is captured per flush; deletion is captured afterward, so deletion requested by a captured action runs in that flush while nested deferred actions wait.
- Reusable `Timer` nodes run through internal Node processing before their own public callbacks; lightweight tree timers run after nodes and before deferred actions. Expired tree timers are disposed even if timeout handlers fail.
- Queued node deletion reaches disposal even when detach callbacks fail or the node became detached; an old tree does not consume a request owned by a new tree.
- Tree events reflect completed lifecycle/structural stages; callback failures do not roll completed state back.
- MainLoop initialization is complete when construction returns; explicit finalization or disposal releases every owned scene object exactly once.
- System notifications use depth-first snapshots and revalidate each candidate before delivery.
- Warmed idle and active-Timer frame lanes do not allocate managed memory in the covered small-hierarchy paths.

## Current implementation status and exclusions

Implemented and covered by executable checks. Core Engine supplies host-driven time scaling, original delta delivery for `Timer.IgnoreTimeScale`, fixed-step scheduling, and interpolation metrics when used, but there is no application/game-loop thread, automatic SDL clock, frame waiting, native system-event creation, permission request implementation, focus-to-input synchronization, automatic current-scene switching/loading, tween, renderer synchronization, input propagation, multiplayer polling, accessibility backend, editor behavior, or physics simulation. Callers may explicitly install a detached root returned by the separate [Packed scenes](packed-scenes.md) component. Blocked reference APIs and their missing domains are enumerated in the [`SceneTree`](../classes/SceneTree.md#official-reference-coverage-inventory) and [`Timer`](../classes/Timer.md#official-reference-coverage-inventory) class documents; no placeholder surface is exposed for them.

## Verification

Tests cover valid and failing activation, packed-factory/unfinished-node activation rejection, inherited loop initialization/frames/explicit finalization, Engine attachment/finalization, system-notification propagation, escaped failed-tree references, ready/timer rollback, stale lifecycle snapshots, lifecycle and tree-event order, pause re-entry/reparent/removal behavior, frame ordering/counters/events, lifecycle execution barriers, typed group order/setting/notification/uniqueness, reusable and lightweight timer behavior, deferred batching, generic/node/cross-tree deletion, owner-thread enforcement, failure-continuing teardown, re-entrant disposal mutation rejection, teardown mutation rejection, concurrent enqueue/disposal stress, and warmed idle/active-Timer allocation. They establish local managed semantics, not real-time cadence, rendering, loaded-scene performance, or platform behavior.

## Decisions

- [0006: Scene-tree ownership, deferred work, and deletion](../decisions/scene.md#adr-0006)
- [0011: Exception-safe lifecycle, typed groups, and timers](../decisions/scene.md#adr-0011)
- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
