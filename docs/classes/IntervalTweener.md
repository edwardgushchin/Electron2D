# IntervalTweener

Last updated: 2026-09-24

**Inherits:** [Tweener](Tweener.md)

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class IntervalTweener : Tweener`

> Consumes a fixed duration without invoking a callback or modifying a value.

## Description

Consumes a fixed duration without invoking a callback or modifying a value.

`IntervalTweener` is created only by `Tween.TweenInterval(double)` and consumes one finite duration without calling user code or modifying a value. A zero or negative interval completes on the first positive step; a zero-delta frame does not advance it. It declares no public members beyond inherited [`Tweener.Finished`](Tweener.md) and `ElectronObject` lifetime API. It preserves delivered-frame overshoot for later steps, resets on each parent loop, and completes at an exact boundary.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
IntervalTweener delay = tween.TweenInterval(0.5);
```

## Inherited API

Public and protected members inherited from [Tweener](Tweener.md). Their lifecycle and error contracts remain applicable unless this page states an override.
