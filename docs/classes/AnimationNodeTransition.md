# AnimationNodeTransition

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeTransition` · **Source:** [AnimationNodeTransition.cs](../../src/Scene/Animation/AnimationNodeTransition.cs).

**Inherits:** [AnimationNodeSync](AnimationNodeSync.md).

## Description

Switches named graph inputs with per-tree requests, crossfades and automatic advancement.

InputCount starts at zero and growing adds state_N captions with Reset=true, AutoAdvance=false and BreakLoop=false. AddInput/RemoveInput/SetInputName are virtual at AnimationNode and dispatch through base references, keeping policy storage aligned before committed edit notifications. InputCount rejects negative counts. Selection follows input-policy identity through rename/removal of earlier ports; each tree reconciles its own CurrentState/CurrentIndex every evaluation, avoiding a shared global update latch. Captions use ordinal lookup; duplicate/empty inherited captions retain first-match/no-request behavior.

TransitionRequest selects an exact caption and is consumed. CurrentState/CurrentIndex switch immediately when the fade begins. Self requests do nothing unless AllowTransitionToSelf; then a Reset input restarts at zero and clears the outgoing fade, while Reset=false only clears it. Internal seek-to-zero clears remaining fade. A switched destination resets only when its input policy requests it and the parent is not already seeking. Sync advances other connected inputs with zero weight. Interrupted fades retain only the prior selected input.

XFadeTime and borrowed XFadeCurve control outgoing weight; the destination uses its complement. Weights sample before the absolute parent delta decrements PreviousXFading. Expired previous input clears on a subsequent nonseek evaluation. Automatic advancement is queued for the next pass when selected remaining time reaches XFadeTime; the last input wraps to the first. Looping inputs need BreakLoopAtEnd for finite predictive completion. Writable PreviousIndex and PreviousXFading project the source runtime schema; externally readonly CurrentState/CurrentIndex remain controller-owned. Invalid name requests are consumed then throw, allowing a subsequent repaired frame.

For input values 10/30 and a one-second transition to the second, updates 0.5/0.25/0.25/0/0 produce approximately 10.0003/20/25/30.0001/30. Requesting 70 while the selected input is 10 makes 10 the outgoing source, even if the displayed pose was still 15 from an earlier interrupted blend. An empty transition contributes no clip, allowing the deterministic mixer rest/RESET profile.

Definitions and fade curves remain borrowed; duplication copies settings and input policies, with deep Resource graph copying preserving shared curve aliases. Parameters belong to each AnimationTree and graph path. Public static typed keys cover requests, timers and readonly diagnostics without a general value container. Scene/parameter mutation follows the Node owner thread; Resource authoring follows Resource. Shared definitions execute sequentially on independent tree state; concurrent evaluation of a shared definition is unsupported. Graph resources remain separate from scene Node.

Weights are computed from the remaining timer before decrementing it. The initial update can retain the outgoing pose; interrupted transitions use the prior selected input rather than capturing the displayed blended pose. The pinned 1e-5 contribution at zero-weight fade endpoints preserves edge-key activity and also adds a small scalar/vector contribution in deterministic mode. Unit Curve samples can be signed or outside one; the existing typed mixer handles them. Nonfinite duration/timer values reject, while finite nonpositive configured durations retain immediate/frozen behavior as documented. Missing connections, invalid keys/indices/requests and disposed curves/resources throw. Mutation commits before synchronous observer delivery; errors from authoring callbacks propagate.

Request consumption happens on real processing. Test-only passes preserve cells/requests and output, and do not draw random restart values. Child evaluation may advance before a later child fails; it is not a global state rollback. Controller timing/selection publication happens after successful child sampling; input requests may already be consumed. Reentrant definition/root/topology edits abandon stale property commits and diagnostics. Disposal releases references/observers without disposing caller-owned curves or connected definitions. No disk/packed/editor constructor-state format is supplied.

## Example

This snippet requires a live BlendTree with the indicated connected clip definitions and an AnimationTree/library/target root. [AnimationActionTests](../../tests/Electron2D.Tests/AnimationActionTests.cs) compiles the public workflow and renders their composition.

```csharp
using var transition = new AnimationNodeTransition { InputCount = 2, XFadeTime = 0.3 };
transition.SetInputName(0, "idle"); transition.SetInputName(1, "run");
graph.AddNode("transition", transition);
graph.ConnectNode("transition", 0, "idle_clip");
graph.ConnectNode("transition", 1, "run_clip");
graph.ConnectNode("output", 0, "transition");
controller.SetParameter("transition", AnimationNodeTransition.TransitionRequest, "run");
controller.Advance(0);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeTransition()` | Creates an empty named transition with default input policies. |

## Constructor Descriptions

<a id="member-9245a5e4cb8c"></a>
### .ctor

`public AnimationNodeTransition()`

Creates an empty named transition with default input policies.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean AllowTransitionToSelf { get; set; }` | Gets or sets whether requesting the selected input may reset it and clear its existing fade. |
| `public System.Int32 InputCount { get; set; }` | Gets or sets the input count; growing creates state_N captions and default policies. |
| `public Electron2D.Curve XFadeCurve { get; set; }` | Gets or sets the borrowed unit curve sampling the outgoing fade weight; null is linear. |
| `public System.Double XFadeTime { get; set; }` | Gets or sets the finite crossfade duration in seconds; nonpositive values switch immediately. |

