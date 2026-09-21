# EventConnection

Last updated: 2026-09-21

## Declaration

- Source: [`EventConnection.cs`](../../src/Core/Object/EventConnection.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class EventConnection : IDisposable`
- Domain: [Core](../domains/core.md)
- Component: [Typed event connections](../components/event-connections.md)

## Responsibility and ownership

`EventConnection` owns one wrapper attached to an ordinary typed C# event. It provides deterministic disconnection, optional one-shot consumption, and optional deferred delivery without string event names, untyped argument containers, reflection, or a second signal registry.

The caller owns the returned token and must dispose it no later than the subscriber. The token holds the supplied add/remove closures, wrapper delegate, handler, and optional scheduler until it becomes terminal. A publisher can still retain an inert wrapper if a custom removal accessor throws.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `bool IsConnected { get; }` | Thread-safe logical-state snapshot; `true` only while this token accepts emissions, but not proof that a publisher has retained the wrapper |
| `Subscribe(Action<Action> subscribe, Action<Action> unsubscribe, Action handler, bool oneShot = false, Action<Action>? defer = null)` | Connects an event without arguments |
| `Subscribe<T>(Action<Action<T>> subscribe, Action<Action<T>> unsubscribe, Action<T> handler, bool oneShot = false, Action<Action>? defer = null)` | Connects an event with one typed argument |
| `Subscribe<T1, T2>(Action<Action<T1, T2>> subscribe, Action<Action<T1, T2>> unsubscribe, Action<T1, T2> handler, bool oneShot = false, Action<Action>? defer = null)` | Connects a sender-first event with one additional typed argument, or any other two-argument event |
| `Dispose()` | Idempotently makes the token terminal, removes an active wrapper, and cancels deferred callbacks that have not started |

The add and remove delegates must attach and detach the exact wrapper they receive. Passing `SceneTree.Defer` as `defer` schedules accepted callbacks on that tree's next available deferred batch; passing `null` invokes synchronously.

```csharp
using var connection = EventConnection.Subscribe<ElectronObject>(
    callback => source.ScriptChanged += callback,
    callback => source.ScriptChanged -= callback,
    HandleScriptChanged,
    oneShot: true,
    defer: tree.Defer);
```

## State transitions

Reusable synchronous connections remain `Connected` until disposal. Reusable deferred connections remain connected while any number of callbacks are queued; disposal makes every queued callback that has not started inert.

One-shot synchronous delivery transitions from connected to completed and removes the wrapper before calling the user handler. This prevents recursive or concurrent emissions from invoking the handler again, including when the handler throws. Terminal transitions clear the token's accessor, handler, and scheduler references so a retained token does not retain its former publisher, subscriber, or scene scheduler.

One-shot deferred delivery transitions from connected to pending on the first accepted emission, removes the wrapper, captures the arguments, and schedules one callback. `IsConnected` is already `false` while that callback is pending. Disposal during the pending interval cancels it. At execution, the token becomes completed before the handler runs, so handler failure cannot reconnect it.

## Exceptions and failure containment

- Null add, remove, or handler delegates are rejected before subscription.
- An add-accessor failure triggers one removal attempt. If rollback succeeds, the original exception is rethrown with its stack; if rollback also fails, both failures are preserved in `AggregateException`.
- A removal-accessor failure propagates, but the token remains terminal and its retained wrapper is inert if the publisher still holds it.
- A reusable scheduler failure rejects only that emission and leaves the connection active.
- A one-shot scheduler failure propagates after the subscription has been consumed; it is not retried.
- Immediate handler exceptions propagate from the event publisher. Deferred handler exceptions follow the supplied scheduler's policy; `SceneTree` reports them from its flush as part of `AggregateException`.

## Threading and re-entrancy

Connection state is serialized by a private lock. One-shot acceptance is atomic, so concurrent emissions select at most one winner. Removal and callbacks execute outside the lock to avoid deadlocking user code. Disposal can race with emission; it prevents callbacks that have not crossed their final state check but does not wait for a callback already beginning execution.

The token does not make the publisher, subscriber, handler state, or custom event accessors thread-safe. The selected deferred scheduler determines callback thread and ordering.

## Deliberate boundaries

- There is no options enum, dynamic connection method, event-name lookup, argument rebinding, or untyped callable.
- Duplicate ordinary `+=` subscriptions retain native multicast-delegate behavior: each occurrence is invoked and each `-=` removes one occurrence.
- Source objects are explicit typed event arguments. The token never appends or reshapes arguments.
- Persistent connections are unavailable until a future component defines declarative typed endpoint identities; in-memory packed scenes do not inspect or copy delegate closures.
- The implemented overloads cover the zero-, one-, and two-argument event shapes used by the current engine API.

## Verification

[`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) covers reusable and idempotently disposed connections; duplicate native subscriptions; synchronous, recursive, throwing, and 1,000-way concurrent one-shot delivery; deferred argument capture; cancellation before flush; deferred one-shot consumption and failures; add rollback; simultaneous add/rollback failures; removal failure; scheduler failure; null validation; and all three public overload shapes.
