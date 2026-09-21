# SubtweenTweener

Last updated: 2026-09-21

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class SubtweenTweener : Tweener`

> Runs another tween as one step in a parent tween.

## Description

Runs another tween as one step in a parent tween.

`SubtweenTweener` is created only by `Tween.TweenSubtween(Tween)` and runs another Tween as one parent step. Its complete declared public API is `SubtweenTweener SetDelay(double delay)`; completion and object API are inherited from [`Tweener`](Tweener.md).

Appending requires a valid same-tree tween, removes it from independent SceneTree processing, rejects self/multiple/cyclic/cross-tree nesting, and transfers final lifetime to the parent. Each parent-loop start resets and plays the child. Parent pause, process lane, speed, and delivered delta control the child; the child's own tree scheduling settings no longer schedule it separately. Parent completion, killing, disposal, or failure invalidates the child. A child failure invalidates the parent and is aggregated.

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

- `delay`: Finite non-negative seconds.

**Returns:** This tweener.

**Exceptions**

- `ArgumentOutOfRangeException`: `delay` is negative, NaN, or infinite.
- `InvalidOperationException`: The call is off the owner thread.
- `ObjectDisposedException`: The tweener is disposing or disposed.

## Inherited API

Public and protected members inherited from [Tweener](Tweener.md). Their lifecycle and error contracts remain applicable unless this page states an override.
