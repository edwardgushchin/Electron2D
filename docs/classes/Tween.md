# Tween

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
Declaration: `public sealed class Tween : ElectronObject`

## Responsibility and ownership

`Tween` owns an ordered list of parallel step groups and coordinates interpolation, callbacks, event waits, nested tweens, looping, and completion. It is constructed only by `SceneTree.CreateTween()` or `Node.CreateTween()`. The SceneTree processes valid top-level tweens; a parent Tween processes a detached subtween. The object remains inspectable after invalidation until explicitly disposed, while its managed memory remains runtime-owned.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `Finished` | Synchronous event after the final loop; the tween is stopped but still valid during delivery |
| `LoopFinished` | Synchronous sender-first event after each non-final loop, with one-based completed count |
| `StepFinished` | Synchronous sender-first event after each sequential/parallel step, with zero-based index |
| `BindNode(Node)` | Halts while outside its owning tree, follows bound process policy in `Bound` pause mode, dies with the node, and rejects a node currently owned by another tree |
| `Chain()` / `Parallel()` | Makes the next append begin a new step or join the preceding one |
| `CustomStep(double)` | Manually advances even while paused; completion unregisters immediately |
| `GetLoopsLeft()` | Returns `-1` for infinite, `0` after completion, otherwise remaining executions |
| `GetTotalElapsedTime()` | Accumulated speed-scaled active delta, including final overshoot; reset by `Stop()` |
| `HasTweeners()` | True once a tweener has been appended, including after tween invalidation |
| `InterpolateValue<TValue>(...)` | Static typed initial-plus-delta interpolation with extrapolation and zero-duration final behavior |
| `IsRunning()` / `IsValid()` | Current play state and SceneTree/parent registration validity |
| `Kill()` | Aborts without completion events and invalidates nested work |
| `Pause()` / `Play()` / `Stop()` | Pause state, resume/restart, and reset without restoring targets |
| `SetEase`, `SetTrans` | Defaults captured by property/method tweeners appended afterward |
| `SetIgnoreTimeScale(bool)` | Chooses Engine's original delta instead of scaled delta |
| `SetLoops(int)` | Zero means infinite; positive values are total runs |
| `SetParallel(bool)` | Sets default grouping and includes the immediately preceding step in the next group |
| `SetPauseMode(TweenPauseMode)` | Bound, tree-stop, or pause-independent behavior |
| `SetProcessMode(TweenProcessMode)` | Process or physics lane |
| `SetSpeedScale(double)` | Finite non-negative multiplier; zero freezes progression |
| `TweenCallback(Action)` | Appends `CallbackTweener` |
| `TweenInterval(double)` | Appends `IntervalTweener` |
| `TweenMethod<TValue>(...)` | Appends typed callback interpolation with optional custom value interpolation |
| `TweenProperty<TTarget,TValue>(...)` | Appends typed explicit getter/setter interpolation for a live `ElectronObject` |
| `TweenSubtween(Tween)` | Transfers a same-tree valid tween into this timeline |
| `TweenAwait(...)` | Three overloads append waits for zero-, one-, or two-argument typed C# events |

Nested public enums are documented separately. Inherited identity, property-list, notification, translation, and disposal API follows [`ElectronObject`](ElectronObject.md).

## Lifecycle, invariants, errors, and threading

An empty tween is valid on creation but fails and invalidates if any matching frame reaches it without appended tweeners, including a zero-delta frame. First processing captures property start values and freezes the append surface. `Stop()` reopens appending and resets cursor/time without changing targets; `Play()` restarts from the resulting state. Exact step completion with no remaining delta defers the next zero-duration step to a later positive-delta frame. Normal final delivery occurs before SceneTree invalidation. Kill, bound-node disposal, tree finalization, an empty sequence, and user callback failure do not raise `Finished`.

Every parallel tweener is attempted before failures are reported. Any processing failure invalidates the complete tween, cancels event subscriptions/nested work, unregisters the tween even when failure came from `CustomStep()`, and reaches SceneTree as an aggregate during frame processing while later tweens, deferred work, and deletion still run. Infinite zero-duration loops fail instead of hanging, even if callbacks mutate speed. Captured SceneTree entries revalidate lane and nested ownership before execution. Invalid durations, speed, loop counts, enum values, cross-tree binding/nesting, cyclic/multiple/in-progress nesting, unsupported built-in value types, recursive stepping, append-after-start, and off-owner-thread mutation fail explicitly.

All public mutation, processing, and disposal are owner-thread operations. Typed event notification may arrive on another thread; `AwaitTweener` only atomically records it. Reads are not a synchronization contract. Disposal is rejected during processing, invalidates nested tweens, disposes owned tweener objects, clears subscribers, and aggregates cleanup failures. A nested Tween object remains managed and inspectable until separately disposed.

## Dependencies, verification, and limitations

Depends on [`SceneTree`](SceneTree.md), [`Node`](Node.md), [`Tweener`](Tweener.md), Core math types, and `EventConnection`. The executable tests cover the behavior listed in the [Tweening component](../components/tweening.md). No string property path, dynamic value, reflection callable, independent clock/thread, editor serialization, or visual acceptance exists.

## Official-reference coverage inventory

The current stable [`Tween` API](https://docs.godotengine.org/en/stable/classes/class_tween.html), its full inheritance chain, and the 4.7.2 stable [`tween.h`](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/animation/tween.h)/[`tween.cpp`](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/animation/tween.cpp) were audited. All listed methods, signals, enums, sequencing, pause/process policies, loops, nested tween, and event-wait behavior are implemented. Untyped `Variant`, `Callable`, `Signal`, and string `NodePath` parameters are intentionally adapted to generic values, delegates, event accessors, and explicit property accessors under ADRs 0001 and 0002. Built-in generic interpolation covers booleans, scalar numbers, and implemented engine math values; strings and collections require an explicit typed interpolator instead of reproducing universal-value conversion. `RefCounted` is intentionally replaced by managed lifetime plus `IDisposable` under ADRs 0003/0014. No inherited Object item outside the existing `ElectronObject` contract is duplicated.
