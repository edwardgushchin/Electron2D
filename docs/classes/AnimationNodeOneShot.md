# AnimationNodeOneShot

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeOneShot` · **Source:** [AnimationNodeOneShot.cs](../../src/Scene/Animation/AnimationNodeOneShot.cs).

**Inherits:** [AnimationNodeSync](AnimationNodeSync.md).

## Description

Plays a requested action over a base graph with fade, filters and automatic restart.

Inputs are in and shot. Fire starts the action at zero, or restarts it while retaining an active fade-in. Abort immediately cancels activity and pending restart. FadeOut sets InternalActive false while Active stays true through fade completion; repeating FadeOut does not reset an existing fade. AbortOnReset handles an internal seek to zero after interruption; an explicit Fire takes effect as a start. CurrentLength/Position/Delta follow the action while internally active, otherwise the base; the naturally detected fade's first frame still returns the action timing.

FadeInTime/FadeOutTime and borrowed unit curves define the envelopes. The base receives 1-weight with Blend filtering, or one with Ignore filtering in Add mode; shot uses Pass filtering. Unfiltered paths stay on the base in filtered Blend mode. Sync keeps silent base clocks advancing. The action always advances while firing/fading. Fade expiry and endpoint detection use absolute delta; fade-out estimates action timing scale from the parent/child delta ratio. External seek delta is previous position minus requested position, while internal seeks retain the supplied delta.

Autorestart begins only after Fire and schedules delay plus a uniform random interval upon completion. Abort cancels the timer while retaining Autorestart configuration. A timer at zero starts only after crossing below zero on a nonseek pass; negative timers are disabled. Random.Shared is prepared on the evaluation thread during cold graph schema preparation when random delay is configured, and reused for restart draws. Test-only passes do not draw restart randomness. Writable FadeInRemaining/FadeOutRemaining/TimeToRestart keys project the runtime timer schema; Active/InternalActive are readonly externally.

BreakLoopAtEnd uses the clip's predictive cycle-end flag propagated through graph timing, so a loop can finish before its position wraps. For a one-second loop advanced from zero by 0.75 seconds, the next-end prediction is true and breaking can finish the action on that update. A looping action without the flag remains active. The before-decay envelope for base 10/shot 30, one-second fade-in, advances 0/0.5/0.25/0.25/0.25 yields approximately 10.0003/10.0003/20/25/30. Explicit one-second fade-out sampled with the same before-decay timing yields 30/30/20/15 before the epsilon endpoint and inactive base.

Definitions and fade curves remain borrowed; duplication copies settings and input policies, with deep Resource graph copying preserving shared curve aliases. Parameters belong to each AnimationTree and graph path. Public static typed keys cover requests, timers and readonly diagnostics without a general value container. Scene/parameter mutation follows the Node owner thread; Resource authoring follows Resource. Shared definitions execute sequentially on independent tree state; concurrent evaluation of a shared definition is unsupported. Graph resources remain separate from scene Node.

Weights are computed from the remaining timer before decrementing it. The initial update can retain the outgoing pose; interrupted transitions use the prior selected input rather than capturing the displayed blended pose. The pinned 1e-5 contribution at zero-weight fade endpoints preserves edge-key activity and also adds a small scalar/vector contribution in deterministic mode. Unit Curve samples can be signed or outside one; the existing typed mixer handles them. Nonfinite duration/timer values reject, while finite nonpositive configured durations retain immediate/frozen behavior as documented. Missing connections, invalid keys/indices/requests and disposed curves/resources throw. Mutation commits before synchronous observer delivery; errors from authoring callbacks propagate.

Request consumption happens on real processing. Test-only passes preserve cells/requests and output, and do not draw random restart values. Child evaluation may advance before a later child fails; it is not a global state rollback. Controller timing/selection publication happens after successful child sampling; input requests may already be consumed. Reentrant definition/root/topology edits abandon stale property commits and diagnostics. Disposal releases references/observers without disposing caller-owned curves or connected definitions. No disk/packed/editor constructor-state format is supplied.

## Example

This snippet requires a live BlendTree with the indicated connected clip definitions and an AnimationTree/library/target root. [AnimationActionTests](../../tests/Electron2D.Tests/AnimationActionTests.cs) compiles the public workflow and renders their composition.

```csharp
using var action = new AnimationNodeOneShot { FadeInTime = 0.2, FadeOutTime = 0.2 };
graph.AddNode("action", action);
graph.ConnectNode("action", 0, "base");
graph.ConnectNode("action", 1, "attack");
graph.ConnectNode("output", 0, "action");
controller.SetParameter("action", AnimationNodeOneShot.Request, AnimationOneShotRequest.Fire);
controller.Advance(0);
bool active = controller.GetParameter("action", AnimationNodeOneShot.Active);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeOneShot()` | Creates the standard in and shot inputs. |

## Constructor Descriptions

<a id="member-5e9c3e3311aa"></a>
### .ctor

`public AnimationNodeOneShot()`

Creates the standard in and shot inputs.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean AbortOnReset { get; set; }` | Whether an internal reset aborts an already active action. |
| `public System.Boolean Autorestart { get; set; }` | Whether a completed fired action schedules another start. |
| `public System.Double AutorestartDelay { get; set; }` | Finite automatic-restart delay in seconds; negative resulting timers do not restart. |
| `public System.Double AutorestartRandomDelay { get; set; }` | Finite random additional delay; nonnegative values add a uniform interval from zero to this value. |
| `public System.Boolean BreakLoopAtEnd { get; set; }` | Whether the action may finish on the predicted end of a loop cycle. |
| `public Electron2D.Curve FadeInCurve { get; set; }` | Borrowed unit curve for fade-in; null is linear. |
| `public System.Double FadeInTime { get; set; }` | Finite fade-in duration in seconds; nonpositive values select immediate blending. |
| `public Electron2D.Curve FadeOutCurve { get; set; }` | Borrowed unit curve for fade-out; null is linear. |
| `public System.Double FadeOutTime { get; set; }` | Finite fade-out duration in seconds; nonpositive values select immediate exit. |
| `public Electron2D.AnimationMixMode MixMode { get; set; }` | Whether the action blends over or adds to the base graph. |

