# SceneTree.DeferredScheduler

Last updated: 2026-10-14

- **Source:** [src/Scene/Main/SceneTree.Deferred.cs](../../src/Scene/Main/SceneTree.Deferred.cs)
- **Declaration:** `private sealed class SceneTree.DeferredScheduler : IDeferredScheduler`
- **Visibility:** Internal implementation only; not a game-facing API.

## Description

Scene-owned provider retaining typed pools under the existing work gate.

This implements the prepared typed delivery boundary under [ADR 0010](../decisions/core-object-runtime.md#adr-0010) and [ADR 0006](../decisions/scene.md#adr-0006). Public consumers use EventConnection.Subscribe with SceneTree.Defer, or SceneTree.SetDeferred.

## Members

| Member | Contract |
| --- | --- |
| `Action<Action> Callback { get; }` | Exact SceneTree.Defer callback. |
| `Prepare<T>()` | Adds a free typed slot under the work gate. Subscription may still target a closed scheduler; failure occurs on emission. |
| `Enqueue<T>(Action<T> callback, T value)` | Validates live acceptance, rents a typed slot and enqueues its immutable Action in the ordinary FIFO. Rejection returns the slot. |
| `Pool<T>()` | Finds or creates the exact typed pool by CLR type identity; no boxed payload or reflected invocation. |

| `Release<T>()` | Releases a subscription reservation once at token completion/disposal without changing queued execution ownership. |

## Lifecycle, threading and errors

A queued invocation owns its copied value/reference identity until callback completion or discard. Ownership of borrowed resources is not extended. Scene acceptance, pool rent/return and queue mutation use the existing work gate; user callbacks execute outside it on the owner thread. Finalization/activation rollback discards pending slots. Captured batches, mixed Action FIFO order and next-flush deferral remain unchanged. Prepared capacities avoid repeated allocation; first type/capacity growth remains cold.

## Verification and limits

[EventConnectionPhysicsTests](../../tests/Electron2D.Tests/EventConnectionPhysicsTests.cs) checks typed setter values/FIFO/reentrancy/errors, closed-scheduler behavior, concurrent producers, reference release after completion/failure/discard/rollback, all event arities and prepared one-shot delivery. Selected CPU/GPU full-frame profiles gate zero warmed owner/all-thread bytes. Software Vulkan proves functional kernels only. Arbitrary custom schedulers retain independent captured Actions and their own allocation/delivery policy.

A 1024-token sequential one-shot churn regression retains one slot for the tested argument type. Reservation release is idempotently coordinated by EventConnection terminal transitions; queued slots still recycle only after execute/discard.
