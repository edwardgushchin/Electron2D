# Typed event connections component

Last updated: 2026-10-14

## Scope

This Core component adds deterministic connection ownership, one-shot consumption, and deferred scheduling to ordinary typed C# events. It does not replace those events with a dynamic signal bus.

## Owned type

| Type | Role |
| --- | --- |
| [`EventConnection`](../classes/EventConnection.md) | Disposable wrapper around one typed event subscription |

## Runtime flow

`EventConnection.Subscribe` receives typed add/remove accessors and a matching handler. It creates one wrapper, records how to remove that wrapper, attaches it, and returns the ownership token. With no scheduler, accepted emissions invoke synchronously. With a scheduler, each accepted emission captures its typed arguments and submits an action for later execution.

One-shot connections atomically stop accepting emissions and detach before invoking or scheduling the first handler. A pending deferred one-shot can be cancelled by disposing its token. Reusable deferred connections can queue multiple emissions; disposal makes queued callbacks inert until the scheduler discards them through execution.

## Dependencies

The component depends only on .NET delegates, `IDisposable`, locking, and exception-dispatch support. It has no runtime dependency on Scene, SDL3-CS, serialization, reflection, or an external reactive library. `SceneTree.Defer` is the standard current scheduler supplied by callers needing frame-boundary delivery.

## Invariants

- The same wrapper delegate is supplied to both event accessors.
- A one-shot connection accepts at most one emission under recursion and concurrency.
- User accessors and handlers never run while the connection-state lock is held.
- Disposal is idempotent and terminal even when removal throws.
- Terminal transitions clear retained accessor, handler, and scheduler references.
- Deferred argument values are captured when the event is emitted.
- Disposal cancels scheduled callbacks that have not started; it does not wait for running callbacks.
- Source identity and payload shape remain explicit in the event's typed signature.

## Current implementation status

Implemented and covered by executable positive, negative, failure-injection, re-entrancy, and concurrency checks for the zero-, one-, and two-argument overloads.

## Exclusions

There is no dynamic event lookup, generic event registry, untyped argument list, weak subscription, priority ordering or automatic subscriber-lifetime discovery for ordinary tokens. Persistent authored scene subscriptions use the separate [SceneConnectionBinding](../classes/SceneConnectionBinding.md) component under [ADR 0010](../decisions/core-object-runtime.md#adr-0010): compiled factories and typed endpoints retain cleanup and packed storage. Ordinary runtime subscriptions are still omitted from scene packing.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies ordinary and one-shot connections, recursive/concurrent emission, deferred ordering and cancellation, exception policy, accessor rollback, removal failures, scheduler failures, null validation, native duplicate handler behavior, and sender-first two-argument preservation.

Prepared reusable synchronous subscriptions with zero, one or two arguments create no per-emission capture. One-shot delivery and deferred scheduling retain their separate capture paths; no zero-allocation claim is made for them, subscription/accessor work or allocating user handlers. This preserves argument values, lock boundaries, detach-before-handler ordering, cancellation and exception policy under ADR 0010.

[EventConnectionPhysicsTests](../../tests/Electron2D.Tests/EventConnectionPhysicsTests.cs) verifies all three prepared immediate arities and full selected CPU/GPU frames: 128 warm/128 measured frames, strict zero owner/all-thread managed bytes, 128 emissions of each arity and 64 Area body enters/exits. Identity, disposed subscriptions and collected callback-error recovery remain observable. Cold two-argument deferred/one-shot checks cover value capture, ordering, cancellation and recursion; the existing EventConnection suite retains concurrency, accessor rollback/removal/scheduler errors and all overloads. GPU execution uses CPU-only Vulkan; hardware/native/network/performance acceptance stays open.