## Property Descriptions

<a id="member-63aefedc1851"></a>
### AbortOnReset

`public System.Boolean AbortOnReset { get; set; }`

Whether an internal reset aborts an already active action.

Value: The configured value, initially false.

System.ObjectDisposedException: The controller or assigned curve is disposed.

<a id="member-39aa7be1292c"></a>
### Autorestart

`public System.Boolean Autorestart { get; set; }`

Whether a completed fired action schedules another start.

Value: The configured value, initially false.

System.ObjectDisposedException: The controller or assigned curve is disposed.

<a id="member-fd11e9dcf678"></a>
### AutorestartDelay

`public System.Double AutorestartDelay { get; set; }`

Finite automatic-restart delay in seconds; negative resulting timers do not restart.

Value: The configured value, initially 1.

System.ObjectDisposedException: The controller or assigned curve is disposed.

System.ArgumentOutOfRangeException: The numeric or enum value is invalid.

<a id="member-6b0f0f56c7a5"></a>
### AutorestartRandomDelay

`public System.Double AutorestartRandomDelay { get; set; }`

Finite random additional delay; nonnegative values add a uniform interval from zero to this value.

Value: The configured value, initially 0.

System.ObjectDisposedException: The controller or assigned curve is disposed.

System.ArgumentOutOfRangeException: The numeric or enum value is invalid.

<a id="member-2cbb3fe3e1ce"></a>
### BreakLoopAtEnd

`public System.Boolean BreakLoopAtEnd { get; set; }`

Whether the action may finish on the predicted end of a loop cycle.

Value: The configured value, initially false.

System.ObjectDisposedException: The controller or assigned curve is disposed.

<a id="member-b2808ea733b5"></a>
### FadeInCurve

`public Electron2D.Curve FadeInCurve { get; set; }`

Borrowed unit curve for fade-in; null is linear.

Value: The configured value, initially null.

System.ObjectDisposedException: The controller or assigned curve is disposed.

<a id="member-3353fa89ad79"></a>
### FadeInTime

`public System.Double FadeInTime { get; set; }`

Finite fade-in duration in seconds; nonpositive values select immediate blending.

Value: The configured value, initially 0.

System.ObjectDisposedException: The controller or assigned curve is disposed.

System.ArgumentOutOfRangeException: The numeric or enum value is invalid.

<a id="member-a539099a0149"></a>
### FadeOutCurve

`public Electron2D.Curve FadeOutCurve { get; set; }`

Borrowed unit curve for fade-out; null is linear.

Value: The configured value, initially null.

System.ObjectDisposedException: The controller or assigned curve is disposed.

<a id="member-70a5944ebbe2"></a>
### FadeOutTime

`public System.Double FadeOutTime { get; set; }`

Finite fade-out duration in seconds; nonpositive values select immediate exit.

Value: The configured value, initially 0.

System.ObjectDisposedException: The controller or assigned curve is disposed.

System.ArgumentOutOfRangeException: The numeric or enum value is invalid.

<a id="member-262a108f23d7"></a>
### MixMode

`public Electron2D.AnimationMixMode MixMode { get; set; }`

Whether the action blends over or adds to the base graph.

Value: The configured value, initially AnimationMixMode.Blend.

System.ObjectDisposedException: The controller or assigned curve is disposed.

System.ArgumentOutOfRangeException: The numeric or enum value is invalid.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource lifecycle for this concrete type. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource lifecycle for this concrete type. |
| `protected override System.String OnGetCaption()` | Supplies the graph resource's caption. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()` | Supplies immutable typed parameter definitions. |
| `protected override System.Boolean OnHasFilter()` | Reports whether child blend helpers apply track filters. |
| `protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)` | Evaluates custom graph behavior; helpers can blend inputs, children and clips. |

