# SceneTree.DeferredPool<T>

Last updated: 2026-10-14

- **Source:** [src/Scene/Main/SceneTree.Deferred.cs](../../src/Scene/Main/SceneTree.Deferred.cs)
- **Declaration:** `private sealed class SceneTree.DeferredPool<T> : IDeferredPool`
- **Visibility:** Internal implementation only; not a game-facing API.

## Description

One owner-scoped free-slot stack for an exact payload type.

This implements the prepared typed delivery boundary under [ADR 0010](../decisions/core-object-runtime.md#adr-0010) and [ADR 0006](../decisions/scene.md#adr-0006). Public consumers use EventConnection.Subscribe with SceneTree.Defer, or SceneTree.SetDeferred.

## Members

| Member | Contract |
| --- | --- |
| `Prepare()` | Creates a slot before the warmed claim. |
| `Rent(Action<T> callback, T value)` | Removes a free slot or grows cold capacity, retaining independent typed arguments. |
| `Return(DeferredInvocation<T> slot)` | Returns a cleared slot under the same work gate; active execution never rents its own still-running slot. |

| `Release()` | Releases one active subscription reservation. Created capacity follows peak simultaneous reservations/pending calls, rather than historical token count. |
| `CreatedCount` | Internal retained-slot census used by the churn regression. |

## Lifecycle, threading and errors

A queued invocation owns its copied value/reference identity until callback completion or discard. Ownership of borrowed resources is not extended. Scene acceptance, pool rent/return and queue mutation use the existing work gate; user callbacks execute outside it on the owner thread. Finalization/activation rollback discards pending slots. Captured batches, mixed Action FIFO order and next-flush deferral remain unchanged. Prepared capacities avoid repeated allocation; first type/capacity growth remains cold.

## Verification and limits

[EventConnectionPhysicsTests](../../tests/Electron2D.Tests/EventConnectionPhysicsTests.cs) checks typed setter values/FIFO/reentrancy/errors, closed-scheduler behavior, concurrent producers, reference release after completion/failure/discard/rollback, all event arities and prepared one-shot delivery. Selected CPU/GPU full-frame profiles gate zero warmed owner/all-thread bytes. Software Vulkan proves functional kernels only. Arbitrary custom schedulers retain independent captured Actions and their own allocation/delivery policy.

A 1024-token sequential one-shot churn regression retains one slot for the tested argument type. Reservation release is idempotently coordinated by EventConnection terminal transitions; queued slots still recycle only after execute/discard.
