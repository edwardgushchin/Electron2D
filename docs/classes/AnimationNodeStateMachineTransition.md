# AnimationNodeStateMachineTransition

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeStateMachineTransition` · **Source:** [AnimationNodeStateMachineTransition.cs](../../src/Scene/Animation/AnimationNodeStateMachineTransition.cs).

**Inherits:** [Resource](Resource.md).

## Description

A borrowed state-machine edge policy with typed conditions and optional unit fade curve.

Defaults are Immediate, Enabled, null condition/predicate/curve, zero fade, BreakLoopAtEnd=false, Reset=true and Priority=1. AdvanceCondition is an exact borrowed AnimationParameter<bool>; each machine declares its key once and conflicting names fail graph preparation. AdvanceExpression is an executable Func<AnimationTree,bool>; it may capture a typed receiver and does not parse strings or perform reflection. Auto requires both conditions; Enabled participates only in explicit travel and Disabled is excluded. Smaller automatic priority wins, with later authored edges winning ties. Priority is nonnegative to preserve route costs; zero is accepted. AtEnd waits until the source has at most XFadeTime remaining; looping sources require BreakLoopAtEnd. Sync transfers the source position, then continues the destination clock. Reset controls destination restart. Nonnegative finite duration and defined enums are validated. Fade position grows before sampling the destination weight through a borrowed unit Curve; signed/overshooting finite curve values remain valid. Contribution endpoints retain 1e-5 for edge keys. Change commits before notifications and attached graphs are invalidated even after Changed handler failure; AdvanceConditionChanged also fires for equal assignments. Copies preserve typed key/predicate identity and use ordinary shallow/deep Curve Resource aliases. Disposal detaches references and handlers without disposing borrowed inputs.

See [state machines](../components/scene-animation.md#state-machines) for runtime order, integration and verification boundaries. Scene controllers execute on their SceneTree owner thread; resource authoring retains the Resource thread/lifetime contract. No external backend dependency or second scene clock is introduced.

## Example

This snippet requires the indicated live tree, borrowed clip definitions and library/target setup. AnimationStateMachineTests compiles and exercises this public workflow.

```csharp
var ready = new AnimationParameter<bool>("ready", false);
using var transition = new AnimationNodeStateMachineTransition
{
    AdvanceMode = AnimationAdvanceMode.Auto,
    AdvanceCondition = ready,
    AdvanceExpression = currentTree => currentTree.GetParameter("", ready),
    SwitchMode = AnimationSwitchMode.AtEnd,
    XFadeTime = 0.2,
};
machine.AddTransition("idle", "run", transition);
tree.SetParameter("", ready, true);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeStateMachineTransition()` | Creates an enabled immediate edge with zero fade, reset enabled and priority one. |

## Constructor Descriptions

<a id="member-9553dbb99f88"></a>
### .ctor

`public AnimationNodeStateMachineTransition()`

Creates an enabled immediate edge with zero fade, reset enabled and priority one.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.AnimationParameter<System.Boolean> AdvanceCondition { get; set; }` | Gets or sets the borrowed typed flag; null imposes no flag condition. |
| `public System.Func<Electron2D.AnimationTree, System.Boolean> AdvanceExpression { get; set; }` | Gets or sets the executable predicate; null imposes no expression condition. |
| `public Electron2D.AnimationAdvanceMode AdvanceMode { get; set; }` | Selects disabled, travel-enabled or automatic advancement. |
| `public System.Boolean BreakLoopAtEnd { get; set; }` | Gets or sets whether AtEnd may leave a looping clip at its predicted cycle endpoint. |
| `public System.Int32 Priority { get; set; }` | Gets or sets the nonnegative route-cost multiplier and automatic priority; smaller wins. |
| `public System.Boolean Reset { get; set; }` | Gets or sets whether the destination timeline restarts when the edge activates. |
| `public Electron2D.AnimationSwitchMode SwitchMode { get; set; }` | Selects immediate, synchronized or end-of-cycle switching. |
| `public Electron2D.Curve XFadeCurve { get; set; }` | Gets or sets the borrowed curve sampling the destination fade weight; null is linear. |
| `public System.Double XFadeTime { get; set; }` | Gets or sets the nonnegative finite crossfade duration in seconds. |

