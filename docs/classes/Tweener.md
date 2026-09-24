# Tweener

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [AwaitTweener](AwaitTweener.md), [CallbackTweener](CallbackTweener.md), [IntervalTweener](IntervalTweener.md), [MethodTweener<TValue>](MethodTweener.Generic.md), [PropertyTweener<TValue>](PropertyTweener.Generic.md), [SubtweenTweener](SubtweenTweener.md)

- **Source:** [`src/Scene/Animation/Tweeners.cs`](../../src/Scene/Animation/Tweeners.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class Tweener : ElectronObject`

> Defines one task executed as part of a [`Tween`](Tween.md) step.

## Description

Defines one task executed as part of a [`Tween`](Tween.md) step.

`Tweener` is the non-constructible public base for one task owned by a [`Tween`](Tween.md). Its sole declared public member is `event Action<Tweener>? Finished`, raised synchronously after successful task completion or loss/disposal of a task target. Killing a parent does not report unfinished tweeners as finished. Inherited `ElectronObject` API remains available.

The parent assigns ownership and controls start, elapsed time, stepping, cancellation, and disposal. A tweener resets when its step is replayed by a parent loop. Explicit disposal uses the parent owner thread, cancels subscriptions, clears subscribers, and makes later parent processing treat the tweener as inactive.

User completion subscriber exceptions propagate into the parent step. Parallel siblings are still attempted; the parent tween is then invalidated and SceneTree aggregates the failure. Tweener instances cannot be constructed or attached independently, moved between parents, or processed on another thread.

Dependencies are `ElectronObject` and `Tween`. `VerifyTweenCallbackIntervals` checks completion ordering, repeated loops, unavailable callback targets, cancellation and failure; the broader tween harness covers parallel failure continuation, waits, nested cancellation and owner-thread behavior. There is no public custom-Tweener extension point, serialization, reference-counted lifetime, or standalone scheduler.

Tweeners are created only by the corresponding [`Tween`](Tween.md) append methods. They are owned by that tween,
can run sequentially or in a parallel group, and use the same owner-thread contract.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
Tweener step = tween.TweenInterval(0.5);
step.Finished += _ => Console.WriteLine("Step complete");
```

## Methods

| Member | Description |
| --- | --- |
| [`protected override void ValidateDisposal()`](#m-electron2d-tweener-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-tweener-dispose-system-boolean) | Releases resources owned by a derived class. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<Tweener> Finished`](#e-electron2d-tweener-finished) | Occurs immediately after this tweener completes or its target becomes unavailable. |

## Method Descriptions

<a id="m-electron2d-tweener-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: The caller is not the owning tween's thread.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

Requires the owning tween's thread while attached.

<a id="m-electron2d-tweener-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

Cancels owned subscriptions, clears completion subscribers, and calls the base implementation.

## Event Descriptions

<a id="e-electron2d-tweener-finished"></a>
### `public event Action<Tweener> Finished`

Occurs immediately after this tweener completes or its target becomes unavailable.

**Remarks:** The source tweener is passed as the sole argument. A killed tween does not complete unfinished tweeners.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.
