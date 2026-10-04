# AnimationNode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public class Electron2D.AnimationNode` · **Source:** [AnimationNode.cs](../../src/Scene/Animation/AnimationNode.cs).

**Inherits:** [Resource](Resource.md).

**Inherited By:** [AnimationRootNode](AnimationRootNode.md), [AnimationNodeSync](AnimationNodeSync.md), [AnimationNodeOutput](AnimationNodeOutput.md), [AnimationNodeTimeScale](AnimationNodeTimeScale.md), [AnimationNodeTimeSeek](AnimationNodeTimeSeek.md).

## Enumeration summary

[FilterAction](AnimationNode.FilterAction.md) controls track-path weighting in child blend helpers.

## Description

A reusable animation-graph resource with typed parameters, inputs and track filters.

Protected hooks supply caption, ordered children, typed parameter definitions/defaults/read-only policy, filter support and processing. OnProcess may call BlendInput, BlendNode or BlendAnimation only during evaluation. Blend helpers return remaining seconds for the selected timeline; looped timelines use a large finite remaining duration. GetParameter/SetParameter access the current instance, never the resource definition. Test-only evaluation suppresses persistent cell changes, leaf signals and mixer output. Child filters use exact full track/property paths. BlendAnimation currently schedules property tracks; external seek/loop endpoint flags have no additional property effects and their event-track effects remain a separate scheduler dependency.

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

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNode()` | Creates a new independent instance with the defaults described above. |

## Constructor Descriptions

<a id="member-49e9777041dd"></a>
### .ctor

`public AnimationNode()`

Creates a new independent instance with the defaults described above.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean FilterEnabled { get; set; }` | Gets or sets whether supported child blends use this node's path filter. |

## Property Descriptions

<a id="member-337ffc5186c0"></a>
### FilterEnabled

`public System.Boolean FilterEnabled { get; set; }`

Gets or sets whether supported child blends use this node's path filter.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public virtual System.Boolean AddInput(System.String name)` | Adds an input; root resources and names with slash/dot are rejected. |
| `public System.Void BlendAnimation(System.String animation, System.Double time, System.Double delta, System.Boolean seeked, System.Boolean isExternalSeeking, System.Double blend, Electron2D.Animation.LoopedFlag loopedFlag = None)` | Adds a named clip to the active tree's typed property mixer. |
| `public System.Double BlendInput(System.Int32 inputIndex, System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Double blend, Electron2D.AnimationNode.FilterAction filter = Ignore, System.Boolean sync = true, System.Boolean testOnly = false)` | Processes an input of the current blend-tree instance. |
| `public System.Double BlendNode(System.String name, Electron2D.AnimationNode node, System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Double blend, Electron2D.AnimationNode.FilterAction filter = Ignore, System.Boolean sync = true, System.Boolean testOnly = false)` | Processes a named child resource of the active graph instance. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public System.Int32 FindInput(System.String name)` | Returns the first input matching an ordinal caption, or minus one. |
| `public System.String GetCaption()` | Returns the display caption of this resource. |
| `public System.Int32 GetInputCount()` | Returns the number of inputs. |
| `public System.String GetInputName(System.Int32 input)` | Returns an input caption. |
| `public TValue GetParameter<TValue>(AnimationParameter<TValue> parameter)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public System.UInt64 GetProcessingAnimationTreeInstanceID()` | Returns the tree instance ID while processing, or zero outside processing. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public System.Boolean IsPathFiltered(System.String path)` | Tests exact filter path membership. |
| `public System.Boolean IsProcessTesting()` | Tests whether the active evaluation suppresses writes and playback state changes. |
| `protected virtual System.String OnGetCaption()` | Supplies the graph resource's caption. |
| `protected virtual Electron2D.AnimationNode OnGetChildByName(System.String name)` | Finds a named child definition. |
| `protected virtual System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, Electron2D.AnimationNode>> OnGetChildNodes()` | Returns ordered named child definitions; no scene nodes are created. |
| `protected virtual TValue OnGetParameterDefaultValue<TValue>(AnimationParameter<TValue> parameter)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `protected virtual System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()` | Supplies immutable typed parameter definitions. |
| `protected virtual System.Boolean OnHasFilter()` | Reports whether child blend helpers apply track filters. |
| `protected virtual System.Boolean OnIsParameterReadOnly(Electron2D.AnimationParameter parameter)` | Reports external write protection for a parameter. |
| `protected virtual System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)` | Evaluates custom graph behavior; helpers can blend inputs, children and clips. |
| `public virtual System.Void RemoveInput(System.Int32 index)` | Removes an existing input. |
| `public System.Void SetFilterPath(System.String path, System.Boolean enable)` | Enables or removes a path from the filter. |
| `public virtual System.Boolean SetInputName(System.Int32 input, System.String name)` | Renames an input, rejecting slash/dot captions. |
| `public System.Void SetParameter<TValue>(AnimationParameter<TValue> parameter, TValue value)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |

## Method Descriptions

<a id="member-4bc4c80504e6"></a>
### AddInput

