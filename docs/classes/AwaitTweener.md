# AwaitTweener

Last updated: 2026-09-21

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class AwaitTweener : Tweener`

> Waits for a typed C# event, an optional timeout, or disposal of the event publisher.

## Description

Waits for a typed C# event, an optional timeout, or disposal of the event publisher.

`AwaitTweener` is created by one of the three typed `Tween.TweenAwait` overloads and waits for a zero-, one-, or two-argument C# event. Its complete declared public API is `AwaitTweener SetTimeout(double timeout)`; `Finished` and lifetime API are inherited from [`Tweener`](Tweener.md).

Append establishes an owned `EventConnection`. Only an event observed while the wait is the active step releases it; observations before the step begins are cleared at start, and replayed parent loops begin a fresh wait. Event receipt atomically marks state and may occur on any thread. The next owner-thread advance completes the task and consumes that frame's remaining delta. A finite non-negative timeout can complete sooner and preserves overshoot. Disposing the publisher, losing the connection, parent completion/killing, or explicit disposal also ends or cancels the wait and disconnects.

The event subscription is established when the tweener is appended and released when the wait or its owner ends.

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

- `timeout`: Finite non-negative seconds.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `timeout` is negative, NaN, or infinite.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

## Inherited API

Public and protected members inherited from [Tweener](Tweener.md). Their lifecycle and error contracts remain applicable unless this page states an override.
