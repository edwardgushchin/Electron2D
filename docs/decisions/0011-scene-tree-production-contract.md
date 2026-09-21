# 0011: Exception-safe SceneTree lifecycle, typed groups, and frame timers

Last updated: 2026-09-21

- Status: Accepted
- Scope: `SceneTree`, `SceneTreeTimer`, `GroupCallFlags`, and their `Node` lifecycle integration
- Refines: [0006](0006-scene-tree-deferred-and-deletion.md)

## Context

The first `SceneTree` version owned a hierarchy and offered frames, deferred actions, and queued node deletion, but constructor callback failures could leave partial membership, teardown failures could strand a live root behind a disposed tree, and cross-thread enqueue could race the one-time disposal clear. The stable reference surface also contains group operations, frame/tree signals, counts, generic queued deletion, and lightweight timers that do not require SDL, rendering, assets, networking, or an editor.

Electron2D must keep typed C# calls, deterministic ownership, Electron2D-owned code in its single public engine assembly, and explicit host-driven frame boundaries. It must not introduce string method/property dispatch or placeholder APIs for missing domains. ADR 0012 permits approved third-party dependencies to remain separate assemblies; it does not relax the one-assembly rule for Electron2D-owned code or change the `SceneTree` contract.

## Decision

- Queue acceptance and disposal closure share one lock. Work accepted before closure may be intentionally dropped by disposal; work reaching a closed tree is rejected and cannot become stranded.
- Constructor activation completes each lifecycle phase as far as possible. Any failure closes acceptance, terminally marks the failed tree, exits attached nodes, restores ready flags, disposes activation-created timers, clears queued work, and returns hierarchy ownership to the caller.
- Node enter, ready, exit, structural removal, node disposal, and tree disposal attempt all cleanup stages and aggregate callback failures after state reaches a coherent endpoint.
- Lifecycle snapshots revalidate membership; enter/ready/exit re-entry and child escape from an exiting or disposing parent's lifecycle, pre-delete, or cleanup callbacks are rejected.
- Frame and flush execution are non-reentrant and cannot begin during entry/exit delivery. Tree disposal from frame, flush, or lifecycle callbacks is rejected before the disposal transition.
- Cancellable node deletion continues through disposal after detach failures or detachment, while a stale request in an old tree cannot consume deletion now owned by a new tree.
- Pause traversal visits each still-attached node at most once; opposite re-entrant and teardown-time pause transitions are rejected.
- Node/tree/frame signals are typed C# events. Group calls and setters accept delegates; group ordering, deferral, and uniqueness use `GroupCallFlags`.
- `Unique` requires `Deferred`, uses operation kind/group/delegate-or-notification identity, ignores setter values, and retains the first accepted operation until its wrapper begins.
- `SceneTreeTimer` is a tree-owned, one-shot, auto-disposed timer updated after nodes and before deferred work in one selected frame lane.
- Node/group/frame counters and uncancellable `QueueDelete(ElectronObject)` are implemented because they require no absent domain.

## Consequences

- Lifecycle callback failures remain visible but no longer leave partial tree ownership, an operational escaped failed tree, or prevent later owned resources from being released.
- Cross-thread scheduling has a precise linearization point at the queue lock. The lock is intentionally small and never held while user code runs.
- Typed group operations require explicit delegates and therefore remain compile-time checked.
- Timers use delivered frame delta. ADR 0016 later adds Engine time scaling before delivery without adding a wall clock; timers still have no independent real-time or ignore-time-scale bypass.
- Scene switching, application quit, tween, interpolation, multiplayer, accessibility, editor signals, and platform notifications remain absent and explicitly dependency-blocked.

## Rejected alternatives

- Keep concurrent queues without a lifetime lock: rejected because a successful enqueue could remain permanently stranded after disposal.
- Swallow lifecycle failures: rejected because user callback failures must remain observable.
- Roll back arbitrary user mutations: rejected because external side effects are not reversible; rollback is limited to owned membership, ready state, and pending queues.
- Add reflection-based group method/property names: rejected by the typed API decision.
- Add placeholder scene, tween, networking, or platform members: rejected because they would advertise behavior without an owning domain.
