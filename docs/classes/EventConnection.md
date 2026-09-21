# EventConnection

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Object/EventConnection.cs`](../../src/Core/Object/EventConnection.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class EventConnection`

> Owns a typed event subscription that can be disconnected, delivered once, or scheduled for later delivery.

## Description

Owns a typed event subscription that can be disconnected, delivered once, or scheduled for later delivery.

`EventConnection` owns one wrapper attached to an ordinary typed C# event. It provides deterministic disconnection, optional one-shot consumption, and optional deferred delivery without string event names, untyped argument containers, reflection, or a second signal registry.

The caller owns the returned token and must dispose it no later than the subscriber. The token holds the supplied add/remove closures, wrapper delegate, handler, and optional scheduler until it becomes terminal. A publisher can still retain an inert wrapper if a custom removal accessor throws.

This type wraps ordinary C# event accessors without introducing string-addressed signals or untyped arguments.
Disposing the connection removes its wrapper and cancels deferred callbacks that have not started. A callback that
has already started can finish concurrently with disposal.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var source = new Resource();
using EventConnection connection = EventConnection.Subscribe<Resource>(
    handler => source.Changed += handler,
    handler => source.Changed -= handler,
    _ => Refresh());
```

## Properties

| Member | Description |
| --- | --- |
| [`public bool IsConnected { get; }`](#p-electron2d-eventconnection-isconnected) | Gets whether this connection still accepts event emissions. |

## Methods

| Member | Description |
| --- | --- |
| [`public static EventConnection Subscribe(Action<Action> subscribe, Action<Action> unsubscribe, Action handler, bool oneShot = false, Action<Action> defer = null)`](#m-electron2d-eventconnection-subscribe-system-action-system-action-system-action-system-action-system-action-system-boolean-system-action-system-action) | Creates a managed subscription to an event with no arguments. |
| [`public static EventConnection Subscribe<T>(Action<Action<T>> subscribe, Action<Action<T>> unsubscribe, Action<T> handler, bool oneShot = false, Action<Action> defer = null)`](#m-electron2d-eventconnection-subscribe-1-system-action-system-action-0-system-action-system-action-0-system-action-0-system-boolean-system-action-system-action) | Creates a managed subscription to an event with one typed argument. |
| [`public static EventConnection Subscribe<T1, T2>(Action<Action<T1, T2>> subscribe, Action<Action<T1, T2>> unsubscribe, Action<T1, T2> handler, bool oneShot = false, Action<Action> defer = null)`](#m-electron2d-eventconnection-subscribe-2-system-action-system-action-0-1-system-action-system-action-0-1-system-action-0-1-system-boolean-system-action-system-action) | Creates a managed subscription to an event with two typed arguments. |
| [`public void Dispose()`](#m-electron2d-eventconnection-dispose) | Disconnects the wrapper and cancels deferred callbacks that have not started. |

## Property Descriptions

<a id="p-electron2d-eventconnection-isconnected"></a>
### `public bool IsConnected { get; }`

Gets whether this connection still accepts event emissions.

**Value:** `true` until disposal or, for a one-shot connection, until the first emission is accepted.
A deferred one-shot callback can still be pending when this property is `false`.

**Remarks:** This reports the token's logical state. It cannot detect a publisher that independently clears or replaces its
event invocation list.

## Method Descriptions

<a id="m-electron2d-eventconnection-subscribe-system-action-system-action-system-action-system-action-system-action-system-boolean-system-action-system-action"></a>
### `public static EventConnection Subscribe(Action<Action> subscribe, Action<Action> unsubscribe, Action handler, bool oneShot = false, Action<Action> defer = null)`

Creates a managed subscription to an event with no arguments.

**Parameters**

- `subscribe`: Adds the supplied wrapper to the event.
- `unsubscribe`: Removes the same wrapper from the event.
- `handler`: Receives accepted event emissions.
- `oneShot`: Whether only the first accepted emission can invoke the handler.
- `defer`: An optional scheduler that accepts work for later execution. Pass [`SceneTree.Defer(Action)`](SceneTree.md#m-electron2d-scenetree-defer-system-action) for scene-tree
deferred delivery; pass `null` for synchronous delivery.

**Returns:** An idempotently disposable connection token.

**Exceptions**

- `ArgumentNullException`: A required delegate is `null`.
- `Exception`: A supplied event accessor or scheduler throws. If subscription fails after partially attaching the wrapper,
rollback is attempted; failures from both operations are reported as an `AggregateException`.

**Remarks:** For one-shot delivery, the wrapper is disconnected before the handler runs, which prevents re-entrant duplicate
delivery. Disposing a deferred connection cancels callbacks that have not started. Handler exceptions propagate
on the invoking thread for synchronous delivery or through the selected scheduler for deferred delivery.

<a id="m-electron2d-eventconnection-subscribe-1-system-action-system-action-0-system-action-system-action-0-system-action-0-system-boolean-system-action-system-action"></a>
### `public static EventConnection Subscribe<T>(Action<Action<T>> subscribe, Action<Action<T>> unsubscribe, Action<T> handler, bool oneShot = false, Action<Action> defer = null)`

Creates a managed subscription to an event with one typed argument.

**Type parameters**

- `T`: The event argument type.

**Parameters**

- `subscribe`: Adds the supplied wrapper to the event.
- `unsubscribe`: Removes the same wrapper from the event.
- `handler`: Receives accepted event emissions.
- `oneShot`: Whether only the first accepted emission can invoke the handler.
- `defer`: An optional scheduler that accepts work for later execution. Pass [`SceneTree.Defer(Action)`](SceneTree.md#m-electron2d-scenetree-defer-system-action) for scene-tree
deferred delivery; pass `null` for synchronous delivery.

**Returns:** An idempotently disposable connection token.

**Exceptions**

- `ArgumentNullException`: A required delegate is `null`.
- `Exception`: A supplied event accessor or scheduler throws. If subscription fails after partially attaching the wrapper,
rollback is attempted; failures from both operations are reported as an `AggregateException`.

**Remarks:** Event arguments are captured at emission time. For one-shot delivery, the wrapper is disconnected before the
handler runs or is scheduled. Disposing a deferred connection cancels callbacks that have not started.

<a id="m-electron2d-eventconnection-subscribe-2-system-action-system-action-0-1-system-action-system-action-0-1-system-action-0-1-system-boolean-system-action-system-action"></a>
### `public static EventConnection Subscribe<T1, T2>(Action<Action<T1, T2>> subscribe, Action<Action<T1, T2>> unsubscribe, Action<T1, T2> handler, bool oneShot = false, Action<Action> defer = null)`

Creates a managed subscription to an event with two typed arguments.

**Type parameters**

- `T1`: The first event argument type.
- `T2`: The second event argument type.

**Parameters**

- `subscribe`: Adds the supplied wrapper to the event.
- `unsubscribe`: Removes the same wrapper from the event.
- `handler`: Receives accepted event emissions.
- `oneShot`: Whether only the first accepted emission can invoke the handler.
- `defer`: An optional scheduler that accepts work for later execution. Pass [`SceneTree.Defer(Action)`](SceneTree.md#m-electron2d-scenetree-defer-system-action) for scene-tree
deferred delivery; pass `null` for synchronous delivery.

**Returns:** An idempotently disposable connection token.

**Exceptions**

- `ArgumentNullException`: A required delegate is `null`.
- `Exception`: A supplied event accessor or scheduler throws. If subscription fails after partially attaching the wrapper,
rollback is attempted; failures from both operations are reported as an `AggregateException`.

**Remarks:** Event arguments are captured at emission time. For one-shot delivery, the wrapper is disconnected before the
handler runs or is scheduled. Disposing a deferred connection cancels callbacks that have not started.

<a id="m-electron2d-eventconnection-dispose"></a>
### `public void Dispose()`

Disconnects the wrapper and cancels deferred callbacks that have not started.

**Exceptions**

- `Exception`: The supplied event removal accessor throws.

**Remarks:** The operation is idempotent and safe to race with event delivery. It does not wait for a handler that has already
started. The connection remains terminal even if the supplied removal accessor throws.

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
