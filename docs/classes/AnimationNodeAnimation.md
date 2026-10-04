# AnimationNodeAnimation

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeAnimation` · **Source:** [AnimationNodeAnimation.cs](../../src/Scene/Animation/AnimationNodeAnimation.cs).

**Inherits:** [AnimationRootNode](AnimationRootNode.md).

## Description

A graph leaf that samples a named clip through a per-tree timeline.

A qualified clip name selects the inherited tree library resource. Forward/backward sampling uses the clip start/end. Nonlooping timelines clamp; Linear wraps; PingPong folds and retains direction per tree. Startup may advance by the incoming delta. Custom length starts at one second and must be positive and finite; offset may be signed. StretchTimeScale multiplies offset timeline time by clip length / timeline length. A nonlooped, unstretched custom timeline trims the track section; looped/stretched evaluation uses the whole clip. Timing cells report the node timeline before the clip sampling scale/reversal. The pinned endpoint delta and single-crossing PingPong direction policy are preserved; very large steps need not act like subdivided updates. Mutable clip changes rebuild bindings without changing the definition.

Graph resources borrow child resources; the blend tree owns only its reserved output. Resource duplication copies definition containers, with shallow/deep resource aliases governed by Resource. Evaluation state and typed parameter cells belong to each AnimationTree and graph path. A definition may be shared sequentially across trees; concurrent processing of the same definition is unsupported. Scene controllers and parameter writes obey the inherited Node owner-thread rule. Resource edits invalidate prepared graph/binding state; cold preparation may allocate. No graph factory, disk format, editor UI or expression evaluator is supplied.

Input/child/parameter names use ordinal comparison. Missing names, invalid input indices, wrong typed keys, duplicate schemas and disconnected required inputs fail explicitly. Disposed resources throw ObjectDisposedException; processing helpers outside evaluation throw InvalidOperationException. Nonfinite times/weights/authoring positions reject. Graph cycles reject before connection/self-containment edits or during preparation for custom graphs. Mutation commits before observer notification; TreeChanged reaches every subscribed tree even when another handler throws, collecting graph notification failures. Authoring callback exceptions propagate; the caller can repair the committed graph. Disposal detaches observers and borrows child resources. Nested graph processing guards connection cycles; inherited mixer Advance rejects recursion and invalidates stale property commits after reentrant edits.

## Example

The complete public authoring pattern below requires existing live clips and a scene root. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) compiles it and the per-node variants.

```csharp
// Existing live clips a and b have typed property tracks targeting the same root.
using var library = new AnimationLibrary();
library.AddAnimation("a", a); library.AddAnimation("b", b);
using var graph = new AnimationNodeBlendTree();
using var first = new AnimationNodeAnimation { Animation = "a" };
using var second = new AnimationNodeAnimation { Animation = "b" };
using var blend = new AnimationNodeBlend2();
graph.AddNode("a", first); graph.AddNode("b", second); graph.AddNode("mix", blend);
graph.ConnectNode("mix", 0, "a"); graph.ConnectNode("mix", 1, "b");
graph.ConnectNode("output", 0, "mix");
var controller = new AnimationTree { TreeRoot = graph };
controller.AddAnimationLibrary("", library);
sceneRoot.AddChild(controller); // Existing owner-thread scene root/targets.
controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, 0.5);
controller.Advance(0);
```

This member-use snippet assumes the indicated node is installed at the matching controller path and all required inputs are connected.

```csharp
using var node = new AnimationNodeAnimation { Animation = "a", UseCustomTimeline = true, TimelineLength = 2, StretchTimeScale = true };
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeAnimation()` | Creates a new independent instance with the defaults described above. |

## Constructor Descriptions

<a id="member-ae62c404b902"></a>
### .ctor

`public AnimationNodeAnimation()`

Creates a new independent instance with the defaults described above.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean AdvanceOnStart { get; set; }` | Gets or sets whether startup consumes the first supplied delta; defaults to false. |
| `public System.String Animation { get; set; }` | Gets or sets the qualified animation name. |
| `public Electron2D.SpriteFrames.LoopMode LoopMode { get; set; }` | Gets or sets the custom timeline endpoint loop mode. |
| `public Electron2D.AnimationPlayMode PlayMode { get; set; }` | Gets or sets forward/reversed clip sampling. |
| `public System.Double StartOffset { get; set; }` | Gets or sets a finite custom start offset in seconds. |
| `public System.Boolean StretchTimeScale { get; set; }` | Gets or sets whether the custom timeline scales the entire clip period. |
| `public System.Double TimelineLength { get; set; }` | Gets or sets the positive finite custom length in seconds, initially one. |
| `public System.Boolean UseCustomTimeline { get; set; }` | Gets or sets whether this resource overrides the clip timeline. |

## Property Descriptions

<a id="member-fcd1564d919c"></a>
### AdvanceOnStart

`public System.Boolean AdvanceOnStart { get; set; }`

Gets or sets whether startup consumes the first supplied delta; defaults to false.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-dbf7683defe3"></a>
### Animation

`public System.String Animation { get; set; }`

Gets or sets the qualified animation name.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-7121c33bf35b"></a>
### LoopMode

`public Electron2D.SpriteFrames.LoopMode LoopMode { get; set; }`

Gets or sets the custom timeline endpoint loop mode.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-f01c21defc5d"></a>
### PlayMode

`public Electron2D.AnimationPlayMode PlayMode { get; set; }`

Gets or sets forward/reversed clip sampling.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-2ce2626267b6"></a>
### StartOffset

`public System.Double StartOffset { get; set; }`

Gets or sets a finite custom start offset in seconds.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.ArgumentOutOfRangeException: A numeric value is nonfinite or outside the stated domain.

<a id="member-9f4eebdbf79d"></a>
### StretchTimeScale

`public System.Boolean StretchTimeScale { get; set; }`

Gets or sets whether the custom timeline scales the entire clip period.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-1dc468aeea95"></a>
### TimelineLength

`public System.Double TimelineLength { get; set; }`

Gets or sets the positive finite custom length in seconds, initially one.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.ArgumentOutOfRangeException: A numeric value is nonfinite or outside the stated domain.

<a id="member-76de5006f61c"></a>
### UseCustomTimeline

`public System.Boolean UseCustomTimeline { get; set; }`

Gets or sets whether this resource overrides the clip timeline.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `protected override System.String OnGetCaption()` | Supplies the graph resource's caption. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()` | Supplies immutable typed parameter definitions. |
| `protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)` | Evaluates custom graph behavior; helpers can blend inputs, children and clips. |

## Method Descriptions

<a id="member-ccc90c0c8977"></a>
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

<a id="member-4a08af388588"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-5ba9eb177115"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

<a id="member-1d54c412ad9b"></a>
### OnGetCaption

`protected override System.String OnGetCaption()`

Supplies the graph resource's caption.

Returns: The caption; defaults to Node.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-50b570a683a9"></a>
### OnGetParameterList

`protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()`

Supplies immutable typed parameter definitions.

Returns: The node's parameter schema, including inherited timing slots.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-1f3092dd8185"></a>
### OnProcess

`protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)`

Evaluates custom graph behavior; helpers can blend inputs, children and clips.

time: Relative delta seconds, or absolute time when seeking.

seek: Whether time is absolute.

isExternalSeeking: Whether the seek originated outside startup.

testOnly: Whether persistent changes and mixer writes are suppressed.

Returns: The selected remaining duration, or zero when no input is evaluated.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines, transitions, OneShot, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
