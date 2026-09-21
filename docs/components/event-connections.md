# Typed event connections component

Last updated: 2026-09-21

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

There is no dynamic event lookup, generic event registry, untyped argument list, weak subscription, priority ordering, automatic subscriber-lifetime discovery, or persistent connection storage. In-memory packed scenes intentionally copy no subscribers; persistence remains blocked by the absence of a typed stable endpoint and handler-binding schema.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies ordinary and one-shot connections, recursive/concurrent emission, deferred ordering and cancellation, exception policy, accessor rollback, removal failures, scheduler failures, null validation, native duplicate handler behavior, and sender-first two-argument preservation.
