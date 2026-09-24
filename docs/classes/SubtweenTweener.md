# SubtweenTweener

Last updated: 2026-09-24

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class SubtweenTweener : Tweener`

> Runs another tween as one step in a parent tween.

## Description

Runs another tween as one step in a parent tween.

`SubtweenTweener` is created only by `Tween.TweenSubtween(Tween)` and runs another Tween as one parent step. Its complete declared public API is `SubtweenTweener SetDelay(double delay)`; completion and object API are inherited from [`Tweener`](Tweener.md).

Appending detaches the child from its original SceneTree, including a different tree on the same owner thread. An already invalid child is accepted and skipped when the parent reaches it. Self, repeated and cyclic nesting, a processing child, and cross-thread mutation are rejected before transfer. Each parent-loop start resets and plays a valid child. Parent pause, process lane, speed, and delivered delta control the child; its original tree no longer schedules it independently. The child retains its own speed policy. On completion, unused time advances the parent's next step. Parent completion, killing, disposal, or failure invalidates the child. A child failure invalidates the parent and is aggregated; a disposed child releases the parent without a disposed-state query.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
SubtweenTweener nested = tween.TweenSubtween(otherTween);
```

## Methods

| Member | Description |
| --- | --- |
| [`public SubtweenTweener SetDelay(double delay)`](#m-electron2d-subtweentweener-setdelay-system-double) | Sets the delay before the nested tween begins. |

## Method Descriptions

<a id="m-electron2d-subtweentweener-setdelay-system-double"></a>
### `public SubtweenTweener SetDelay(double delay)`

Sets the delay before the nested tween begins.

**Parameters**

- `delay`: Finite seconds; a negative delay begins on the first positive step.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `delay` is NaN or infinite.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

**Remarks:** On completion, unused child time advances the parent's next step. A disposed child releases the parent without a disposed-state query.

## Inherited API

Public and protected members inherited from [Tweener](Tweener.md). Their lifecycle and error contracts remain applicable unless this page states an override.
