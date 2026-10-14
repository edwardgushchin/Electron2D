# EventConnection

Last updated: 2026-10-14

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

Prepared reusable synchronous subscriptions with zero through four arguments create no per-emission capture. One-shot delivery and deferred argument capture retain their separate allocation paths; prepared reusable zero-argument deferred delivery uses one cached immutable callback. No zero-allocation claim is made for subscription/accessor work or allocating user handlers. This preserves argument values, lock boundaries, detach-before-handler ordering, cancellation and exception policy under ADR 0010.

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
| [`public static EventConnection Subscribe<T1, T2, T3>(Action<Action<T1, T2, T3>> subscribe, Action<Action<T1, T2, T3>> unsubscribe, Action<T1, T2, T3> handler, bool oneShot = false, Action<Action> defer = null)`](#m-electron2d-eventconnection-subscribe-3-system-action-system-action-0-1-2-system-action-system-action-0-1-2-system-action-0-1-2-system-boolean-system-action-system-action) | Creates a managed subscription to an event with three typed arguments. |
| [`public static EventConnection Subscribe<T1, T2, T3, T4>(Action<Action<T1, T2, T3, T4>> subscribe, Action<Action<T1, T2, T3, T4>> unsubscribe, Action<T1, T2, T3, T4> handler, bool oneShot = false, Action<Action> defer = null)`](#m-electron2d-eventconnection-subscribe-4-system-action-system-action-0-1-2-3-system-action-system-action-0-1-2-3-system-action-0-1-2-3-system-boolean-system-action-system-action) | Creates a managed subscription to an event with four typed arguments. |
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


<a id="m-electron2d-eventconnection-subscribe-3-system-action-system-action-0-1-2-system-action-system-action-0-1-2-system-action-0-1-2-system-boolean-system-action-system-action"></a>
### `public static EventConnection Subscribe<T1, T2, T3>(Action<Action<T1, T2, T3>> subscribe, Action<Action<T1, T2, T3>> unsubscribe, Action<T1, T2, T3> handler, bool oneShot = false, Action<Action> defer = null)`

Creates a managed subscription to an event with three typed arguments.

**Type parameters**

- `T1`: The first event argument type.
- `T2`: The second event argument type.
- `T3`: The third event argument type.

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


<a id="m-electron2d-eventconnection-subscribe-4-system-action-system-action-0-1-2-3-system-action-system-action-0-1-2-3-system-action-0-1-2-3-system-boolean-system-action-system-action"></a>
### `public static EventConnection Subscribe<T1, T2, T3, T4>(Action<Action<T1, T2, T3, T4>> subscribe, Action<Action<T1, T2, T3, T4>> unsubscribe, Action<T1, T2, T3, T4> handler, bool oneShot = false, Action<Action> defer = null)`

Creates a managed subscription to an event with four typed arguments.

**Type parameters**

- `T1`: The first event argument type.
- `T2`: The second event argument type.
- `T3`: The third event argument type.
- `T4`: The fourth event argument type.

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
- Authored persistent connections use registered SceneConnectionBinding factories; ordinary delegate closures are never copied or inspected. Both Node endpoints own deterministic cleanup.
- The implemented overloads cover the zero-, one-, and two-argument event shapes used by the current engine API.

## Verification

[`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) covers reusable and idempotently disposed connections; duplicate native subscriptions; synchronous, recursive, throwing, and 1,000-way concurrent one-shot delivery; deferred argument capture; cancellation before flush; deferred one-shot consumption and failures; add rollback; simultaneous add/rollback failures; removal failure; scheduler failure; null validation; and all three public overload shapes.


Authored typed scene connections use registered compiled `SceneConnectionBinding` factories, copied payloads and dual endpoint cleanup. PackedScene captures/rebinds these subscriptions, including file archives v3. SceneState queries expose paths, diagnostic labels, flags, unbind count and typed payload snapshots. See [packed scenes](../components/packed-scenes.md) for ownership and persistence limits.

[EventConnectionPhysicsTests](../../tests/Electron2D.Tests/EventConnectionPhysicsTests.cs) verifies all five prepared immediate arities and full selected CPU/GPU frames: 128 warm/128 measured frames, strict zero owner/all-thread managed bytes, 128 emissions of each arity and 64 Area body enters/exits. Identity, disposed subscriptions and collected callback-error recovery remain observable. Cold two-argument deferred/one-shot checks cover value capture, ordering, cancellation and recursion; the existing EventConnection suite retains concurrency, accessor rollback/removal/scheduler errors and all overloads. GPU execution uses CPU-only Vulkan; hardware/native/network/performance acceptance stays open.

The three/four-argument overloads preserve the same connection token policy without reducing physical event payloads. EventConnectionPhysicsTests exercises all five immediate arities in full CPU/GPU frames at 0/0 warm owner/all-thread bytes, including Area shape RID/owner/index payloads with a disabled first body slot. Cold checks cover actual deferred shape one-shot capture, generic argument ordering/cancellation, recursive consumption, nulls, partial subscription rollback and failed removal.

Captured reference arguments retain their original identity and ownership. Deferred delivery does not extend a borrowed input event lifetime; physical input subscriptions can inspect it synchronously, and independent deferred work must capture an owned copy during that callback. The three-argument picking fixture verifies the receiving viewport, live borrowed event and logical shape index through actual PushInput/PhysicsFrame.

Prepared reusable deferred zero-argument subscriptions now retain one immutable callback in the token and submit it for every emission. Each submission remains a separate queue entry; multiplicity, cancellation, scheduler errors and handler state checks are unchanged. There is no shared mutable argument slot or coalescing. With prepared SceneTree queue capacity and nonallocating handlers, the full physical frame retains zero owner/all-thread managed bytes. Deferred payload/one-shot capture, subscription and queue capacity growth remain separate allocating paths.

The shared EventConnectionPhysicsTests now verifies a three-emission deferred batch, 128 warm/128 measured prepared emit/drain cycles, pending cancellation and 128 real SceneTree.Defer callbacks inside measured CPU/GPU physical frames, alongside all five immediate arities, Area body/shape transitions and picking/lifetime/error cases. GPU profiles use CPU-only Vulkan; no hardware performance/native/platform acceptance is inferred.