`public virtual System.Boolean AddInput(System.String name)`

Adds an input; root resources and names with slash/dot are rejected.

name: The non-null input caption; duplicates and empty captions are accepted.

Returns: Whether the input was added.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-9fe00e92cc83"></a>
### BlendAnimation

`public System.Void BlendAnimation(System.String animation, System.Double time, System.Double delta, System.Boolean seeked, System.Boolean isExternalSeeking, System.Double blend, Electron2D.Animation.LoopedFlag loopedFlag = None)`

Adds a named clip to the active tree's typed property mixer.

animation: The exact qualified clip name.

time: The clip position in seconds.

delta: The signed clip delta in seconds.

seeked: Whether the position was explicitly sought.

isExternalSeeking: Whether the seek is external.

blend: The finite signed contribution.

loopedFlag: The crossed endpoint flag.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.InvalidOperationException: Processing context is absent, an input is disconnected, or an external write targets a read-only slot.

<a id="member-d0d133934f7d"></a>
### BlendInput

`public System.Double BlendInput(System.Int32 inputIndex, System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Double blend, Electron2D.AnimationNode.FilterAction filter = Ignore, System.Boolean sync = true, System.Boolean testOnly = false)`

Processes an input of the current blend-tree instance.

inputIndex: The existing input index.

time: Relative seconds, or an absolute seek position.

seek: Whether time is absolute.

isExternalSeeking: Whether seeking is external to graph startup.

blend: The finite signed contribution.

filter: The child path filtering action.

sync: Whether zero-weight branches advance.

testOnly: Whether to suppress persistent changes.

Returns: The selected child's remaining timeline duration.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.InvalidOperationException: Processing context is absent, an input is disconnected, or an external write targets a read-only slot.

<a id="member-810220b0b9ef"></a>
### BlendNode

`public System.Double BlendNode(System.String name, Electron2D.AnimationNode node, System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Double blend, Electron2D.AnimationNode.FilterAction filter = Ignore, System.Boolean sync = true, System.Boolean testOnly = false)`

Processes a named child resource of the active graph instance.

name: The exact local child name.

node: The live declared child resource.

time: Relative seconds, or an absolute seek position.

seek: Whether time is absolute.

isExternalSeeking: Whether seeking is external.

blend: The signed contribution.

filter: The filtering action.

sync: Whether zero-weight branches advance.

testOnly: Whether persistent changes are suppressed.

Returns: The child's remaining duration.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

System.InvalidOperationException: Processing context is absent, an input is disconnected, or an external write targets a read-only slot.

<a id="member-d425a6f1c84d"></a>
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

<a id="member-1aeaf6251ca8"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-c887261d44b9"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-386c754bb121"></a>
### FindInput

`public System.Int32 FindInput(System.String name)`

Returns the first input matching an ordinal caption, or minus one.

name: The exact non-null caption.

Returns: The first index or minus one.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-ad55b27524f4"></a>
### GetCaption

`public System.String GetCaption()`

Returns the display caption of this resource.

Returns: The typed caption.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-58d0a795d902"></a>
### GetInputCount

`public System.Int32 GetInputCount()`

Returns the number of inputs.

Returns: The input count.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-3de61473f6f3"></a>
### GetInputName

`public System.String GetInputName(System.Int32 input)`

Returns an input caption.

input: The zero-based index.

Returns: The exact caption.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-f2d58051307c"></a>
### GetParameter

`public TValue GetParameter<TValue>(AnimationParameter<TValue> parameter)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

<a id="member-f3af99a5b46e"></a>
### GetProcessingAnimationTreeInstanceID

`public System.UInt64 GetProcessingAnimationTreeInstanceID()`

Returns the tree instance ID while processing, or zero outside processing.

Returns: The current tree identity or zero.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-6c286addfb7e"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

<a id="member-82d233dccdb6"></a>
### IsPathFiltered

`public System.Boolean IsPathFiltered(System.String path)`

Tests exact filter path membership.

path: The non-null track path.

Returns: Whether the path is selected.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-4bc11395595a"></a>
### IsProcessTesting

`public System.Boolean IsProcessTesting()`

Tests whether the active evaluation suppresses writes and playback state changes.

Returns: Whether the current evaluation is a test.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-aa3768c70ca1"></a>
### OnGetCaption

`protected virtual System.String OnGetCaption()`

Supplies the graph resource's caption.

Returns: The caption; defaults to Node.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-46f23e7b1f15"></a>
### OnGetChildByName

`protected virtual Electron2D.AnimationNode OnGetChildByName(System.String name)`

Finds a named child definition.

name: The exact local name.

Returns: The borrowed child or null.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-9b6a0bbf987b"></a>
### OnGetChildNodes

`protected virtual System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, Electron2D.AnimationNode>> OnGetChildNodes()`

Returns ordered named child definitions; no scene nodes are created.

Returns: The graph's borrowed children.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-13fa7ac87c02"></a>
### OnGetParameterDefaultValue

`protected virtual TValue OnGetParameterDefaultValue<TValue>(AnimationParameter<TValue> parameter)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

