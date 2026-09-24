# Tweening component

Last updated: 2026-09-24

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

`SceneTree.CreateTween()` registers an empty running tween; `Node.CreateTween()` additionally binds it to that node. Each frame first runs node callbacks, then lightweight tree timers, then a captured list of matching tweens, and finally deferred/deletion work. Tween creation during tween processing waits until the next frame. Each active tween scales the selected scaled or original delta, starts its current step, and attempts every tweener in a parallel group. The least unconsumed delta controls advancement to later steps. Completion events run synchronously; a finished tween stays registered until its next eligible tree step, unless a subscriber restarts it. Killing invalidates immediately but retains the registry entry until that sweep.

Property and method tweeners resolve interpolation once at creation. Built-in allocation-free interpolation covers `bool`, `float`, `double`, `int`, `long`, `Vector2`, `Vector2i`, `Vector3`, `Vector3i`, `Vector4`, `Vector4i`, `Color`, `Rect`, `Rect2i`, and `Transform`; caller-supplied typed interpolation covers other values. Boolean values switch at the numeric half threshold. Integer components round midpoint values away from zero and throw on result overflow. Relative addition is arithmetic for scalar/vector/color/rectangle values, replacement by the configured delta for booleans, and parent-right affine composition for `Transform`. String and collection interpolation is not built in and instead requires an explicit typed interpolator. No dynamic value container, string property path, reflection lookup, or background scheduler is used.

Callback and interval tasks accept finite signed delays or durations. Negative values complete on the first positive step, while the parent caps the time forwarded between steps at the delivered frame delta. A zero interval waits for positive frame time. `Tweener.Finished` fires before `StepFinished`, then `LoopFinished` for a non-final loop or `Tween.Finished` for the final loop. An unavailable direct callback target finishes without invocation; killing an unfinished task does not emit completion.

Typed event waits reuse Core `EventConnection`. They subscribe on append, accept events from any thread through an atomic received flag, consume the active frame, and disconnect on tween completion, killing, or disposal. A negative timeout disables expiry; a non-negative timeout wins a same-frame race with event receipt and forwards overshoot. The typed token cannot detect an event list independently cleared by the publisher; a timeout bounds that wait. Nested tweens are removed from independent SceneTree processing and follow the parent timeline and final lifetime.

## Dependencies and invariants

- Depends on `SceneTree`, `Node`, `ElectronObject`, `EventConnection`, `Mathf`, and current Core math values.
- Creation, configuration, stepping, killing, and disposal use the creating SceneTree's owner thread. Event receipt alone may occur on another thread.
- A top-level tween has one SceneTree registry entry until an eligible cleanup step, even if Kill has already made it invalid; a nested tween instead belongs to one parent. Captured top-level entries revalidate lane and nested ownership, so a transfer during an earlier tween callback cannot process twice. Ordinary completion invalidates on the next matching tree step; killing, bound-node disposal, tree finalization, or processing failure invalidates immediately. Binding to a node already owned by another tree and nesting a currently processing tween are rejected.
- Appending is allowed only before first processing or after `Stop()` resets the sequence. Tweeners cannot be constructed independently.
- A parallel callback failure does not prevent sibling tweeners or later SceneTree tweens/phases from being attempted. The failed tween becomes invalid and errors are aggregated.
- Infinite loops that consume no time are terminated after two unchanged executions in one advance.
- Warmed active processing reuses SceneTree buffers and performs no managed allocation in the covered path.

## Current status and exclusions

Implemented and executable-tested. Typed delegates deliberately replace untyped callables, dynamic values, and property paths. `AwaitTweener` supports events with zero, one, or two typed arguments because that is the current `EventConnection` surface. String interpolation, arbitrary collection interpolation, more-than-two-argument event waits, editor curve resources, serialization of live tween state, and cross-thread tween mutation are absent. Tween objects use managed lifetime plus explicit `IDisposable`; no public reference-counting protocol exists.

## Verification

The executable harness covers stable enum identities and every transition/ease endpoint, manual scalar/vector/integer/boolean/transform interpolation and invalid values, sequential/parallel order, exact boundaries, all property-start/relative/custom-weight controls, restart, finite/infinite loops, pause and physics lanes, binding disposal and cross-tree rejection, manual stepping and its failure cleanup, pre-start event reset, active typed event receipt, timeout and cancellation failure, nested ownership, lane/nesting snapshot revalidation, callback and every completion-event failure, Engine time-scale bypass, tree rollback/finalization, validation, owner-thread rejection, and zero warmed active-frame allocation. It does not verify visual motion, real host cadence, all five target hosts, editor tooling, or large tween counts.

The policy audit additionally checks every enum value, persistent and one-shot parallel grouping, per-append curve defaults, remaining loop counts including negative infinite loops, detached binding and later attachment, all three tree-pause policies, zero, negative and changed speed, both frame lanes, time-scale bypass reset, and invalid enum/non-finite configuration values. These 16 declaration rows are Implemented in coverage.

The lifecycle audit checks creation, pause/resume, stop/restart after a partial loop, overshoot time, signed manual deltas, `Finished` restart, finishing-frame validity, next-tree-frame removal and tweener clearing, immediate/idempotent invalidation by Kill with delayed registry cleanup, invalid-object calls and nested completion timing. Nine lifecycle/query rows are Implemented; type and remaining task rows retain their own status.

The callback/interval audit checks null and non-finite rollback, zero/exact/negative timing, live delay changes, owner-thread guards, disposed direct targets, cancellation, callback failure, per-loop completion and the full tweener/step/loop/final event order. The callback/interval types and their own methods plus shared completion-signal rows are Implemented.

The method-tweener audit checks typed built-in and custom interpolation, finite signed duration/delay, exact start and final values, per-tweener default/override curves, live configuration, loop resets, unavailable direct targets, and callback/interpolator failures. Its own declaration rows are Implemented.

The property-tweener audit checks typed append validation, immediate and deferred start capture around the pinned `1e-5` threshold, `From`/`FromCurrent` chaining and live displacement, delayed relative final values, scalar/vector/transform/bool interpolation, signed times, custom final-weight overshoot, curve changes, error continuation and zero warmed managed allocations. Its own declaration rows are Implemented.

The subtween audit checks signed/live delay, source-tree detachment including cross-tree transfer on a shared owner thread, parent pause/lane and combined speed, child reset across loops, next-frame completion and unused-time forwarding. Invalid children are skipped; explicit child disposal and nested callback failure release or invalidate the parent safely. The subtween's own rows are Implemented.

The await audit checks all three typed event arities, append-time subscription and rollback, pre-start receipt reset, cross-thread receipt with owner-thread completion, same-frame timeout priority, negative/zero/live timeout, source disposal, loop replay, cancellation and completion subscriber failure. Its own rows are Implemented under the accepted typed-event scope; event arities above two and publisher-side invocation-list inspection remain explicit future boundaries.

The static interpolation audit checks 96 numeric samples from the pinned easing equations across all 12 transitions, four eases and two elapsed fractions, plus every supported typed value family, signed and zero durations, extrapolation, invalid inputs, overflow and zero warmed managed allocations. Int64 interpolation now uses wide intermediates for exact near-limit endpoints. All own Tween and Tweener declaration and type rows are Implemented; inherited managed-lifetime roles and platform acceptance retain their separate evidence limits.

## Decisions

- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0014: Managed lifetime and allocation policy](../decisions/resources.md#adr-0014)
- [0016: Engine dual-delta scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0037: Typed SceneTree tween scheduling](../decisions/scene.md#adr-0037)
