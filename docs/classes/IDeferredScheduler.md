# IDeferredScheduler

Last updated: 2026-10-14

- **Source:** [src/Core/Object/DeferredSchedulerRegistry.cs](../../src/Core/Object/DeferredSchedulerRegistry.cs)
- **Declaration:** `internal interface IDeferredScheduler`
- **Visibility:** Internal implementation only; not a game-facing API.

## Description

Core protocol for a prepared strongly typed scheduler, registered by an owner.

This implements the prepared typed delivery boundary under [ADR 0010](../decisions/core-object-runtime.md#adr-0010) and [ADR 0006](../decisions/scene.md#adr-0006). Public consumers use EventConnection.Subscribe with SceneTree.Defer, or SceneTree.SetDeferred.

## Members

| Member | Contract |
| --- | --- |
| `Action<Action> Callback { get; }` | Exact immutable legacy callback recognized by delegate equality. |
| `void Prepare<T>()` | Reserves a typed invocation slot during subscription; does not change delayed closed-scheduler failure behavior. |
| `void Enqueue<T>(Action<T> callback, T value)` | Atomically accepts a typed value snapshot into the owner queue or rejects closed ownership. |

| `Release<T>()` | Releases a subscription reservation once at token completion/disposal without changing queued execution ownership. |

## Lifecycle, threading and errors

A queued invocation owns its copied value/reference identity until callback completion or discard. Ownership of borrowed resources is not extended. Scene acceptance, pool rent/return and queue mutation use the existing work gate; user callbacks execute outside it on the owner thread. Finalization/activation rollback discards pending slots. Captured batches, mixed Action FIFO order and next-flush deferral remain unchanged. Prepared capacities avoid repeated allocation; first type/capacity growth remains cold.

## Verification and limits

[EventConnectionPhysicsTests](../../tests/Electron2D.Tests/EventConnectionPhysicsTests.cs) checks typed setter values/FIFO/reentrancy/errors, closed-scheduler behavior, concurrent producers, reference release after completion/failure/discard/rollback, all event arities and prepared one-shot delivery. Selected CPU/GPU full-frame profiles gate zero warmed owner/all-thread bytes. Software Vulkan proves functional kernels only. Arbitrary custom schedulers retain independent captured Actions and their own allocation/delivery policy.

A 1024-token sequential one-shot churn regression retains one slot for the tested argument type. Reservation release is idempotently coordinated by EventConnection terminal transitions; queued slots still recycle only after execute/discard.