<a id="member-68d4ec4baf4b"></a>
### OnGetParameterList

`protected virtual System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()`

Supplies immutable typed parameter definitions.

Returns: The node's parameter schema, including inherited timing slots.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-8bd34de767b9"></a>
### OnHasFilter

`protected virtual System.Boolean OnHasFilter()`

Reports whether child blend helpers apply track filters.

Returns: False for the base resource.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-17c6230307fe"></a>
### OnIsParameterReadOnly

`protected virtual System.Boolean OnIsParameterReadOnly(Electron2D.AnimationParameter parameter)`

Reports external write protection for a parameter.

parameter: The immutable schema definition.

Returns: Whether external writes are blocked.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-6117375cc771"></a>
### OnProcess

`protected virtual System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)`

Evaluates custom graph behavior; helpers can blend inputs, children and clips.

time: Relative delta seconds, or absolute time when seeking.

seek: Whether time is absolute.

isExternalSeeking: Whether the seek originated outside startup.

testOnly: Whether persistent changes and mixer writes are suppressed.

Returns: The selected remaining duration, or zero when no input is evaluated.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-c5a37f90835a"></a>
### RemoveInput

`public virtual System.Void RemoveInput(System.Int32 index)`

Removes an existing input.

index: The zero-based input index.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-c22c87574354"></a>
### SetFilterPath

`public System.Void SetFilterPath(System.String path, System.Boolean enable)`

Enables or removes a path from the filter.

path: An exact track path including the typed property suffix.

enable: Whether to include the path.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-0b7a6b056e5e"></a>
### SetInputName

`public virtual System.Boolean SetInputName(System.Int32 input, System.String name)`

Renames an input, rejecting slash/dot captions.

input: The existing zero-based input index.

name: The non-null caption.

Returns: Whether the caption was accepted.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-e556cf3e0fea"></a>
### SetParameter

`public System.Void SetParameter<TValue>(AnimationParameter<TValue> parameter, TValue value)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.UInt64, System.String> AnimationNodeRemoved` | Occurs after a named graph node is removed. |
| `public event System.Action<System.UInt64, System.String, System.String> AnimationNodeRenamed` | Occurs after a named graph node is renamed. |
| `public event System.Action<System.UInt64> NodeUpdated` | Occurs after input definitions or a named child connection changes. |
| `public event System.Action TreeChanged` | Occurs after graph structure or parameter definitions change. |

## Event Descriptions

<a id="member-a235fe35b272"></a>
### AnimationNodeRemoved

`public event System.Action<System.UInt64, System.String> AnimationNodeRemoved`

Occurs after a named graph node is removed.

<a id="member-12006dc697e1"></a>
### AnimationNodeRenamed

`public event System.Action<System.UInt64, System.String, System.String> AnimationNodeRenamed`

Occurs after a named graph node is renamed.

<a id="member-b3803faf3e6f"></a>
### NodeUpdated

`public event System.Action<System.UInt64> NodeUpdated`

Occurs after input definitions or a named child connection changes.

<a id="member-a87e2c2f0be5"></a>
### TreeChanged

`public event System.Action TreeChanged`

Occurs after graph structure or parameter definitions change.

## Field summary

| Complete C# signature | Contract |
| --- | --- |
| `public static readonly Electron2D.AnimationParameter<System.Double> CurrentDelta` | Read-only effective delta of the previously processed node timeline in seconds. |
| `public static readonly Electron2D.AnimationParameter<System.Double> CurrentLength` | Read-only length of the previously processed node timeline in seconds. |
| `public static readonly Electron2D.AnimationParameter<System.Double> CurrentPosition` | Read-only position of the previously processed node timeline in seconds. |

## Field Descriptions

<a id="member-69f4c9c2213c"></a>
### CurrentDelta

`public static readonly Electron2D.AnimationParameter<System.Double> CurrentDelta`

Read-only effective delta of the previously processed node timeline in seconds.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-12e54c0c8f3b"></a>
### CurrentLength

`public static readonly Electron2D.AnimationParameter<System.Double> CurrentLength`

Read-only length of the previously processed node timeline in seconds.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-8a67cf5c6ea4"></a>
### CurrentPosition

`public static readonly Electron2D.AnimationParameter<System.Double> CurrentPosition`

Read-only position of the previously processed node timeline in seconds.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines/grouped controllers, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.

## Blend-space integration

[Blend spaces](../components/scene-animation.md#blend-spaces) now execute linear/triangle mixing, discrete/carry and synchronized clocks through these existing graph hooks. Their definitions stay borrowed and their clocks/selection remain per tree/path.

## Action-controller integration

[Action controllers](../components/scene-animation.md#action-controllers) now use this graph contract. Virtual input mutation keeps policy/connection storage aligned; external seek delta is previous position minus requested time, predictive end flags support loop breaks, and prepared deferred lifecycle notices preserve delivery through repeated starts/finishes. Reentrant graph edits skip stale diagnostics.