## Property Descriptions

<a id="member-b456acfb4c00"></a>
### AdvanceCondition

`public Electron2D.AnimationParameter<System.Boolean> AdvanceCondition { get; set; }`

Gets or sets the borrowed typed flag; null imposes no flag condition.

Value: Initially null.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

<a id="member-fbc73dcc08c6"></a>
### AdvanceExpression

`public System.Func<Electron2D.AnimationTree, System.Boolean> AdvanceExpression { get; set; }`

Gets or sets the executable predicate; null imposes no expression condition.

Value: Initially null.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

<a id="member-5eeb7d9eb975"></a>
### AdvanceMode

`public Electron2D.AnimationAdvanceMode AdvanceMode { get; set; }`

Selects disabled, travel-enabled or automatic advancement.

Value: Initially AnimationAdvanceMode.Enabled.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

System.ArgumentOutOfRangeException: The value is nonfinite, negative or an undefined enumeration.

<a id="member-3bbecb981896"></a>
### BreakLoopAtEnd

`public System.Boolean BreakLoopAtEnd { get; set; }`

Gets or sets whether AtEnd may leave a looping clip at its predicted cycle endpoint.

Value: Initially false.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

<a id="member-5d6178f69186"></a>
### Priority

`public System.Int32 Priority { get; set; }`

Gets or sets the nonnegative route-cost multiplier and automatic priority; smaller wins.

Value: Initially 1.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

System.ArgumentOutOfRangeException: The value is nonfinite, negative or an undefined enumeration.

<a id="member-a20082a83df1"></a>
### Reset

`public System.Boolean Reset { get; set; }`

Gets or sets whether the destination timeline restarts when the edge activates.

Value: Initially true.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

<a id="member-67f885f12572"></a>
### SwitchMode

`public Electron2D.AnimationSwitchMode SwitchMode { get; set; }`

Selects immediate, synchronized or end-of-cycle switching.

Value: Initially AnimationSwitchMode.Immediate.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

System.ArgumentOutOfRangeException: The value is nonfinite, negative or an undefined enumeration.

<a id="member-c2075de0cb86"></a>
### XFadeCurve

`public Electron2D.Curve XFadeCurve { get; set; }`

Gets or sets the borrowed curve sampling the destination fade weight; null is linear.

Value: Initially null.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

<a id="member-8e0650dd38cd"></a>
### XFadeTime

`public System.Double XFadeTime { get; set; }`

Gets or sets the nonnegative finite crossfade duration in seconds.

Value: Initially 0.

System.ObjectDisposedException: This resource or a supplied curve is disposed.

System.ArgumentOutOfRangeException: The value is nonfinite, negative or an undefined enumeration.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource lifecycle for this concrete type. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource lifecycle for this concrete type. |

## Method Descriptions

<a id="member-087a87d7ab32"></a>
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

<a id="member-07e66f8f3c2f"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-6049f03786cb"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-08ec4d0c3d30"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action AdvanceConditionChanged` | Occurs after assigning the typed condition key, including an equal assignment. |

## Event Descriptions

<a id="member-a58f2ad0fa18"></a>
### AdvanceConditionChanged

`public event System.Action AdvanceConditionChanged`

Occurs after assigning the typed condition key, including an equal assignment.


## Verification and dependencies

[AnimationStateMachineTests](../../tests/Electron2D.Tests/AnimationStateMachineTests.cs) covers public authoring, defaults, edge guards, copies/aliases, route costs/fallback, timing/fades/boundaries, typed conditions, multiple groups, state events, independent sharing, rename/removal, preparation and observer failures, reentry, tested output and method/nested effects. Warm checks use 64 prepared command/route cycles, 64 cached grouped starts and 64 idle frames with zero owner managed bytes. Separate SDL-dummy/FAudio PCM checks verify state audio start/restart/stop and departed nonblended cues. Linux Wayland GPU/compatibility hosts each run twice and check six actual pixel poses and borrowed-resource cleanup. Native/driver allocator totals, physical listening, other platforms, file/editor and human acceptance remain unverified. [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed graph/condition/ownership contract.
