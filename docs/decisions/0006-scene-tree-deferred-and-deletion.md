# 0006: Own hierarchy, deferred work, and queued deletion in SceneTree

Last updated: 2026-09-20

- Status: Accepted; frame scheduling refined by [0008](0008-unified-2d-node.md), lifecycle and queue safety refined by [0011](0011-scene-tree-production-contract.md)
- Scope: `Node` hierarchy and `SceneTree` scheduling

## Context

Godot's object surface includes deferred calls and queued deletion, but both require a safe execution boundary and hierarchy ownership. Putting them on every `ElectronObject` would hide scheduling state and allow objects outside a scene to pretend that a game-loop queue exists.

## Decision

- `Node` owns ordered child relationships and lifecycle callbacks; `SceneTree` owns one active root hierarchy.
- SceneTree-managed enter runs parent-first, ready runs child-first once per node lifetime, and exit runs child-first. Manual notification dispatch remains possible but does not mutate lifecycle state.
- Attached hierarchy mutation, flushing, and disposal run on the thread that created the tree; invalid disposal is rejected before object state begins changing.
- `SceneTree.Defer(Action)` and `SetDeferred<T>` accept typed work without string method lookup.
- Core's `EventConnection` can receive `SceneTree.Defer` as its scheduler for cancellable deferred event delivery.
- A flush atomically captures one action batch. Work enqueued during execution waits for the next flush.
- Node deletion is a separate atomic request processed after deferred actions. It detaches and disposes the full subtree; it can be cancelled before processing.
- The application owns time and the safe point. It may call `ProcessFrame`, `PhysicsFrame`, or `FlushDeferred`; the frame methods run their callback lane and then flush. `SceneTree` does not create a hidden thread or clock.

## Consequences

- Ordering and thread affinity are explicit and testable.
- Deferred execution does not require `Callable`, reflection, `Variant`, or method names.
- Queueing is thread-safe, while game-state mutation remains single-threaded.
- Historical implementation note: queueing was originally not atomic with disposal. ADR 0011 replaced that behavior with a shared queue-lifetime lock.
- Cancelling deletion may leave a stale queue entry, which is ignored during flush.
- Historical implementation note: teardown originally stopped after some callback failures. ADR 0011 requires all owned cleanup stages to be attempted and failures to be aggregated.

## Rejected alternatives

- Put `CallDeferred` and `Free` on every `ElectronObject`: rejected because scheduling belongs to a tree/game-loop boundary.
- Execute each deferred action as soon as it is queued: rejected because it would not defer re-entrant mutation.
- Add a background scene thread: rejected because SDL and game-state ownership require an explicit application thread; the host drives the implemented frame methods.
- Use dynamic method names: rejected by ADR 0001.