## Property Descriptions

<a id="member-601a977c55f8"></a>
### AllowTransitionToSelf

`public System.Boolean AllowTransitionToSelf { get; set; }`

Gets or sets whether requesting the selected input may reset it and clear its existing fade.

Value: The permission, initially false.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-f4f2537a7216"></a>
### InputCount

`public System.Int32 InputCount { get; set; }`

Gets or sets the input count; growing creates state_N captions and default policies.

Value: The nonnegative input count, initially zero.

System.ArgumentOutOfRangeException: The count is negative.

System.ObjectDisposedException: The resource is disposed.

<a id="member-e8ebf7b3c972"></a>
### XFadeCurve

`public Electron2D.Curve XFadeCurve { get; set; }`

Gets or sets the borrowed unit curve sampling the outgoing fade weight; null is linear.

Value: The borrowed curve, initially null.

System.ObjectDisposedException: This resource or the assigned curve is disposed.

<a id="member-fff2830c5a24"></a>
### XFadeTime

`public System.Double XFadeTime { get; set; }`

Gets or sets the finite crossfade duration in seconds; nonpositive values switch immediately.

Value: The duration, initially zero.

System.ArgumentOutOfRangeException: The duration is nonfinite.

System.ObjectDisposedException: The resource has been disposed.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public override System.Boolean AddInput(System.String name)` | Adds an input; root resources and names with slash/dot are rejected. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource lifecycle for this concrete type. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource lifecycle for this concrete type. |
| `public System.Boolean IsInputLoopBrokenAtEnd(System.Int32 input)` | Tests the loop-break policy. |
| `public System.Boolean IsInputReset(System.Int32 input)` | Tests the reset policy. |
| `public System.Boolean IsInputSetAsAutoAdvance(System.Int32 input)` | Tests the automatic-advance policy. |
| `protected override System.String OnGetCaption()` | Supplies the graph resource's caption. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()` | Supplies immutable typed parameter definitions. |
| `protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)` | Evaluates custom graph behavior; helpers can blend inputs, children and clips. |
| `public override System.Void RemoveInput(System.Int32 index)` | Removes an existing input. |
| `public System.Void SetInputAsAutoAdvance(System.Int32 input, System.Boolean enable)` | Sets whether a completed input requests the following input, wrapping at the end. |
| `public System.Void SetInputBreakLoopAtEnd(System.Int32 input, System.Boolean enable)` | Sets whether automatic advancement uses the predicted end of a looping cycle. |
| `public override System.Boolean SetInputName(System.Int32 input, System.String name)` | Renames an input, rejecting slash/dot captions. |
| `public System.Void SetInputReset(System.Int32 input, System.Boolean enable)` | Sets whether a destination starts at zero on a requested switch or allowed self transition. |

## Method Descriptions

<a id="member-858dee0d0704"></a>
### AddInput

`public override System.Boolean AddInput(System.String name)`

Adds an input; root resources and names with slash/dot are rejected.

name: The non-null input caption; duplicates and empty captions are accepted.

Returns: Whether the input was added.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

Remarks: Overrides preserve associated input policy storage before delivering committed edit notifications.

<a id="member-180f320eb786"></a>
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

<a id="member-2f8a0913ffb6"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-a6ee0a261887"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-7d299df721cb"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

<a id="member-adca0cca11c8"></a>
### IsInputLoopBrokenAtEnd

`public System.Boolean IsInputLoopBrokenAtEnd(System.Int32 input)`

Tests the loop-break policy.

input: An existing input index.

Returns: Whether looping timelines may automatically advance.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The input index is outside the current port list.

<a id="member-d0f2acacf7f3"></a>
### IsInputReset

`public System.Boolean IsInputReset(System.Int32 input)`

Tests the reset policy.

input: An existing input index.

Returns: Whether requested activation resets this input.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The input index is outside the current port list.

<a id="member-fed0f4f91c23"></a>
### IsInputSetAsAutoAdvance

`public System.Boolean IsInputSetAsAutoAdvance(System.Int32 input)`

Tests the automatic-advance policy.

input: An existing input index.

Returns: Whether it requests the next input near its endpoint.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The input index is outside the current port list.

<a id="member-54245ae49bfc"></a>
### OnGetCaption

`protected override System.String OnGetCaption()`

Supplies the graph resource's caption.

