# AwaitTweener

Last updated: 2026-09-24

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class AwaitTweener : Tweener`

> Waits for a typed C# event, an optional timeout, or disposal of the event publisher.

## Description

Waits for a typed C# event, an optional timeout, or disposal of the event publisher.

`AwaitTweener` is created by one of the three typed `Tween.TweenAwait` overloads and waits for a zero-, one-, or two-argument C# event. Its complete declared public API is `AwaitTweener SetTimeout(double timeout)`; `Finished` and lifetime API are inherited from [`Tweener`](Tweener.md).

Append establishes an owned `EventConnection`. Only an event observed while the wait is the active step releases it; observations before the step begins are cleared at start, and replayed parent loops begin a fresh wait. Event receipt atomically marks state and may occur on any thread. The next owner-thread advance completes the task and consumes that frame's remaining delta. A finite non-negative timeout can complete sooner, takes priority over an event observed in the same frame, and preserves overshoot. A negative timeout disables expiry. Publisher disposal or loss of the connection ends the wait. The subscription remains available across parent loops and is disconnected when the parent is canceled, disposed, or removed from its tree.

The connection reports its own logical state; it cannot observe a publisher that independently removes its wrapper or clears its event invocation list. Supply a timeout when such external changes are possible. Event arities above two require a future typed `EventConnection` overload under ADR 0037.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
AwaitTweener wait = tween.TweenAwait(source,
    handler => source.Changed += handler,
    handler => source.Changed -= handler);
wait.SetTimeout(5.0);
```

## Methods

| Member | Description |
| --- | --- |
| [`public AwaitTweener SetTimeout(double timeout)`](#m-electron2d-awaittweener-settimeout-system-double) | Sets the maximum time to wait for the event. |

## Method Descriptions

<a id="m-electron2d-awaittweener-settimeout-system-double"></a>
### `public AwaitTweener SetTimeout(double timeout)`

Sets the maximum time to wait for the event.

**Parameters**

- `timeout`: Finite seconds; a negative value disables the timeout.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `timeout` is NaN or infinite.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

**Remarks:** A non-negative timeout takes priority when an event is also observed in the same processing frame.

## Inherited API

Public and protected members inherited from [Tweener](Tweener.md). Their lifecycle and error contracts remain applicable unless this page states an override.