## Method Descriptions

<a id="member-d9fbe79ceb84"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`

Copies derived stored state into a duplicate or copy target.

target: A live resource with the exact same runtime type.

deep: Whether typed collection containers should be cloned recursively.

subresourceMode: The nested-resource policy for this copy.

duplicateSubresource: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource. Pass every nested resource through this function when deep is true.

forceDuplicateSubresource: A graph-preserving function that duplicates a nested resource even when the current policy would share it. Use it for typed properties whose contract requires duplication; assign the original reference directly for properties whose contract forbids duplication.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Derived implementations must copy all stored custom state and call the base implementation only when they intentionally want its validation. Assigning the original nested-resource reference directly expresses a never-duplicate property.

System.NotSupportedException: A derived resource has not explicitly implemented custom-state copying.

<a id="member-6b6c4533ddfb"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-16e2ee47d0bc"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-9d537fcf269f"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

<a id="member-1a03ce72ebaf"></a>
### OnGetCaption

`protected override System.String OnGetCaption()`

Supplies the graph resource's caption.

Returns: The caption; defaults to Node.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-119645a1e7d5"></a>
### OnGetParameterList

`protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()`

Supplies immutable typed parameter definitions.

Returns: The node's parameter schema, including inherited timing slots.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-9a904f13cd35"></a>
### OnHasFilter

`protected override System.Boolean OnHasFilter()`

Reports whether child blend helpers apply track filters.

Returns: False for the base resource.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-158d0eb18aec"></a>
### OnProcess

`protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)`

Evaluates custom graph behavior; helpers can blend inputs, children and clips.

time: Relative delta seconds, or absolute time when seeking.

seek: Whether time is absolute.

isExternalSeeking: Whether the seek originated outside startup.

testOnly: Whether persistent changes and mixer writes are suppressed.

Returns: The selected remaining duration, or zero when no input is evaluated.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Field summary

| Complete C# signature | Contract |
| --- | --- |
| `public static readonly Electron2D.AnimationParameter<System.Boolean> Active` | Read-only activity, including the fade-out period. |
| `public static readonly Electron2D.AnimationParameter<System.Double> FadeInRemaining` | The writable remaining fade-in seconds, initially zero. |
| `public static readonly Electron2D.AnimationParameter<System.Double> FadeOutRemaining` | The writable remaining fade-out seconds, initially zero. |
| `public static readonly Electron2D.AnimationParameter<System.Boolean> InternalActive` | Read-only activity before fade-out; diagnostics select the action while this is true. |
| `public static readonly Electron2D.AnimationParameter<Electron2D.AnimationOneShotRequest> Request` | The writable request, consumed on the next real evaluation. |
| `public static readonly Electron2D.AnimationParameter<System.Double> TimeToRestart` | The writable automatic-restart timer; minus one disables a pending restart. |

## Field Descriptions

<a id="member-f109e19854c7"></a>
### Active

`public static readonly Electron2D.AnimationParameter<System.Boolean> Active`

Read-only activity, including the fade-out period.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-5d450207b118"></a>
### FadeInRemaining

`public static readonly Electron2D.AnimationParameter<System.Double> FadeInRemaining`

The writable remaining fade-in seconds, initially zero.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-de168b7e4c21"></a>
### FadeOutRemaining

`public static readonly Electron2D.AnimationParameter<System.Double> FadeOutRemaining`

The writable remaining fade-out seconds, initially zero.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-b8da7345af02"></a>
### InternalActive

`public static readonly Electron2D.AnimationParameter<System.Boolean> InternalActive`

Read-only activity before fade-out; diagnostics select the action while this is true.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-3c665a38c133"></a>
### Request

`public static readonly Electron2D.AnimationParameter<Electron2D.AnimationOneShotRequest> Request`

The writable request, consumed on the next real evaluation.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-51372d01d0be"></a>
### TimeToRestart

`public static readonly Electron2D.AnimationParameter<System.Double> TimeToRestart`

The writable automatic-restart timer; minus one disables a pending restart.

System.ObjectDisposedException: The resource has been disposed.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) and [action controllers](../components/scene-animation.md#action-controllers) define this executed contract. AnimationActionTests covers default/edit/copy/curve semantics, requests/timers/filtering/loops, named/interrupted/self/automatic transitions, shared instances, external/internal seeks, test-only behavior, callback failure/reentry and 256 warmed restarted/interrupted frame passes with deferred events and zero managed bytes. Two Wayland GPU and two compatibility public Engine.Run cycles each verify thirteen composed poses and borrowed-resource cleanup. Cold schema/binding/input/notice-capacity preparation may allocate; manual advances without a flush can grow deferred capacity. Native/driver allocations, other platforms and human visual acceptance remain unmeasured. State-machine/grouped/expression control, event-track schedulers and disk/editor round trips remain separate coverage dependencies.