Returns: The caption; defaults to Node.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-2dbbeacc4a26"></a>
### OnGetParameterList

`protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()`

Supplies immutable typed parameter definitions.

Returns: The node's parameter schema, including inherited timing slots.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-650902455136"></a>
### OnProcess

`protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)`

Evaluates custom graph behavior; helpers can blend inputs, children and clips.

time: Relative delta seconds, or absolute time when seeking.

seek: Whether time is absolute.

isExternalSeeking: Whether the seek originated outside startup.

testOnly: Whether persistent changes and mixer writes are suppressed.

Returns: The selected remaining duration, or zero when no input is evaluated.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-6ca66289eddd"></a>
### RemoveInput

`public override System.Void RemoveInput(System.Int32 index)`

Removes an existing input.

index: The zero-based input index.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

Remarks: Overrides remove associated policy storage before invoking this committed input removal.

<a id="member-fa5c999cb4d7"></a>
### SetInputAsAutoAdvance

`public System.Void SetInputAsAutoAdvance(System.Int32 input, System.Boolean enable)`

Sets whether a completed input requests the following input, wrapping at the end.

input: An existing input index.

enable: The automatic-advance policy.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The input index is outside the current port list.

<a id="member-bcc3efd7ec0e"></a>
### SetInputBreakLoopAtEnd

`public System.Void SetInputBreakLoopAtEnd(System.Int32 input, System.Boolean enable)`

Sets whether automatic advancement uses the predicted end of a looping cycle.

input: An existing input index.

enable: The loop-break policy.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The input index is outside the current port list.

<a id="member-00de1edd80cc"></a>
### SetInputName

`public override System.Boolean SetInputName(System.Int32 input, System.String name)`

Renames an input, rejecting slash/dot captions.

input: The existing zero-based input index.

name: The non-null caption.

Returns: Whether the caption was accepted.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-591caede414b"></a>
### SetInputReset

`public System.Void SetInputReset(System.Int32 input, System.Boolean enable)`

Sets whether a destination starts at zero on a requested switch or allowed self transition.

input: An existing input index.

enable: The reset policy, initially true.

System.ObjectDisposedException: The resource has been disposed.

System.ArgumentOutOfRangeException: The input index is outside the current port list.

## Field summary

| Complete C# signature | Contract |
| --- | --- |
| `public static readonly Electron2D.AnimationParameter<System.Int32> CurrentIndex` | The read-only selected input index, initially minus one. |
| `public static readonly Electron2D.AnimationParameter<System.String> CurrentState` | The read-only selected input name. |
| `public static readonly Electron2D.AnimationParameter<System.Int32> PreviousIndex` | The writable outgoing input index; minus one disables an outgoing clip. |
| `public static readonly Electron2D.AnimationParameter<System.Double> PreviousXFading` | The writable outgoing fade time remaining in seconds. |
| `public static readonly Electron2D.AnimationParameter<System.String> TransitionRequest` | The writable exact input-name request; empty means no request and real processing consumes it. |

## Field Descriptions

<a id="member-0c1c4769ce82"></a>
### CurrentIndex

`public static readonly Electron2D.AnimationParameter<System.Int32> CurrentIndex`

The read-only selected input index, initially minus one.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-72a84a4fcc8d"></a>
### CurrentState

`public static readonly Electron2D.AnimationParameter<System.String> CurrentState`

The read-only selected input name.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-55e5542f1ded"></a>
### PreviousIndex

`public static readonly Electron2D.AnimationParameter<System.Int32> PreviousIndex`

The writable outgoing input index; minus one disables an outgoing clip.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-e7dfd0fcc768"></a>
### PreviousXFading

`public static readonly Electron2D.AnimationParameter<System.Double> PreviousXFading`

The writable outgoing fade time remaining in seconds.

System.ObjectDisposedException: The resource has been disposed.

<a id="member-16c3a726d224"></a>
### TransitionRequest

`public static readonly Electron2D.AnimationParameter<System.String> TransitionRequest`

The writable exact input-name request; empty means no request and real processing consumes it.

System.ObjectDisposedException: The resource has been disposed.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) and [action controllers](../components/scene-animation.md#action-controllers) define this executed contract. AnimationActionTests covers default/edit/copy/curve semantics, requests/timers/filtering/loops, named/interrupted/self/automatic transitions, shared instances, external/internal seeks, test-only behavior, callback failure/reentry and 256 warmed restarted/interrupted frame passes with deferred events and zero managed bytes. Two Wayland GPU and two compatibility public Engine.Run cycles each verify thirteen composed poses and borrowed-resource cleanup. Cold schema/binding/input/notice-capacity preparation may allocate; manual advances without a flush can grow deferred capacity. Native/driver allocations, other platforms and human visual acceptance remain unmeasured. State-machine/grouped/expression control, event-track schedulers and disk/editor round trips remain separate coverage dependencies.
