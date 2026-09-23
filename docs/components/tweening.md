# Tweening component

Last updated: 2026-09-23

## Scope

This Scene component owns frame-driven, typed interpolation sequences. It provides scalar and engine-math interpolation, sequential and parallel steps, callbacks, intervals, nested sequences, typed event waits, finite and infinite loops, binding to Node lifetime/process policy, process/physics lanes, pause policy, speed scaling, and Engine time-scale bypass.

## Owned types

| Type | Role |
| --- | --- |
| [`Tween`](../classes/Tween.md) | SceneTree-registered sequence, processing policy, lifecycle, events, and typed append API |
| [`Tween.TweenProcessMode`](../classes/Tween.TweenProcessMode.md) | Process versus physics frame lane |
| [`Tween.TweenPauseMode`](../classes/Tween.TweenPauseMode.md) | Bound-node, tree-stop, or always-process pause behavior |
| [`Tween.TransitionType`](../classes/Tween.TransitionType.md) | Twelve interpolation curve families |
| [`Tween.EaseType`](../classes/Tween.EaseType.md) | In, out, in-out, and out-in curve direction |
| [`Tweener`](../classes/Tweener.md) | Owned abstract unit and completion event |
| [`PropertyTweener<TValue>`](../classes/PropertyTweener.Generic.md) | Typed property accessors, start policy, relative mode, delay, and custom weight mapping |
| [`MethodTweener<TValue>`](../classes/MethodTweener.Generic.md) | Typed interpolated callback |
| [`CallbackTweener`](../classes/CallbackTweener.md) | Delayed parameterless callback |
| [`IntervalTweener`](../classes/IntervalTweener.md) | Duration-only step |
| [`SubtweenTweener`](../classes/SubtweenTweener.md) | Nested Tween step |
| [`AwaitTweener`](../classes/AwaitTweener.md) | Typed C# event wait with optional timeout |

## Runtime flow

`SceneTree.CreateTween()` registers an empty running tween; `Node.CreateTween()` additionally binds it to that node. Each frame first runs node callbacks, then lightweight tree timers, then a captured list of matching tweens, and finally deferred/deletion work. Tween creation during tween processing waits until the next frame. Each active tween scales the selected scaled or original delta, starts its current step, and attempts every tweener in a parallel group. The least unconsumed delta controls advancement to later steps. Completion events run synchronously before tree invalidation.

Property and method tweeners resolve interpolation once at creation. Built-in allocation-free interpolation covers `bool`, `float`, `double`, `int`, `long`, `Vector2`, `Vector2I`, `Vector3`, `Vector3I`, `Vector4`, `Vector4I`, `Color`, `Rect`, `RectI`, and `Transform`; caller-supplied typed interpolation covers other values. Boolean values switch at the numeric half threshold. Integer components round midpoint values away from zero and throw on result overflow. Relative addition is arithmetic for scalar/vector/color/rectangle values, replacement by the configured delta for booleans, and parent-right affine composition for `Transform`. String and collection interpolation is not built in and instead requires an explicit typed interpolator. No dynamic value container, string property path, reflection lookup, or background scheduler is used.

Typed event waits reuse Core `EventConnection`. They subscribe on append, accept events from any thread through an atomic received flag, consume the active frame, and disconnect on tween completion, killing, or disposal. Nested tweens are removed from independent SceneTree processing and follow the parent timeline and final lifetime.

## Dependencies and invariants

- Depends on `SceneTree`, `Node`, `ElectronObject`, `EventConnection`, `MathF`, and current Core math values.
- Creation, configuration, stepping, killing, and disposal use the creating SceneTree's owner thread. Event receipt alone may occur on another thread.
- A valid tween belongs to exactly one tree processing list or one parent subtween. Captured top-level entries revalidate lane and nested ownership, so a transfer during an earlier tween callback cannot process twice. Completion, killing, bound-node disposal, tree finalization, or processing failure invalidates it. Binding to a node already owned by another tree and nesting a currently processing tween are rejected.
- Appending is allowed only before first processing or after `Stop()` resets the sequence. Tweeners cannot be constructed independently.
- A parallel callback failure does not prevent sibling tweeners or later SceneTree tweens/phases from being attempted. The failed tween becomes invalid and errors are aggregated.
- Infinite loops that consume no time are terminated after two unchanged executions in one advance.
- Warmed active processing reuses SceneTree buffers and performs no managed allocation in the covered path.

## Current status and exclusions

Implemented and executable-tested. Typed delegates deliberately replace untyped callables, dynamic values, and property paths. `AwaitTweener` supports events with zero, one, or two typed arguments because that is the current `EventConnection` surface. String interpolation, arbitrary collection interpolation, more-than-two-argument event waits, editor curve resources, serialization of live tween state, and cross-thread tween mutation are absent. Tween objects use managed lifetime plus explicit `IDisposable`; no public reference-counting protocol exists.

## Verification

The executable harness covers stable enum identities and every transition/ease endpoint, manual scalar/vector/integer/boolean/transform interpolation and invalid values, sequential/parallel order, exact boundaries, all property-start/relative/custom-weight controls, restart, finite/infinite loops, pause and physics lanes, binding disposal and cross-tree rejection, manual stepping and its failure cleanup, pre-start event reset, active typed event receipt, timeout and cancellation failure, nested ownership, lane/nesting snapshot revalidation, callback and every completion-event failure, Engine time-scale bypass, tree rollback/finalization, validation, owner-thread rejection, and zero warmed active-frame allocation. It does not verify visual motion, real host cadence, all five target hosts, editor tooling, or large tween counts.

## Decisions

- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0014: Managed lifetime and allocation policy](../decisions/resources.md#adr-0014)
- [0016: Engine dual-delta scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0037: Typed SceneTree tween scheduling](../decisions/scene.md#adr-0037)
