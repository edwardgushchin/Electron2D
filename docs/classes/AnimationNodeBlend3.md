# AnimationNodeBlend3

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeBlend3` · **Source:** [AnimationNodeOperations.cs](../../src/Scene/Animation/AnimationNodeOperations.cs).

**Inherits:** [AnimationNodeSync](AnimationNodeSync.md).

## Description

Combines 3 graph inputs with a per-tree typed blend_amount parameter.

Inputs are -blend, in, +blend. Their weights are max(0,-amount), 1-abs(amount), max(0,amount). Amount below -0.5 selects the first timeline, above 0.5 the third, otherwise the center. No filter is applied by this resource.

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
using var node = new AnimationNodeBlend3();
controller.SetParameter("op", AnimationNodeBlend3.BlendAmount, -0.5);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeBlend3()` | Creates the standard input captions. |

## Constructor Descriptions

<a id="member-582546621680"></a>
### .ctor

`public AnimationNodeBlend3()`

Creates the standard input captions.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.String OnGetCaption()` | Supplies the graph resource's caption. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()` | Supplies immutable typed parameter definitions. |
| `protected override System.Boolean OnHasFilter()` | Reports whether child blend helpers apply track filters. |
| `protected override System.Double OnProcess(System.Double time, System.Boolean seek, System.Boolean isExternalSeeking, System.Boolean testOnly)` | Evaluates custom graph behavior; helpers can blend inputs, children and clips. |

## Method Descriptions

<a id="member-b4418264d717"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-f555b25e5f5f"></a>
### OnGetCaption

`protected override System.String OnGetCaption()`

Supplies the graph resource's caption.

Returns: The caption; defaults to Node.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-d34c29795a6f"></a>
### OnGetParameterList

`protected override System.Collections.Generic.IEnumerable<Electron2D.AnimationParameter> OnGetParameterList()`

Supplies immutable typed parameter definitions.

Returns: The node's parameter schema, including inherited timing slots.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-a86070452171"></a>
### OnHasFilter

`protected override System.Boolean OnHasFilter()`

Reports whether child blend helpers apply track filters.

Returns: False for the base resource.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-f66a385cf61a"></a>
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
| `public static readonly Electron2D.AnimationParameter<System.Double> BlendAmount` | The finite signed contribution parameter, initially zero. |

## Field Descriptions

<a id="member-254f230e7b68"></a>
### BlendAmount

`public static readonly Electron2D.AnimationParameter<System.Double> BlendAmount`

The finite signed contribution parameter, initially zero.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines/grouped controllers, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
